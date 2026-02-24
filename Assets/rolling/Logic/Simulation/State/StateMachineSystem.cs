using FGLogic.Core;
using FGLogic.Input;
using FGLogic.State;
//using UnityEngine;
//using static UnityEditor.VersionControl.Asset;

namespace Assets.Logic.State
{
    public class StateMachineSystem
    {
        public CharacterConfig[] CharConfigs { get; set; }


        //static readonly Fixed FIXED_707 = Fixed.FromFloat(0.707106781f);

        public void Update(ref PlayerState player, FrameInput input, int absoluteFrame)
        {
            //首次运行初始化（防止Unity序列化干扰）
            if (player.LastUpdateAbsoluteFrame == -1)
            {
                player.StateEnterAbsoluteFrame = absoluteFrame;
            }

            //幂等性保护：如果这一帧已经算过了，不再重复计算
            if (player.LastUpdateAbsoluteFrame == absoluteFrame) return;
            player.LastUpdateAbsoluteFrame = absoluteFrame;


            int stateBefore = player.StateId;


            //计算相对帧号：现在过了多少帧 = 现在绝对帧 - 进入时绝对帧
            //这样无论重模拟多少次，StateFrame 都是正确的
            player.StateFrame = absoluteFrame - player.StateEnterAbsoluteFrame;


            switch (player.StateId)
            {
                case 0:
                    UpdateIdle(ref player, input, absoluteFrame);
                    break;
                case 1:
                    UpdateRun(ref player, input, absoluteFrame);
                    break;
                case 2:
                    UpdateAttack(ref player, input, absoluteFrame);
                    break;
                case 3:
                    UpdateRolling(ref player, input, absoluteFrame);
                    break;
                case 4:
                    UpdateBlock(ref player, input, absoluteFrame);
                    break;
                case 5:
                    UpdateHurt(ref player, input, absoluteFrame);
                    break;

            }
            //立即重置 StateEnterFrame 和 StateFrame
            if (player.StateId != stateBefore)
            {
                player.StateEnterAbsoluteFrame = absoluteFrame;
                player.StateFrame = 0;  // 新状态第0帧
            }

        }


        void UpdateAttack(ref PlayerState player, FrameInput input, int absoluteFrame)
        {

            if (player.StateFrame <= 1)
            {
                player.Velocity = FixedVector2.Zero;
                player.AttackType = player.ComboCount;
                player.HitTargetMask = 0;
            }
            player.Velocity = FixedVector2.Zero;  // 攻击时不移动

            if (player.StateFrame >= GameConstants.ATTACK_FRAME)
            {
                if (player.ComboCount <= 1)
                {
                    player.ComboCount++;
                }
                else player.ComboCount = 0;


                //停止攻击
                player.IsAttacking = false;
                player.AttackType = -1;

                // 状态转移：回 Idle 或 Run
                ApplyToRemoveOrIdle(ref player, input);
            }

        }

        void UpdateIdle(ref PlayerState player, FrameInput input, int absoluteFrame)
        {

            InitCombo(ref player);

            //Debug.Log($"[Idle] P{player.PlayerId} Frame{player.StateFrame} InputDir:{input.HasDirection}");

            if (player.StateFrame == 0)
            {
                player.Velocity = FixedVector2.Zero;
            }


            player.Velocity = FixedVector2.Zero;
            ApplyFacing(ref player, input);

            if (TrySwitchToAnySkill(ref player, input))
                return;

            ApplyToRemoveOrIdle(ref player, input);

        }

        void UpdateRun(ref PlayerState player, FrameInput input, int absoluteFrame)
        {
            var cfg = GetCfg(player.PlayerId);

            if (!input.HasDirection)
            {
                player.StateId = 0;
                player.Velocity = FixedVector2.Zero;
                return;
            }

            Fixed speed = Fixed.FromFloat(cfg.RunSpeed);

            // 【关键】Stick 已经是单位向量（8方向查表保证），直接乘速度
            // 不要判断斜向再乘 0.707！
            player.Velocity = new FixedVector2(
                input.Stick.X * speed,
                input.Stick.Y * speed
            );

            ApplyFacing(ref player, input);
            if (TrySwitchToAnySkill(ref player, input))
                return;
        }

