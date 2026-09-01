using System;
using UnityEngine;

namespace BunsKun.Combat
{
    /// <summary>
    /// Generic health component shared by the player and enemies.
    /// Handles damage, death, and a brief invulnerability window after being hit.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private Team team = Team.Enemy;
        [SerializeField] private float invulnerabilityDuration = 0.1f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public Team Team => team;
        public bool IsDead { get; private set; }

        /// <summary>Raised whenever damage is applied. Passes the amount and the new current health.</summary>
        public event Action<float, float> OnDamaged;

        /// <summary>Raised once when health reaches zero.</summary>
        public event Action OnDeath;

        private float invulnerableUntil = -1f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void SetMaxHealth(float value, bool healToFull = true)
        {
            maxHealth = Mathf.Max(1f, value);
            if (healToFull)
            {
                CurrentHealth = maxHealth;
            }
            else
            {
                CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
            }
        }

        public void SetTeam(Team newTeam)
        {
            team = newTeam;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (IsDead || amount <= 0f) return;
            if (Time.time < invulnerableUntil) return;

            CurrentHealth -= amount;
            invulnerableUntil = Time.time + invulnerabilityDuration;
            OnDamaged?.Invoke(amount, CurrentHealth);

            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        public float HealthFraction => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);
    }
}
