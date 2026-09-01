using UnityEngine;

namespace BunsKun.Game
{
    /// <summary>Smoothly follows a target on the X/Y plane, keeping a fixed 2D camera distance.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float zOffset = -10f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (Target == null) return;
            Vector3 desired = new Vector3(Target.position.x, Target.position.y, zOffset);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        public void SnapTo(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y, zOffset);
            velocity = Vector3.zero;
        }
    }
}
