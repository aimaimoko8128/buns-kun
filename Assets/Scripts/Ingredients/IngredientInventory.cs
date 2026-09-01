using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.Ingredients
{
    /// <summary>
    /// Holds everything the player owns and has equipped for the current run.
    /// Once an ingredient type is picked up it is "owned" for the rest of the run and can be
    /// equipped into any number of slots (there is no per-copy scarcity - this keeps the
    /// sequencing puzzle about ORDER rather than inventory management of duplicates).
    /// </summary>
    public class IngredientInventory : MonoBehaviour
    {
        private readonly List<IngredientData> owned = new List<IngredientData>();

        /// <summary>Ordered attack sequence. Length always equals the current bun's slot count. Entries may be null (empty slot).</summary>
        private readonly List<IngredientData> equipped = new List<IngredientData>();

        private int nextActivationIndex = 0;

        public IReadOnlyList<IngredientData> Owned => owned;
        public IReadOnlyList<IngredientData> Equipped => equipped;

        public event Action OnInventoryChanged;

        public bool AddIngredient(IngredientData data)
        {
            if (data == null) return false;
            if (owned.Contains(data)) return false;
            owned.Add(data);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void ResizeSlots(int newSlotCount)
        {
            newSlotCount = Mathf.Max(0, newSlotCount);
            if (equipped.Count > newSlotCount)
            {
                equipped.RemoveRange(newSlotCount, equipped.Count - newSlotCount);
            }
            while (equipped.Count < newSlotCount)
            {
                equipped.Add(null);
            }
            nextActivationIndex = 0;
            OnInventoryChanged?.Invoke();
        }

        public bool EquipToSlot(int slotIndex, IngredientData data)
        {
            if (slotIndex < 0 || slotIndex >= equipped.Count) return false;
            equipped[slotIndex] = data;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void UnequipSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equipped.Count) return;
            equipped[slotIndex] = null;
            OnInventoryChanged?.Invoke();
        }

        public void SwapSlots(int a, int b)
        {
            if (a < 0 || a >= equipped.Count || b < 0 || b >= equipped.Count) return;
            (equipped[a], equipped[b]) = (equipped[b], equipped[a]);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>Index of the slot that will activate on the next left click, or -1 if nothing is equipped.</summary>
        public int PeekNextIndex()
        {
            if (equipped.Count == 0) return -1;
            for (int i = 0; i < equipped.Count; i++)
            {
                int idx = (nextActivationIndex + i) % equipped.Count;
                if (equipped[idx] != null) return idx;
            }
            return -1;
        }

        public IngredientData PeekNext()
        {
            int idx = PeekNextIndex();
            return idx < 0 ? null : equipped[idx];
        }

        /// <summary>Advances the sequence pointer past the given slot, ready for the following click.</summary>
        public void AdvancePast(int slotIndex)
        {
            if (equipped.Count == 0) return;
            nextActivationIndex = (slotIndex + 1) % equipped.Count;
        }

        /// <summary>Returns up to `count` upcoming equipped ingredients, in activation order, for the HUD preview.</summary>
        public List<IngredientData> PreviewUpcoming(int count)
        {
            var result = new List<IngredientData>();
            if (equipped.Count == 0) return result;
            int idx = nextActivationIndex;
            int safety = equipped.Count * 2;
            while (result.Count < count && safety-- > 0)
            {
                if (equipped[idx] != null) result.Add(equipped[idx]);
                idx = (idx + 1) % equipped.Count;
            }
            return result;
        }

        public void ClearForNewRun()
        {
            owned.Clear();
            equipped.Clear();
            nextActivationIndex = 0;
            OnInventoryChanged?.Invoke();
        }

        public void NotifyChanged() => OnInventoryChanged?.Invoke();
    }
}
