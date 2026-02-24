using UnityEngine;
using FGLogic.State;
using FGLogic.Core;

public class PlayerView : MonoBehaviour
{
    public Animator animator;
    [Header("玩家ID")]
    public int playerId;

    // 【插值缓存】只缓存位置，2D朝向不插值
    private Vector3 currentPos;

    void Start()
    {
        // 初始化位置缓存
        currentPos = transform.position;
    }

    public void UpdateVisual(PlayerState state)
    {
        // 【1】位置插值平滑（0.2f可调，推荐0.15f-0.3f）
        Vector3 targetPos = new Vector3(
            state.Position.X.ToFloat(),
            state.Position.Y.ToFloat(),
            0
        );
        currentPos = Vector3.Lerp(currentPos, targetPos, 0.2f);
        transform.position = currentPos;

        // 【2】2D朝向处理（立即翻转，不插值！）
        if (TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.flipX = state.FaceingDirection == -1;
        }
        // 如果用Scale翻转，用这个代替：
        // Vector3 scale = transform.localScale;
        // scale.x = Mathf.Abs(scale.x) * state.FaceingDirection;
        // transform.localScale = scale;

        // 【3】动画状态（严格定格，不插值）
        switch (state.StateId)
        {
            case 0: // Idle - 正常循环
                animator.speed = 1f;
                animator.Play("Idle");
                break;

            case 1: // Run - 正常循环  
                animator.speed = 1f;
                animator.Play("Run");
                break;

            case 2: // Attack1 - 定格在第几帧
                string animName = state.AttackType switch
                {
                    0 => "Attack1",
                    1 => "Attack2",
                    2 => "Attack3",
                    _ => "Attack1"
                };

                int frameIndex = Mathf.Min(state.StateFrame, GameConstants.ATTACK_FRAME - 1);
                float normalizedTime = (float)frameIndex / GameConstants.ATTACK_FRAME;
                animator.speed = 0f; // 暂停，定格
                animator.Play(animName, 0, normalizedTime);
                break;

            case 3: // Roll - 定格在第几帧
                int rollFrame = Mathf.Min(state.StateFrame, GameConstants.ROLLING_FRAME - 1);
                float rollNormTime = (float)rollFrame / GameConstants.ROLLING_FRAME;
                animator.speed = 0f;
                animator.Play("Rolling", 0, rollNormTime);
                break;

            case 4: // Block - 定格在第几帧
                int blockFrame = Mathf.Min(state.StateFrame, GameConstants.BLOCK_FRAME - 1);
                float blockNormTime = (float)blockFrame / GameConstants.BLOCK_FRAME;
                animator.speed = 0f;
                animator.Play("Block", 0, blockNormTime);
                break;

            case 5: // Hurt - 定格在第几帧
                int hurtFrame = Mathf.Min(state.StateFrame, GameConstants.HURT_FRAME - 1);
                float hurtNormTime = (float)hurtFrame / GameConstants.HURT_FRAME;
                animator.speed = 0f;
                animator.Play("Hurt", 0, hurtNormTime);
                break;
        }
    }
}