using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Core;
using MyGame.Pathfinding;
using MyGame.World; // Added reference to Door script
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

        private Animator animator;
        private GridManager gridManager;
        private Grid sceneGrid;
        private GameManager gameManager;

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
                    Debug.Log("ClosedWorldMovement: GridManager already initialized in Awake. Calling OnGridReady proactively.");
                }
                GridManager.OnGridReady += OnGridReady;
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
            Debug.Log("ClosedWorldMovement: Enabled.");
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
            Debug.Log("ClosedWorldMovement: Disabled.");
        }

        private void Start()
        {
            // Initial setup can go here
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing ||
                gridManager == null || !gridManager.IsGridDataInitialized || sceneGrid == null || !_isGridReady)
            {
                StopAllMovement();
                if (gridManager == null || !gridManager.IsGridDataInitialized || sceneGrid == null || !_isGridReady)
                {
                    Debug.LogWarning($"ClosedWorldMovement: Movement blocked because grid is not yet fully ready. " +
                                     $"GM Ready: {gameManager?.IsGameReadyForInput}, GM State: {gameManager?.currentGameState}, " +
                                     $"GridManager Exists: {gridManager != null}, GridDataInitialized: {gridManager?.IsGridDataInitialized}, " +
                                     $"SceneGrid Exists: {sceneGrid != null}, _isGridReady Flag: {_isGridReady}");
                }
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

                Debug.Log($"--- Player Movement Debug ---");
                Debug.Log($"Player World Pos: {transform.position}");
                Debug.Log($"GridManager Bounds: {gridManager.GetMapGridBounds()} (Size: {gridManager.GetMapGridBounds().size.x}x{gridManager.GetMapGridBounds().size.y})");
                Debug.Log($"Player Cell Pos (Unity Grid): {sceneGrid.WorldToCell(transform.position)}");
                Debug.Log($"Calculated Start Grid Pos (0-indexed): {startGridPos}");
                Debug.Log($"Calculated Target Grid Pos (0-indexed): {targetGridPos}");

                if (!gridManager.IsPositionValid(startGridPos))
                {
                    Debug.LogWarning($"ClosedWorldMovement: Player's start grid position ({startGridPos}) is OUTSIDE current map bounds. Cannot move. Check player placement or SceneGridInitializer bounds.");
                    UpdateAnimation(Vector2.zero);
                    return;
                }
                if (!gridManager.IsPositionValid(targetGridPos))
                {
                    Debug.LogWarning($"ClosedWorldMovement: Target grid position ({targetGridPos}) is OUTSIDE current map bounds. Cannot move. Check target position calculation or SceneGridInitializer bounds.");
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
        private void OnMoveInputPerformed(Vector2 movement) { }
        private void OnMoveInputCanceled() { }
        private void OnSelectPerformed() { }
        private void OnToggleGridPerformed() { }

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
