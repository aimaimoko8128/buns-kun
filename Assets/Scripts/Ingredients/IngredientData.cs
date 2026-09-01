using UnityEngine;

namespace BunsKun.Ingredients
{
    /// <summary>
    /// Data-driven definition of a single ingredient. Instances are created in code by
    /// IngredientDatabase rather than as .asset files, so the ingredient roster can be
    /// extended purely by editing the database - no editor authoring required.
    /// </summary>
    [CreateAssetMenu(menuName = "BunsKun/Ingredient", fileName = "NewIngredient")]
    public class IngredientData : ScriptableObject
    {
        public string ingredientId;
        public string ingredientName;
        [TextArea] public string description;
        public Color color = Color.white;
        public float manaCost = 10f;
        public float activationDelay = 0.4f;
        public IngredientKind kind = IngredientKind.Attack;

        [Tooltip("The weak ingredient every run starts equipped with. Never appears as a pickup.")]
        public bool isStarter = false;

        [Header("Attack (used when kind == Attack)")]
        public float baseDamage = 10f;
        public float projectileSpeed = 12f;
        public float range = 10f;
        public int basePierce = 0;
        public float baseCritChance = 0.05f;

        [Header("Buff (used when kind == Buff, applies to next attack)")]
        public float nextAttackDamageBonus = 0f;
        public int nextAttackExtraProjectiles = 0;
        public int nextAttackPierceBonus = 0;
        public float nextAttackRangeBonus = 0f;
        public float nextAttackSizeBonus = 0f;
        public float nextAttackSpeedBonus = 0f;
        public float nextAttackCritBonus = 0f;
        public float nextAttackManaCostReduction = 0f;
        public int buffChargeCount = 1;

        public static IngredientData Create(string id, string name, string desc, Color color, float mana, float delay)
        {
            var data = CreateInstance<IngredientData>();
            data.ingredientId = id;
            data.ingredientName = name;
            data.description = desc;
            data.color = color;
            data.manaCost = mana;
            data.activationDelay = delay;
            data.name = "Ingredient_" + id;
            return data;
        }
    }
}
