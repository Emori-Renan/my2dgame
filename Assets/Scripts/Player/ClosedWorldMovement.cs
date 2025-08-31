using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Core;
using MyGame.Pathfinding;
using MyGame.World;
using System;

namespace MyGame.Player
{
    public class ClosedWorldMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float speedMultiplier = 1.0f;

        [Header("Interaction Settings")]
        [SerializeField] private LayerMask interactableLayer;

        [Header("Grid Toggle")]
        [SerializeField] private bool _isGridToggled = false;

        private Animator animator;
        private GridManager gridManager;
        private Grid sceneGrid;
        private GameManager gameManager;
        private Camera mainCamera;

        private bool isMoving = false;
        private Vector2 lastDirection = Vector2.down;
        private Queue<Vector2Int> currentPath;

        private bool _isGridReady = false;
        private bool _isDiagonalOnlyActive = false;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            gameManager = GameManager.Instance;
            gridManager = GridManager.Instance;
            mainCamera = Camera.main;

            if (gameManager == null) Debug.LogError("ClosedWorldMovement: GameManager not found.");
            if (gridManager == null)
            {
                Debug.LogError("ClosedWorldMovement: GridManager not found.");
            }
            else
            {
                if (gridManager.IsGridDataInitialized)
                {
                    OnGridReady();
                }
                GridManager.OnGridReady += OnGridReady;
            }

