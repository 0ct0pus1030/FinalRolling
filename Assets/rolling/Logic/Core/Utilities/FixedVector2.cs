// File: Logic/Core/FixedVector2.cs
using System;
using System.Runtime.CompilerServices;

namespace FGLogic.Core
{
    /// <summary>
    /// 基于 Fixed 的二维向量，用于位置、速度、方向
    /// 所有运算确定性，跨平台结果一致
    /// </summary>
    public struct FixedVector2 : IEquatable<FixedVector2>
    {
        public readonly Fixed X;
        public readonly Fixed Y;

        public static readonly FixedVector2 Zero = new FixedVector2(Fixed.Zero, Fixed.Zero);
        public static readonly FixedVector2 One = new FixedVector2(Fixed.One, Fixed.One);
        public static readonly FixedVector2 Right = new FixedVector2(Fixed.One, Fixed.Zero);
        public static readonly FixedVector2 Up = new FixedVector2(Fixed.Zero, Fixed.One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedVector2(Fixed x, Fixed y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedVector2(float x, float y)
        {
            X = Fixed.FromFloat(x);
            Y = Fixed.FromFloat(y);
        }

        #region 运算符

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b)
        {
            return new FixedVector2(a.X + b.X, a.Y + b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b)
        {
            return new FixedVector2(a.X - b.X, a.Y - b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector2 operator -(FixedVector2 a)
        {
            return new FixedVector2(-a.X, -a.Y);
        }

        // 数乘 (向量 * 标量)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector2 operator *(FixedVector2 v, Fixed scalar)
        {
            return new FixedVector2(v.X * scalar, v.Y * scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector2 operator *(Fixed scalar, FixedVector2 v)
        {
            return v * scalar;
        }

        // 数除
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedVector2 operator /(FixedVector2 v, Fixed scalar)
        {
            if (scalar == Fixed.Zero) throw new DivideByZeroException();
            return new FixedVector2(v.X / scalar, v.Y / scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FixedVector2 a, FixedVector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FixedVector2 a, FixedVector2 b)
        {
            return !(a == b);
        }

        #endregion

        #region 几何运算（确定性）

        /// <summary>
        /// 点积 (a·b = |a||b|cosθ)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Dot(FixedVector2 a, FixedVector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>
        /// 叉积（2D叉积是标量，代表Z轴分量，用于判断左右关系）
        /// 结果 > 0 表示 b 在 a 的左侧，< 0 在右侧
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Cross(FixedVector2 a, FixedVector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>
        /// 长度的平方 (|v|² = x² + y²)
        /// 比 Length 快，避免开方，用于比较距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed LengthSquared()
        {
            return X * X + Y * Y;
        }

        /// <summary>
        /// 长度 (|v| = sqrt(x² + y²))
        /// 慎用，涉及开方运算较慢
        /// </summary>
        public Fixed Length()
        {
            return Fixed.Sqrt(LengthSquared());
        }

        /// <summary>
        /// 归一化（得到单位向量）
        /// 注意：零向量归一化结果为 Zero（避免NaN）
        /// </summary>
        public FixedVector2 Normalized()
        {
            Fixed lenSq = LengthSquared();
            if (lenSq == Fixed.Zero) return Zero;

            Fixed len = Fixed.Sqrt(lenSq);
            return new FixedVector2(X / len, Y / len);
        }

        /// <summary>
        /// 距离平方（比 Distance 快，用于碰撞检测）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed DistanceSquared(FixedVector2 a, FixedVector2 b)
        {
            Fixed dx = a.X - b.X;
            Fixed dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 距离（涉及开方，较慢）
        /// </summary>
        public static Fixed Distance(FixedVector2 a, FixedVector2 b)
        {
            return Fixed.Sqrt(DistanceSquared(a, b));
        }

        /// <summary>
        /// 线性插值 (1-t)*a + t*b
        /// t 在 [0,1] 之间
        /// </summary>
        public static FixedVector2 Lerp(FixedVector2 a, FixedVector2 b, Fixed t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 按分量取最大值
        /// </summary>
        public static FixedVector2 Max(FixedVector2 a, FixedVector2 b)
        {
            return new FixedVector2(
                a.X > b.X ? a.X : b.X,
                a.Y > b.Y ? a.Y : b.Y
            );
        }

        /// <summary>
        /// 按分量取最小值
        /// </summary>
        public static FixedVector2 Min(FixedVector2 a, FixedVector2 b)
        {
            return new FixedVector2(
                a.X < b.X ? a.X : b.X,
                a.Y < b.Y ? a.Y : b.Y
            );
        }

        #endregion

        #region AABB 碰撞检测（格斗游戏核心）

        /// <summary>
        /// 矩形包围盒（用于碰撞检测）
        /// </summary>
        public struct Bounds
        {
            public FixedVector2 Min;
            public FixedVector2 Max;

            public Bounds(FixedVector2 min, FixedVector2 max)
            {
                Min = min;
                Max = max;
            }

            public FixedVector2 Center => (Min + Max) * Fixed.FromFloat(0.5f);
            public FixedVector2 Size => Max - Min;
            public FixedVector2 Extents => (Max - Min) * Fixed.FromFloat(0.5f);

            /// <summary>
            /// 是否包含点
            /// </summary>
            public bool Contains(FixedVector2 point)
            {
                return point.X >= Min.X && point.X <= Max.X &&
                       point.Y >= Min.Y && point.Y <= Max.Y;
            }

            /// <summary>
            /// 是否与另一个包围盒相交（AABB碰撞检测）
            /// </summary>
            public bool Intersects(in Bounds other)
            {
                return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                       Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;
            }

            /// <summary>
            /// 从中心点和半尺寸构建（Center + Extents）
            /// </summary>
            public static Bounds FromCenterExtents(FixedVector2 center, FixedVector2 extents)
            {
                return new Bounds(center - extents, center + extents);
            }

            // 在FixedVector2中补充
            public static bool CircleCollision(FixedVector2 a, Fixed r1, FixedVector2 b, Fixed r2)
            {
                Fixed distSq = DistanceSquared(a, b);
                Fixed radiusSum = r1 + r2;
                return distSq <= radiusSum * radiusSum;  // 避免开方
            }

        }

        #endregion

        #region 转换与工具

        /// <summary>
        /// 转换为 Unity Vector2（仅渲染层使用）
        /// </summary>
        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(X.ToFloat(), Y.ToFloat());
        }

        /// <summary>
        /// 从 Unity Vector2 构造（仅初始化使用）
        /// </summary>
        public static FixedVector2 FromVector2(UnityEngine.Vector2 v)
        {
            return new FixedVector2(Fixed.FromFloat(v.x), Fixed.FromFloat(v.y));
        }

        #endregion

        #region 接口实现

        public bool Equals(FixedVector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is FixedVector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        #endregion
    }
}