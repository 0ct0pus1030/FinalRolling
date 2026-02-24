
public class HitStopSystem
{
    int remainingFrames = 0;

    // 触发卡肉（格斗游戏通常8-12帧）
    public void Trigger(int durationFrames)
    {
        remainingFrames = durationFrames;
    }

    // 每帧调用，返回是否仍处于卡肉状态
    public bool IsFreezed()
    {
        return remainingFrames > 0;
    }


    
    public void Update()
    {
        if (remainingFrames > 0) remainingFrames--;
    }

    public void Clear()
    {
        remainingFrames = 0;
    }

    public int RemainingFrames => remainingFrames;
}