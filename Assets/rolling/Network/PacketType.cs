public enum PacketType : byte
{
    AssignId = 0,      // S->C: [Type, PlayerId] 分配ID
    Ready = 1,         // C->S: [Type] 客户端准备就绪
    GameStart = 2,     // S->C: [Type] 游戏开始（收到2个Ready后发送）
    Input = 3,         // C->S: [Type, FrameId(4), Buttons(1), Dir(1)] 输入
    Broadcast = 4      // S->C: [Type, FrameId(4), Input0(8), Input1(8)] 广播
}