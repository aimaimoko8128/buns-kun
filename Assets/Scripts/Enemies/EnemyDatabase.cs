using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.Enemies
{
    public static class EnemyDatabase
    {
        private static List<EnemyData> all;
        private static EnemyData boss;

        public static IReadOnlyList<EnemyData> All
        {
            get { if (all == null) Build(); return all; }
        }

        public static EnemyData Boss
        {
            get { if (boss == null) Build(); return boss; }
        }

        public static void ResetCache()
        {
            all = null;
            boss = null;
        }

        public static EnemyData GetById(string id)
        {
            foreach (var e in All) if (e.enemyId == id) return e;
            if (boss != null && boss.enemyId == id) return boss;
            return null;
        }

        public static EnemyData GetRandom(System.Random rng)
        {
            var list = All;
            return list[rng.Next(list.Count)];
        }

        /// <summary>Returns non-boss enemies appropriate for the given area depth (0 = first area).</summary>
        public static List<EnemyData> GetForAreaDepth(int depth)
        {
            var list = new List<EnemyData>();
            foreach (var e in All)
            {
                if (depth >= MinimumDepthFor(e.enemyId)) list.Add(e);
            }
            return list.Count > 0 ? list : new List<EnemyData>(All);
        }

        /// <summary>How deep the run has to be before this enemy starts appearing.</summary>
        private static int MinimumDepthFor(string enemyId)
        {
            switch (enemyId)
            {
                case "pickle_swarm": return 0;
                case "onion_ring": return 0;
                case "fry_sniper": return 1;
                case "meat_tank": return 1;
                case "wing_flapper": return 2;
                default: return 0;
            }
        }

        private static void Build()
        {
            all = new List<EnemyData>
            {
                EnemyData.Create("onion_ring", "Onion Ring", new Color(0.9f, 0.75f, 0.3f), EnemyBehaviorType.Melee, 24f, 3f, 8f, 0.9f),
                EnemyData.Create("pickle_swarm", "Pickle Swarmer", new Color(0.4f, 0.8f, 0.2f), EnemyBehaviorType.Fast, 14f, 6f, 5f, 0.6f),
                EnemyData.Create("fry_sniper", "Fry Sniper", new Color(0.95f, 0.7f, 0.2f), EnemyBehaviorType.Ranged, 18f, 2.5f, 4f, 1.4f),
                EnemyData.Create("meat_tank", "Meat Tank", new Color(0.5f, 0.3f, 0.2f), EnemyBehaviorType.Heavy, 60f, 1.6f, 16f, 1.2f),
                EnemyData.Create("wing_flapper", "Wing Flapper", new Color(0.85f, 0.55f, 0.65f), EnemyBehaviorType.Flying, 20f, 4f, 7f, 1f),
            };

            all[2].projectileDamage = 6f;
            all[2].projectileSpeed = 9f;
            all[2].preferredRange = 7f;
            all[2].bodySize = 0.9f;

            all[3].bodySize = 1.4f;
            all[1].bodySize = 0.7f;
            all[4].bodySize = 0.85f;

            boss = EnemyData.Create("head_chef", "The Head Chef", new Color(0.85f, 0.15f, 0.15f), EnemyBehaviorType.Boss,
                380f, 2.6f, 14f, 0.5f);
            boss.bodySize = 2.2f;
            boss.detectionRange = 30f;
            boss.preferredRange = 8f;
            boss.projectileDamage = 10f;
            boss.projectileSpeed = 10f;
            boss.slamCooldown = 4.5f;
            boss.slamRadius = 4f;
            boss.slamDamage = 22f;
            boss.slamTelegraph = 0.8f;
            boss.spreadShotCount = 5;
        }
    }
}
