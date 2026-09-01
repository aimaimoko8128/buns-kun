using System.Collections.Generic;
using UnityEngine;
using BunsKun.Rooms;
using BunsKun.Enemies;
using BunsKun.Pickups;
using BunsKun.Game;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Instantiates a validated LayerPlan into the scene: merged terrain, one Room object
    /// per room, the enemies/pickups/hazards the generator placed, and the lock barriers
    /// that hold a combat room's exits shut until it is cleared.
    ///
    /// All decisions were already made (and validated) by LayerGenerator; nothing here
    /// is random, so what you see is exactly what the seed described.
    /// </summary>
    public static class RoomBuilder
    {
        private const float PlayerBodyHalfHeight = 0.55f;

        public class BuiltLayer
        {
            public GameObject Root;
            public readonly Dictionary<RoomNode, Room> Rooms = new Dictionary<RoomNode, Room>();
            public Vector3 SpawnPosition;
            public Vector3 GoalPosition;
            public Room GoalRoom;
        }

        public static BuiltLayer Build(LayerPlan plan)
        {
            var built = new BuiltLayer();
            built.Root = new GameObject("Layer_" + plan.Depth + "_Seed" + plan.Seed);

            TerrainRenderer.Build(plan.Tiles, built.Root.transform);

            foreach (var kv in plan.Rooms)
            {
                RoomNode node = kv.Key;
                RoomPlan roomPlan = kv.Value;

                var roomGO = new GameObject($"Room_{node.Type}_{node.GridPos.x}_{node.GridPos.y}");
                roomGO.transform.SetParent(built.Root.transform, false);

                var room = roomGO.AddComponent<Room>();
                Vector2 center = plan.Tiles.TileCenterToWorld(
                    roomPlan.Bounds.xMin + roomPlan.Bounds.width / 2,
                    roomPlan.Bounds.yMin + roomPlan.Bounds.height / 2);
                room.Setup(node, roomPlan.RequiresClear, center);
                built.Rooms[node] = room;
            }

            BuildLockBarriers(plan, built);
            SpawnEnemies(plan, built);
            SpawnPickups(plan, built);
            SpawnHazards(plan, built);

            // Anything that demanded clearing but received no enemies opens immediately,
            // so a room can never lock the player in.
            foreach (var room in built.Rooms.Values) room.FinalizeSpawning();

            built.SpawnPosition = plan.Tiles.StandPositionToWorld(plan.SpawnTile, PlayerBodyHalfHeight);
            built.GoalPosition = plan.Tiles.StandPositionToWorld(plan.GoalTile, PlayerBodyHalfHeight);
            built.GoalRoom = built.Rooms[plan.GoalRoom];
            return built;
        }

        private static void BuildLockBarriers(LayerPlan plan, BuiltLayer built)
        {
            foreach (var door in plan.Doors)
            {
                if (!door.Parent.RequiresClearLock()) continue;
                if (!built.Rooms.TryGetValue(door.Parent, out Room parentRoom)) continue;

                RectInt rect = door.BarrierRect;
                float width = rect.width + 1f;
                float height = rect.height + 1f;
                Vector2 min = plan.Tiles.TileCenterToWorld(rect.xMin, rect.yMin) - new Vector2(0.5f, 0.5f);
                var center = new Vector3(min.x + width * 0.5f, min.y + height * 0.5f, 0f);

                var barrier = new GameObject("LockBarrier");
                barrier.transform.SetParent(parentRoom.transform, true);
                barrier.transform.position = center;

                var sr = barrier.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Square(new Color(0.75f, 0.25f, 0.25f, 0.85f));
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(width, height);
                sr.sortingOrder = 2;

                var col = barrier.AddComponent<BoxCollider2D>();
                col.size = new Vector2(width, height);

                parentRoom.AddLockBarrier(barrier);
            }
        }

        private static void SpawnEnemies(LayerPlan plan, BuiltLayer built)
        {
            foreach (var placement in plan.Enemies)
            {
                if (!built.Rooms.TryGetValue(placement.Room, out Room room)) continue;
                Vector3 pos = plan.Tiles.StandPositionToWorld(placement.Tile, 0.6f);
                var enemy = EnemySpawner.Spawn(placement.Data, pos, room.transform,
                    placement.HealthScale, placement.DamageScale);
                room.RegisterEnemy(enemy);
            }

            // The boss stands where the final room's goal is.
            if (plan.IsFinalLayer && built.Rooms.TryGetValue(plan.GoalRoom, out Room bossRoom))
            {
                Vector3 pos = plan.Tiles.StandPositionToWorld(plan.GoalTile, 1.2f);
                var boss = EnemySpawner.Spawn(EnemyDatabase.Boss, pos, bossRoom.transform, 1f, 1f);
                bossRoom.RegisterEnemy(boss);
            }
        }

        private static void SpawnPickups(LayerPlan plan, BuiltLayer built)
        {
            foreach (var placement in plan.Pickups)
            {
                if (!built.Rooms.TryGetValue(placement.Room, out Room room)) continue;
                Vector3 pos = plan.Tiles.StandPositionToWorld(placement.Tile, 0.7f);
                if (placement.Ingredient != null)
                {
                    PickupFactory.SpawnIngredientPickup(placement.Ingredient, pos, room.transform);
                }
                else if (placement.Bun != null)
                {
                    PickupFactory.SpawnBunPickup(placement.Bun, pos, room.transform);
                }
            }
        }

        private static void SpawnHazards(LayerPlan plan, BuiltLayer built)
        {
            foreach (var placement in plan.Hazards)
            {
                if (!built.Rooms.TryGetValue(placement.Room, out Room room)) continue;
                Vector3 pos = plan.Tiles.StandPositionToWorld(placement.Tile, 0.3f);

                var go = new GameObject("Hazard");
                go.transform.SetParent(room.transform, true);
                go.transform.position = pos;

                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(placement.Width, 0.6f);

                var hazard = go.AddComponent<HazardZone>();
                hazard.Configure(6f + plan.Depth * 2f, 0.7f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Triangle(new Color(0.9f, 0.25f, 0.2f));
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(placement.Width, 0.6f);
                sr.sortingOrder = 2;
            }
        }
    }
}
