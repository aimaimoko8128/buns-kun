using UnityEngine;

namespace BunsKun.Buns
{
    /// <summary>
    /// Data-driven definition of a bun: the player's equippable "body" that determines
    /// ingredient capacity, mana characteristics, and movement/defense traits.
    /// Created in code by BunDatabase, the same way IngredientData is.
    /// </summary>
    [CreateAssetMenu(menuName = "BunsKun/Bun", fileName = "NewBun")]
    public class BunData : ScriptableObject
    {
        public string bunId;
        public string bunName;
        [TextArea] public string description;
        public Color color = Color.white;

        public int ingredientSlotCount = 5;
        public float maxMana = 100f;
        public float manaRegenPerSecond = 10f;

        [Tooltip("Multiplies base player move speed. >1 is faster.")]
        public float moveSpeedMultiplier = 1f;

        [Tooltip("Multiplies incoming damage. <1 means the bun takes less damage.")]
        public float damageTakenMultiplier = 1f;

        public static BunData Create(string id, string name, string desc, Color color,
            int slots, float mana, float manaRegen, float moveMult, float defenseMult)
        {
            var data = CreateInstance<BunData>();
            data.bunId = id;
            data.bunName = name;
            data.description = desc;
            data.color = color;
            data.ingredientSlotCount = slots;
            data.maxMana = mana;
            data.manaRegenPerSecond = manaRegen;
            data.moveSpeedMultiplier = moveMult;
            data.damageTakenMultiplier = defenseMult;
            data.name = "Bun_" + id;
            return data;
        }
    }
}
