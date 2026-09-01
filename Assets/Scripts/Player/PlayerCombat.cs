using System;
using UnityEngine;
using BunsKun.Combat;
using BunsKun.Ingredients;
using BunsKun.Game;

namespace BunsKun.Player
{
    /// <summary>
    /// Drives the ingredient activation sequence: reads left-click input, spends mana,
    /// applies the activation delay, and either fires a damaging attack or accumulates
    /// a buff onto AttackModifiers for the next damaging attack(s) in the sequence.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(IngredientInventory))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private float muzzleForwardOffset = 0.7f;
        [SerializeField] private float muzzleHeightOffset = 0.1f;
        [SerializeField] private float multiShotSpreadDegrees = 12f;

        private PlayerStats stats;
        private IngredientInventory inventory;
        private PlayerController controller;
        private readonly AttackModifiers modifiers = new AttackModifiers();

        public float CurrentMana { get; private set; }
        public float DelayTimer { get; private set; }
        public AttackModifiers PendingModifiers => modifiers;

        /// <summary>success, ingredient activated (or null), reason if it failed.</summary>
        public event Action<bool, IngredientData, string> OnActivationAttempted;
        public event Action OnManaChanged;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            inventory = GetComponent<IngredientInventory>();
            controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            CurrentMana = stats.MaxMana;
        }

        public void ClampManaToMax()
        {
            CurrentMana = Mathf.Min(CurrentMana, stats.MaxMana);
            OnManaChanged?.Invoke();
        }

        public void ResetForNewRun()
        {
            modifiers.Reset();
            CurrentMana = stats.MaxMana;
            DelayTimer = 0f;
            OnManaChanged?.Invoke();
        }

        private void Update()
        {
            if (DelayTimer > 0f) DelayTimer -= Time.deltaTime;

            float before = CurrentMana;
            CurrentMana = Mathf.Min(stats.MaxMana, CurrentMana + stats.ManaRegenPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(before, CurrentMana)) OnManaChanged?.Invoke();

            if (Input.GetMouseButtonDown(0))
            {
                TryActivateNext();
            }
        }

        private void TryActivateNext()
        {
            if (DelayTimer > 0f)
            {
                OnActivationAttempted?.Invoke(false, null, "cooling down");
                return;
            }

            int slotIndex = inventory.PeekNextIndex();
            if (slotIndex < 0)
            {
                OnActivationAttempted?.Invoke(false, null, "no ingredients equipped");
                return;
            }

            IngredientData data = inventory.Equipped[slotIndex];
            float cost = data.manaCost * modifiers.ManaCostMultiplier;
            if (CurrentMana < cost)
            {
                OnActivationAttempted?.Invoke(false, data, "not enough mana");
                return;
            }

            CurrentMana -= cost;
            OnManaChanged?.Invoke();

            if (data.kind == IngredientKind.Attack)
            {
                FireAttack(data);
                modifiers.ConsumeCharge();
            }
            else
            {
                modifiers.Accumulate(data);
            }

            DelayTimer = data.activationDelay * stats.DelayMultiplier * modifiers.DelayMultiplier;
            inventory.AdvancePast(slotIndex);
            OnActivationAttempted?.Invoke(true, data, null);
        }

        private void FireAttack(IngredientData data)
        {
            bool facingRight = controller == null || controller.FacingRight;
            Vector2 baseDir = facingRight ? Vector2.right : Vector2.left;
            Vector3 origin = transform.position + new Vector3(muzzleForwardOffset * (facingRight ? 1f : -1f), muzzleHeightOffset, 0f);

            float damage = data.baseDamage * stats.DamageMultiplier * modifiers.DamageMultiplier;
            int pierce = data.basePierce + modifiers.PierceCount;
            float crit = Mathf.Clamp01(data.baseCritChance + stats.CritChanceBonus + modifiers.CritChanceBonus);
            float speed = data.projectileSpeed * stats.ProjectileSpeedMultiplier * modifiers.ProjectileSpeedMultiplier;
            float range = data.range * modifiers.RangeMultiplier;
            float size = modifiers.SizeMultiplier * stats.ProjectileSizeMultiplier;
            int shotCount = 1 + Mathf.Max(0, modifiers.ExtraProjectiles);

            for (int i = 0; i < shotCount; i++)
            {
                float angleOffset = 0f;
                if (shotCount > 1)
                {
                    float t = shotCount == 1 ? 0f : (i / (float)(shotCount - 1)) - 0.5f;
                    angleOffset = t * multiShotSpreadDegrees;
                }
                Vector2 dir = Quaternion.Euler(0, 0, angleOffset) * baseDir;

                var go = new GameObject("Projectile_" + data.ingredientId);
                go.transform.position = origin;
                var projectile = go.AddComponent<Projectile>();
                projectile.Initialize(dir, speed, damage, pierce, crit, Team.Player, range, size, data.color);
            }
        }
    }
}
