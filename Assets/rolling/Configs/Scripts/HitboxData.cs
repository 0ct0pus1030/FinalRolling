using UnityEngine;

[System.Serializable]
public struct HitboxData
{
    public int StartFrame;
    public int EndFrame;
    public Vector2 Offset;
    public Vector2 Size;
    public int Damage;
    public int HitStunFrames;
    
}