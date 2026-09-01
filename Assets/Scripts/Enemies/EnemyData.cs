using UnityEngine;

namespace BunsKun.Enemies
{
    [CreateAssetMenu(menuName = "BunsKun/Enemy", fileName = "NewEnemy")]
    public class EnemyData : ScriptableObject
    {
        public string enemyId;
        public string enemyName;
        public Color color = Color.red;
        public EnemyBehaviorType behavior = EnemyBehaviorType.Melee;

        public float maxHealth = 30f;
        public float moveSpeed = 3f;
        public float contactDamage = 8f;
        public float attackCooldown = 1f;
        public float detectionRange = 12f;
        public float preferredRange = 6f; // ranged enemies try to stay near this distance
        public float bodySize = 1f;

        [Header("Ranged only")]
        public float projectileDamage = 8f;
        public float projectileSpeed = 8f;

        [Header("Boss only")]
        public float slamCooldown = 4f;
        public float slamRadius = 3.5f;
        public float slamDamage = 18f;
        public float slamTelegraph = 0.7f;
        public int spreadShotCount = 3;

        public static EnemyData Create(string id, string name, Color color, EnemyBehaviorType behavior,
            float hp, float speed, float damage, float cooldown)
        {
            var d = CreateInstance<EnemyData>();
            d.enemyId = id;
            d.enemyName = name;
            d.color = color;
            d.behavior = behavior;
            d.maxHealth = hp;
            d.moveSpeed = speed;
            d.contactDamage = damage;
            d.attackCooldown = cooldown;
            d.name = "Enemy_" + id;
            return d;
        }
    }
}
