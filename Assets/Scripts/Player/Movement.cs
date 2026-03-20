using Audio;
using UnityEngine;

namespace Player
{
    public class Movement : MonoBehaviour
    {
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        [Header("Movement Settings")]
        public Rigidbody2D rb;
        public float speed;
        public float jump;

        [Header("Ground Settings")]
        public LayerMask groundLayer;
        public Transform groundCheckTransform;
        public Vector2 groundCheckBoxSize;

        [Header("Animation Settings")]
        public Animator animator;

        [Header("Footstep Audio")]
        [Tooltip("Seconds between footstep sounds while moving")]
        public float stepInterval = 0.35f;
        [Tooltip("Minimum horizontal speed to trigger footsteps")]
        public float stepMoveThreshold = 0.1f;
        [Tooltip("Default surface type (matches SoundLibrary: footstep_grass)")]
        public string surfaceType = "grass";
        
        private float _horizontal;
        private bool _isFacingRight = true;
        private bool _jumpPressed;
        private bool _isGrounded;
        private float _stepTimer;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }
        
        private void Update()
        {   
            // ReSharper disable once Unity.UnknownInputAxes
            _horizontal = Input.GetAxisRaw("Horizontal");
            
            // ReSharper disable once Unity.UnknownInputAxes
            _jumpPressed = Input.GetButtonDown("Jump");
            
            Flip(_horizontal);
            CheckGround();
            VerticalMove();
            HandleFootsteps();
            
            animator.SetBool(IsGrounded, _isGrounded);
            animator.SetFloat(Horizontal, Mathf.Abs(rb.linearVelocity.x));
        }
        private void FixedUpdate()
        {
            HorizontalMove();
        }
        private void HorizontalMove()
        {
            rb.linearVelocity = new Vector2(_horizontal * speed, rb.linearVelocity.y);
        }
        private void CheckGround()
        {
            _isGrounded = Physics2D.BoxCast(
                groundCheckTransform.position,
                groundCheckBoxSize,
                0f,
                Vector2.down,
                0.1f,
                groundLayer
            );
        }
        private void VerticalMove()
        {
            if (_isGrounded && _jumpPressed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump);
            }
        }

        private void HandleFootsteps()
        {
            if (!_isGrounded || SoundManager.Instance == null)
            {
                _stepTimer = 0f;
                return;
            }

            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > stepMoveThreshold;
            if (!isMoving)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= stepInterval)
            {
                _stepTimer = 0f;
                SoundManager.Instance.PlayFootstep(surfaceType);
            }
        }

        private void Flip(float horizontalInput)
        {
            switch (horizontalInput)
            {
                case 0:
                    return;
                case > 0 when !_isFacingRight:
                case < 0 when _isFacingRight:
                    PerformFlip();
                    break;
            }
        }
        
        private void PerformFlip()
        {
            _isFacingRight = !_isFacingRight;
            var scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
}