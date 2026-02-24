using UnityEngine;
using FGLogic.Input;
using FGLogic.State;
using FGLogic.Core;

public class LocalBattleController : MonoBehaviour
{
    [Header("玩家配置")]
    public bool[] isHumanPlayer;

    [Header("配置")]
    public AttackConfig[] attackConfigs;
    public CharacterConfig[] charConfigs;

    [Header("视图")]
    public PlayerView[] playerViews;

    // 核心系统
    BattleSystem battleSystem;

    // 时间累积
    float accumulator;
    const float FIXED_DT = 1f / 30f;
    const int PLAYER_COUNT = 3;

    void Start()
    {
        // 创建 BattleSystem（参数和旧版一致）
        battleSystem = new BattleSystem(
            playerCount: PLAYER_COUNT,
            logicFps: 30f,
            inputDelayFrames: 2,  // 和旧版一致！
            enableHitStop: true,
            hitStopFrames: 6,
            attackConfigs: attackConfigs,
            charConfigs: charConfigs,
            isNetworkMode: false
        );

        // 初始化玩家（和旧版完全一致）
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            var player = battleSystem.CurrentState.Players[i];
            player.PlayerId = i;
            player.IsActive = true;

            if (isHumanPlayer[i])
            {
                player.Position = new FixedVector2(Fixed.FromFloat(-2f), Fixed.Zero);
                player.FaceingDirection = 1;
            }
            else
            {
                int dummyIndex = 0;
                for (int j = 0; j < i; j++)
                    if (!isHumanPlayer[j]) dummyIndex++;

                float yPos = dummyIndex * 1f;
                player.Position = new FixedVector2(Fixed.FromFloat(2f), Fixed.FromFloat(yPos));
                player.FaceingDirection = -1;
            }

            player.Health = charConfigs[i].MaxHealth;
            battleSystem.SetPlayerState(i, player);
        }
    }

    void Update()
    {
        accumulator += Time.deltaTime;

        while (accumulator >= FIXED_DT)
        {
            Step();
            accumulator -= FIXED_DT;
        }

        Render();
    }

    void Step()
    {
        // 采集输入（和旧版完全一致）
        FrameInput[] inputs = new FrameInput[PLAYER_COUNT];
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (isHumanPlayer[i])
                inputs[i] = LocalInputProvider.GetLocalInput(battleSystem.CurrentFrame, i);
            else
                inputs[i] = FrameInput.CreateEmpty(i, battleSystem.CurrentFrame);
        }

        // 驱动 BattleSystem
        battleSystem.Step(inputs);
    }

    void Render()
    {
        var state = battleSystem.CurrentState;
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (playerViews[i] != null)
                playerViews[i].UpdateVisual(state.Players[i]);
        }
    }
}