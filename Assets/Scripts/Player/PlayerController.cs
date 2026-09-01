using UnityEngine;
using BunsKun.Combat;

namespace BunsKun.Player
{
    /// <summary>
    /// 2D movement for the hamburger: walking, jumping, and a fuel-limited jetpack that
    /// actually climbs while the jump key is held (not just a slow fall).
    ///
    /// The physics values here are also what the dungeon generator reads to decide how
    /// far apart it may place ledges - see PlayerMovementProfile. Changing them changes
    /// the terrain that gets generated, which is intentional.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private float bodyHeight = 1.1f;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);

        [Header("Jetpack (hold Space in the air)")]
        [SerializeField] private float maxJetpackFuel = 2f;
        [SerializeField] private float jetpackRiseSpeed = 5f;
        [SerializeField] private float jetpackAcceleration = 40f;
        [SerializeField] private float jetpackRefuelPerSecond = 1.2f;
        [SerializeField] private float jetpackRefuelDelay = 0.15f;

        public float JumpForce => jumpForce;
        public float GravityScale => gravityScale;
        public float BodyHeight => bodyHeight;
        public float JetpackRiseSpeed => jetpackRiseSpeed;
        public float MaxJetpackFuel => maxJetpackFuel;

        private Rigidbody2D rb;
        private PlayerStats stats;
        private Health health;
        private Transform thrustFlame;
        private readonly Collider2D[] overlapResults = new Collider2D[8];

        private float horizontalInput;
        private bool holdingJumpKey;
        private float groundedRefuelTimer;

        public bool FacingRight { get; private set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsThrusting { get; private set; }
        public float JetpackFuel { get; private set; }
        public float JetpackFuelFraction => maxJetpackFuel <= 0f ? 0f : Mathf.Clamp01(JetpackFuel / maxJetpackFuel);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            stats = GetComponent<PlayerStats>();
            health = GetComponent<Health>();
            health.SetTeam(Team.Player);

            JetpackFuel = maxJetpackFuel;
            CreateThrustFlame();
        }

        private void CreateThrustFlame()
        {
            var flame = new GameObject("JetpackFlame");
            flame.transform.SetParent(transform, false);
            flame.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            flame.transform.localScale = new Vector3(0.5f, 0.7f, 1f);

            var sr = flame.AddComponent<SpriteRenderer>();
            sr.sprite = BunsKun.Game.SpriteFactory.Circle(new Color(1f, 0.65f, 0.15f, 0.9f));
            sr.sortingOrder = 5;

            thrustFlame = flame.transform;
            flame.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;

            horizontalInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontalInput += 1f;

            if (horizontalInput > 0.01f) SetFacing(true);
            else if (horizontalInput < -0.01f) SetFacing(false);

            holdingJumpKey = Input.GetKey(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                groundedRefuelTimer = 0f;
            }

            if (IsGrounded && !IsThrusting)
            {
                groundedRefuelTimer += Time.deltaTime;
                if (groundedRefuelTimer >= jetpackRefuelDelay)
                {
                    JetpackFuel = Mathf.Min(maxJetpackFuel, JetpackFuel + jetpackRefuelPerSecond * Time.deltaTime);
                }
            }

            if (thrustFlame != null && thrustFlame.gameObject.activeSelf != IsThrusting)
            {
                thrustFlame.gameObject.SetActive(IsThrusting);
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGrounded();

            float speed = stats != null ? stats.MoveSpeed : 6f;
            rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);

            // Holding the jump key in the air fires the jetpack: it climbs, it does not hover.
            IsThrusting = holdingJumpKey && !IsGrounded && JetpackFuel > 0f;
            if (IsThrusting)
            {
                float newY = Mathf.MoveTowards(rb.linearVelocity.y, jetpackRiseSpeed,
                    jetpackAcceleration * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
                JetpackFuel = Mathf.Max(0f, JetpackFuel - Time.fixedDeltaTime);
                groundedRefuelTimer = 0f;
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

        public void RefillJetpack()
        {
            JetpackFuel = maxJetpackFuel;
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
