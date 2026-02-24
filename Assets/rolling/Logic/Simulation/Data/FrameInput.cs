using System;
using FGLogic.Core;
using UnityEngine;

namespace FGLogic.Input
{
    public static class ButtonFlags
    {
        public const int Attack = 1 << 0;
        public const int Block = 1 << 1;
        public const int Roll = 1 << 2;
    }

    public struct FrameInput
    {
        // 创建空输入（用于初始化或缺省）
        public static FrameInput CreateEmpty(int playerId, int frameId)
        {
            return new FrameInput
            {
                PlayerId = playerId,
                FrameId = frameId,
                Stick = FixedVector2.Zero,
                Buttons = 0,
                IsPredicted = true
            };
        }

        public bool IsValid => FrameId >= 0;

        public int PlayerId;
        public FixedVector2 Stick;
        public int Buttons;
        public int FrameId;
        public bool IsPredicted;

        public bool HasAttack => (Buttons & ButtonFlags.Attack) != 0;
        public bool HasBlock => (Buttons & ButtonFlags.Block) != 0;
        public bool HasRoll => (Buttons & ButtonFlags.Roll) != 0;

        public bool HasDirection =>
            Fixed.Abs(Stick.X) > Fixed.FromFloat(0.02f) ||
            Fixed.Abs(Stick.Y) > Fixed.FromFloat(0.02f);

        /// <summary>
        /// 【修改】序列化：不再传x,y坐标，而是传方向数字(1-9)
        /// 7字节：FrameId(4) + Buttons(1) + Direction(1) + 保留(1)
        /// </summary>
        public byte[] Serialize()
        {
            int dir = Direction.FromStick(Stick);

            // 【修改】8字节：增加 PlayerId
            return new byte[]
            {
        (byte)(FrameId >> 24),
        (byte)(FrameId >> 16),
        (byte)(FrameId >> 8),
        (byte)(FrameId),
        (byte)Buttons,
        (byte)dir,
        (byte)PlayerId,      // ← 新增：第6字节是 PlayerId
        0                    // 保留字节
            };
        }

        public static FrameInput Deserialize(byte[] data)  // 【修改】去掉 playerId 参数
        {
            if (data == null || data.Length < 7)
                throw new ArgumentException("数据长度不足7字节");

            int frameId = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
            int buttons = data[4];
            int dir = data[5];
            int playerId = data[6];           // ← 从数据包解析 PlayerId
            FixedVector2 stick = Direction.ToVector(dir);

            return new FrameInput
            {
                PlayerId = playerId,          // ← 使用解析出的真实 PlayerId
                FrameId = frameId,
                Buttons = buttons,
                Stick = stick,
                IsPredicted = false
            };
        }

        /// <summary>
        /// 【修改】反序列化：从方向数字(1-9)查表得到精确向量
        /// </summary>
        public static FrameInput Deserialize(byte[] data, int playerId)
        {
            if (data == null || data.Length < 7)
                throw new ArgumentException("数据长度不足7字节");

            // 解析帧号（4字节）
            int frameId = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];

            // 解析按键
            int buttons = data[4];

            // 【关键】解析方向数字（1-9），然后查表得到精确向量
            int dir = data[5];
            FixedVector2 stick = Direction.ToVector(dir);  // ← 查表，不是从float还原

            return new FrameInput
            {
                PlayerId = playerId,
                FrameId = frameId,
                Buttons = buttons,
                Stick = stick,      // ← 两边用同一个表，得到完全一样的值
                IsPredicted = false
            };
        }
    }
}