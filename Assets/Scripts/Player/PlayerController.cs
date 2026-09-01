using UnityEngine;
using BunsKun.Combat;

namespace BunsKun.Player
{
    /// <summary>
    /// Basic 2D platformer movement: left/right walking, a single jump, and a hover
    /// ("stay airborne") ability triggered by holding the jump key while airborne. Hovering
    /// drains a limited gauge that only regenerates while grounded, so the descent always
    /// stays risky rather than turning into free flight.
    /// Grounded state is checked with a small overlap circle below the player's feet,
    /// so no special physics layers need to be configured in the project for it to work.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float jumpForce = 13f;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);

        [Header("Hover (hold Space in the air)")]
        [SerializeField] private float maxHoverTime = 1.5f;
        [SerializeField] private float hoverRegenPerSecond = 0.9f;
        [SerializeField] private float hoverHoldSpeed = 0.6f;
        [SerializeField] private float hoverResponsiveness = 25f;

        private Rigidbody2D rb;
        private PlayerStats stats;
        private Health health;
        private SpriteRenderer spriteRenderer;
        private readonly Collider2D[] overlapResults = new Collider2D[8];

        private float horizontalInput = 0f;
        private bool holdingHoverKey;

        public bool FacingRight { get; private set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsHovering { get; private set; }
        public float MaxHoverTime => maxHoverTime;
        public float HoverTimeRemaining { get; private set; }
        public float HoverFraction => maxHoverTime <= 0f ? 0f : Mathf.Clamp01(HoverTimeRemaining / maxHoverTime);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 4.5f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            stats = GetComponent<PlayerStats>();
            health = GetComponent<Health>();
            health.SetTeam(Team.Player);

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            HoverTimeRemaining = maxHoverTime;
        }

        private void Update()
        {
            horizontalInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontalInput += 1f;

            if (horizontalInput > 0.01f) SetFacing(true);
            else if (horizontalInput < -0.01f) SetFacing(false);

            holdingHoverKey = Input.GetKey(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            if (IsGrounded && HoverTimeRemaining < maxHoverTime)
            {
                HoverTimeRemaining = Mathf.Min(maxHoverTime, HoverTimeRemaining + hoverRegenPerSecond * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGrounded();

            float speed = stats != null ? stats.MoveSpeed : 6f;
            rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);

            IsHovering = !IsGrounded && holdingHoverKey && HoverTimeRemaining > 0f;
            if (IsHovering)
            {
                float newY = Mathf.MoveTowards(rb.linearVelocity.y, hoverHoldSpeed, hoverResponsiveness * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
                HoverTimeRemaining = Mathf.Max(0f, HoverTimeRemaining - Time.fixedDeltaTime);
            }
        }

        private bool CheckGrounded()
        {
            Vector2 origin = (Vector2)transform.position + groundCheckOffset;
            int count = Physics2D.OverlapCircleNonAlloc(origin, groundCheckRadius, overlapResults);
            for (int i = 0; i < count; i++)
            {
                var col = overlapResults[i];
                if (col == null || col.isTrigger) continue;
                if (col.attachedRigidbody == rb) continue;
                return true;
            }
            return false;
        }

        private void SetFacing(bool right)
        {
            FacingRight = right;
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            transform.localScale = new Vector3(right ? absX : -absX, scale.y, scale.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector2)transform.position + groundCheckOffset, groundCheckRadius);
        }
    }
}
