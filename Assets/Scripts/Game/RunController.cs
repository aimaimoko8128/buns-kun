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
    /// The top-level orchestrator for a single run: spawns the player, generates each
    /// area in sequence, reacts to room clears (offering upgrades / unlocking the area
    /// exit), and handles the Game Over / Victory -> New Run cycle.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        public const int TotalAreas = 3;
        private readonly int[] mainPathLengths = { 5, 6, 6 };
        private readonly int[] optionalBranchCounts = { 2, 2, 1 };

        public GameObject CurrentPlayer { get; private set; }
        public Health PlayerHealth { get; private set; }
        public PlayerStats PlayerStats { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }
        public IngredientInventory Inventory { get; private set; }
        public BunInventory BunInventory { get; private set; }

        public int AreaDepth { get; private set; }
        public RunState State { get; private set; } = RunState.GameOver;

        private RoomBuilder.BuiltArea currentBuiltArea;
        private Room endRoomRef;
        private GameObject portalGO;
        private CameraFollow cameraFollow;
        private System.Random rng;

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

        public void StartNewRun()
        {
            if (currentBuiltArea != null) Destroy(currentBuiltArea.Root);
            if (CurrentPlayer != null) Destroy(CurrentPlayer);

            Time.timeScale = 1f;
            AreaDepth = 0;
            State = RunState.Playing;

            CurrentPlayer = PlayerFactory.Spawn(Vector3.zero);
            PlayerHealth = CurrentPlayer.GetComponent<Health>();
            PlayerStats = CurrentPlayer.GetComponent<PlayerStats>();
            PlayerCombat = CurrentPlayer.GetComponent<PlayerCombat>();
            Inventory = CurrentPlayer.GetComponent<IngredientInventory>();
            BunInventory = CurrentPlayer.GetComponent<BunInventory>();

            BunInventory.ResetForNewRun(BunDatabase.Starter);
            PlayerHealth.SetMaxHealth(PlayerStats.MaxHealth, true);
            PlayerHealth.OnDeath += HandleGameOver;

            if (cameraFollow != null) cameraFollow.Target = CurrentPlayer.transform;

            GenerateArea(0);
            OnRunStarted?.Invoke();
        }

        private void GenerateArea(int depth)
        {
            if (currentBuiltArea != null) Destroy(currentBuiltArea.Root);
            endRoomRef = null;
            portalGO = null;

            AreaDepth = depth;
            rng = new System.Random();
            bool isFinal = depth == TotalAreas - 1;
            int pathLen = mainPathLengths[Mathf.Clamp(depth, 0, mainPathLengths.Length - 1)];
            int branches = optionalBranchCounts[Mathf.Clamp(depth, 0, optionalBranchCounts.Length - 1)];

            var generated = MapGenerator.Generate(rng, pathLen, isFinal, branches);
            currentBuiltArea = RoomBuilder.Build(generated, Vector3.zero, rng, depth);

            foreach (var room in currentBuiltArea.Rooms.Values)
            {
                if (room.RequiresClear)
                {
                    room.OnCombatCleared += HandleRoomCleared;
                }
            }

            endRoomRef = currentBuiltArea.Rooms[generated.End];
            if (!isFinal)
            {
                portalGO = CreatePortal(endRoomRef.WorldCenter);
            }

            if (CurrentPlayer != null)
            {
                CurrentPlayer.transform.position = currentBuiltArea.StartSpawnPosition + Vector3.up * 1f;
            }
            if (cameraFollow != null && CurrentPlayer != null)
            {
                cameraFollow.SnapTo(CurrentPlayer.transform.position);
            }

            OnAreaChanged?.Invoke(depth);
        }

        private GameObject CreatePortal(Vector3 center)
        {
            var go = new GameObject("AreaExitPortal");
            go.transform.position = center;
            go.transform.localScale = Vector3.one * 1.4f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(new Color(0.4f, 0.9f, 1f, 0.85f));
            sr.sortingOrder = 3;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.7f;

            var trigger = go.AddComponent<AreaExitTrigger>();
            trigger.OnEntered += AdvanceToNextArea;
            go.SetActive(false);
            return go;
        }

        private void HandleRoomCleared(Room room)
        {
            OnRoomCleared?.Invoke(room);

            if (room == endRoomRef && portalGO != null)
            {
                portalGO.SetActive(true);
            }

            if (room.Node.Type == RoomType.Boss)
            {
                HandleVictory();
                return;
            }

            OfferUpgradeChoice();
        }

        private void OfferUpgradeChoice()
        {
            State = RunState.UpgradeChoice;
            Time.timeScale = 0f;
            var choices = UpgradeDatabase.RollChoices(rng ?? new System.Random(), 3);
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

        private void AdvanceToNextArea()
        {
            if (State != RunState.Playing) return;
            GenerateArea(AreaDepth + 1);
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
