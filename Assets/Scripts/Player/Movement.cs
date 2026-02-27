using UnityEngine;

namespace Player
{
    public class Movement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public Rigidbody2D rb;
        public float speed;
        public float jump;

        [Header("Ground Settings")]
        public LayerMask groundLayer;
        public Transform groundCheckTransform;
        public Vector2 groundCheckBoxSize;


        //Private Variables
        private float _horizontal;
        private bool _isFacingRight;
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
            CheckGround();
            VerticalMove();
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
    }
}