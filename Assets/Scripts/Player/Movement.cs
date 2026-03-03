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
        public Vector2   groundCheckBoxSize;

        [Header("Animation Settings")]
        public Animator animator;

        float _horizontal;
        bool  _jumpPressed;
        bool  _isGrounded;
        bool  _isFacingRight = true;

        // set by PlayerHealth
        public bool InputEnabled { get; set; } = true;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (!InputEnabled) return;

            _horizontal  = Input.GetAxisRaw("Horizontal");
            _jumpPressed = Input.GetButtonDown("Jump");

            Flip(_horizontal);
            CheckGround();
            VerticalMove();

            animator.SetBool(IsGrounded, _isGrounded);
            animator.SetFloat(Horizontal, Mathf.Abs(rb.linearVelocity.x));
        }

        void FixedUpdate()
        {
            if (!InputEnabled) return;
            HorizontalMove();
        }

        void HorizontalMove()
        {
            rb.linearVelocity = new Vector2(_horizontal * speed, rb.linearVelocity.y);
        }

        void CheckGround()
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

        void VerticalMove()
        {
            if (_isGrounded && _jumpPressed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump);
        }

        void Flip(float input)
        {
            if (input == 0) return;
            if ((input > 0 && !_isFacingRight) || (input < 0 && _isFacingRight))
                PerformFlip();
        }

        void PerformFlip()
        {
            _isFacingRight = !_isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
}