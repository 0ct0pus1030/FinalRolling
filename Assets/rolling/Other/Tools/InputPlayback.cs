using FGLogic.Input;
using System.Linq;

public class InputPlayer
{
    private byte[] _data;
    private int _currentFrame = 0;
    private int _totalFrames;

    public void LoadRecording(string path)
    {
        //_data = File.ReadAllBytes(path);
        // 跳过头部，计算总帧数
        _totalFrames = (_data.Length - 10) / 5; // 10字节头部
    }

    public FrameInput GetInput(int frameId, int playerId)
    {
        int index = 10 + frameId * 5; // 10字节头部 + 每帧5字节
        if (index + 5 > _data.Length)
            return FrameInput.CreateEmpty(playerId, frameId);

        // 使用你现有的Deserialize，但注意你的Deserialize有bug：
        // 你写了两次Stick赋值，而且data[3]/data[4]是整数除法，应该转float
        return FrameInput.Deserialize(_data.Skip(index).Take(5).ToArray(), playerId);
    }

    public bool IsFinished(int frameId) => frameId >= _totalFrames;
}