            if (mainCamera == null)
            {
                Debug.LogError("ClosedWorldMovement: Main camera not found! Please ensure your scene has a camera tagged as 'MainCamera'.");
            }
        }

        private void OnDestroy()
        {
            if (gridManager != null)
            {
                GridManager.OnGridReady -= OnGridReady;
            }
            if (InputManager.Instance != null)
            {
                InputManager.Instance.onMovePerformed -= OnMoveInputPerformed;
                InputManager.Instance.onMoveCanceled -= OnMoveInputCanceled;
                InputManager.Instance.onSelectPerformed -= OnSelectPerformed;
                InputManager.Instance.onToggleGridPerformed -= OnToggleGridPerformed;
                InputManager.Instance.onDiagonalMovePerformed -= HandleDiagonalMovePerformed;
                InputManager.Instance.onDiagonalMoveCanceled -= HandleDiagonalMoveCanceled;
                InputManager.Instance.onInteractPerformed -= OnInteractPerformed;
            }
            StopAllMovement();
        }

        private void OnGridReady()
        {
            sceneGrid = gridManager.GetMainGameGrid();
            if (sceneGrid == null) Debug.LogError("ClosedWorldMovement: Main Game Grid from GridManager is null! Ensure GridManager's 'Main Unity Grid' is assigned in the Inspector.");
            else Debug.Log("ClosedWorldMovement: GridManager's sceneGrid reference obtained and player is ready for grid checks.");

            _isGridReady = true;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.onMovePerformed += OnMoveInputPerformed;
                InputManager.Instance.onMoveCanceled += OnMoveInputCanceled;
                InputManager.Instance.onSelectPerformed += OnSelectPerformed;
                InputManager.Instance.onToggleGridPerformed += OnToggleGridPerformed;
                InputManager.Instance.onDiagonalMovePerformed += HandleDiagonalMovePerformed;
                InputManager.Instance.onDiagonalMoveCanceled += HandleDiagonalMoveCanceled;
                InputManager.Instance.onInteractPerformed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.onMovePerformed -= OnMoveInputPerformed;
                InputManager.Instance.onMoveCanceled -= OnMoveInputCanceled;
                InputManager.Instance.onSelectPerformed -= OnSelectPerformed;
                InputManager.Instance.onToggleGridPerformed -= OnToggleGridPerformed;
                InputManager.Instance.onDiagonalMovePerformed -= HandleDiagonalMovePerformed;
                InputManager.Instance.onDiagonalMoveCanceled -= HandleDiagonalMoveCanceled;
                InputManager.Instance.onInteractPerformed -= OnInteractPerformed;
            }
            StopAllMovement();
        }

        private void Start()
        {
            // Initial setup can go here
        }

        private void Update()
        {
            // If the grid is toggled for aiming, movement is disabled.
            if (_isGridToggled)
            {
                return;
            }

            // If we are not in the playing state or the grid is not ready, do nothing.
            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing ||
                gridManager == null || !gridManager.IsGridDataInitialized || sceneGrid == null || !_isGridReady)
            {
                StopAllMovement();
                return;
            }

            Vector2 movementInput = InputManager.Instance.GetMovementDirection();

            if (_isDiagonalOnlyActive)
            {
                if (Mathf.Abs(movementInput.x) < 0.1f || Mathf.Abs(movementInput.y) < 0.1f)
                {
                    if (isMoving) StopAllMovement();
                    UpdateAnimation(Vector2.zero);
                    return;
                }
            }

            if (!isMoving && movementInput != Vector2.zero)
            {
                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = startGridPos + new Vector2Int(Mathf.RoundToInt(movementInput.x), Mathf.RoundToInt(movementInput.y));

                if (!gridManager.IsPositionValid(startGridPos) || !gridManager.IsPositionValid(targetGridPos))
                {
                    Debug.LogWarning($"ClosedWorldMovement: Invalid start ({startGridPos}) or target ({targetGridPos}) grid position. Cannot move.");
                    UpdateAnimation(Vector2.zero);
                    return;
                }

                StartMoveToPath(startGridPos, targetGridPos);
            }
            else if (!isMoving)
            {
                UpdateAnimation(Vector2.zero);
            }
        }

        // --- Event Handlers from InputManager ---
        private void OnSelectPerformed()
        {
            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing)
            {
                return;
            }
            if (_isGridToggled)
            {
                // If the grid is toggled for aiming, a select input should not cause movement.
                return;
            }

            if (!isMoving)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
                Vector3Int targetCell = sceneGrid.WorldToCell(worldPosition);
                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = gridManager.GetGridCoordinates(sceneGrid.CellToWorld(targetCell));

                if (!gridManager.IsPositionValid(startGridPos) || !gridManager.IsPositionValid(targetGridPos))
                {
                    Debug.LogWarning($"ClosedWorldMovement: Invalid start ({startGridPos}) or target ({targetGridPos}) grid position for OnSelect. Cannot move.");
                    return;
                }

                StartMoveToPath(startGridPos, targetGridPos);
            }
        }

        private void OnMoveInputPerformed(Vector2 movement) { }
        private void OnMoveInputCanceled() { }

        private void OnToggleGridPerformed()
        {
            // Stop any current movement before changing state
            StopAllMovement();

            // Toggle the grid state
            _isGridToggled = !_isGridToggled;
            Debug.Log($"Grid toggled. Movement is now: {!_isGridToggled}");

            if (_isGridToggled)
            {
                // Start the continuous aiming routine
                StartCoroutine(AimAtMouseRoutine());
            }
            else
            {
                // When toggled off, stop the aiming routine and reset animation
                StopAllCoroutines();
                UpdateAnimation(Vector2.zero);
            }
        }

        private void HandleDiagonalMovePerformed()
        {
            _isDiagonalOnlyActive = true;
            Debug.Log("Diagonal move active");
        }

        private void HandleDiagonalMoveCanceled()
        {
            _isDiagonalOnlyActive = false;
            Debug.Log("Diagonal move inactive");
        }

        private void OnInteractPerformed()
        {
            Debug.Log("OnInteractPerformed: Interaction button pressed.");
            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing)
            {
                Debug.Log("OnInteractPerformed: Game is not ready for input. Interaction cancelled.");
                return;
            }

            // Determine the tile in front of the player based on their last facing direction
            Vector2Int currentGridPos = gridManager.GetGridCoordinates(transform.position);
            Vector2Int targetGridPos = currentGridPos + new Vector2Int(Mathf.RoundToInt(lastDirection.x), Mathf.RoundToInt(lastDirection.y));
            Vector3 targetWorldPosition = gridManager.GetWorldPosition(targetGridPos);

            Debug.Log($"OnInteractPerformed: Checking for interaction at grid position {targetGridPos} (world pos: {targetWorldPosition}). Player last direction: {lastDirection}");

            // Check for an interactable object at the target position, specifically a Door
            Collider2D[] colliders = Physics2D.OverlapPointAll(targetWorldPosition, interactableLayer);
            Debug.Log($"OnInteractPerformed: Found {colliders.Length} colliders at the target position.");

            foreach (Collider2D collider in colliders)
            {
                Debug.Log($"OnInteractPerformed: Checking collider with name '{collider.name}'.");
                Door door = collider.GetComponent<Door>();
                if (door != null)
                {
                    Debug.Log($"OnInteractPerformed: Found Door component. Toggling door on object '{door.gameObject.name}'.");
                    door.ToggleDoor();
                    return; // Interact with only one door at a time
                }
            }
            Debug.Log("OnInteractPerformed: No door found at the target position.");
        }
        // --- End of Event Handlers ---

        public void SetSpeedMultiplier(float newMultiplier)
        {
            speedMultiplier = newMultiplier;
        }

        private void StartMoveToPath(Vector2Int startPos, Vector2Int targetPos)
        {
            if (isMoving) return;
            if (AStarPathfinding.Instance == null)
            {
                Debug.LogError("ClosedWorldMovement: AStarPathfinding instance not found!");
                return;
            }
            if (gridManager == null || sceneGrid == null || !gridManager.IsGridDataInitialized || !_isGridReady)
            {
                Debug.LogWarning("ClosedWorldMovement: GridManager or sceneGrid not ready or grid data not initialized when trying to start path! Stopping.");
                return;
            }

            List<Vector2Int> path = AStarPathfinding.Instance.FindPath(startPos, targetPos);
            if (path != null && path.Count > 0)
            {
                currentPath = new Queue<Vector2Int>(path);
                StopAllCoroutines(); // Stop any existing movement routines
                StartCoroutine(FollowPathRoutine());
            }
            else
            {
                Debug.LogWarning($"No path found from {startPos} to {targetPos}. Target walkable: {gridManager.IsWalkable(targetPos)}");
                UpdateAnimation(Vector2.zero);
            }
        }

        private IEnumerator FollowPathRoutine()
        {
            isMoving = true;
            while (currentPath.Count > 0)
            {
                Vector2Int nextTilePos = currentPath.Dequeue();
                if (gridManager == null || sceneGrid == null || !gridManager.IsGridDataInitialized || !_isGridReady)
                {
                    Debug.LogError("FollowPathRoutine: GridManager or sceneGrid is null or grid data not initialized during path following! Stopping movement.");
                    isMoving = false;
                    yield break;
                }

                Vector3 targetWorldPosition = gridManager.GetWorldPosition(nextTilePos);
                Vector3 startPosition = transform.position;
                float travelProgress = 0f;
                float finalSpeed = moveSpeed * speedMultiplier;

                Vector2 direction = (targetWorldPosition - startPosition).normalized;
                UpdateAnimation(direction);

                while (travelProgress < 1f)
                {
                    travelProgress += Time.deltaTime * finalSpeed;
                    transform.position = Vector3.Lerp(startPosition, targetWorldPosition, travelProgress);
                    yield return null;
                }
                transform.position = targetWorldPosition;
            }
            isMoving = false;
            UpdateAnimation(Vector2.zero);
        }

        private IEnumerator AimAtMouseRoutine()
        {
            while (_isGridToggled)
            {
                // Get the world position of the mouse cursor
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCamera.nearClipPlane));

                // Calculate the direction vector from the player to the mouse
                Vector2 directionToMouse = (worldPosition - transform.position).normalized;

                // Update the player's lastDirection to face the mouse
                if (Mathf.Abs(directionToMouse.x) > Mathf.Abs(directionToMouse.y))
                {
                    lastDirection = new Vector2(Mathf.Sign(directionToMouse.x), 0);
                }
                else
                {
                    lastDirection = new Vector2(0, Mathf.Sign(directionToMouse.y));
                }

                // Update the animation to show the aiming direction without moving the character
                UpdateAnimation(Vector2.zero);

                yield return null; // Wait until the next frame
            }
        }

        public void StopAllMovement()
        {
            if (isMoving)
            {
                StopAllCoroutines();
                isMoving = false;
                UpdateAnimation(Vector2.zero);
            }
        }

        private void UpdateAnimation(Vector2 direction)
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
    }
}
