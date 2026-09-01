using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.Ingredients
{
    /// <summary>
    /// Central, code-defined roster of every ingredient in the game. Building the roster in
    /// code (rather than as individual .asset files) keeps content changes to a single file
    /// and avoids any risk of broken ScriptableObject references in the project.
    /// </summary>
    public static class IngredientDatabase
    {
        private static List<IngredientData> all;

        public static IReadOnlyList<IngredientData> All
        {
            get
            {
                if (all == null) Build();
                return all;
            }
        }

        public static IngredientData GetById(string id)
        {
            foreach (var ing in All)
            {
                if (ing.ingredientId == id) return ing;
            }
            return null;
        }

        public static IngredientData GetRandom(System.Random rng)
        {
            var list = All;
            return list[rng.Next(list.Count)];
        }

        private static void Build()
        {
            all = new List<IngredientData>();

            // --- Attack ingredients ---
            var tomato = IngredientData.Create("tomato", "Tomato", "Launches a fireball that explodes on contact.",
                new Color(1f, 0.35f, 0.2f), 12f, 0.35f);
            tomato.kind = IngredientKind.Attack;
            tomato.baseDamage = 14f;
            tomato.projectileSpeed = 14f;
            tomato.range = 12f;
            tomato.baseCritChance = 0.05f;
            all.Add(tomato);

            var lettuce = IngredientData.Create("lettuce", "Lettuce", "Fires a chilling ice shard that grazes through one extra foe.",
                new Color(0.4f, 0.9f, 0.55f), 10f, 0.4f);
            lettuce.kind = IngredientKind.Attack;
            lettuce.baseDamage = 9f;
            lettuce.projectileSpeed = 11f;
            lettuce.range = 10f;
            lettuce.basePierce = 1;
            all.Add(lettuce);

            var onion = IngredientData.Create("onion", "Onion", "A pungent, heavy burst that deals strong damage.",
                new Color(0.75f, 0.5f, 0.85f), 15f, 0.6f);
            onion.kind = IngredientKind.Attack;
            onion.baseDamage = 20f;
            onion.projectileSpeed = 9f;
            onion.range = 9f;
            all.Add(onion);

            var pickle = IngredientData.Create("pickle", "Pickle", "A quick, cheap flick of brine. Weak but almost instant.",
                new Color(0.55f, 0.8f, 0.2f), 6f, 0.18f);
            pickle.kind = IngredientKind.Attack;
            pickle.baseDamage = 6f;
            pickle.projectileSpeed = 18f;
            pickle.range = 9f;
            all.Add(pickle);

            var patty = IngredientData.Create("patty", "Patty", "A heavy, slow-cooking shot that punches through enemies.",
                new Color(0.45f, 0.28f, 0.15f), 18f, 0.9f);
            patty.kind = IngredientKind.Attack;
            patty.baseDamage = 30f;
            patty.projectileSpeed = 8f;
            patty.range = 11f;
            patty.basePierce = 2;
            all.Add(patty);

            // --- Buff ingredients (apply to the next attack) ---
            var cheese = IngredientData.Create("cheese", "Cheese", "Melts into the next attack, boosting its damage by 50%.",
                new Color(1f, 0.85f, 0.2f), 8f, 0.2f);
            cheese.kind = IngredientKind.Buff;
            cheese.nextAttackDamageBonus = 0.5f;
            cheese.buffChargeCount = 1;
            all.Add(cheese);

            var sesame = IngredientData.Create("sesame", "Sesame Bun Top", "Splits the next attack into two projectiles.",
                new Color(0.85f, 0.72f, 0.4f), 10f, 0.25f);
            sesame.kind = IngredientKind.Buff;
            sesame.nextAttackExtraProjectiles = 1;
            sesame.buffChargeCount = 1;
            all.Add(sesame);

            var bacon = IngredientData.Create("bacon", "Bacon", "Crisps the next attack so it pierces through 2 extra enemies.",
                new Color(0.65f, 0.2f, 0.15f), 8f, 0.25f);
            bacon.kind = IngredientKind.Buff;
            bacon.nextAttackPierceBonus = 2;
            bacon.buffChargeCount = 1;
            all.Add(bacon);

            var mustard = IngredientData.Create("mustard", "Mustard", "A sharp kick that speeds up and extends the next attack.",
                new Color(0.95f, 0.8f, 0.1f), 7f, 0.2f);
            mustard.kind = IngredientKind.Buff;
            mustard.nextAttackSpeedBonus = 0.5f;
            mustard.nextAttackRangeBonus = 0.5f;
            mustard.buffChargeCount = 1;
            all.Add(mustard);

            var ketchup = IngredientData.Create("ketchup", "Ketchup", "A tangy sauce that raises crit chance and lowers mana cost for the next 2 attacks.",
                new Color(0.85f, 0.1f, 0.15f), 6f, 0.2f);
            ketchup.kind = IngredientKind.Buff;
            ketchup.nextAttackCritBonus = 0.35f;
            ketchup.nextAttackManaCostReduction = 0.3f;
            ketchup.buffChargeCount = 2;
            all.Add(ketchup);
        }
    }
}
