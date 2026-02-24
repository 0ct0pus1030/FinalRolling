namespace FGLogic.Core
{
    public static class GameConstants
    {
        //!!!!!!!!!!!!!!注意，动画用的，不参与逻辑,动画帧数是固定的才敢这样做
        public const int ATTACK_FRAME = 14;
        public const int ATTACK2_FRAME = 14;
        public const int ATTACK3_FRAME = 14;
        public const int ROLLING_FRAME = 14;
        public const int BLOCK_FRAME = 12;
        public const int HURT_FRAME = 12;


        //public const int LOGIC_FRAME = 30;
        //public const float LOGIC_DELTA_TIME = 1f / LOGIC_FRAME;

        //public const int  RUN_SPEED_FLOAT = 4;// 跑速
        //public const int ROLL_SPEED_FLOAT = 10;//滚速

        // 角色受击盒（ Capsule 或 AABB 的一半）
        //public const float HURTBOX_WIDTH = 0.4f;              // 角色宽度一半
        //public const float HURTBOX_HEIGHT = 0.9f;

        // 攻击1的判定盒（相对于角色中心）
        //public const float ATK1_OFFSET_X = 0.8f;              // 身前 0.8 单位
        //public const float ATK1_OFFSET_Y = 0f;
        //public const float ATK1_WIDTH = 0.5f;                 // 攻击盒半边长
        //public const float ATK1_HEIGHT = 0.3f;

        //public const int ATTACK1_DAMAGE = 10;



        // === 网络与回滚 ===
        //public const int INPUT_DELAY_FRAMES = 2;              // 本地输入延迟（防抖动）
        //public const int MAX_ROLLBACK_FRAMES = 8;             // 最大回滚帧数（约 266ms）
        //public const int SNAPSHOT_BUFFER_SIZE = 64;           // 快照环缓冲区大小（2的幂）

        // === 场景限制 ===
        //public const float STAGE_LEFT_BOUND = -8f;
        //public const float STAGE_RIGHT_BOUND = 8f;
        //public const float STAGE_GROUND_Y = 0f;


    }
}