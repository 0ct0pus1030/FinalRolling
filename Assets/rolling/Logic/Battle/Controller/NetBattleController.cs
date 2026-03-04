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
    public int CurrentStateHash { get; private set; }

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
        int startFrame = BitConverter.ToInt32(data, 1);
        long targetTime = BitConverter.ToInt64(data, 5);
        StartCoroutine(SyncStartFlow(startFrame, targetTime));
    }

    IEnumerator SyncStartFlow(int startFrame, long targetTime)
    {
        Signals.Get<ToPVPVPSignal>()?.Dispatch();
        
        yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "pvpvp");
        yield return null;
    
        Debug.Log("[NBC] 场景加载完成");
        
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long waitMs = targetTime - now;
    
        if (waitMs > 0)
        {
            Debug.Log($"[NBC] 等待同步 {waitMs}ms");
            yield return new WaitForSeconds(waitMs / 1000f);
        }
        else
        {
            Debug.LogWarning($"[NBC] 已错过 {(int)-waitMs}ms");
        }
    
        Debug.Log($"=== 同步开始！{DateTimeOffset.UtcNow:HH:mm:ss.fff} ===");
        
        InitBattle(startFrame);
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
        // 新包大小：Type(1) + CurFrame(4) + 3×[FrameId(4)+Input0(8)+Input1(8)] = 65字节
        if (data.Length < 65) 
        {
            Debug.LogWarning($"[NBC] 广播包长度不足: {data.Length}, 期望65");
            return;
        }
    
        int serverCurFrame = BitConverter.ToInt32(data, 1);
        lastServerFrame = serverCurFrame;
        hasNewBroadcast = true;
    
        // 检查是否连续丢包（当前帧号 vs 上次收到的）
        if (lastServerFrame > 0 && serverCurFrame > lastServerFrame + 1)
        {
            int lost = serverCurFrame - lastServerFrame - 1;
            Debug.LogWarning($"[NBC] 检测到下行丢包: 丢失 {lost} 帧，尝试从冗余恢复...");
        }
        
        // 从第5字节开始解析3帧数据
        int offset = 5;
    
        for (int f = 0; f < 3; f++) // 解析3帧冗余数据
        {
            int frameId = BitConverter.ToInt32(data, offset);
            offset += 4;
        
            // 只存储有效的帧（在未来10帧范围内）
            if (frameId >= executeFrame - 10 && frameId < executeFrame + 20)
            {
                int idx = frameId & (BUFFER_SIZE - 1);
            
                for (int i = 0; i < 2; i++) // 两个玩家
                {
                    byte[] inputData = new byte[8];
                    Buffer.BlockCopy(data, offset, inputData, 0, 8);
                    offset += 8;
                
                    var input = FrameInput.Deserialize(inputData);
                    input.FrameId = frameId;  // 强制修正
                    input.PlayerId = i;
                    serverInputBuffer[idx, i] = input;
                }
            
                // 如果补上了之前缺失的帧，打个日志
                if (frameId == executeFrame)
                {
                    Debug.Log($"[NBC] 冗余恢复帧 {frameId}");
                }
            }
            else
            {
                // 跳过这帧数据（太旧或太远）
                offset += 16;
            }
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
        int targetFrame = lastServerFrame + INPUT_DELAY + 2;
        sendFrame = targetFrame;

        // 发送当前帧 + 前两帧（共3帧冗余，防丢包）
        for (int i = 0; i < 3; i++)
        {
            int frameToSend = sendFrame - i;
            if (frameToSend < 0) break;
        
            var input = inputBuffer.GetDelayed(frameToSend, myPlayerId);
        
            // 构造包
            byte[] inputBytes = input.Serialize();
            byte[] packet = new byte[9];
            packet[0] = (byte)PacketType.Input;
            Buffer.BlockCopy(inputBytes, 0, packet, 1, 8);
        
            network.Send(packet);
        }
    
        sendFrame++;
    }

    void ExecuteLogic()
    {
        // 执行到服务器允许的最大帧（滞后 INPUT_DELAY）
        int maxExecuteFrame = lastServerFrame - INPUT_DELAY;
    
        // 限制每帧最多执行2帧，防止一次性追赶过多导致卡顿
        int maxFramesToExecute = 2;
        int executed = 0;
    
        while (executeFrame <= maxExecuteFrame && executed < maxFramesToExecute)
        {
            int idx = executeFrame & (BUFFER_SIZE - 1);
            var inputs = new FrameInput[2];
            bool hasAllData = true;

            for (int i = 0; i < 2; i++)
            {
                inputs[i] = serverInputBuffer[idx, i];
            
                // 如果这帧数据缺失（即使冗余也没收到），用空输入补上
                if (inputs[i].FrameId != executeFrame)
                {
                    inputs[i] = FrameInput.CreateEmpty(i, executeFrame);
                    hasAllData = false;
                }
            }

            // 执行这一帧
            battleSystem.Step(inputs);
            executeFrame++;
            executed++;
            CurrentStateHash = battleSystem.CurrentState.ComputeSyncHash();
        
            // 如果数据齐全且只落后1帧，正常速度执行
            if (hasAllData && executeFrame > maxExecuteFrame - 1) 
                break;
        }
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