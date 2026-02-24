using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace FGLogic.Core
{
    /// <summary>
    /// 32.32 定点数
    /// 整数部分32位，范围约 ±2,147,483,648
    /// 小数部分32位，精度约 2.3e-10
    /// </summary>
    public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed FromRaw(long raw)
        {
            return new Fixed(raw);
        }

        public readonly long Raw;

        private const int FRACTIONAL_BITS = 32;
        private const long ONE = 1L << FRACTIONAL_BITS;

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

        #region 类型转换

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

        // 乘法：BigInteger 确保不溢出，跨平台确定性
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator *(Fixed a, Fixed b)
        {
            var result = (BigInteger)a.Raw * b.Raw >> FRACTIONAL_BITS;
            
            if (result > long.MaxValue) return MaxValue;
            if (result < long.MinValue) return MinValue;
            
            return new Fixed((long)result);
        }

        // 除法：BigInteger 确保精度，跨平台确定性
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator /(Fixed a, Fixed b)
        {
            if (b.Raw == 0) throw new DivideByZeroException();
            var result = ((BigInteger)a.Raw << FRACTIONAL_BITS) / b.Raw;
            
            if (result > long.MaxValue) return MaxValue;
            if (result < long.MinValue) return MinValue;
            
            return new Fixed((long)result);
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

        // Sqrt：纯整数牛顿法，固定10次迭代，跨平台确定性
        public static Fixed Sqrt(Fixed value)
        {
            if (value.Raw < 0) throw new ArithmeticException("Square root of negative number");
            if (value.Raw == 0) return Zero;

            long x = value.Raw;
            long y = (x + ONE) >> 1;

            for (int i = 0; i < 10; i++)
            {
                long div = (long)(((BigInteger)x << FRACTIONAL_BITS) / y);
                y = (y + div) >> 1;
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