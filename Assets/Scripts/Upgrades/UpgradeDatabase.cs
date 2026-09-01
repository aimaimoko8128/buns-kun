using System.Collections.Generic;

namespace BunsKun.Upgrades
{
    public static class UpgradeDatabase
    {
        private static List<UpgradeData> all;

        public static IReadOnlyList<UpgradeData> All
        {
            get { if (all == null) Build(); return all; }
        }

        public static void ResetCache()
        {
            all = null;
        }

        /// <summary>Returns `count` distinct random upgrades to offer as a reward choice.</summary>
        public static List<UpgradeData> RollChoices(System.Random rng, int count)
        {
            var pool = new List<UpgradeData>(All);
            var result = new List<UpgradeData>();
            count = UnityEngine.Mathf.Min(count, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }

        private static void Build()
        {
            all = new List<UpgradeData>
            {
                MakeHp(),
                MakeSpeed(),
                MakeDamage(),
                MakeAttackSpeed(),
                MakeMana(),
                MakeManaRegen(),
                MakeProjectileSpeed(),
                MakeProjectileSize(),
                MakeCrit(),
                MakeDefense(),
            };
        }

        private static UpgradeData MakeHp()
        {
            var u = UpgradeData.Create("max_hp", "Bigger Bite", "+20 Max HP", new UnityEngine.Color(0.9f, 0.3f, 0.3f));
            u.maxHpBonus = 20f;
            return u;
        }
        private static UpgradeData MakeSpeed()
        {
            var u = UpgradeData.Create("move_speed", "Fresh Lettuce Legs", "+15% Move Speed", new UnityEngine.Color(0.4f, 0.9f, 0.5f));
            u.moveSpeedPercent = 0.15f;
            return u;
        }
        private static UpgradeData MakeDamage()
        {
            var u = UpgradeData.Create("damage", "Extra Spice", "+15% Attack Damage", new UnityEngine.Color(0.95f, 0.5f, 0.1f));
            u.damagePercent = 0.15f;
            return u;
        }
        private static UpgradeData MakeAttackSpeed()
        {
            var u = UpgradeData.Create("attack_speed", "Quick Griddle", "-15% Activation Delay", new UnityEngine.Color(1f, 0.8f, 0.2f));
            u.delayReductionPercent = 0.15f;
            return u;
        }
        private static UpgradeData MakeMana()
        {
            var u = UpgradeData.Create("max_mana", "Secret Sauce Reserve", "+20 Max Mana", new UnityEngine.Color(0.3f, 0.5f, 0.95f));
            u.maxManaBonus = 20f;
            return u;
        }
        private static UpgradeData MakeManaRegen()
        {
            var u = UpgradeData.Create("mana_regen", "Simmering Broth", "+20% Mana Regen", new UnityEngine.Color(0.4f, 0.75f, 0.95f));
            u.manaRegenPercent = 0.2f;
            return u;
        }
        private static UpgradeData MakeProjectileSpeed()
        {
            var u = UpgradeData.Create("proj_speed", "Sizzling Toss", "+20% Projectile Speed", new UnityEngine.Color(0.9f, 0.9f, 0.3f));
            u.projectileSpeedPercent = 0.2f;
            return u;
        }
        private static UpgradeData MakeProjectileSize()
        {
            var u = UpgradeData.Create("proj_size", "Extra Helping", "+20% Projectile Size", new UnityEngine.Color(0.8f, 0.6f, 0.9f));
            u.projectileSizePercent = 0.2f;
            return u;
        }
        private static UpgradeData MakeCrit()
        {
            var u = UpgradeData.Create("crit", "Sharp Toothpick", "+10% Crit Chance", new UnityEngine.Color(0.95f, 0.95f, 0.95f));
            u.critChanceBonus = 0.1f;
            return u;
        }
        private static UpgradeData MakeDefense()
        {
            var u = UpgradeData.Create("defense", "Toasted Crust", "-15% Damage Taken", new UnityEngine.Color(0.6f, 0.4f, 0.2f));
            u.damageTakenReductionPercent = 0.15f;
            return u;
        }
    }
}
