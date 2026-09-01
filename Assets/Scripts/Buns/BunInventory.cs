using System;
using System.Collections.Generic;
using UnityEngine;
using BunsKun.Ingredients;
using BunsKun.Player;

namespace BunsKun.Buns
{
    /// <summary>
    /// Tracks every bun the player has found this run and which one is currently equipped.
    /// Equipping is a deliberate action (done from the Tab inventory menu) rather than
    /// automatic on pickup, so switching buns stays a meaningful strategic choice.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(IngredientInventory))]
    public class BunInventory : MonoBehaviour
    {
        private readonly List<BunData> owned = new List<BunData>();
        public IReadOnlyList<BunData> Owned => owned;
        public BunData CurrentBun { get; private set; }

        public event Action OnChanged;

        private PlayerStats stats;
        private IngredientInventory ingredientInventory;
        private BunsKun.Player.PlayerCombat combat;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            ingredientInventory = GetComponent<IngredientInventory>();
            combat = GetComponent<BunsKun.Player.PlayerCombat>();
        }

        public void AddBun(BunData bun)
        {
            if (bun == null || owned.Contains(bun)) return;
            owned.Add(bun);
            OnChanged?.Invoke();
        }

        public void EquipBun(BunData bun)
        {
            if (bun == null || !owned.Contains(bun)) return;
            CurrentBun = bun;
            stats.SetBun(bun);
            ingredientInventory.ResizeSlots(bun.ingredientSlotCount);
            combat?.ClampManaToMax();
            OnChanged?.Invoke();
        }

        public void ResetForNewRun(BunData starterBun)
        {
            owned.Clear();
            owned.Add(starterBun);
            CurrentBun = starterBun;
            stats.SetBun(starterBun);
            ingredientInventory.ResizeSlots(starterBun.ingredientSlotCount);
            OnChanged?.Invoke();
        }
    }
}
