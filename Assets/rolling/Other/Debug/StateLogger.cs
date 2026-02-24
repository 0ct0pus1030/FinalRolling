using FGLogic.Input;
using FGLogic.State;
using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 游戏状态日志记录器 - 用于验证录制/回放一致性
/// </summary>
public class StateLogger : IDisposable
{
    private StringBuilder _sb = new StringBuilder();
    private string _filePath;
    private bool _isRecording = false;
    private int _lastFrame = -1;

    // 列宽设置（便于对齐和对比）
    private const int COL_FRAME = 6;
    private const int COL_POS = 8;
    private const int COL_VEL = 8;
    private const int COL_STATE = 5;
    private const int COL_INPUT = 10;

    /// <summary>
    /// 开始记录到文件
    /// </summary>
    public void StartLogging(string fileName)
    {
        _filePath = Path.Combine(Application.persistentDataPath, fileName);
        _sb.Clear();

        // 写入文件头
        _sb.AppendLine("=== 游戏状态日志 ===");
        _sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _sb.AppendLine($"模式: {(Application.isEditor ? "Editor" : "Build")}");
        _sb.AppendLine(new string('=', 80));

        // 表头
        _sb.AppendFormat(
            "{0," + COL_FRAME + "} | " +
            "{1," + COL_POS + "} {2," + COL_POS + "} | " +
            "{3," + COL_VEL + "} {4," + COL_VEL + "} | " +
            "{5," + COL_STATE + "} {6," + COL_STATE + "} | " +
            "{7," + COL_INPUT + "} {8," + COL_INPUT + "} | " +
            "{9," + COL_STATE + "}\n",
            "Frame", "PosX", "PosY", "VelX", "VelY", "State", "StFrm", "InpX", "InpY", "Btns"
        );
        _sb.AppendLine(new string('-', 80));

        _isRecording = true;
        Debug.Log($"[StateLogger] 开始记录: {_filePath}");
    }

    /// <summary>
    /// 记录单帧状态
    /// </summary>
    public void LogFrame(int frameId, in PlayerState player, in FrameInput input)
    {
        if (!_isRecording) return;
        if (frameId == _lastFrame) return; // 避免重复记录
        _lastFrame = frameId;

        // 格式化数据（保留4位小数确保精度）
        string line = string.Format(
            "{0," + COL_FRAME + "} | " +
            "{1," + COL_POS + ":F4} {2," + COL_POS + ":F4} | " +
            "{3," + COL_VEL + ":F4} {4," + COL_VEL + ":F4} | " +
            "{5," + COL_STATE + "} {6," + COL_STATE + "} | " +
            "{7," + COL_INPUT + ":F2} {8," + COL_INPUT + ":F2} | " +
            "{9," + COL_STATE + "}\n",
            frameId,
            player.Position.X.ToFloat(),
            player.Position.Y.ToFloat(),
            player.Velocity.X.ToFloat(),
            player.Velocity.Y.ToFloat(),
            player.StateId,
            player.StateFrame,
            input.Stick.X.ToFloat(),
            input.Stick.Y.ToFloat(),
            input.Buttons
        );

        _sb.Append(line);

        // 每100帧自动保存一次（防止崩溃丢失数据）
        if (frameId % 100 == 0)
        {
            FlushToFile();
        }
    }

    /// <summary>
    /// 记录状态变化事件（调试用）
    /// </summary>
    public void LogEvent(int frameId, string message)
    {
        if (!_isRecording) return;
        _sb.AppendLine($"[Frame {frameId}] EVENT: {message}");
    }

    /// <summary>
    /// 保存到文件
    /// </summary>
    public void SaveToFile()
    {
        if (!_isRecording) return;

        FlushToFile();
        Debug.Log($"[StateLogger] 日志已保存: {_filePath} ({_lastFrame} 帧)");
    }

    private void FlushToFile()
    {
        try
        {
            File.WriteAllText(_filePath, _sb.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"[StateLogger] 保存失败: {e.Message}");
        }
    }

    public void Dispose()
    {
        SaveToFile();
        _isRecording = false;
    }
}