using Assets.Logic.State;
using FGLogic.Core;
using FGLogic.Input;
using FGLogic.State;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

public class BattleController : MonoBehaviour
{
    [Header("玩家配置")]
    public bool[] isHumanPlayer;

    [Header("配置")]
    public AttackConfig[] attackConfigs;    // 拖拽赋值：轻中重攻击配置
    public CharacterConfig[] charConfigs;   // 拖拽赋值：角色配置（受击框大小等）

    [Header("视图")]
    public PlayerView[] playerViews;        // 对应每个玩家的视图

    // 系统实例（可new或ScriptableObject）
    StateMachineSystem stateMachine = new StateMachineSystem();
    AttackHitboxSystem hitboxSystem = new AttackHitboxSystem();
    EventProcessor eventProcessor = new EventProcessor();
    PhysicsSystem physics = new PhysicsSystem();

    InputDelayBuffer inputBuffer = new InputDelayBuffer(2, PLAYER_COUNT); // 2帧延迟

    StateSerializer serializer = new StateSerializer();
    HitStopSystem hitStop = new HitStopSystem();
    //LogicVerifier verifier; // 从场景获取

    // 运行时状态（唯一真相）
    GameState currentState;
    int currentFrame = 0;
    float accumulator = 0f;
    const float FIXED_DT = 1f / 30f;
    const int PLAYER_COUNT = 3; // 或根据场景动态获取

    void Start()
    {
        // 初始化GameState
        currentState = new GameState
        {
            PlayerCount = PLAYER_COUNT,
            FrameId = 0
        };

        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            var player = currentState.GetPlayer(i);
            player.PlayerId = i;
            player.IsActive = true;

            if (isHumanPlayer[i])
            {
                // 真人固定在左侧中间
                player.Position = new FixedVector2(Fixed.FromFloat(-2f), Fixed.Zero);
                player.FaceingDirection = 1; // 朝右打木桩
            }
            else
            {
                // 木桩在右侧上下排列，间距1
                // 计算这是第几个木桩（0, 1, 2...）
                int dummyIndex = 0;
                for (int j = 0; j < i; j++)
                {
                    if (!isHumanPlayer[j]) dummyIndex++;
                }

                float yPos = dummyIndex * 1f; // 间距1：P1在y=0, P2在y=1, P3在y=2...
                                              // 如果想让木桩群居中，用：float yPos = (dummyIndex - (dummyCount-1)/2f) * 1f;

                player.Position = new FixedVector2(Fixed.FromFloat(2f), Fixed.FromFloat(yPos));
                player.FaceingDirection = -1; // 朝左对着真人
            }

            player.Health = charConfigs[i].MaxHealth;
            currentState.SetPlayer(i, player);
        }

        physics.Init(PLAYER_COUNT);

        stateMachine.CharConfigs = charConfigs;
        //verifier = GetComponent<LogicVerifier>();
    }

    void Update()
    {
       

        // 1. 时间累积
        accumulator += Time.deltaTime;

        while (accumulator >= FIXED_DT)
        {
            StepLogicFrame();
            accumulator -= FIXED_DT;
        }

        // 2. 渲染（无插值，直接显示当前状态）
        Render();
    }


    public GameState? GetCurrentState()
    {
        return currentState;
    }


    void StepLogicFrame()
    {
        // 2.1 采集输入（区分真人和木桩）
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            FrameInput input;
            if (isHumanPlayer[i])
            {
                // 真人：采集当前帧，但目标是写入延迟缓冲
                input = LocalInputProvider.GetLocalInput(currentFrame, i);
            }
            else
            {
                // 木桩：空输入，帧号必须是当前帧（缓冲系统会处理延迟）
                input = FrameInput.CreateEmpty(i, currentFrame);
            }

            inputBuffer.Push(input, i);
        }
        // 2.2 获取延迟后的输入（比如第100帧实际用第98帧的输入）
        FrameInput[] inputs = new FrameInput[PLAYER_COUNT];
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            inputs[i] = inputBuffer.GetDelayed(currentFrame, i);
        }

        // 3.3 卡肉更新（每逻辑帧减1）
        hitStop.Update();

        // 3.4 关键：如果正在卡肉，只增长帧号，不更新状态（表现定格）
        if (hitStop.IsFreezed())
        {
            currentFrame++;
            return; // 跳过状态机、攻击判定等
        }

        // 2.3 状态机更新（设置Velocity，处理状态转移）
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            var player = currentState.Players[i];          // 取出（拷贝）
            stateMachine.Update(ref player, inputs[i], currentFrame);  // 修改
            currentState.Players[i] = player;              // 写回（覆盖）
        }

        // 2.4 攻击判定（检测碰撞，生成HitConfirm事件到currentState.Events）
        hitboxSystem.Update(currentFrame, ref currentState, attackConfigs, charConfigs);

        for (int i = 0; i < currentState.EventCount; i++)
        {
            Signals.Get<GameEventSignal>().Dispatch(currentState.Events[i]);
        }

        // 触发卡肉（分离出去）
        CheckHitStop();

        // 2.5 事件处理（扣血、切Hurt状态、设置硬直）
        eventProcessor.Process(ref currentState);



        // 2.6 物理更新（Velocity → Position，地面约束）
        physics.Update(ref currentState, FIXED_DT, charConfigs);

        // 2.7 验证与记录
        currentState.FrameId = currentFrame;
        //verifier?.LogLogicState(currentFrame, currentState.Players[0]);


        //100帧哈希
        /*
        if (currentFrame == 100)
        {
            int hash = ComputeStateHash(currentState);
            Debug.LogError($"========== 第100帧哈希: {hash} ==========");
            Debug.LogError($"P0位置: {currentState.Players[0].Position.X.ToFloat()}, {currentState.Players[0].Position.Y.ToFloat()}");
            Debug.LogError($"P0血量: {currentState.Players[0].Health}");
            Debug.LogError($"P1血量: {currentState.Players[1].Health}");

            // 停止游戏
            Time.timeScale = 0;
            enabled = false;
            return; // 第100帧不执行后续逻辑
        }
        */
        //发送信号
        Signals.Get<GameStateChangedSignal>().Dispatch(currentState);

        currentFrame++;
    }

    void Render()
    {
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (playerViews[i] != null)
            {
                playerViews[i].UpdateVisual(currentState.Players[i]);
            }
        }
    }


    void CheckHitStop()
    {
        // 只要有命中事件就触发
        for (int i = 0; i < currentState.EventCount; i++)
        {
            if (currentState.Events[i].Type == FGLogic.State.EventType.HitConfirm)
            {
                hitStop.Trigger(6); //卡肉
                break;
            }
        }
    }


    // 放在 BattleController 类里面，和 Start()/Update() 同级
    int ComputeStateHash(GameState state)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < state.PlayerCount; i++)
            {
                var p = state.Players[i];
                hash = hash * 31 + (int)(p.Position.X.Raw >> 32);
                hash = hash * 31 + (int)(p.Position.Y.Raw >> 32);
                hash = hash * 31 + p.Health;
                hash = hash * 31 + p.StateId;
                hash = hash * 31 + p.StateFrame;
            }
            return hash;
        }
    }


    // 断线重连入口：收到完整状态包后调用
    public void OnResync(byte[] stateData)
    {
        currentState = serializer.Deserialize(stateData);
        currentFrame = currentState.FrameId;
        Debug.Log($"Resync to frame {currentFrame}");
    }
}