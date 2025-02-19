using UnityEngine;

namespace Characters
{
    public class Movement : MonoBehaviour
    {
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int Speed = Animator.StringToHash("Speed");

        [Header("Player Settings")] 
        public Rigidbody2D rigidbody2d;
        public Transform groundCheck;
        public LayerMask groundLayer;

        [Header("Animation Settings")] 
        public Animator animator;
        
        [Header("Movement Settings")]
        public float moveSpeed;
        public float jumpForce;
        private bool _isGrounded;
        private float _moveInput;

        private void Update()
        {
            //Horizontal Movement
            _moveInput = Input.GetAxis("Horizontal");
            
            //Check if player is on the ground
            _isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            {
                rigidbody2d.linearVelocity = new Vector2(rigidbody2d.linearVelocity.x, jumpForce);
            }
            
            //Set animator Value
            animator.SetFloat(Horizontal, _moveInput);
            animator.SetFloat(Speed, Mathf.Abs(rigidbody2d.linearVelocity.x));
        }
        private void FixedUpdate()
        {
            rigidbody2d.linearVelocity = new Vector2(_moveInput * moveSpeed, rigidbody2d.linearVelocity.y);
        }
    }
}