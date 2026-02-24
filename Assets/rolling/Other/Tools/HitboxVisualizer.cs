using UnityEngine;
using FGLogic.State;
using FGLogic.Core;

[RequireComponent(typeof(PlayerView))]
public class HitboxVisualizer : MonoBehaviour
{
    [Header("数据引用")]
    public AttackConfig[] attackConfigs;    // 拖入该角色的攻击配置
    public CharacterConfig charConfig;      // 拖入角色配置（受击框）

    [Header("调试设置")]
    public bool showHurtbox = true;         // 显示受击框（绿色）
    public bool showHitbox = true;          // 显示攻击框（红色）
    public bool showAllHitboxes = false;    // false=只显示当前帧，true=显示所有时间段的框（用于编辑）
    public int previewFrame = 0;            // 当 showAllHitboxes=true 时，预览指定帧

    [Header("运行时引用")]
    public PlayerState currentState;        // 由 BattleController 每帧更新

    void OnDrawGizmos()
    {
        if (charConfig == null) return;

        // 获取朝向（运行时从 state 取，编辑时默认朝右）
        int facing = currentState.FaceingDirection != 0 ? currentState.FaceingDirection : 1;

        // 1. 绘制受击框（Hurtbox）- 绿色半透明
        if (showHurtbox)
        {
            DrawHurtbox(facing);
        }

        // 2. 绘制攻击框（Hitbox）- 红色半透明
        if (showHitbox && attackConfigs != null)
        {
            if (showAllHitboxes)
            {
                // 编辑模式：显示指定预览帧的所有攻击框
                DrawHitboxesForFrame(previewFrame, facing);
            }
            else if (currentState.StateId == 2) // Attack 状态
            {
                // 运行时模式：只显示当前 StateFrame 的激活框
                DrawHitboxesForFrame(currentState.StateFrame, facing);
            }
        }
    }

    void DrawHurtbox(int facing)
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f); // 绿色半透明

        // 从 CharacterConfig 读取数据
        Vector2 offset = charConfig.HurtboxOffset;
        float width = charConfig.HurtboxWidth;
        float height = charConfig.HurtboxHeight;

        // 应用朝向（只翻转X偏移）
        offset.x *= facing;

        Vector3 center = transform.position + new Vector3(offset.x, offset.y, 0);
        Vector3 size = new Vector3(width, height, 0.1f);

        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size); // 边框
    }

    void DrawHitboxesForFrame(int frame, int facing)
    {
        if (currentState.AttackType < 0 || currentState.AttackType >= attackConfigs.Length)
            return;

        var config = attackConfigs[currentState.AttackType];
        if (config == null || config.HitBoxes == null) return;

        foreach (var box in config.HitBoxes)
        {
            // 检查该攻击框在当前帧是否激活
            bool isActive = frame >= box.StartFrame && frame <= box.EndFrame;

            if (isActive)
            {
                Gizmos.color = new Color(1, 0, 0, 0.4f); // 激活框：红色不透明
            }
            else
            {
                Gizmos.color = new Color(1, 0.5f, 0.5f, 0.1f); // 未激活：浅红半透明（仅编辑模式可见）
            }

            // 计算世界坐标
            Vector2 offset = box.Offset;
            offset.x *= facing; // 应用朝向

            Vector3 center = transform.position + new Vector3(offset.x, offset.y, 0);
            Vector3 size = new Vector3(box.Size.x, box.Size.y, 0.1f);

            Gizmos.DrawCube(center, size);

            // 激活框画边框和文字
            if (isActive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(center + Vector3.up * 0.5f,
                    $"帧{frame}\n伤害{box.Damage}");
#endif
            }
        }
    }

    // 辅助方法：在 Scene 视图中快速测试不同帧
    [ContextMenu("下一帧预览")]
    void NextPreviewFrame()
    {
        if (attackConfigs == null || currentState.AttackType < 0) return;
        var config = attackConfigs[currentState.AttackType];
        if (config != null)
        {
            previewFrame = (previewFrame + 1) % config.TotalFrames;
            Debug.Log($"预览帧: {previewFrame}");
        }
    }
}