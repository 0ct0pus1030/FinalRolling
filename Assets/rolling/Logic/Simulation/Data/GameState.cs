using FGLogic.Core;  // 你的命名空间
using System;

namespace FGLogic.State
{
    // 1. 事件定义
    public enum EventType : 
        byte { None, HitConfirm, BlockBreak, Dead }

    public struct GameEvent
    {
        public int Frame;
        public EventType Type;
        public int SourceId;      // 攻击者
        public int TargetId;      // 受击者
        public int Damage;        // 伤害值（用int存，Fixed.ToInt()转换后）
        public FixedVector2 HitPos; // 命中位置（定点数）
        public int HitStun;       // 硬直帧数
    }

    // 2. 固定数组（解决之前编译错误）
    public struct FixedArray4<T> where T : struct
    {
        public T Item0;
        public T Item1;
        public T Item2;
        public T Item3;

        public T this[int index]
        {
            get => index switch
            {
                0 => Item0,
                1 => Item1,
                2 => Item2,
                3 => Item3,
                _ => throw new IndexOutOfRangeException()
            };
            set
            {
                switch (index)
                {
                    case 0: Item0 = value; break;
                    case 1: Item1 = value; break;
                    case 2: Item2 = value; break;
                    case 3: Item3 = value; break;
                }
            }
        }
    }


    // 4. 攻击框状态
    public struct HitboxState
    {
        public bool IsActive;
        public int OwnerId;              // 谁发出的（0=玩家0）
        public int RemainingFrames;      // 还剩几帧消失
        public FixedVector2 Offset;      // 相对角色的偏移（考虑朝向）
        public FixedVector2 Size;        // 攻击框大小（宽x高）
        public int Damage;
        public int HitStun;

        // 计算世界空间包围盒（关键！）
        public FixedVector2.Bounds GetWorldBounds(FixedVector2 ownerPos, int ownerFacing)
        {
            // 根据朝向翻转X偏移
            Fixed actualOffsetX = (ownerFacing == 1) ? Offset.X : -Offset.X;
            FixedVector2 worldOffset = new FixedVector2(actualOffsetX, Offset.Y);
            FixedVector2 center = ownerPos + worldOffset;

            FixedVector2 extents = new FixedVector2(
                Size.X * Fixed.FromFloat(0.5f),
                Size.Y * Fixed.FromFloat(0.5f)
            );

            return FixedVector2.Bounds.FromCenterExtents(center, extents);
        }
    }

    // 5. 主游戏状态（每帧的快照）
    public struct GameState
    {
        public int FrameId;
        public int RandomSeed;

        public FixedArray4<PlayerState> Players;
        public int PlayerCount;

        public FixedArray4<HitboxState> Hitboxes;  // 对象池
        public int ActiveHitboxCount;

        public FixedArray4<GameEvent> Events;      // 本帧事件
        public int EventCount;

        // 访问器
        public PlayerState GetPlayer(int id) => Players[id];
        public void SetPlayer(int id, in PlayerState player) => Players[id] = player;

        // 添加事件（辅助方法）
        public void AddEvent(in GameEvent evt)
        {
            if (EventCount >= 4) return; // 满了
            Events[EventCount] = evt;
            EventCount++;
        }
        
        
        public int ComputeSyncHash()
        {
            int hash = 0;
    
            for (int i = 0; i < PlayerCount; i++)
            {
                hash = HashCode.Combine(hash, Players[i].ComputeSyncHash());
            }
    
            return hash;
        }
    }




}