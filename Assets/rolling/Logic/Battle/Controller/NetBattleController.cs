using FGLogic.Core;
using FGLogic.Input;
using FGLogic.State;
using System;
using System.Collections;
using System.Collections.Generic;
using UIFramework.Rolling;
using UnityEngine;
using Utils;

public class NetworkBattleController : MonoBehaviour
{
    [Header("配置")]
    public CharacterConfig[] charConfigs;
    public AttackConfig[] attackConfigs;
    public PlayerView[] playerViews;

    [Header("网络")]
    public NetworkClient network;
    public string serverIP = "127.0.0.1";
    public ushort serverPort = 7777;

    public static NetworkBattleController Instance;

    // 游戏状态
    private enum State { None, Matching, Gaming }
    private State state = State.None;
    private int myPlayerId = -1;
    private bool isGaming = false;

    // 帧管理（事件驱动）
    private int sendFrame = 0;           // 下次要发送的帧号
    private int executeFrame = 0;        // 下次要执行的帧号
    private int lastServerFrame = -1;    // 收到的最新服务器帧号
    private bool hasNewBroadcast = false; // 【新增】收到新广播标志

    // 输入缓冲
    private InputDelayBuffer inputBuffer;
    private const int BUFFER_SIZE = 64;
    private const int INPUT_DELAY = 2;
    private FrameInput[,] serverInputBuffer = new FrameInput[BUFFER_SIZE, 2];

    private BattleSystem battleSystem;
    private int serverStartFrame = 0;

    public BattleSystem BattleSystem => battleSystem;

    public int GetMyPlayerId() => myPlayerId;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;

