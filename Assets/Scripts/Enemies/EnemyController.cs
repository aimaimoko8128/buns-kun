using UnityEngine;
using BunsKun.Combat;
using BunsKun.Game;

namespace BunsKun.Enemies
{
    /// <summary>
    /// Drives a single enemy's movement and attacks based on its EnemyData.behavior.
    /// One data-driven class covers every archetype - including the boss, via the Boss
    /// behavior case - so new enemy types can be added purely through EnemyDatabase.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        private static Transform cachedPlayer;

        private EnemyData data;
        private Rigidbody2D rb;
        private Health health;
        private SpriteRenderer spriteRenderer;

        private float attackTimer;
        private float slamTimer;
        private float slamTelegraphRemaining = -1f;
        private float hpMultiplier = 1f;
        private float dmgMultiplier = 1f;

        public bool IsBoss => data != null && data.behavior == EnemyBehaviorType.Boss;
        public event System.Action<EnemyController> OnDied;

        public void Initialize(EnemyData enemyData, float hpScale = 1f, float dmgScale = 1f)
        {
            data = enemyData;
            hpMultiplier = hpScale;
            dmgMultiplier = dmgScale;

            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = data.behavior == EnemyBehaviorType.Flying ? 0f : 4.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            health = GetComponent<Health>();
            health.SetTeam(Team.Enemy);
            health.SetMaxHealth(data.maxHealth * hpMultiplier);
            health.OnDeath += HandleDeath;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = SpriteFactory.Square(data.color, 0.15f);
            }
            transform.localScale = Vector3.one * data.bodySize;

            var box = GetComponent<BoxCollider2D>();
            if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
            box.size = Vector2.one * 0.95f;

            slamTimer = data.slamCooldown;
        }

        /// <summary>Clears the cached player reference. See SpriteFactory.ResetCache for why
        /// this must run once at the start of every Play session.</summary>
        public static void ResetCachedPlayer()
        {
            cachedPlayer = null;
        }

        private static Transform FindPlayer()
        {
            if (cachedPlayer == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) cachedPlayer = go.transform;
            }
            return cachedPlayer;
        }

        private void Update()
        {
            if (health == null || health.IsDead || data == null) return;
            if (attackTimer > 0f) attackTimer -= Time.deltaTime;

            Transform player = FindPlayer();
            if (player == null) return;

            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;

            if (data.behavior == EnemyBehaviorType.Boss)
            {
                HandleBoss(toPlayer, distance, player);
                return;
            }

            if (distance > data.detectionRange) return;

            switch (data.behavior)
            {
                case EnemyBehaviorType.Melee:
                case EnemyBehaviorType.Fast:
                case EnemyBehaviorType.Heavy:
                    MoveToward(toPlayer, distance, 0.8f);
                    break;
                case EnemyBehaviorType.Flying:
                    MoveTowardFree(toPlayer, distance);
                    break;
                case EnemyBehaviorType.Ranged:
                    HandleRanged(toPlayer, distance);
                    break;
            }
        }

        private void MoveToward(Vector2 toPlayer, float distance, float stopDistance)
        {
            float dirX = distance > stopDistance ? Mathf.Sign(toPlayer.x) : 0f;
            rb.linearVelocity = new Vector2(dirX * data.moveSpeed, rb.linearVelocity.y);
            FaceDirection(toPlayer.x);
        }

        private void MoveTowardFree(Vector2 toPlayer, float distance)
        {
            Vector2 dir = distance > 0.6f ? toPlayer.normalized : Vector2.zero;
            rb.linearVelocity = dir * data.moveSpeed;
            FaceDirection(toPlayer.x);
        }

        private void HandleRanged(Vector2 toPlayer, float distance)
        {
            float dirX = 0f;
            if (distance > data.preferredRange + 1f) dirX = Mathf.Sign(toPlayer.x);
            else if (distance < data.preferredRange - 1f) dirX = -Mathf.Sign(toPlayer.x);
            rb.linearVelocity = new Vector2(dirX * data.moveSpeed, rb.linearVelocity.y);
            FaceDirection(toPlayer.x);

            if (attackTimer <= 0f && distance <= data.detectionRange)
            {
                FireProjectile(toPlayer.normalized);
                attackTimer = data.attackCooldown;
            }
        }

        private void HandleBoss(Vector2 toPlayer, float distance, Transform player)
        {
            if (slamTelegraphRemaining > 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                slamTelegraphRemaining -= Time.deltaTime;
                if (slamTelegraphRemaining <= 0f)
                {
                    ResolveSlam(player);
                }
                return;
            }

            if (slamTimer > 0f) slamTimer -= Time.deltaTime;

            float dirX = 0f;
            if (distance > data.preferredRange + 1.5f) dirX = Mathf.Sign(toPlayer.x);
            else if (distance < data.preferredRange - 1.5f) dirX = -Mathf.Sign(toPlayer.x);
            rb.linearVelocity = new Vector2(dirX * data.moveSpeed, rb.linearVelocity.y);
            FaceDirection(toPlayer.x);

            if (slamTimer <= 0f && distance <= data.slamRadius + 2f)
            {
                slamTelegraphRemaining = data.slamTelegraph;
                slamTimer = data.slamCooldown;
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                return;
            }

            if (attackTimer <= 0f && distance <= data.detectionRange)
            {
                FireSpread(toPlayer.normalized);
                attackTimer = data.attackCooldown;
            }
        }

        private void ResolveSlam(Transform player)
        {
            if (spriteRenderer != null) spriteRenderer.color = data.color;
            float distance = Vector2.Distance(player.position, transform.position);
            if (distance <= data.slamRadius)
            {
                var playerHealth = player.GetComponentInParent<Health>();
                playerHealth?.TakeDamage(data.slamDamage * dmgMultiplier, gameObject);
            }
        }

        private void FireSpread(Vector2 baseDir)
        {
            int count = Mathf.Max(1, data.spreadShotCount);
            float totalSpread = 40f;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1)) - 0.5f;
                Vector2 dir = Quaternion.Euler(0, 0, t * totalSpread) * baseDir;
                var go = new GameObject("BossProjectile");
                go.transform.position = transform.position;
                var projectile = go.AddComponent<Projectile>();
                projectile.Initialize(dir, data.projectileSpeed, data.projectileDamage * dmgMultiplier, 0, 0.1f, Team.Enemy, 16f, 1.2f, data.color);
            }
        }

        private void FaceDirection(float x)
        {
            if (Mathf.Abs(x) < 0.05f) return;
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            transform.localScale = new Vector3(x > 0 ? absX : -absX, scale.y, scale.z);
        }

        private void FireProjectile(Vector2 direction)
        {
            var go = new GameObject("EnemyProjectile_" + data.enemyId);
            go.transform.position = transform.position;
            var projectile = go.AddComponent<Projectile>();
            projectile.Initialize(direction, data.projectileSpeed, data.projectileDamage * dmgMultiplier, 0, 0f,
                Team.Enemy, 14f, 1f, data.color);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (attackTimer > 0f) return;
            var health2 = collision.collider.GetComponentInParent<Health>();
            if (health2 == null || health2.Team != Team.Player) return;

            health2.TakeDamage(data.contactDamage * dmgMultiplier, gameObject);
            attackTimer = data.attackCooldown;
        }

        private void HandleDeath()
        {
            OnDied?.Invoke(this);
            if (rb != null) rb.linearVelocity = Vector2.zero;
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            enabled = false;
            Destroy(gameObject, 0.15f);
        }
    }
}
