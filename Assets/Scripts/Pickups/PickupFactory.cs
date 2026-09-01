using UnityEngine;
using BunsKun.Game;
using BunsKun.Ingredients;
using BunsKun.Buns;

namespace BunsKun.Pickups
{
    /// <summary>Builds fully-wired pickup GameObjects at runtime (no prefab assets needed).</summary>
    public static class PickupFactory
    {
        public static GameObject SpawnIngredientPickup(IngredientData data, Vector3 position, Transform parent)
        {
            var go = new GameObject("Pickup_Ingredient_" + data.ingredientId);
            go.transform.SetParent(parent, true);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(data.color);
            sr.sortingOrder = 3;
            go.transform.localScale = Vector3.one * 0.6f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            go.AddComponent<IngredientPickup>().Configure(data);
            go.AddComponent<Bobber>();
            return go;
        }

        public static GameObject SpawnBunPickup(BunData data, Vector3 position, Transform parent)
        {
            var go = new GameObject("Pickup_Bun_" + data.bunId);
            go.transform.SetParent(parent, true);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(data.color, 0.4f);
            sr.sortingOrder = 3;
            go.transform.localScale = Vector3.one * 0.9f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.7f;

            go.AddComponent<BunPickup>().Configure(data);
            go.AddComponent<Bobber>();
            return go;
        }
    }
}
