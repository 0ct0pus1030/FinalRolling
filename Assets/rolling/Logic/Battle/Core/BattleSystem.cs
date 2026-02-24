using Assets.Logic.State;
using FGLogic.Input;
using FGLogic.State;
using System;
using Utils;

public class BattleSystem
{
    GameState currentState;
    int currentFrame;

    public GameState CurrentState => currentState;
    public int CurrentFrame => currentFrame;

    //子系统
    readonly StateMachineSystem stateMachine = new StateMachineSystem();
    readonly AttackHitboxSystem hitboxSystem = new AttackHitboxSystem();
    readonly EventProcessor eventProcessor = new EventProcessor();
    readonly PhysicsSystem physics = new PhysicsSystem();
    readonly InputDelayBuffer inputBuffer;
    readonly HitStopSystem hitStop = new HitStopSystem();

    //配置
    readonly int playerCount;
    readonly float fixedDt;
    readonly int inputDelayFrames;
    readonly bool enableHitStop;
    readonly int hitStopFrames;
    readonly bool isNetworkMode;
    readonly AttackConfig[] attackConfigs;
    readonly CharacterConfig[] charConfigs;

    //
    public event Action<int, GameState> OnFrameAdvanced;
    public event Action<int, FrameInput[]> OnInputApplied;

    private GameEvent lastEvent;
    public GameEvent LastEvent => lastEvent;

    public BattleSystem(int playerCount, float logicFps, int inputDelayFrames,
        bool enableHitStop, int hitStopFrames,
        AttackConfig[] attackConfigs, CharacterConfig[] charConfigs,
        bool isNetworkMode = false)
    {
        this.playerCount = playerCount;
        this.fixedDt = 1f / logicFps;
        this.inputDelayFrames = inputDelayFrames;
        this.enableHitStop = enableHitStop;
        this.hitStopFrames = hitStopFrames;
        this.attackConfigs = attackConfigs;
        this.charConfigs = charConfigs;
        this.isNetworkMode = isNetworkMode;

        inputBuffer = new InputDelayBuffer(inputDelayFrames, playerCount);
        currentFrame = 0;

        InitState();
    }

    public void Step(FrameInput[] inputs)
    {
        // 输入缓冲（本地模式 inputDelayFrames=0，直接透传）
        FrameInput[] delayedInputs;
        if (inputDelayFrames == 0)
        {
            delayedInputs = inputs;
        }
        else
        {
            for (int i = 0; i < playerCount; i++)
                inputBuffer.Push(inputs[i], i);

            delayedInputs = new FrameInput[playerCount];
            for (int i = 0; i < playerCount; i++)
                delayedInputs[i] = inputBuffer.GetDelayed(currentFrame, i);
        }

        // 顿帧更新
        hitStop.Update();

        // 顿帧检查
        if (enableHitStop && hitStop.IsFreezed())
        {
            currentFrame++;
            currentState.FrameId = currentFrame;
            if (isNetworkMode) OnFrameAdvanced?.Invoke(currentFrame, currentState);
            return;
        }

        // 状态机更新
        for (int i = 0; i < playerCount; i++)
        {
            var player = currentState.Players[i];
            stateMachine.Update(ref player, delayedInputs[i], currentFrame);
            currentState.SetPlayer(i, player);
        }

        // 攻击判定
        hitboxSystem.Update(currentFrame, ref currentState, attackConfigs, charConfigs);

        // 事件分发
        for (int i = 0; i < currentState.EventCount; i++)
        {
            var evt = currentState.Events[i];
            Signals.Get<GameEventSignal>()?.Dispatch(currentState.Events[i]);
            if (evt.Type != EventType.None)
            {
                lastEvent = evt;
            }
        }

        // 触发顿帧
        if (enableHitStop && CheckHitConfirm())
        {
            hitStop.Trigger(hitStopFrames);
        }

        // 事件处理
        eventProcessor.Process(ref currentState);

        // 物理更新
        physics.Update(ref currentState, fixedDt, charConfigs);

        // 推进帧
        currentFrame++;
        currentState.FrameId = currentFrame;

        // 发送状态变化信号（关键！）
        if (isNetworkMode)
        {
            OnFrameAdvanced?.Invoke(currentFrame, currentState);
            OnInputApplied?.Invoke(currentFrame, delayedInputs);
            Signals.Get<GameStateChangedSignal>()?.Dispatch(currentState);
        }
        else
        {
            // 本地模式：发送状态变化信号
            Signals.Get<GameStateChangedSignal>()?.Dispatch(currentState);
        }
    }

    bool CheckHitConfirm()
    {
        for (int i = 0; i < currentState.EventCount; i++)
            if (currentState.Events[i].Type == EventType.HitConfirm)
                return true;
        return false;
    }

    /// <summary>
    /// 计算状态哈希（网络校验用）
    /// </summary>
    public int ComputeStateHash()
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < currentState.PlayerCount; i++)
            {
                var p = currentState.Players[i];
                hash = hash * 31 + (int)(p.Position.X.Raw >> 32);
                hash = hash * 31 + (int)(p.Position.Y.Raw >> 32);
                hash = hash * 31 + p.Health;
                hash = hash * 31 + p.StateId;
                hash = hash * 31 + p.StateFrame;
            }
            return hash;
        }
    }

    void InitState()
    {
        currentState = new GameState
        {
            PlayerCount = playerCount,
            FrameId = 0
        };

        physics.Init(playerCount);
        stateMachine.CharConfigs = charConfigs;

        // 注意：玩家初始化由外部完成，这里只创建空状态
        for (int i = 0; i < playerCount; i++)
        {
            currentState.SetPlayer(i, new PlayerState { PlayerId = i });
        }
    }

    // 外部设置玩家初始状态（位置、血量等）
    public void SetPlayerState(int id, PlayerState player)
    {
        currentState.SetPlayer(id, player);
    }

    public void LoadState(GameState state)
    {
        currentState = state;
        currentFrame = state.FrameId;
        inputBuffer?.Clear();
    }



}