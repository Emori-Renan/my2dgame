using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Core;
using MyGame.Pathfinding;
// Removed MyGame.World as Door interaction is removed for simplicity
using System;

namespace MyGame.Player
{
    public class ClosedWorldMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float speedMultiplier = 1.0f;
        // Removed LayerMasks for solidObjectsLayer and interactableLayer for simplicity

        private Animator animator;
        private GridManager gridManager;
        private Grid sceneGrid;
        private GameManager gameManager;

        private bool isMoving = false;
        private Vector2 lastDirection = Vector2.down;
        private Queue<Vector2Int> currentPath;
        // Removed isAiming and isDiagonalOnly for simplicity

        // Removed isTeleporting and key management for simplicity
        // private bool isTeleporting = false;
        // [SerializeField] private List<string> collectedKeys = new List<string>();

        private bool _isGridReady = false; // Internal flag for ClosedWorldMovement's readiness

        // Removed public methods for key management and teleporting for simplicity
        // public void AddKey(string keyName) { /* ... */ }
        // public bool HasKey(string keyName) { /* ... */ }
        // public void RemoveKey(string keyName) { /* ... */ }
        // public bool IsTeleporting() { return isTeleporting; }
        // public void SetIsTeleporting(bool state) { /* ... */ }


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
                // Removed unsubscriptions for Interact, ToggleGrid, DiagonalMove
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
                // Removed subscriptions for Interact, ToggleGrid, DiagonalMove
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
                // Removed unsubscriptions for Interact, ToggleGrid, DiagonalMove
            }
            StopAllMovement();
            Debug.Log("ClosedWorldMovement: Disabled.");
        }

        private void Start()
        {
            // Removed AddKey for simplicity
        }

        private void Update()
        {
            // Simplified: No isTeleporting check
            // if (isTeleporting) { StopAllMovement(); return; }

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
            // Simplified: No isAiming check
            // if (isAiming) { StopAllMovement(); RotatePlayerToMouse(); return; }

            Vector2 movementInput = InputManager.Instance.GetMovementDirection();

            // Simplified: No isDiagonalOnly check
            // if (isDiagonalOnly) { /* ... */ }

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

        // Renamed to match InputManager events directly
        private void OnMoveInputPerformed(Vector2 movement)
        {
            // InputManager's GetMovementDirection handles continuous input; this is mainly for event trigger
        }

        private void OnMoveInputCanceled()
        {
            // InputManager's GetMovementDirection handles continuous input; this is mainly for event trigger
        }

        private void OnSelectPerformed() // No parameters
        {
            // Simplified: No isAiming or isTeleporting check
            // if (isAiming || isTeleporting) return;
            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing)
            {
                return;
            }
            if (gridManager == null || sceneGrid == null || !gridManager.IsGridDataInitialized || !_isGridReady)
            {
                Debug.LogWarning("ClosedWorldMovement: GridManager or sceneGrid not ready or grid data not initialized for OnSelectPerformed. Cannot initiate movement.");
                return;
            }

            if (!isMoving)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
                Vector3Int targetCell = sceneGrid.WorldToCell(worldPosition);
                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = gridManager.GetGridCoordinates(sceneGrid.CellToWorld(targetCell));

                Debug.Log($"--- Player Select Debug ---");
                Debug.Log($"Mouse World Pos: {worldPosition}");
                Debug.Log($"Target Cell Pos (Unity Grid): {targetCell}");
                Debug.Log($"Calculated Start Grid Pos (0-indexed): {startGridPos}");
                Debug.Log($"Calculated Target Grid Pos (0-indexed): {targetGridPos}");

                if (!gridManager.IsPositionValid(startGridPos) || !gridManager.IsPositionValid(targetGridPos))
                {
                    Debug.LogWarning($"ClosedWorldMovement: Invalid start ({startGridPos}) or target ({targetGridPos}) grid position for OnSelect. Cannot move.");
                    return;
                }

                StartMoveToPath(startGridPos, targetGridPos);
            }
        }

        // Removed OnInteractPerformed for simplicity
        // private void OnInteractPerformed() { /* ... */ }

        // Removed OnToggleGridPerformed for simplicity
        // private void OnToggleGridPerformed() { /* ... */ }

        // Removed OnDiagonalMovePerformed and OnDiagonalMoveCanceled for simplicity
        // private void OnDiagonalMovePerformed() { /* ... */ }
        // private void OnDiagonalMoveCanceled() { /* ... */ }

        public void SetSpeedMultiplier(float newMultiplier)
        {
            speedMultiplier = newMultiplier;
        }

        // Removed RotatePlayerToMouse for simplicity
        // private void RotatePlayerToMouse() { /* ... */ }


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
