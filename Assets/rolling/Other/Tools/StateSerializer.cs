using System;
using System.IO;
using FGLogic.Core;
using FGLogic.State;

public class StateSerializer
{
    public byte[] Serialize(GameState state)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(state.FrameId);
            writer.Write(state.RandomSeed);
            writer.Write(state.PlayerCount);

            for (int i = 0; i < state.PlayerCount; i++)
            {
                WritePlayerState(writer, state.Players[i]);
            }

            return ms.ToArray();
        }
    }

    public GameState Deserialize(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            var state = new GameState();
            state.FrameId = reader.ReadInt32();
            //state.RandomSeed = reader.ReadUInt32();
            state.PlayerCount = reader.ReadInt32();

            for (int i = 0; i < state.PlayerCount; i++)
            {
                state.Players[i] = ReadPlayerState(reader);
            }

            state.EventCount = 0;
            state.ActiveHitboxCount = 0;

            return state;
        }
    }

    void WritePlayerState(BinaryWriter w, PlayerState p)
    {
        w.Write(p.PlayerId);
        w.Write(p.IsActive);
        w.Write(p.IsResimulating);

        // FixedVector2 Position: 写入 X.Raw, Y.Raw
        w.Write(p.Position.X.Raw);
        w.Write(p.Position.Y.Raw);

        // FixedVector2 Velocity
        w.Write(p.Velocity.X.Raw);
        w.Write(p.Velocity.Y.Raw);

        w.Write(p.FaceingDirection);
        w.Write(p.StateId);
        w.Write(p.StateFrame);
        w.Write(p.StateEnterAbsoluteFrame);
        w.Write(p.LastUpdateAbsoluteFrame);
        w.Write(p.IsAttacking);
        w.Write(p.AttackType);
        w.Write(p.Health);
        w.Write(p.InvincibleFrames);
        w.Write(p.HitstunFrames);
        w.Write(p.ComboCount);
        w.Write(p.InputHistoryButtons);
        w.Write(p.InputHistoryDir);
    }

    PlayerState ReadPlayerState(BinaryReader r)
    {
        var p = new PlayerState();
        p.PlayerId = r.ReadInt32();
        p.IsActive = r.ReadBoolean();
        p.IsResimulating = r.ReadBoolean();

        // 读取 long -> Fixed.FromRaw
        p.Position = new FixedVector2(
            Fixed.FromRaw(r.ReadInt64()),
            Fixed.FromRaw(r.ReadInt64())
        );
        p.Velocity = new FixedVector2(
            Fixed.FromRaw(r.ReadInt64()),
            Fixed.FromRaw(r.ReadInt64())
        );

        p.FaceingDirection = r.ReadInt32();
        p.StateId = r.ReadInt32();
        p.StateFrame = r.ReadInt32();
        p.StateEnterAbsoluteFrame = r.ReadInt32();
        p.LastUpdateAbsoluteFrame = r.ReadInt32();
        p.IsAttacking = r.ReadBoolean();
        p.AttackType = r.ReadInt32();
        p.Health = r.ReadInt32();
        p.InvincibleFrames = r.ReadInt32();
        p.HitstunFrames = r.ReadInt32();
        p.ComboCount = r.ReadInt32();
        p.InputHistoryButtons = r.ReadUInt64();
        p.InputHistoryDir = r.ReadUInt32();

        return p;
    }
}