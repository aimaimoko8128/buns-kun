using UnityEngine;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Turns the player's actual physics settings into the tile distances the generator
    /// is allowed to rely on. Safety margins are applied here rather than at each call
    /// site, so terrain is never built at the theoretical limit of a jump.
    ///
    /// The values come from PlayerController / PlayerStats. If those are retuned, terrain
    /// generation follows automatically.
    /// </summary>
    public class PlayerMovementProfile
    {
        // Fractions of the theoretical maximum the generator trusts.
        private const float VerticalMarginUnits = 0.6f;
        private const float HorizontalMarginFraction = 0.65f;
        private const float JetpackMarginFraction = 0.6f;

        public float MoveSpeed { get; private set; }
        public float JumpForce { get; private set; }
        public float GravityScale { get; private set; }
        public float BodyHeight { get; private set; }
        public float JetpackRiseSpeed { get; private set; }
        public float JetpackFuelSeconds { get; private set; }

        public float Gravity => Mathf.Abs(Physics2D.gravity.y) * GravityScale;
        public float JumpHeight => (JumpForce * JumpForce) / (2f * Gravity);
        public float JumpAirTime => (2f * JumpForce) / Gravity;
        public float JumpDistance => MoveSpeed * JumpAirTime;
        public float JetpackClimb => JetpackRiseSpeed * JetpackFuelSeconds;

        /// <summary>Highest ledge (in tiles) the player can step up onto, with margin.</summary>
        public int MaxStepUpTiles => Mathf.Max(1, Mathf.FloorToInt(JumpHeight - VerticalMarginUnits));

        /// <summary>Widest gap (in tiles) the player can jump across, with margin.</summary>
        public int MaxJumpAcrossTiles => Mathf.Max(1, Mathf.FloorToInt(JumpDistance * HorizontalMarginFraction));

        /// <summary>How far up one tank of jetpack fuel can carry the player, with margin.</summary>
        public int MaxFlyUpTiles => Mathf.Max(1, Mathf.FloorToInt(JetpackClimb * JetpackMarginFraction));

        public int MaxFlyDriftTiles => 3;

        /// <summary>Tiles of head room the player's body needs.</summary>
        public int ClearanceTiles => Mathf.Max(2, Mathf.CeilToInt(BodyHeight));

        public PlayerMovementProfile(float moveSpeed, float jumpForce, float gravityScale,
            float bodyHeight, float jetpackRiseSpeed, float jetpackFuelSeconds)
        {
            MoveSpeed = moveSpeed;
            JumpForce = jumpForce;
            GravityScale = gravityScale;
            BodyHeight = bodyHeight;
            JetpackRiseSpeed = jetpackRiseSpeed;
            JetpackFuelSeconds = jetpackFuelSeconds;
        }

        /// <summary>
        /// Reads the profile straight off the player prefab-less runtime object, so the
        /// generator always matches how the player actually moves.
        /// </summary>
        public static PlayerMovementProfile FromPlayer(BunsKun.Player.PlayerController controller,
            BunsKun.Player.PlayerStats stats)
        {
            if (controller == null || stats == null) return Default();
            return new PlayerMovementProfile(
                stats.MoveSpeed,
                controller.JumpForce,
                controller.GravityScale,
                controller.BodyHeight,
                controller.JetpackRiseSpeed,
                controller.MaxJetpackFuel);
        }

        public static PlayerMovementProfile Default()
        {
            return new PlayerMovementProfile(6f, 15f, 3f, 1.1f, 5f, 2f);
        }

        public override string ToString()
        {
            return $"jumpH={JumpHeight:F2} jumpD={JumpDistance:F2} stepUp={MaxStepUpTiles} " +
                   $"across={MaxJumpAcrossTiles} flyUp={MaxFlyUpTiles}";
        }
    }
}
