using System;
using System.Collections.Generic;
using UnityEngine;
using BunsKun.Combat;
using BunsKun.Player;
using BunsKun.Ingredients;
using BunsKun.Buns;
using BunsKun.Rooms;
using BunsKun.ProceduralGeneration;
using BunsKun.Upgrades;

namespace BunsKun.Game
{
    public enum RunState
    {
        Playing,
        UpgradeChoice,
        GameOver,
        Victory
    }

    /// <summary>
    /// The top-level orchestrator for a single run: rolls the run seed, spawns the player,
    /// generates each layer of the descent from that seed, reacts to room clears, and
    /// handles the Game Over / Victory -> New Run cycle.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        /// <summary>Layers the dungeon descends through. The last one holds the boss.</summary>
        public const int TotalLayers = 5;
        public const int TotalAreas = TotalLayers;   // kept for older UI code

        private readonly int[] mainPathLengths = { 5, 5, 6, 6, 7 };
        private readonly int[] optionalBranchCounts = { 2, 2, 2, 1, 1 };

        /// <summary>One seed per run. Every layer's terrain, enemies, items and rewards
        /// derive from it, so entering the same seed replays the same descent.</summary>
        public int RunSeed { get; private set; }

        public GameObject CurrentPlayer { get; private set; }
        public Health PlayerHealth { get; private set; }
        public PlayerStats PlayerStats { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }
        public PlayerController PlayerController { get; private set; }
        public IngredientInventory Inventory { get; private set; }
        public BunInventory BunInventory { get; private set; }

        public int LayerDepth { get; private set; }
        public int AreaDepth => LayerDepth;          // kept for older UI code
        public RunState State { get; private set; } = RunState.GameOver;

        private LayerPlan currentPlan;
        private RoomBuilder.BuiltLayer currentLayer;
        private GameObject portalGO;
        private CameraFollow cameraFollow;

        public event Action OnRunStarted;
        public event Action<int> OnAreaChanged;
        public event Action<List<UpgradeData>> OnUpgradeChoiceOffered;
        public event Action OnGameOver;
        public event Action OnVictory;
        public event Action<Room> OnRoomCleared;

        public void Bind(CameraFollow follow)
        {
            cameraFollow = follow;
        }

        /// <summary>Starts a run on a fresh random seed.</summary>
        public void StartNewRun()
        {
            StartNewRun(new System.Random().Next(1, int.MaxValue));
        }

        /// <summary>Starts a run on a specific seed, replaying an earlier descent exactly.</summary>
        public void StartNewRun(int seed)
        {
            if (currentLayer != null) Destroy(currentLayer.Root);
            if (CurrentPlayer != null) Destroy(CurrentPlayer);

            Time.timeScale = 1f;
            RunSeed = seed;
            LayerDepth = 0;
            State = RunState.Playing;

            CurrentPlayer = PlayerFactory.Spawn(Vector3.zero);
            PlayerHealth = CurrentPlayer.GetComponent<Health>();
            PlayerStats = CurrentPlayer.GetComponent<PlayerStats>();
            PlayerCombat = CurrentPlayer.GetComponent<PlayerCombat>();
            PlayerController = CurrentPlayer.GetComponent<PlayerController>();
            Inventory = CurrentPlayer.GetComponent<IngredientInventory>();
            BunInventory = CurrentPlayer.GetComponent<BunInventory>();

            BunInventory.ResetForNewRun(BunDatabase.Starter);

            // A run always begins with one basic attack equipped, so the first locked
            // combat room is never a dead end.
            IngredientData starter = IngredientDatabase.Starter;
            Inventory.AddIngredient(starter);
            Inventory.EquipToSlot(0, starter);

            PlayerHealth.SetMaxHealth(PlayerStats.MaxHealth, true);
            PlayerHealth.OnDeath += HandleGameOver;

            if (cameraFollow != null) cameraFollow.Target = CurrentPlayer.transform;

            GenerateLayer(0);
            OnRunStarted?.Invoke();
        }

