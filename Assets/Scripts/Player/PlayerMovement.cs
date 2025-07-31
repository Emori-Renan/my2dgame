using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    private Vector2 currentMovementInput;
    private Vector2 lastDirection = Vector2.down; // Start facing down

    private Rigidbody2D rb;
    private Animator animator;

    public void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null) Debug.LogError("Rigidbody2D not found on Player!", this);
        if (animator == null) Debug.LogError("Animator not found on Player!", this);
    }

    void Update()
    {
        bool isMoving = currentMovementInput.sqrMagnitude > Mathf.Epsilon;
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            float x = currentMovementInput.x;
            float y = currentMovementInput.y;

            // Snap input to nearest axis (prevents diagonal weirdness)
            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                x = Mathf.Sign(x);
                y = 0;
            }
            else
            {
                y = Mathf.Sign(y);
                x = 0;
            }

            animator.SetFloat("MoveX", x);
            animator.SetFloat("MoveY", y);
            lastDirection = new Vector2(x, y);
        }
        else
        {
            // Stay facing the last direction
            animator.SetFloat("MoveX", lastDirection.x);
            animator.SetFloat("MoveY", lastDirection.y);
        }
    }

    void FixedUpdate()
    {
        Vector2 movement = currentMovementInput.normalized * moveSpeed;
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
