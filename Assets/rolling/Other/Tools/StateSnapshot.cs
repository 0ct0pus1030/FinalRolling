using FGLogic.State;
using System;

public class StateSnapshot
{
    private const int BUFFER_SIZE = 64;  // 保存最近64帧（约1秒@60fps）

    private GameState[] _buffer = new GameState[BUFFER_SIZE];

    // 保存某一帧的状态
    public void Save(int frameId, in GameState state)
    {
        int idx = frameId & (BUFFER_SIZE - 1);  // 快速取模（要求BUFFER_SIZE是2的幂）
        _buffer[idx] = state;  // struct 值拷贝，安全！
    }

    // 读取某一帧的状态（返回拷贝，原数据不会被改）
    public GameState Load(int frameId)
    {
        int idx = frameId & (BUFFER_SIZE - 1);
        return _buffer[idx];
    }

    // 检查某帧是否在缓冲区范围内
    public bool CanRollbackTo(int frameId, int currentFrame)
    {
        // 不能回滚到太久以前（超出环缓冲区）
        return frameId >= currentFrame - BUFFER_SIZE + 1 && frameId >= 0;
    }
}