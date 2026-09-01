using UnityEngine;
using BunsKun.Buns;
using BunsKun.Upgrades;

namespace BunsKun.Player
{
    /// <summary>
    /// Combines the equipped bun's base traits with the run's accumulated temporary
    /// upgrades into the final numbers other systems (movement, combat, health) read from.
    /// This is the single source of truth for "how strong is the player right now".
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float baseMoveSpeed = 6f;
        [SerializeField] private float baseMaxHp = 100f;

        public BunData CurrentBun { get; private set; }
        public RunUpgradeStats Upgrades { get; private set; } = new RunUpgradeStats();

        public void SetBun(BunData bun)
        {
            CurrentBun = bun;
        }

        public float MaxHealth => baseMaxHp + Upgrades.MaxHpBonus;
        public float MoveSpeed => baseMoveSpeed * (CurrentBun != null ? CurrentBun.moveSpeedMultiplier : 1f) * (1f + Upgrades.MoveSpeedPercent);
        public float MaxMana => (CurrentBun != null ? CurrentBun.maxMana : 100f) + Upgrades.MaxManaBonus;
        public float ManaRegenPerSecond => (CurrentBun != null ? CurrentBun.manaRegenPerSecond : 10f) * (1f + Upgrades.ManaRegenPercent);
        public float DamageMultiplier => 1f + Upgrades.DamagePercent;
        public float DelayMultiplier => Mathf.Max(0.2f, 1f - Upgrades.DelayReductionPercent);
        public float ProjectileSpeedMultiplier => 1f + Upgrades.ProjectileSpeedPercent;
        public float ProjectileSizeMultiplier => 1f + Upgrades.ProjectileSizePercent;
        public float CritChanceBonus => Upgrades.CritChanceBonus;
        public float DamageTakenMultiplier => (CurrentBun != null ? CurrentBun.damageTakenMultiplier : 1f) * Mathf.Max(0.1f, 1f - Upgrades.DamageTakenReductionPercent);
        public int IngredientSlotCount => CurrentBun != null ? CurrentBun.ingredientSlotCount : 5;

        public void ResetForNewRun(BunData startingBun)
        {
            Upgrades.Reset();
            SetBun(startingBun);
        }
    }
}
