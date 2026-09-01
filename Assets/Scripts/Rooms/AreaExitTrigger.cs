using System;
using UnityEngine;

namespace BunsKun.Rooms
{
    /// <summary>Placed in the final room of a (non-boss) area. Becomes active once that
    /// room is cleared; walking into it advances the run to the next area.</summary>
    public class AreaExitTrigger : MonoBehaviour
    {
        public event Action OnEntered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            OnEntered?.Invoke();
        }
    }
}
