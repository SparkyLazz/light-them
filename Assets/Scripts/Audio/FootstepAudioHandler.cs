using UnityEngine;
// ReSharper disable LocalVariableHidesMember

namespace Audio
{
    /// <summary>
    /// Handles footstep audio. Attach to the Player.
    /// 
    /// Two ways to trigger footsteps:
    /// 1. Animation Events — add an event on walk frames that calls PlayFootstep()
    /// 2. Timer-based — set useTimer = true for automatic stepping while grounded & moving
    /// 
    /// Surface detection uses a short raycast downward to check the ground layer's tag.
    /// </summary>
    public class FootstepHandler : MonoBehaviour
    {
        [Header("Surface Detection")]
        [Tooltip("Layer mask for ground detection raycast")]
        public LayerMask groundLayer;
        [Tooltip("Raycast distance below the player's feet")]
        public float rayDistance = 1.5f;
        [Tooltip("Default surface when no tag is matched")]
        public string defaultSurface = "grass";

        [Header("Timer Mode (optional)")]
        [Tooltip("Enable timer-based footsteps instead of animation events")]
        public bool useTimer;
        [Tooltip("Seconds between footstep sounds while moving")]
        public float stepInterval = 0.35f;

        [Header("Movement Detection (for timer mode)")]
        [Tooltip("Minimum horizontal speed to count as moving")]
        public float moveThreshold = 0.1f;

        private float _stepTimer;
        private Rigidbody2D _rb;
        private bool _isGrounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!useTimer || SoundManager.Instance == null) return;

            _isGrounded = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundLayer);

            if (!_isGrounded || _rb == null) return;

            bool isMoving = Mathf.Abs(_rb.linearVelocity.x) > moveThreshold;
            if (!isMoving)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= stepInterval)
            {
                _stepTimer = 0f;
                PlayFootstep();
            }
        }

        /// <summary>
        /// Call this from Animation Events or directly.
        /// Detects the surface below and plays the matching footstep sound.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public void PlayFootstep()
        {
            if (SoundManager.Instance == null) return;

            string surface = DetectSurface();
            SoundManager.Instance.PlayFootstep(surface);
        }

        /// <summary>
        /// Raycast down to determine surface type based on collider tag.
        /// Tags should match: "Grass", "Stone", "Wood", etc.
        /// The tag is lowered to match SoundLibrary convention: "footstep_grass"
        /// </summary>
        private string DetectSurface()
        {
            var hit = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundLayer);
            if (hit.collider == null) return defaultSurface;

            // Use the collider's tag as the surface type
            string tag = hit.collider.tag;

            return tag switch
            {
                "Ground" => "grass",   // your current Ground layer/tag → grass
                "Stone" => "stone",
                "Wood" => "wood",
                _ => defaultSurface
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * rayDistance);
        }
    }
}