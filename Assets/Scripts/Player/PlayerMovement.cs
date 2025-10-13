using System;
using UnityEngine;

namespace Players
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float jumpForce = 10f;

        [Header("Ground Check Settings")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.2f;
        public LayerMask groundLayer;

        private Rigidbody2D _rigidbody2D;
        private Animator _animator;
        private bool _isGrounded;
        private float _moveInput;
        private bool _isFacingRight = true;

        private void Start()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            _moveInput = Input.GetAxisRaw("Horizontal");
            _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            // ✅ Set Animator parameter
            _animator.SetFloat("Speed", Mathf.Abs(_moveInput));

            // ✅ Jump logic
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
            }

            Flip();
        }

        private void FixedUpdate()
        {
            // ✅ Apply horizontal movement
            _rigidbody2D.linearVelocity = new Vector2(_moveInput * moveSpeed, _rigidbody2D.linearVelocity.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        private void Flip()
        {
            // ✅ Flip only when direction changes
            if ((_isFacingRight && _moveInput < 0f) || (!_isFacingRight && _moveInput > 0f))
            {
                _isFacingRight = !_isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
        }
    }
}
