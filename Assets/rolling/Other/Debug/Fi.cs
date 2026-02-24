using FGLogic.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 把这个脚本挂到空场景，Mac 和 Win 各跑一次
public class FixedVerify : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== 验证测试 ===");

        // 测试 1：基础运算
        Fixed a = Fixed.FromInt(2);
        Fixed b = Fixed.FromInt(3);
        Debug.Log($"2 * 3 = {a * b} (期望 6)");
        Debug.Log($"2 / 3 = {a / b} (期望 0.666667)");

        // 测试 2：Sqrt
        Fixed two = Fixed.FromInt(2);
        Fixed sqrt2 = Fixed.Sqrt(two);
        Debug.Log($"Sqrt(2) = {sqrt2} (期望 1.414214)");
        Debug.Log($"Sqrt(2)^2 = {sqrt2 * sqrt2} (期望接近 2)");

        // 测试 3：之前崩溃的大数
        Fixed big1 = Fixed.FromRaw(1234567890123456789L);
        Fixed big2 = Fixed.FromRaw(987654321098765432L);
        Debug.Log($"大数乘法: {big1 * big2}");
        Debug.Log($"大数除法: {big1 / big2}");

        // 测试 4：Vectors 里的值
        Fixed sqrt2Over2 = Fixed.FromRaw(3037000499L);
        Debug.Log($"0.707 验证: {sqrt2Over2} (期望 0.707107)");
        Debug.Log($"0.707^2 = {sqrt2Over2 * sqrt2Over2} (期望接近 0.5)");
    }
}