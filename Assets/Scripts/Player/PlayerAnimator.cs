using UnityEngine;

namespace MyGame.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator animator;
        private Vector2 lastMoveDir = Vector2.down;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on player object.");
            }
        }

        public void UpdateAnimation(Vector2 moveInput)
        {
            if (animator == null) return;

            if (moveInput != Vector2.zero)
            {
                // Set the IsMoving boolean to true and update direction floats.
                animator.SetBool("IsMoving", true);
                lastMoveDir = moveInput;
                animator.SetFloat("MoveX", moveInput.x);
                animator.SetFloat("MoveY", moveInput.y);
            }
            else
            {
                // Set the IsMoving boolean to false and maintain the last direction for idle state.
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
            }
        }

        public void SetIdleDirection(Vector2 direction)
        {
            if (animator == null) return;
            lastMoveDir = direction;
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
        }

        public Vector2 GetLastDirection()
        {
            return lastMoveDir;
        }
    }
}
