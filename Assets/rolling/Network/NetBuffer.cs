using FGLogic.Input;
using System;

public class NetBuffer
{
    public int DelayFrames { get; private set; }
    readonly FrameInput[,] buffer; // [playerId, frame % size]
    const int BUFFER_SIZE = 64;
    readonly int playerCount;

    public NetBuffer(int delay, int playerCount)  
    {
        this.DelayFrames = delay;
        this.playerCount = playerCount;
        buffer = new FrameInput[playerCount, BUFFER_SIZE]; 
    }

    public void Push(FrameInput input, int playerId)
    {
        if (playerId >= playerCount) return; 
        buffer[playerId, input.FrameId % BUFFER_SIZE] = input;
    }

    public FrameInput GetDelayed(int currentFrame, int playerId)
    {
        int targetFrame = currentFrame;


        if (playerId >= playerCount)
            return FrameInput.CreateEmpty(playerId, currentFrame);


        if (targetFrame < 0)
            return FrameInput.CreateEmpty(playerId, currentFrame);

        var input = buffer[playerId, targetFrame % BUFFER_SIZE];

        
        if (input.FrameId != targetFrame)
            return FrameInput.CreateEmpty(playerId, currentFrame);

        return input;
    }

    public void Clear()
    {
        Array.Clear(buffer, 0, buffer.Length);
    }
}