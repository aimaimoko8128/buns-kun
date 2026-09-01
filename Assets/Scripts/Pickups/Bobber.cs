using UnityEngine;

namespace BunsKun.Pickups
{
    /// <summary>Small idle bob/rotate animation so pickups read clearly against the terrain.</summary>
    public class Bobber : MonoBehaviour
    {
        private Vector3 startPos;
        private float phase;

        private void Awake()
        {
            startPos = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float y = Mathf.Sin(Time.time * 2f + phase) * 0.15f;
            transform.position = startPos + new Vector3(0f, y, 0f);
            transform.Rotate(0f, 0f, 30f * Time.deltaTime);
        }
    }
}