        private void GenerateLayer(int depth)
        {
            if (currentLayer != null) Destroy(currentLayer.Root);
            portalGO = null;

            LayerDepth = depth;
            bool isFinal = depth == TotalLayers - 1;
            int pathLength = mainPathLengths[Mathf.Clamp(depth, 0, mainPathLengths.Length - 1)];
            int branches = optionalBranchCounts[Mathf.Clamp(depth, 0, optionalBranchCounts.Length - 1)];

            // Each layer's seed is derived from the run seed, so the whole descent is
            // reproducible from that single number.
            int layerSeed = unchecked(RunSeed + depth * 104729);
            var profile = PlayerMovementProfile.FromPlayer(PlayerController, PlayerStats);

            currentPlan = LayerGenerator.Generate(layerSeed, depth, isFinal, pathLength, branches, profile);
            foreach (string warning in currentPlan.ValidationWarnings)
            {
                Debug.LogWarning($"[Layer {depth} seed {layerSeed}] {warning}");
            }

            currentLayer = RoomBuilder.Build(currentPlan);

            foreach (var room in currentLayer.Rooms.Values)
            {
                if (room.RequiresClear) room.OnCombatCleared += HandleRoomCleared;
            }

            if (!isFinal)
            {
                portalGO = CreatePortal(currentLayer.GoalPosition);
            }

            if (CurrentPlayer != null)
            {
                CurrentPlayer.transform.position = currentLayer.SpawnPosition;
                var rb = CurrentPlayer.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                PlayerController?.RefillJetpack();
            }
            if (cameraFollow != null && CurrentPlayer != null)
            {
                cameraFollow.SnapTo(CurrentPlayer.transform.position);
            }

            OnAreaChanged?.Invoke(depth);
        }

        private GameObject CreatePortal(Vector3 position)
        {
            var go = new GameObject("LayerExitPortal");
            // Parented to the layer so it is destroyed with it - an orphaned portal would
            // still be standing in the next layer and could be walked into again.
            if (currentLayer != null) go.transform.SetParent(currentLayer.Root.transform, true);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.4f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(new Color(0.4f, 0.9f, 1f, 0.85f));
            sr.sortingOrder = 3;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.7f;

            var trigger = go.AddComponent<AreaExitTrigger>();
            trigger.OnEntered += AdvanceToNextLayer;
            go.SetActive(false);
            return go;
        }

        private void Update()
        {
            if (State != RunState.Playing || CurrentPlayer == null || currentPlan == null) return;

            // Safety net: if the player ever ends up under the world, put them back on the
            // layer's spawn instead of falling forever.
            float floor = currentPlan.Tiles.WorldOrigin.y - 12f;
            if (CurrentPlayer.transform.position.y < floor)
            {
                CurrentPlayer.transform.position = currentLayer.SpawnPosition;
                var rb = CurrentPlayer.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                PlayerHealth?.TakeDamage(10f, gameObject);
                cameraFollow?.SnapTo(CurrentPlayer.transform.position);
            }
        }

        private void HandleRoomCleared(Room room)
        {
            OnRoomCleared?.Invoke(room);

            if (currentLayer != null && room == currentLayer.GoalRoom && portalGO != null)
            {
                portalGO.SetActive(true);
            }

            if (room.Node.Type == RoomType.Boss)
            {
                HandleVictory();
                return;
            }

            OfferUpgradeChoice(room);
        }

        private void OfferUpgradeChoice(Room room)
        {
            // The three cards were rolled during generation, so rewards follow the seed too.
            List<UpgradeData> choices = null;
            if (currentPlan != null && room.Node != null)
            {
                currentPlan.RoomRewards.TryGetValue(room.Node, out choices);
            }
            if (choices == null || choices.Count == 0)
            {
                choices = UpgradeDatabase.RollChoices(new System.Random(RunSeed + LayerDepth), 3);
            }

            State = RunState.UpgradeChoice;
            Time.timeScale = 0f;
            OnUpgradeChoiceOffered?.Invoke(choices);
        }

        public void ApplyUpgradeChoice(UpgradeData data)
        {
            if (State != RunState.UpgradeChoice) return;

            float before = PlayerStats.MaxHealth;
            PlayerStats.Upgrades.Apply(data);
            float after = PlayerStats.MaxHealth;
            PlayerHealth.SetMaxHealth(after, healToFull: false);
            if (after > before) PlayerHealth.Heal(after - before);

            State = RunState.Playing;
            Time.timeScale = 1f;
        }

        private void AdvanceToNextLayer()
        {
            if (State != RunState.Playing) return;
            GenerateLayer(LayerDepth + 1);
        }

        private void HandleGameOver()
        {
            if (State == RunState.GameOver || State == RunState.Victory) return;
            State = RunState.GameOver;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }

        private void HandleVictory()
        {
            State = RunState.Victory;
            Time.timeScale = 0f;
            OnVictory?.Invoke();
        }
    }
}
