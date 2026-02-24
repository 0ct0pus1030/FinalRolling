using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace FGLogic.Core
{
    /// <summary>
    /// 32.32 定点数
    /// 整数部分32位：±2,147,483,648
    /// 小数部分32位：精度约 2.3e-10
    /// </summary>
    public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed FromRaw(long raw)
        {
            return new Fixed(raw);
        }


        public readonly long Raw; // 实际存储的64位定点数值

        private const int FRACTIONAL_BITS = 32;
        private const long ONE = 1L << FRACTIONAL_BITS; // 1.0 的表示

        // 常用常量预计算
        public static readonly Fixed Zero = new Fixed(0);
        public static readonly Fixed One = new Fixed(ONE);
        public static readonly Fixed Half = new Fixed(ONE >> 1);
        public static readonly Fixed Epsilon = new Fixed(1L);
        public static readonly Fixed MaxValue = new Fixed(long.MaxValue);
        public static readonly Fixed MinValue = new Fixed(long.MinValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Fixed(long raw)
        {
            Raw = raw;
        }

        #region 构造与转换

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed FromInt(int value)
        {
            return new Fixed((long)value << FRACTIONAL_BITS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed FromFloat(float value)
        {
            return new Fixed((long)(value * ONE + (value >= 0 ? 0.5f : -0.5f)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToFloat()
        {
            return (float)Raw / ONE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToInt()
        {
            return (int)(Raw >> FRACTIONAL_BITS);
        }

        #endregion

        #region 运算符

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator +(Fixed a, Fixed b)
        {
            return new Fixed(a.Raw + b.Raw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator -(Fixed a, Fixed b)
        {
            return new Fixed(a.Raw - b.Raw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator -(Fixed a)
        {
            return new Fixed(-a.Raw);
        }

        // 乘法：使用 decimal 作为中间类型避免溢出，然后除以 2^32
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator *(Fixed a, Fixed b)
        {
            // (a * b) / 2^32
            long result = (long)((((decimal)a.Raw * (decimal)b.Raw) / (decimal)ONE));
            return new Fixed(result);
        }

        // 除法：被除数先乘 2^32，再除
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator /(Fixed a, Fixed b)
        {
            if (b.Raw == 0) throw new DivideByZeroException();
            long result = (long)((((decimal)a.Raw * (decimal)ONE) / (decimal)b.Raw));
            return new Fixed(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Fixed a, Fixed b) => a.Raw > b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Fixed a, Fixed b) => a.Raw < b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Fixed a, Fixed b) => a.Raw == b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Fixed a, Fixed b) => a.Raw != b.Raw;

        #endregion

        #region 数学函数

        /// <summary>
        /// 平方根（整数牛顿迭代法，确定性）
        /// </summary>
        public static Fixed Sqrt(Fixed value)
        {
            if (value.Raw < 0) throw new ArithmeticException("Square root of negative number");
            if (value.Raw == 0) return Zero;

            long x = value.Raw;
            long y = (x + ONE) >> 1; // 初始猜测：平均值

            // 牛顿迭代：y = (y + x/y) / 2
            // 使用 decimal 避免 (x * ONE) 溢出 64 位
            for (int i = 0; i < 10; i++) // 10次迭代确保 32.32 精度收敛
            {
                // 定点数除法：x / y = (x * ONE) / y
                long div = (long)(((decimal)x * ONE) / (decimal)y);
                long next = (y + div) >> 1;

                if (next == y) break; // 已收敛
                y = next;
            }

            return new Fixed(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Abs(Fixed value)
        {
            return new Fixed(Math.Abs(value.Raw));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Clamp(Fixed value, Fixed min, Fixed max)
        {
            if (value < min) return min;
            if (value > max) return value;
            return value;
        }


        
        
        #endregion

        #region 接口实现

        public bool Equals(Fixed other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is Fixed other && Equals(other);
        public override int GetHashCode() => Raw.GetHashCode();
        public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);
        public override string ToString() => $"{ToFloat():F6}";

        #endregion
    }
}