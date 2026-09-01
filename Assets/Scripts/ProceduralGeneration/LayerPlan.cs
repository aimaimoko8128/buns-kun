using System.Collections.Generic;
using UnityEngine;
using BunsKun.Rooms;
using BunsKun.Enemies;
using BunsKun.Ingredients;
using BunsKun.Buns;
using BunsKun.Upgrades;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>Where one room sits in tile space, plus the points the generator reasons about.</summary>
    public class RoomPlan
    {
        public RoomNode Node;
        public RectInt Bounds;       // full room including its 1-tile wall ring
        public RectInt Interior;     // carvable area
        public Vector2Int Hub;       // the room's central clearing

        public bool RequiresClear => Node.RequiresClearLock();
    }

    /// <summary>One carved connection between two rooms.</summary>
    public class DoorPlan
    {
        public RoomNode Parent;
        public RoomNode Child;
        public Direction DirFromParent;

        /// <summary>Where a lock barrier is placed while the parent room is uncleared.</summary>
        public RectInt BarrierRect;

        public Vector2Int ParentAnchor;
        public Vector2Int ChildAnchor;
    }

    public class EnemyPlacement
    {
        public RoomNode Room;
        public EnemyData Data;
        public Vector2Int Tile;
        public float HealthScale = 1f;
        public float DamageScale = 1f;
    }

    public class PickupPlacement
    {
        public RoomNode Room;
        public IngredientData Ingredient;   // one of these two is set
        public BunData Bun;
        public Vector2Int Tile;
    }

    public class HazardPlacement
    {
        public RoomNode Room;
        public Vector2Int Tile;
        public int Width;
    }

    /// <summary>
    /// The complete, validated description of one layer: terrain plus everything that
    /// should be spawned into it. RoomBuilder turns this into GameObjects; nothing in
    /// here touches the scene, which keeps generation testable and deterministic.
    /// </summary>
    public class LayerPlan
    {
        public int Seed;
        public int Depth;
        public bool IsFinalLayer;

        public TileMap Tiles;
        public MapGenerator.GeneratedArea Graph;
        public readonly Dictionary<RoomNode, RoomPlan> Rooms = new Dictionary<RoomNode, RoomPlan>();
        public readonly List<DoorPlan> Doors = new List<DoorPlan>();

        public Vector2Int SpawnTile;
        public Vector2Int GoalTile;          // area exit portal, or the boss's stand
        public RoomNode GoalRoom;

        public readonly List<EnemyPlacement> Enemies = new List<EnemyPlacement>();
        public readonly List<PickupPlacement> Pickups = new List<PickupPlacement>();
        public readonly List<HazardPlacement> Hazards = new List<HazardPlacement>();

        /// <summary>Room-clear rewards, rolled during generation so they follow the seed.</summary>
        public readonly Dictionary<RoomNode, List<UpgradeData>> RoomRewards =
            new Dictionary<RoomNode, List<UpgradeData>>();

        public int RepairCount;
        public readonly List<string> ValidationWarnings = new List<string>();
    }
}
