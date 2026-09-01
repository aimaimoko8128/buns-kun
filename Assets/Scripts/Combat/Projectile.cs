using UnityEngine;

namespace BunsKun.Combat
{
    /// <summary>
    /// A simple physics-free projectile: travels in a straight line, deals damage to the
    /// first (or several, if piercing) opposing Health component it touches, then despawns.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Projectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private float damage;
        private int pierceRemaining;
        private float critChance;
        private Team team;
        private float maxLifetime;
        private float traveled;
        private float maxRange;

        private float spawnTime;

        public void Initialize(Vector2 dir, float moveSpeed, float dmg, int pierce, float critChanceValue,
            Team owningTeam, float range, float sizeMultiplier, Color color, float lifetime = 4f)
        {
            direction = dir.normalized;
            speed = moveSpeed;
            damage = dmg;
            pierceRemaining = pierce;
            critChance = critChanceValue;
            team = owningTeam;
            maxRange = range;
            maxLifetime = lifetime;
            spawnTime = Time.time;

            transform.right = direction;
            transform.localScale = Vector3.one * Mathf.Max(0.2f, sizeMultiplier);

            var sr = GetComponent<SpriteRenderer>();
            sr.sprite = BunsKun.Game.SpriteFactory.Circle(color);
            sr.sortingOrder = 5;

            var col = gameObject.GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;
            transform.position += (Vector3)(direction * step);
            traveled += step;

            if (traveled >= maxRange || Time.time - spawnTime >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.Team == team || health.IsDead) return;

            float finalDamage = damage;
            if (critChance > 0f && Random.value < critChance)
            {
                finalDamage *= 2f;
            }

            health.TakeDamage(finalDamage, gameObject);

            if (pierceRemaining > 0)
            {
                pierceRemaining--;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
