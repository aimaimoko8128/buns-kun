using System.Collections.Generic;

namespace BunsKun.Upgrades
{
    /// <summary>
    /// Accumulates every run-limited upgrade the player has chosen. Purely additive,
    /// reset at the start of every new run - no permanent progression is stored.
    /// </summary>
    public class RunUpgradeStats
    {
        public float MaxHpBonus { get; private set; }
        public float MoveSpeedPercent { get; private set; }
        public float DamagePercent { get; private set; }
        public float DelayReductionPercent { get; private set; }
        public float MaxManaBonus { get; private set; }
        public float ManaRegenPercent { get; private set; }
        public float ProjectileSpeedPercent { get; private set; }
        public float ProjectileSizePercent { get; private set; }
        public float CritChanceBonus { get; private set; }
        public float DamageTakenReductionPercent { get; private set; }

        public List<UpgradeData> Acquired { get; } = new List<UpgradeData>();

        public void Apply(UpgradeData data)
        {
            if (data == null) return;
            Acquired.Add(data);
            MaxHpBonus += data.maxHpBonus;
            MoveSpeedPercent += data.moveSpeedPercent;
            DamagePercent += data.damagePercent;
            DelayReductionPercent += data.delayReductionPercent;
            MaxManaBonus += data.maxManaBonus;
            ManaRegenPercent += data.manaRegenPercent;
            ProjectileSpeedPercent += data.projectileSpeedPercent;
            ProjectileSizePercent += data.projectileSizePercent;
            CritChanceBonus += data.critChanceBonus;
            DamageTakenReductionPercent += data.damageTakenReductionPercent;
        }

        public void Reset()
        {
            Acquired.Clear();
            MaxHpBonus = MoveSpeedPercent = DamagePercent = DelayReductionPercent = 0f;
            MaxManaBonus = ManaRegenPercent = ProjectileSpeedPercent = ProjectileSizePercent = 0f;
            CritChanceBonus = DamageTakenReductionPercent = 0f;
        }
    }
}
