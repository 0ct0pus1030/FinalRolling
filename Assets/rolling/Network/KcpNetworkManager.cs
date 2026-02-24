using FGLogic.Input;
using kcp2k;
using System;
using UnityEngine;

public class KcpNetworkManager : MonoBehaviour
{
    public static KcpNetworkManager Instance;

    [Header("配置")]
    public string serverIp = "127.0.0.1";
    public ushort serverPort = 7777;

    // 状态
    public int MyPlayerId { get; private set; } = -1;  // 服务器分配
    public bool IsConnected { get; private set; } = false;
    public bool IsReady { get; private set; } = false;
    public bool GameStarted { get; private set; } = false;
    public int StartFrame { get; private set; } = 0;

    // 事件
    public event Action OnGameStart;  // 游戏开始事件

    // 帧数据回调：frame, inputs[3]
    public event Action<int, FrameInput[]> OnFrameDataReceived;

    KcpClient client;

    void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
    }

    void Start()
    {
        KcpConfig config = new KcpConfig
        {
            NoDelay = true,
            Interval = 10,
            FastResend = 2,
            CongestionWindow = false,
            SendWindowSize = 128,
            ReceiveWindowSize = 128
        };

        client = new KcpClient(
            OnConnected,
            OnDataReceived,
            OnDisconnected,
            (code, msg) => Debug.LogError($"[网络错误] {code}: {msg}"),
            config
        );

        client.Connect(serverIp, serverPort);
        Debug.Log($"[网络] 连接 {serverIp}:{serverPort}");
    }

    void Update()
    {
        client?.Tick();
    }

    void OnConnected()
    {
        IsConnected = true;
        Debug.Log("[网络] 连接成功，点击准备按钮发送Ready");
    }

    void OnDisconnected()
    {
        IsConnected = false;
        Debug.Log("[网络] 断开连接");
    }

    // 点击准备按钮调用
    public void SendReady()
    {
        if (!IsConnected || IsReady) return;

        IsReady = true;

        // 注意：此时还不知道自己的PlayerId，先发占位符0
        // 服务器会忽略这个值，在GameStart中分配
        byte[] data = new byte[] { 0xFF, 0 };
        client.Send(new ArraySegment<byte>(data), KcpChannel.Reliable);

        Debug.Log("[准备] 已发送Ready，等待服务器分配PlayerId");
    }

    // 发送输入（延迟2帧，所以发送的是 currentFrame + 2）
    public void SendInput(FrameInput input)
    {
        if (!IsConnected || !GameStarted) return;

        byte[] data = input.Serialize();  // 8字节
        client.Send(new ArraySegment<byte>(data), KcpChannel.Reliable);
    }

    void OnDataReceived(ArraySegment<byte> data, KcpChannel channel)
    {
        if (data.Count == 0) return;

        byte msgType = data[0];

        switch (msgType)
        {
            case 0x10:  // GameStart
                ParseGameStart(data);
                break;

            case 0x11:  // FrameData
                ParseFrameData(data);
                break;

            default:
                Debug.LogWarning($"[网络] 未知消息类型: {msgType}");
                break;
        }
    }

    void ParseGameStart(ArraySegment<byte> data)
    {
        if (data.Count < 6) return;

        MyPlayerId = data[1];
        StartFrame = (data[2] << 24) | (data[3] << 16) | (data[4] << 8) | data[5];

        GameStarted = true;
        Debug.Log($"[游戏] 开始！PlayerId={MyPlayerId}, StartFrame={StartFrame}");

        OnGameStart?.Invoke();
    }

    void ParseFrameData(ArraySegment<byte> data)
    {
        if (data.Count < 29) return;  // 1 + 4 + 8*3 = 29

        // 解析帧号
        int frame = (data[1] << 24) | (data[2] << 16) | (data[3] << 8) | data[4];

        // 解析3个输入
        var inputs = new FrameInput[3];
        for (int i = 0; i < 3; i++)
        {
            byte[] inputBytes = new byte[8];
            Array.Copy(data.Array, data.Offset + 5 + i * 8, inputBytes, 0, 8);
            inputs[i] = FrameInput.Deserialize(inputBytes);
        }

        OnFrameDataReceived?.Invoke(frame, inputs);
    }
}