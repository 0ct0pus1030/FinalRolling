using FGLogic.Core;
using FGLogic.State;

public class EventProcessor
{
    public void Process(ref GameState state)
    {
        for (int i = 0; i < state.EventCount; i++)
        {
            var evt = state.Events[i];
            if (evt.Type == EventType.HitConfirm)
            {
                var target = state.GetPlayer(evt.TargetId);

                //扣血
                target.Health -= evt.Damage;

                //切受击状态
                target.StateId = 5; // Hurt
                target.HitstunFrames = evt.HitStun;
                target.StateEnterAbsoluteFrame = state.FrameId;
                target.StateFrame = 0;
                target.Velocity = FixedVector2.Zero; // 受击时速度清零

                state.SetPlayer(evt.TargetId, target);
            }
        }
        state.EventCount = 0; // 消费完清空
    }
}