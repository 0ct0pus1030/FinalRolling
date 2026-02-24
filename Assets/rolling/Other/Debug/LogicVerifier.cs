using FGLogic.Core;
using FGLogic.State;
using System.IO;
using UnityEngine;

public class LogicVerifier : MonoBehaviour
{
    [Header("设置")]
    public bool enableLogging = true;
    public int logEveryNFrames = 1; // 每几帧记录一次（1=每帧都记）

    private StreamWriter logFile;
    private string logPath;

    void Start()
    {
        // 日志文件放在桌面，方便对比
        logPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            $"logic_log_{System.DateTime.Now:HHmmss}.txt"
        );

        logFile = new StreamWriter(logPath);
        logFile.WriteLine("Frame,PosX,PosY,VelX,VelY,StateId,FaceDir,StateFrame,AttackType,Hash");

        Debug.Log($"<color=green>[LogicVerifier] 逻辑日志: {logPath}</color>");
    }

    /// <summary>
    /// 每帧调用，记录纯逻辑值
    /// </summary>
    public void LogLogicState(int frameId, PlayerState state)
    {
        if (!enableLogging || logFile == null) return;

        // 只记录逻辑值，不记录任何视觉相关
        string line = string.Format("{0},{1:F6},{2:F6},{3:F6},{4:F6},{5},{6},{7},{8},{9}",
            frameId,
            state.Position.X.ToFloat(),
            state.Position.Y.ToFloat(),
            state.Velocity.X.ToFloat(),
            state.Velocity.Y.ToFloat(),
            state.StateId,
            state.FaceingDirection,
            state.StateFrame,      // 状态内帧号（如果有）
            state.AttackType,      // 攻击类型（如果有）
            CalculateHash(state)   // 综合哈希
        );

        logFile.WriteLine(line);

        // 立即刷新，防止崩溃丢失
        if (frameId % 10 == 0) logFile.Flush();
    }

    string CalculateHash(PlayerState state)
    {
        // 把所有逻辑值拼成字符串算哈希
        string data = $"{state.Position.X.ToFloat():F6}|{state.Position.Y.ToFloat():F6}|" +
                     $"{state.Velocity.X.ToFloat():F6}|{state.Velocity.Y.ToFloat():F6}|" +
                     $"{state.StateId}|{state.FaceingDirection}|{state.StateFrame}";
        return data.GetHashCode().ToString("X8");
    }

    void OnDestroy()
    {
        logFile?.Flush();
        logFile?.Close();
        Debug.Log($"<color=green>[LogicVerifier] 日志已保存: {logPath}</color>");
    }
}