        void UpdateRolling(ref PlayerState player, FrameInput input, int absoluteFrame)
        {
            var cfg = GetCfg(player.PlayerId);
            InitCombo(ref player);

            if (player.StateFrame <= 1)
            {
                Fixed speed = Fixed.FromFloat(cfg.RollSpeed);

                // 【关键修改】直接使用 input.Stick，它已经是单位向量（8方向查表保证）
                // 不需要再判断斜向或归一化！
                Fixed X = input.Stick.X;
                Fixed Y = input.Stick.Y;

                if (X != Fixed.Zero || Y != Fixed.Zero)
                {
                    // 直接应用速度，Stick已经是单位向量（长度=1）
                    player.Velocity = new FixedVector2(X * speed, Y * speed);

                    // 更新朝向
                    ApplyFacing(ref player, input);
                }
                else
                {
                    player.Velocity = FixedVector2.Zero;
                }
            }

            if (player.StateFrame >= GameConstants.ROLLING_FRAME)
            {
                ApplyToRemoveOrIdle(ref player, input);
                return;
            }
        }

        void UpdateBlock(ref PlayerState player, FrameInput input, int absoluteFrame)
        {
            if (player.StateFrame <= 1)
            {
                player.ComboCount = 0;
            }

            player.Velocity = FixedVector2.Zero;

            if (player.StateFrame >= GameConstants.BLOCK_FRAME)
            {
                ApplyToRemoveOrIdle(ref player, input);
                return;
            }
        }

        void UpdateHurt(ref PlayerState player, FrameInput input, int absoluteFrame)
        {
            player.ComboCount = 0;
            player.Velocity = FixedVector2.Zero;  // 受击时不能动

            // 【缺失的关键】硬直结束恢复
            if (player.StateFrame >= player.HitstunFrames)  // 需要确保 HitstunFrames 有值
            {
                if (TrySwitchToAnySkill(ref player, input))
                    return;
                ApplyToRemoveOrIdle(ref player, input);
            }
        }


        //申请切换状态
        void ApplyToRemoveOrIdle(ref PlayerState player, FrameInput input)
        {
            if (input.HasDirection)
                player.StateId = 1;
            else
                player.StateId = 0;

            return;

        }
        bool ApplyToAttack1(ref PlayerState player, FrameInput input)
        {
            if (input.HasAttack)
            {
                player.IsAttacking = true;
                player.AttackType = 0;
                player.StateId = 2;
                return true;
            }
            return false;
        }
        bool ApplyToRolling(ref PlayerState player, FrameInput input)
        {
            if (input.HasRoll)
            {
                player.StateId = 3;
                return true;
            }
            return false;
        }
        bool ApplyToBlock(ref PlayerState player, FrameInput input)
        {
            if (input.HasBlock)
            {
                player.StateId = 4;
                return true;
            }
            return false;
        }

        bool TrySwitchToAnySkill(ref PlayerState player, FrameInput input)
        {
            return ApplyToAttack1(ref player, input)
                || ApplyToRolling(ref player, input)
                || ApplyToBlock(ref player, input);
        }

        void ApplyFacing(ref PlayerState player, FrameInput input)
        {
            if (input.Stick.X > Fixed.Zero) player.FaceingDirection = 1;
            if (input.Stick.X < Fixed.Zero) player.FaceingDirection = -1;
            return;
        }

        void InitCombo(ref PlayerState player)
        {
            if (player.StateFrame > 20)
            {
                player.ComboCount = 0;
            }
        }

        // 辅助方法：安全获取配置
        CharacterConfig GetCfg(int playerId)
        {
            if (CharConfigs == null || playerId < 0 || playerId >= CharConfigs.Length)
                return null;
            return CharConfigs[playerId];
        }


    }


}