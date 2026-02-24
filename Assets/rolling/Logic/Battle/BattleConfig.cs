using FGLogic.Core;
using UnityEngine;

[System.Serializable]
public class BattleConfig
{
    // ========== 基础配置 ==========
    public float LogicFps = 30f;
    public float FixedDeltaTime => 1f / LogicFps;

    public int InputDelayFrames = 2;
    public int HitStopFrames = 6;

    // 关键：是否启用顿帧（本地true，网络false）
    public bool EnableHitStop = true;

    // 资源引用
    public AttackConfig[] AttackConfigs;
    public CharacterConfig[] CharConfigs;

    // ========== 动态部分 ==========
    [System.NonSerialized] public int PlayerCount;
    [System.NonSerialized] public bool IsNetworkMode;
    [System.NonSerialized] public int LocalPlayerId;  // 网络模式下本机玩家ID

    // 网络配置
    public int HashCheckInterval = 30;

    // ========== 出生点 ==========
    public FixedVector2 GetSpawnPosition(int playerId)
    {
        switch (playerId)
        {
            case 0: return new FixedVector2(Fixed.FromFloat(-2f), Fixed.Zero);
            case 1: return new FixedVector2(Fixed.FromFloat(2f), Fixed.Zero);
            case 2: return new FixedVector2(Fixed.FromFloat(0f), Fixed.FromFloat(2f));
            case 3: return new FixedVector2(Fixed.FromFloat(0f), Fixed.FromFloat(-2f));
            default: return FixedVector2.Zero;
        }
    }

    public int GetFacingDirection(int playerId) => (playerId % 2 == 0) ? 1 : -1;

    // ========== 工厂方法 ==========
    public static BattleConfig CreateLocal(AttackConfig[] attacks, CharacterConfig[] chars)
    {
        return new BattleConfig
        {
            PlayerCount = 3,
            IsNetworkMode = false,
            EnableHitStop = true,      // 本地启用顿帧
            InputDelayFrames = 0,       // 本地无延迟
            LogicFps = 30f,
            AttackConfigs = attacks,
            CharConfigs = chars
        };
    }

    public static BattleConfig CreateNetwork(int localPlayerId, AttackConfig[] attacks, CharacterConfig[] chars)
    {
        return new BattleConfig
        {
            PlayerCount = 3,
            IsNetworkMode = true,
            LocalPlayerId = localPlayerId,
            EnableHitStop = false,      // 网络禁用顿帧
            InputDelayFrames = 2,       // 网络延迟缓冲
            LogicFps = 30f,
            AttackConfigs = attacks,
            CharConfigs = chars
        };
    }
}