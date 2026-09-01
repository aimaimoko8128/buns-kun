using UnityEngine;

namespace BunsKun.Combat
{
    /// <summary>
    /// Accumulated bonuses applied by "buff" ingredients (e.g. Cheese) to the next
    /// damaging attack(s) in the ingredient sequence. This is the only way ingredients
    /// interact with each other - there is no elemental combination system.
    /// </summary>
    public class AttackModifiers
    {
        public float DamageMultiplier = 1f;
        public int ExtraProjectiles = 0;
        public int PierceCount = 0;
        public float RangeMultiplier = 1f;
        public float SizeMultiplier = 1f;
        public float ProjectileSpeedMultiplier = 1f;
        public float CritChanceBonus = 0f;
        public float ManaCostMultiplier = 1f;
        public float DelayMultiplier = 1f;

        /// <summary>How many upcoming damaging attacks this buff still applies to.</summary>
        private int chargesRemaining = 0;

        public bool HasCharges => chargesRemaining > 0;

        public void AddCharges(int amount)
        {
            chargesRemaining += amount;
        }

        /// <summary>Merges a buff ingredient's bonuses into the pending modifier stack.</summary>
        public void Accumulate(BunsKun.Ingredients.IngredientData data)
        {
            DamageMultiplier += data.nextAttackDamageBonus;
            ExtraProjectiles += data.nextAttackExtraProjectiles;
            PierceCount += data.nextAttackPierceBonus;
            RangeMultiplier += data.nextAttackRangeBonus;
            SizeMultiplier += data.nextAttackSizeBonus;
            ProjectileSpeedMultiplier += data.nextAttackSpeedBonus;
            CritChanceBonus += data.nextAttackCritBonus;
            ManaCostMultiplier -= data.nextAttackManaCostReduction;
            ManaCostMultiplier = Mathf.Max(0.1f, ManaCostMultiplier);
            AddCharges(Mathf.Max(1, data.buffChargeCount));
        }

        /// <summary>Called after a damaging attack consumes one charge of the pending buffs.</summary>
        public void ConsumeCharge()
        {
            chargesRemaining = Mathf.Max(0, chargesRemaining - 1);
            if (chargesRemaining == 0)
            {
                Reset();
            }
        }

        public void Reset()
        {
            DamageMultiplier = 1f;
            ExtraProjectiles = 0;
            PierceCount = 0;
            RangeMultiplier = 1f;
            SizeMultiplier = 1f;
            ProjectileSpeedMultiplier = 1f;
            CritChanceBonus = 0f;
            ManaCostMultiplier = 1f;
            DelayMultiplier = 1f;
            chargesRemaining = 0;
        }
    }
}
