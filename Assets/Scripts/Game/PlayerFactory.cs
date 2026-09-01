using UnityEngine;
using BunsKun.Combat;
using BunsKun.Player;
using BunsKun.Ingredients;
using BunsKun.Buns;

namespace BunsKun.Game
{
    /// <summary>Builds the fully-wired player GameObject (the hamburger) at runtime.</summary>
    public static class PlayerFactory
    {
        public static GameObject Spawn(Vector3 position)
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.transform.position = position;

            // Body (bun-colored square torso)
            var bodySprite = new GameObject("Sprite");
            bodySprite.transform.SetParent(go.transform, false);
            var sr = bodySprite.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square(new Color(0.87f, 0.68f, 0.4f), 0.4f);
            sr.sortingOrder = 6;
            bodySprite.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            // Simple eyes for character/readability.
            CreateEye(bodySprite.transform, new Vector3(-0.22f, 0.15f, 0f));
            CreateEye(bodySprite.transform, new Vector3(0.22f, 0.15f, 0f));

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 1.1f);
            col.direction = CapsuleDirection2D.Vertical;

            go.AddComponent<Health>();
            go.AddComponent<PlayerStats>();
            go.AddComponent<IngredientInventory>();
            go.AddComponent<BunInventory>();
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerCombat>();

            return go;
        }

        private static void CreateEye(Transform parent, Vector3 localPos)
        {
            var eye = new GameObject("Eye");
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * 0.18f;
            var sr = eye.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(Color.black);
            sr.sortingOrder = 7;
        }
    }
}
