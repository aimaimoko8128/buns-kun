using UnityEngine;
using BunsKun.Combat;
using BunsKun.Buns;

namespace BunsKun.Pickups
{
    /// <summary>Adds the discovered bun to the player's owned buns. Equipping it is a
    /// deliberate choice made afterwards from the Tab inventory menu.</summary>
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

            var bunInventory = health.GetComponent<BunInventory>();
            bunInventory?.AddBun(Data);
            Destroy(gameObject);
        }
    }
}
