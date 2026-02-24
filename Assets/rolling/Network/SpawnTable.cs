// UnityClient/Assets/Scripts/Network/SpawnTable.cs
using FGLogic.Core;

public static class SpawnTable
{
    /// <summary>
    /// 2人PVP出生配置（0号左，1号右）
    /// </summary>
    public static (FixedVector2 pos, int facing) GetSpawnInfo(int playerId)
    {
        return playerId switch
        {
            0 => (new FixedVector2(Fixed.FromFloat(-2f), Fixed.Zero), 1),   // 左边，朝右
            1 => (new FixedVector2(Fixed.FromFloat(2f), Fixed.Zero), -1),    // 右边，朝左
            _ => (FixedVector2.Zero, 1)
        };
    }
}