        if (playerViews == null || playerViews.Length == 0)
        {
            playerViews = GetComponentsInChildren<PlayerView>(true);
            System.Array.Sort(playerViews, (a, b) => a.playerId.CompareTo(b.playerId));
        }
    }

    public void StartMatching()
    {
        if (state != State.None) return;

        network.OnConnected += OnConnected;
        network.OnDisconnected += OnDisconnected;
        network.OnDataReceived += OnData;

        network.Connect(serverIP, serverPort);
        state = State.Matching;
        Debug.Log("[NBC] 开始连接服务器...");
    }

    void OnConnected()
    {
        Debug.Log("[NBC] 已连接，等待分配ID...");
    }

    void OnDisconnected()
    {
        Debug.LogWarning("[NBC] 断开连接");
        isGaming = false;
    }

    void OnData(byte[] data)
    {
        if (data.Length == 0) return;
        var type = (PacketType)data[0];

        switch (type)
        {
            case PacketType.AssignId:
                myPlayerId = data[1];
                Debug.Log($"[NBC] 分配PlayerId: {myPlayerId}");
                network.Send(new byte[] { (byte)PacketType.Ready });
                break;

            case PacketType.GameStart:
                HandleGameStart(data);
                break;

            case PacketType.Broadcast:
                HandleBroadcast(data);
                break;
        }
    }

    void HandleGameStart(byte[] data)
    {
        serverStartFrame = 0;
        if (data.Length >= 5)
        {
            serverStartFrame = BitConverter.ToInt32(data, 1);
        }
        Debug.Log($"[NBC] 收到GameStart，服务端起始帧: {serverStartFrame}");
        StartCoroutine(CountdownAndInit(serverStartFrame));
    }

    IEnumerator CountdownAndInit(int startFrame)
    {
        yield return new WaitForSeconds(3);
        InitBattle(startFrame);

        var readyWindow = FindObjectOfType<PlayerReadyWindowController>();
        readyWindow?.OnMatchFound();
        Signals.Get<ToPVPVPSignal>()?.Dispatch();
    }

    void InitBattle(int startFrame)
    {
        sendFrame = startFrame;
        executeFrame = startFrame;
        lastServerFrame = startFrame - 1;
        hasNewBroadcast = false;

        inputBuffer = new InputDelayBuffer(INPUT_DELAY, 2);

        battleSystem = new BattleSystem(
            playerCount: 2,
            logicFps: 30f,
            inputDelayFrames: 0,
            enableHitStop: false,
            hitStopFrames: 0,
            attackConfigs: attackConfigs,
            charConfigs: charConfigs,
            isNetworkMode: true
        );

        for (int i = 0; i < 2; i++)
        {
            var (pos, facing) = SpawnTable.GetSpawnInfo(i);
            var player = battleSystem.CurrentState.Players[i];
            player.PlayerId = i;
            player.IsActive = true;
            player.Position = pos;
            player.FaceingDirection = facing;
            player.Health = charConfigs[i].MaxHealth;
            battleSystem.SetPlayerState(i, player);
        }

        isGaming = true;
        Debug.Log($"[NBC] 战斗初始化完成，等待服务端广播...");
    }

    void HandleBroadcast(byte[] data)
    {
        if (data.Length < 9) return;

        int frame = BitConverter.ToInt32(data, 1);

        // 【关键】检查是否丢帧（调试用途）
        if (lastServerFrame >= 0 && frame != lastServerFrame + 1)
        {
            Debug.LogWarning($"[NBC] 跳帧检测: 从{lastServerFrame}跳到{frame}, 差值{frame - lastServerFrame - 1}");
        }

        lastServerFrame = frame;
        hasNewBroadcast = true;  // 【关键】标记收到新帧

        int idx = frame & (BUFFER_SIZE - 1);

        for (int i = 0; i < 2; i++)
        {
            int offset = 5 + i * 2;
            serverInputBuffer[idx, i] = new FrameInput
            {
                FrameId = frame,
                PlayerId = i,
                Buttons = data[offset],
                Stick = Direction.ToVector(data[offset + 1]),
                IsPredicted = false
            };
        }
    }

    void Update()
    {
        if (!isGaming) return;

        // 【关键修改】事件驱动：收到广播才处理逻辑
        if (hasNewBroadcast)
        {
            hasNewBroadcast = false;

            // 1. 发送输入（基于新的服务端帧号）
            SendInput();

            // 2. 执行逻辑（处理这一帧）
            ExecuteLogic();
        }

        // 3. 持续采集输入（确保不丢按键，渲染帧采样）
        var currentInput = LocalInputProvider.GetLocalInput(sendFrame, myPlayerId);
        inputBuffer.Push(currentInput, myPlayerId);

        // 4. 渲染（每帧都跑，与逻辑分离）
        Render();
    }

    void SendInput()
    {
        // 基于 lastServerFrame 计算要发送的帧（提前 INPUT_DELAY+1 帧）
        int targetFrame = lastServerFrame + INPUT_DELAY + 1;

        
        /*
        // 如果本地已经发过了，等待（防超前）
        if (sendFrame > targetFrame)
        {
            return;
        }

        // 如果落后太多，追赶
        if (sendFrame < targetFrame)
        {
            sendFrame = targetFrame;
        }
        */
        
        //强制对齐
        sendFrame = targetFrame;

        var inputToSend = inputBuffer.GetDelayed(sendFrame, myPlayerId);
        int dir = Direction.FromStick(inputToSend.Stick);

        byte[] data = new byte[7];
        data[0] = (byte)PacketType.Input;
        BitConverter.GetBytes(sendFrame).CopyTo(data, 1);
        data[5] = (byte)inputToSend.Buttons;
        data[6] = (byte)dir;

        network.Send(data);
        sendFrame++;
    }

    void ExecuteLogic()
    {
        // 执行当前应执行的帧（滞后 INPUT_DELAY 帧）
        int frameToExecute = lastServerFrame - INPUT_DELAY;

        // 检查是否已执行过
        if (frameToExecute < executeFrame) return;

        // 如果落后，追赶（理论上不应发生，除非刚启动）
        if (frameToExecute > executeFrame)
        {
            Debug.LogWarning($"[NBC] 追赶: {executeFrame} -> {frameToExecute}");
            executeFrame = frameToExecute;
        }

        int idx = executeFrame & (BUFFER_SIZE - 1);
        var inputs = new FrameInput[2];

        for (int i = 0; i < 2; i++)
        {
            inputs[i] = serverInputBuffer[idx, i];
            if (inputs[i].FrameId != executeFrame)
            {
                //inputs[i] = FrameInput.CreateEmpty(i, executeFrame);
                Debug.Log($"[NBC] 等待帧 {executeFrame} 玩家 {i} 数据");
                return;
            }
        }

        battleSystem.Step(inputs);
        executeFrame++;
    }

    void Render()
    {
        if (battleSystem == null) return;

        var state = battleSystem.CurrentState;
        for (int i = 0; i < 2; i++)
        {
            if (playerViews[i] != null)
                playerViews[i].UpdateVisual(state.Players[i]);
        }
    }

    public void CancelMatching()
    {
        network?.Disconnect();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}