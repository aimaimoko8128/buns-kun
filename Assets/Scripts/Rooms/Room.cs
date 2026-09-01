using System;
using System.Collections.Generic;
using UnityEngine;
using BunsKun.Enemies;
using BunsKun.ProceduralGeneration;

namespace BunsKun.Rooms
{
    /// <summary>
    /// Runtime behaviour attached to the root of a generated room. Tracks the enemies
    /// spawned into it and, once they are all defeated, removes any lock barriers on the
    /// doors leading onward from this room and notifies the run controller for rewards.
    /// </summary>
    public class Room : MonoBehaviour
    {
        public RoomNode Node { get; private set; }
        public bool RequiresClear { get; private set; }
        public bool Cleared { get; private set; }
        public Vector3 WorldCenter { get; private set; }

        private readonly List<EnemyController> aliveEnemies = new List<EnemyController>();
        private readonly List<GameObject> lockBarriers = new List<GameObject>();

        /// <summary>Raised once, the moment this room's enemies are all defeated (never for rooms that never required clearing).</summary>
        public event Action<Room> OnCombatCleared;

        public void Setup(RoomNode node, bool requiresClear, Vector3 worldCenter)
        {
            Node = node;
            RequiresClear = requiresClear;
            Cleared = !requiresClear;
            WorldCenter = worldCenter;
        }

        public void AddLockBarrier(GameObject barrier)
        {
            lockBarriers.Add(barrier);
        }

        /// <summary>
        /// Called once the layer has finished spawning. A room that demands clearing but
        /// ended up with no enemies would keep its exits locked forever, so it is opened
        /// straight away - quietly, without paying out a room-clear reward.
        /// </summary>
        public void FinalizeSpawning()
        {
            if (!RequiresClear || Cleared || aliveEnemies.Count > 0) return;

            Cleared = true;
            foreach (var barrier in lockBarriers)
            {
                if (barrier != null) Destroy(barrier);
            }
            lockBarriers.Clear();
        }

        public void RegisterEnemy(EnemyController enemy)
        {
            aliveEnemies.Add(enemy);
            enemy.OnDied += HandleEnemyDied;
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            aliveEnemies.Remove(enemy);
            if (aliveEnemies.Count == 0 && !Cleared)
            {
                ClearRoom();
            }
        }

        private void ClearRoom()
        {
            Cleared = true;
            foreach (var barrier in lockBarriers)
            {
                if (barrier != null) Destroy(barrier);
            }
            lockBarriers.Clear();
            OnCombatCleared?.Invoke(this);
        }
    }
}
