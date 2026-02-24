using UnityEngine;

[CreateAssetMenu(fileName = "Character_Warrior", menuName = "FG/Character Config")]
public class CharacterConfig : ScriptableObject
{
    [Header("速度")]
    public int RunSpeed = 4;           
    public int RollSpeed = 10;         

    [Header("受击框大小")]
    public float HurtboxWidth = 0.4f;   
    public float HurtboxHeight = 0.9f;  

    //默认中心点在脚下
    [Tooltip("受击框y偏移")]
    public Vector2 HurtboxOffset = new Vector2(0, 0.45f);  


    //目前没用
    [Header("逻辑帧")]
    public int LogicFrameRate = 30;     
    public float LogicDeltaTime => 1f / LogicFrameRate;  


    //目前没用
    [Header("输入延迟检测，回滚帧数")]
    public int InputDelayFrames = 2;    
    public int MaxRollbackFrames = 8;  

    [Header("边界")]
    public float StageLeftBound = -8f;
    public float StageRightBound = 8f;
    public float StageGroundY = -8f;

    [Header("其他")]
    public int MaxHealth = 100;
}


