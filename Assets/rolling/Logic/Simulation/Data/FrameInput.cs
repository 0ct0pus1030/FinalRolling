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

        public byte[] Serialize()
        {
            int dir = Direction.FromStick(Stick);
            byte[] result = new byte[8];
    
            // 使用 BitConverter（小端，与服务器一致）
            BitConverter.GetBytes(FrameId).CopyTo(result, 0);  // [0-3]
            result[4] = (byte)Buttons;
            result[5] = (byte)dir;
            result[6] = (byte)PlayerId;
            result[7] = 0;
    
            return result;
        }

        public static FrameInput Deserialize(byte[] data)
        {
            if (data == null || data.Length < 8)  // 改为 8！
                throw new ArgumentException("数据长度不足8字节");

            // 使用 BitConverter（小端，与服务器一致）
            int frameId = BitConverter.ToInt32(data, 0);  // 改为 BitConverter！
            int buttons = data[4];
            int dir = data[5];
            int playerId = data[6];
    
            return new FrameInput
            {
                PlayerId = playerId,
                FrameId = frameId,
                Buttons = buttons,
                Stick = Direction.ToVector(dir),
                IsPredicted = false
            };
        }

       //旧方法
        public static FrameInput Deserialize(byte[] data, int playerId)
        {
            if (data == null || data.Length < 7)
                throw new ArgumentException("数据长度不足7字节");

            // 解析帧号（4字节）
            int frameId = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];

            // 解析按键
            int buttons = data[4];

            //查表
            int dir = data[5];
            FixedVector2 stick = Direction.ToVector(dir);

            return new FrameInput
            {
                PlayerId = playerId,
                FrameId = frameId,
                Buttons = buttons,
                Stick = stick,
                IsPredicted = false
            };
        }
    }
}