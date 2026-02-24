using FGLogic.Core;
using FGLogic.State;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
//using static UnityEditor.Experimental.GraphView.GraphView;


namespace FGLogic.State
{
    /// <summary>
    /// 单个玩家的完整状态（值类型，可直接memcpy复制）
    /// 包含：空间信息 + 状态机信息 + 战斗信息
    /// </summary>


    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PlayerState
    {
        //玩家ID
        public int PlayerId;
        //是否再活跃
        public bool IsActive;
        //是否正在重新模拟
        public bool IsResimulating;


        public FixedVector2 Position;
        public FixedVector2 Velocity;
        public int FaceingDirection;

        //状态ID
        public int StateId;
        //状态内帧数
        public int StateFrame;


        // === 新增：回滚必需 ===
        public int StateEnterAbsoluteFrame;   // 进入当前状态时的全局帧号（全局第几帧进入的）
        public int LastUpdateAbsoluteFrame;   // 上次被更新的全局帧号（防止一帧内重复Update）


        public bool IsAttacking;
        //是否命中
        public bool HasAttackHit;
        // 位图：bit0=玩家0已命中, bit1=玩家1已命中...
        public int HitTargetMask; 
        public int AttackType;



        public int Health;
        public int InvincibleFrames;    // 无敌帧倒计时（>0时不受伤害，翻滚/受击时用）
        public int HitstunFrames;       // 硬直倒计时（>0时无法控制角色，被打了）

        //连击数
        public int ComboCount;


        // 用两个ulong压缩存储，避免数组分配（C# struct内数组是引用类型，不好回滚）
        public ulong InputHistoryButtons;   // 最近16帧的按键（每帧4bit）
        public uint InputHistoryDir;        // 最近8帧的方向（每帧4bit，0-8表示8方向）



        public void PushInput(int directionId, int buttons)
        {
            // 方向历史：左移4位，新方向塞右边（只保留8帧）
            InputHistoryDir = (uint)((InputHistoryDir << 4) | (uint)(directionId & 0xF));

            // 按键历史：左移4位，新按键塞右边（保留16帧）
            InputHistoryButtons = (InputHistoryButtons << 4) | (ulong)(buttons & 0xF);
        }

        /// <summary>
        /// 获取N帧前的方向（0=当前，1=上一帧...）
        /// </summary>
        public int GetDirection(int framesAgo)
        {

            if (framesAgo > 7)
            {
                return FGLogic.Input.Direction.Neutral; // 超出记录返回中立
            }
            return (int)((InputHistoryDir >> (framesAgo * 4)) & 0xF);
        }

    }


    // 3. 攻击框配置（ScriptableObject，只读配置）
    [CreateAssetMenu]
    public class AttackConfig : ScriptableObject 
    {
        public HitboxData[] Hitboxes;  // 攻击框时间轴配置

    }

    // 4. 临时攻击框状态（用于碰撞检测，每帧从 Config + PlayerState 计算）
    

}