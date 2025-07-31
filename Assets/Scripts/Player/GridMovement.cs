using UnityEngine;
using System.Collections;

public class GridMovement : MonoBehaviour
{
    // A reference to your existing Animator component
    private Animator animator;

    // A variable to track if the character is currently moving,
    // so we don't start a new move while one is in progress.
    private bool isMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        // Get the Animator component from this GameObject
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on Player!");
        }

        // We also need to make sure the GridManager exists in the scene
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager not found in the scene! Make sure it exists.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check for a left mouse button click and ensure the character isn't already moving
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            // Get the mouse click position in world space
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Pass the world position to our movement function
            StartMove(mousePosition);
        }
    }

    /// <summary>
    /// This method is called when the player tries to move.
    /// It validates the move against the grid.
    /// </summary>
    private void StartMove(Vector3 targetPosition)
    {
        // Convert the mouse's world position to a grid coordinate
        Vector2Int targetGridPos = GridManager.Instance.GetGridCoordinates(targetPosition);

        // Get the TileData for that specific grid coordinate
        TileData targetTileData = GridManager.Instance.GetTileData(targetGridPos);

        // Check if the tile is within the grid bounds and is walkable
        if (targetTileData != null && targetTileData.isWalkable)
        {
            // If the tile is walkable, start the smooth movement coroutine
            StartCoroutine(SmoothMove(targetPosition));
        }
        else
        {
            Debug.Log("Cannot move to that location. It is unwalkable.");
            // You could add a sound effect or a red flash here to give player feedback.
        }
    }

    /// <summary>
    /// A coroutine to handle the smooth movement and animation of the character.
    /// </summary>
    IEnumerator SmoothMove(Vector3 targetPosition)
    {
        isMoving = true;

        // Tell your existing Animator to play the walking animation
        animator.SetBool("IsWalking", true);

        // Set the Z position to 0 to prevent it from moving into the background
        targetPosition.z = 0; 

        float duration = 0.5f; // The time it takes to move one square
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < duration)
        {
            // Lerp (Linear Interpolation) for smooth movement
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the character is exactly at the target position
        transform.position = targetPosition;

        // Tell your existing Animator to stop the walking animation and go back to Idle
        animator.SetBool("IsWalking", false);

        isMoving = false;
    }
}