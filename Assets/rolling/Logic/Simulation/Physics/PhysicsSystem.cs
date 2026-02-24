using FGLogic.Core;
using FGLogic.State;

public class PhysicsSystem
{
    int playerCount;

    public void Init(int playerCount)
    {
        this.playerCount = playerCount;
    }

    public void Update(ref GameState state, float dt, CharacterConfig[] configs)
    {
        if (configs == null || configs.Length < state.PlayerCount)
        {
            return;
        }

        for (int i = 0; i < state.PlayerCount; i++)
        {
            var player = state.Players[i];
            var cfg = configs[i];

            Fixed fixedDt = Fixed.FromFloat(dt);

            Fixed desiredX = player.Position.X + player.Velocity.X * fixedDt;
            Fixed desiredY = player.Position.Y + player.Velocity.Y * fixedDt;
            

            Fixed newX = TryMoveX(player, cfg, desiredX, ref state, configs);
            Fixed newY = TryMoveY(player, cfg, desiredY, ref state, configs);
           
            player.Position = new FixedVector2(newX, newY);

            ApplyBoundaryConstraints(ref player, cfg);

            state.Players[i] = player;
        }
    }

   
    Fixed TryMoveX(PlayerState player, CharacterConfig cfg, Fixed desiredX, ref GameState state, CharacterConfig[] configs)
    {
        FixedVector2 testPos = new FixedVector2(desiredX, player.Position.Y);

        
        bool wouldCollide = false;
        for (int j = 0; j < state.PlayerCount; j++)
        {
            if (j == player.PlayerId) continue;

            var other = state.GetPlayer(j);
            if (WouldOverlap(testPos, cfg, other.Position, configs[j]))
            {
                wouldCollide = true;
                break;
            }
        }

        if (wouldCollide)
        {
            
            player.Velocity = new FixedVector2(Fixed.Zero, player.Velocity.Y);
            return player.Position.X; 
        }
        else
        {
            return desiredX; 
        }
    }

    
    Fixed TryMoveY(PlayerState player, CharacterConfig cfg, Fixed desiredY, ref GameState state, CharacterConfig[] configs)
    {
        
        FixedVector2 testPos = new FixedVector2(player.Position.X, desiredY);

        bool wouldCollide = false;
        for (int j = 0; j < state.PlayerCount; j++)
        {
            if (j == player.PlayerId) continue;

            var other = state.GetPlayer(j);
            if (WouldOverlap(testPos, cfg, other.Position, configs[j]))
            {
                wouldCollide = true;
                break;
            }
        }

        if (wouldCollide)
        {
            player.Velocity = new FixedVector2(player.Velocity.X, Fixed.Zero);
            return player.Position.Y;
        }
        else
        {
            return desiredY;
        }
    }

   
    bool WouldOverlap(FixedVector2 pos1, CharacterConfig cfg1, FixedVector2 pos2, CharacterConfig cfg2)
    {
        Fixed halfW1 = Fixed.FromFloat(cfg1.HurtboxWidth * 0.5f);
        Fixed halfH1 = Fixed.FromFloat(cfg1.HurtboxHeight * 0.5f);
        Fixed halfW2 = Fixed.FromFloat(cfg2.HurtboxWidth * 0.5f);
        Fixed halfH2 = Fixed.FromFloat(cfg2.HurtboxHeight * 0.5f);

        bool overlapX = Fixed.Abs(pos1.X - pos2.X) < (halfW1 + halfW2);
        bool overlapY = Fixed.Abs(pos1.Y - pos2.Y) < (halfH1 + halfH2);

        return overlapX && overlapY;
    }

    void ApplyBoundaryConstraints(ref PlayerState player, CharacterConfig cfg)
    {
        Fixed newX = player.Position.X;
        Fixed newY = player.Position.Y;

       
        Fixed groundY = Fixed.FromFloat(cfg.StageGroundY);
        if (player.Position.Y < groundY)
        {
            newY = groundY;
        }

        
        Fixed left = Fixed.FromFloat(cfg.StageLeftBound);
        Fixed right = Fixed.FromFloat(cfg.StageRightBound);

        if (player.Position.X < left) newX = left;
        if (player.Position.X > right) newX = right;

        player.Position = new FixedVector2(newX, newY);
    }
}