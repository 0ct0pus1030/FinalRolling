using FGLogic.Core;
using FGLogic.State;

public class AttackHitboxSystem
{
    public void Update(int currentFrame, ref GameState gameState, AttackConfig[] attackConfigs,CharacterConfig[] charConfig)
    {
        //清空事件
        gameState.EventCount = 0;

        //遍历实体
        for (int i = 0; i < gameState.PlayerCount; i++)
        {
            var player = gameState.GetPlayer(i);

            //攻击状态检测
            if (player.StateId != 2) 
                continue;

            //获取配置
            var config = attackConfigs[player.AttackType];
            if (config == null) 
                continue;

            //引入当前帧数
            int attackFrame = player.StateFrame;

            //遍历配置的每个攻击框
            for (int boxIdx = 0; boxIdx < config.HitBoxes.Length; boxIdx++)
            {
                var boxData = config.HitBoxes[boxIdx];

                //检查当前帧是否在配置的时间窗口内
                if (attackFrame >= boxData.StartFrame &&
                    attackFrame <= boxData.EndFrame)
                {
                    PerformHitCheck(i, boxData,boxIdx, ref gameState, attackConfigs, charConfig);
                }
            }
        }
    }


    void PerformHitCheck(int attackerIdx, HitboxData boxData, int boxIdx,
                     ref GameState gameState, AttackConfig[] attackConfigs,
                     CharacterConfig[] charConfig )
    {
        var attacker = gameState.GetPlayer(attackerIdx);
       

        //计算攻击框世界坐标
        Fixed offsetX = Fixed.FromFloat(boxData.Offset.x * attacker.FaceingDirection);
        Fixed offsetY = Fixed.FromFloat(boxData.Offset.y);
        FixedVector2 center = attacker.Position + new FixedVector2(offsetX, offsetY);

        //引入配置数据
        FixedVector2 size = new FixedVector2(
            Fixed.FromFloat(boxData.Size.x),
            Fixed.FromFloat(boxData.Size.y)
        );
        //创建框
        var attackBounds = FixedVector2.Bounds.FromCenterExtents(center, size * Fixed.FromFloat(0.5f));

        //遍历所有其他玩家检测碰撞
        for (int t = 0; t < gameState.PlayerCount; t++)
        {
            var target = gameState.GetPlayer(t);
            if (target.PlayerId == attacker.PlayerId)
                continue;  // 不打自己

            //位图检查：是否已经命中过这个目标
            int targetBit = 1 << target.PlayerId;
            if ((attacker.HitTargetMask & targetBit) != 0)
                continue;  // 已经打中过这个人，跳过（防止重复伤害同一人）

            // 目标受击框（从CharacterConfig读取大小，PlayerState读取位置）
            var targetBounds = GetTargetHurtbox(target, charConfig[target.PlayerId]);

            // 3. AABB碰撞检测！
            if (attackBounds.Intersects(targetBounds))
            {
                // 4. 【生成事件】不直接改血，创建GameEvent
                var evt = new GameEvent
                {
                    Type = EventType.HitConfirm,
                    Frame = gameState.FrameId,      // 发生的帧号
                    SourceId = attacker.PlayerId,
                    TargetId = target.PlayerId,
                    Damage = boxData.Damage,        // 从配置读取
                    HitStun = boxData.HitStunFrames,// 硬直帧数
                    HitPos = target.Position        // 命中位置（用于飘字特效）
                };

                // 5. 【存储事件】加入GameState的临时事件数组
                gameState.AddEvent(evt);

                //关键：标记该目标已命中（修改位图）
                attacker.HitTargetMask |= targetBit;

                //关键：写回 gameState（struct 必须显式写回才能保存修改）
                gameState.SetPlayer(attackerIdx, attacker);
            }
        }
    }

    //辅助：获取目标受击框
    FixedVector2.Bounds GetTargetHurtbox(PlayerState target, CharacterConfig cfg)
    {
        FixedVector2 center = target.Position + new FixedVector2(
            Fixed.FromFloat(cfg.HurtboxOffset.x),
            Fixed.FromFloat(cfg.HurtboxOffset.y)
        );
        FixedVector2 extents = new FixedVector2(
            Fixed.FromFloat(cfg.HurtboxWidth * 0.5f),
            Fixed.FromFloat(cfg.HurtboxHeight * 0.5f)
        );
        return FixedVector2.Bounds.FromCenterExtents(center, extents);
    }

}

