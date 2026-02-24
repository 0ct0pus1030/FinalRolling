using UnityEngine;

[CreateAssetMenu(fileName = "Attack_Light", menuName = "FG/Attack Config")]
public class AttackConfig : ScriptableObject
{
    [Header("信息")]
    public string AttackName = "轻攻击";
    public int AttackId = 0;  

    [Header("时间")]
    public int TotalFrames = 14;  // 攻击总帧数

    [Header("判定")]
    public HitboxData[] HitBoxes;  // 支持多段攻击


   

    

    //校验
    void OnValidate()
    {
        if (HitBoxes != null)
        {
            foreach (var hb in HitBoxes)
            {
                if (hb.StartFrame > hb.EndFrame)
                {
                    Debug.LogError($"[{AttackName}] 攻击框起始帧({hb.StartFrame})不能大于结束帧({hb.EndFrame})！");
                }
                if (hb.StartFrame < 0 || hb.EndFrame > TotalFrames)
                {
                    Debug.LogWarning($"[{AttackName}] 攻击框帧数范围超出总时长！");
                }
            }
        }
    }
}