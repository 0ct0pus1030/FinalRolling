using FGLogic.Core;
using UnityEngine;

namespace FGLogic.Input
{
    public static class Direction
    {
       //数字键盘
        public const int Neutral = 5;   
        public const int DownBack = 1;   
        public const int Down = 2;       
        public const int DownForward = 3; 
        public const int Back = 4;       
        public const int Forward = 6;    
        public const int UpBack = 7;    
        public const int Up = 8;         
        public const int UpForward = 9;  

        
        private static readonly FixedVector2[] Vectors = new FixedVector2[]
        {
            new FixedVector2(Fixed.Zero, Fixed.Zero),                                    // 0 ����
            
            new FixedVector2(Fixed.FromRaw(-3037000499L), Fixed.FromRaw(-3037000499L)), // 1 �L (-0.707, -0.707)
            new FixedVector2(Fixed.Zero, Fixed.FromRaw(-4294967296L)),                 // 2 �� (0, -1)
            new FixedVector2(Fixed.FromRaw(3037000499L), Fixed.FromRaw(-3037000499L)),  // 3 �K (0.707, -0.707)
            new FixedVector2(Fixed.FromRaw(-4294967296L), Fixed.Zero),                 // 4 �� (-1, 0)
            new FixedVector2(Fixed.Zero, Fixed.Zero),                                    // 5 �� (0, 0)
            new FixedVector2(Fixed.FromRaw(4294967296L), Fixed.Zero),                  // 6 �� (1, 0)
            new FixedVector2(Fixed.FromRaw(-3037000499L), Fixed.FromRaw(3037000499L)),  // 7 �I (-0.707, 0.707)
            new FixedVector2(Fixed.Zero, Fixed.FromRaw(4294967296L)),                  // 8 �� (0, 1)
            new FixedVector2(Fixed.FromRaw(3037000499L), Fixed.FromRaw(3037000499L)),   // 9 �J (0.707, 0.707)
        };

       
        public static int FromStick(FixedVector2 stick)
        {
            Fixed deadzone = Fixed.FromFloat(0.3f);

            
            if (Fixed.Abs(stick.X) < deadzone && Fixed.Abs(stick.Y) < deadzone)
                return Neutral;
            
            int x = stick.X < -deadzone ? 4 : (stick.X > deadzone ? 6 : 5);
            int y = stick.Y < -deadzone ? 2 : (stick.Y > deadzone ? 8 : 5);
            
            if (x == 5) return y;      
            if (y == 5) return x;    

            return x + y - 5;
        }

        public static FixedVector2 ToVector(int dir)
        {
           
            if (dir < 0 || dir >= Vectors.Length)
                return FixedVector2.Zero;

            return Vectors[dir];
        }
    }
}