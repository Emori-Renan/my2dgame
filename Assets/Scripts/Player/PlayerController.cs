using UnityEngine;
using UnityEngine.InputSystem; // Make sure this is at the top of your script

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // The method that the new Input System will call.
    // It must be public and take a single InputValue parameter.
    public void OnMove(InputValue value)
    {
        Debug.Log("OnMove called! Input value: " + value.Get<Vector2>());
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
    }
}