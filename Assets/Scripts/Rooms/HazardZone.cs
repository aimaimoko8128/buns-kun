using UnityEngine;
using BunsKun.Combat;

namespace BunsKun.Rooms
{
    /// <summary>Simple damage-over-time trap zone (spikes, hazard pits) that hurts the player on contact.</summary>
    public class HazardZone : MonoBehaviour
    {
        [SerializeField] private float damage = 8f;
        [SerializeField] private float tickCooldown = 0.6f;
        private float timer;

        public void Configure(float dmg, float cooldown)
        {
            damage = dmg;
            tickCooldown = cooldown;
        }

        private void Update()
        {
            if (timer > 0f) timer -= Time.deltaTime;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (timer > 0f) return;
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player) return;
            health.TakeDamage(damage, gameObject);
            timer = tickCooldown;
        }
    }
}
