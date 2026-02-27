using UnityEngine;

namespace Player
{
    public class Movement : MonoBehaviour
    {
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");

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
        
        private float _horizontal;
        private bool _isFacingRight = true;
        private bool _jumpPressed;
        private bool _isGrounded;
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }
        // Update is called once per frame
        void Update()
        {   
            _horizontal = Input.GetAxisRaw("Horizontal");
            _jumpPressed = Input.GetButtonDown("Jump");
            
            Flip(_horizontal);
            CheckGround();
            VerticalMove();
            
            animator.SetBool(IsGrounded, _isGrounded);
            animator.SetFloat("Horizontal", Mathf.Abs(rb.linearVelocity.x));
        }
        private void FixedUpdate()
        {
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
                0.1f, // Adjust this to your desired ground detection distance
                groundLayer
            );
        }
        void VerticalMove()
        {
            if (_isGrounded && _jumpPressed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump);
            }
        }

        void Flip(float horizontalInput)
        {
            // Don't flip if no input
            if (horizontalInput == 0) return;

            // If moving right but facing left
            if (horizontalInput > 0 && !_isFacingRight)
            {
                PerformFlip();
            }
            // If moving left but facing right
            else if (horizontalInput < 0 && _isFacingRight)
            {
                PerformFlip();
            }
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