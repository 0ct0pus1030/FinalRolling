using System;
using kcp2k;
using UnityEngine;

/// <summary>
/// KCP客户端封装 - 只负责收发原始字节，不解析游戏内容
/// </summary>
public class NetworkClient : MonoBehaviour
{
    private KcpClient client;
    private bool isConnected = false;

    // 事件回调（供NetworkBattleController订阅）
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<byte[]> OnDataReceived;  // 原始字节透传

    public bool Connected => isConnected;

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public void Connect(string ip, ushort port)
    {
        if (client != null) return;

        // KCP配置（必须与服务器完全匹配）
        var config = new KcpConfig(
            DualMode: false,
            RecvBufferSize: 1024 * 1024,   // 1MB
            SendBufferSize: 1024 * 1024,   // 1MB
            Mtu: 1400,
            NoDelay: true,                 // 低延迟模式
            Interval: 10,                  // 10ms间隔
            FastResend: 2,                 // 快速重传
            CongestionWindow: false,       // 关闭拥塞控制（格斗游戏需要稳定延迟）
            SendWindowSize: 32,
            ReceiveWindowSize: 128
        );

        client = new KcpClient(
            OnConnectCallback,
            OnDataCallback,
            OnDisconnectCallback,
            OnErrorCallback,
            config
        );

        client.Connect(ip, port);
        Debug.Log($"[NetClient] 正在连接 {ip}:{port}...");
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        client?.Disconnect();
        client = null;
        isConnected = false;
    }

    /// <summary>
    /// 发送原始字节（上层构造好包体直接发）
    /// </summary>
    public void Send(byte[] data)
    {
        if (!isConnected || client == null) return;
        client.Send(new ArraySegment<byte>(data), KcpChannel.Reliable);
    }

    /// <summary>
    /// 每帧调用，驱动KCP内部循环
    /// </summary>
    void Update()
    {
        client?.Tick();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    #region KCP回调

    private void OnConnectCallback()
    {
        isConnected = true;
        Debug.Log("[NetClient] 已连接");
        OnConnected?.Invoke();
    }

    private void OnDataCallback(ArraySegment<byte> segment, KcpChannel channel)
    {
        // 拷贝数据（因为segment是复用缓冲区）
        byte[] data = new byte[segment.Count];
        Buffer.BlockCopy(segment.Array, segment.Offset, data, 0, segment.Count);

        // 透传给上层，不解析
        OnDataReceived?.Invoke(data);
    }

    private void OnDisconnectCallback()
    {
        isConnected = false;
        Debug.Log("[NetClient] 已断开");
        OnDisconnected?.Invoke();
    }

    private void OnErrorCallback(ErrorCode error, string message)
    {
        Debug.LogError($"[NetClient] 错误: {error} - {message}");
    }

    #endregion
}