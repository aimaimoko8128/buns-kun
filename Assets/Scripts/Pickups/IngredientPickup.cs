using UnityEngine;
using BunsKun.Combat;
using BunsKun.Ingredients;

namespace BunsKun.Pickups
{
    public class IngredientPickup : MonoBehaviour
    {
        public IngredientData Data { get; private set; }

        public void Configure(IngredientData data)
        {
            Data = data;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player) return;

            var inventory = other.GetComponentInParent<IngredientInventory>();
            if (inventory != null)
            {
                inventory.AddIngredient(Data);
            }
            Destroy(gameObject);
        }
    }
}
