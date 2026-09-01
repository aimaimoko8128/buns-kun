using UnityEngine;
using BunsKun.Combat;
using BunsKun.Buns;
using BunsKun.Player;
using BunsKun.Ingredients;

namespace BunsKun.Pickups
{
    public class BunPickup : MonoBehaviour
    {
        public BunData Data { get; private set; }

        public void Configure(BunData data)
        {
            Data = data;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player) return;

            var root = health.transform;
            var stats = root.GetComponent<PlayerStats>();
            var inventory = root.GetComponent<IngredientInventory>();
            var combat = root.GetComponent<PlayerCombat>();
            if (stats != null && inventory != null)
            {
                stats.SetBun(Data);
                inventory.ResizeSlots(Data.ingredientSlotCount);
                combat?.ClampManaToMax();
            }
            Destroy(gameObject);
        }
    }
}
