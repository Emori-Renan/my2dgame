using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;

        [Range(0f, 1f)]
        public float smoothTime = 0.1f;

        private Rigidbody2D rb;
        private Animator animator;

        private Vector2 moveInput;

        private Vector2 currentVelocity = Vector2.zero;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        void FixedUpdate()
        {
            Vector2 targetVelocity = moveInput * moveSpeed;

            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocity, smoothTime);

            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
        }
    }
}
