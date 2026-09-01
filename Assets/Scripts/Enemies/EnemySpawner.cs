using UnityEngine;
using BunsKun.Combat;

namespace BunsKun.Enemies
{
    /// <summary>
    /// Builds a fully-wired enemy GameObject from an EnemyData at runtime - no prefab
    /// asset is needed, which avoids any risk of a broken prefab reference.
    /// </summary>
    public static class EnemySpawner
    {
        public static EnemyController Spawn(EnemyData data, Vector3 position, Transform parent, float hpScale = 1f, float dmgScale = 1f)
        {
            var go = new GameObject("Enemy_" + data.enemyId);
            go.transform.SetParent(parent, true);
            go.transform.position = position;

            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(go.transform, false);
            var sr = spriteChild.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;

            go.AddComponent<Rigidbody2D>();
            go.AddComponent<Health>();
            var controller = go.AddComponent<EnemyController>();
            controller.Initialize(data, hpScale, dmgScale);

            return controller;
        }
    }
}
