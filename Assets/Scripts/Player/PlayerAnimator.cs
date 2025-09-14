using UnityEngine;

namespace MyGame.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator animator;
        private Vector2 lastDirection = Vector2.down;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void UpdateAnimation(Vector2 direction)
        {
            if (animator == null) return;

            bool isCurrentlyMoving = direction.magnitude > 0.1f;
            animator.SetBool("IsMoving", isCurrentlyMoving);

            if (isCurrentlyMoving)
            {
                animator.SetFloat("MoveX", direction.x);
                animator.SetFloat("MoveY", direction.y);
                lastDirection = direction;
            }
            else
            {
                animator.SetFloat("MoveX", lastDirection.x);
                animator.SetFloat("MoveY", lastDirection.y);
            }
        }

        public Vector2 GetLastDirection()
        {
            return lastDirection;
        }
    }
}