using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Core;
using MyGame.Pathfinding;

namespace MyGame.Player
{
    public class ClosedWorldMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float speedMultiplier = 1.0f;

        private Animator animator;
        private GridManager gridManager;
        private Grid sceneGrid;

        private GameManager gameManager;

        private Vector2 currentInputDirection;
        private bool isMoving = false;
        private Vector2 lastDirection = Vector2.down;

        private Queue<Vector2Int> currentPath;
        private bool isAiming = false;
        private bool isDiagonalOnly = false;

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction selectAction;
        private InputAction toggleGridAction;
        private InputAction diagonalMoveAction; // New action

        private void Awake()
        {
            animator = GetComponent<Animator>();
            gameManager = GameManager.Instance;
            gridManager = GridManager.Instance;
            playerInput = GetComponent<PlayerInput>();

            if (gameManager == null) Debug.LogError("ClosedWorldMovement: GameManager not found.");
            if (gridManager == null) Debug.LogError("ClosedWorldMovement: GridManager not found.");
            if (playerInput == null) Debug.LogError("ClosedWorldMovement: PlayerInput not found.");
        }

        private void OnEnable()
        {
            if (playerInput != null)
            {
                var playerControls = playerInput.actions.FindActionMap("Player");
                if (playerControls != null)
                {
                    moveAction = playerControls.FindAction("Move");
                    selectAction = playerControls.FindAction("Select");
                    toggleGridAction = playerControls.FindAction("ToggleGrid");
                    diagonalMoveAction = playerControls.FindAction("DiagonalMove");

                    if (moveAction != null)
                    {
                        moveAction.performed += OnMovePerformed;
                        moveAction.canceled += OnMoveCanceled;
                        moveAction.Enable();
                    }
                    if (selectAction != null)
                    {
                        selectAction.performed += OnSelectPerformed;
                        selectAction.Enable();
                    }
                    if (toggleGridAction != null)
                    {
                        toggleGridAction.performed += OnToggleGridPerformed;
                        toggleGridAction.Enable();
                    }
                    if (diagonalMoveAction != null)
                    {
                        diagonalMoveAction.performed += OnDiagonalMovePerformed;
                        diagonalMoveAction.canceled += OnDiagonalMoveCanceled;
                        diagonalMoveAction.Enable();
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled -= OnMoveCanceled;
                moveAction.Disable();
            }
            if (selectAction != null)
            {
                selectAction.performed -= OnSelectPerformed;
                selectAction.Disable();
            }
            if (toggleGridAction != null)
            {
                toggleGridAction.performed -= OnToggleGridPerformed;
                toggleGridAction.Disable();
            }
            if (diagonalMoveAction != null)
            {
                diagonalMoveAction.performed -= OnDiagonalMovePerformed;
                diagonalMoveAction.canceled -= OnDiagonalMoveCanceled;
                diagonalMoveAction.Disable();
            }
        }

        private void Start()
        {
            if (gridManager != null)
            {
                sceneGrid = gridManager.GetMainGameGrid();
                if (sceneGrid == null) Debug.LogError("ClosedWorldMovement: Could not get Grid component from GridManager.");
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            currentInputDirection = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            currentInputDirection = Vector2.zero;
        }

        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            if (isAiming) return;

            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing)
            {
                return;
            }

            if (!isMoving)
            {
                if (Camera.main == null)
                {
                    Debug.LogError("OnSelect: No camera tagged 'MainCamera' found in the scene.");
                    return;
                }

                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
                Vector3Int targetCell = sceneGrid.WorldToCell(worldPosition);

                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = gridManager.GetGridCoordinates(sceneGrid.CellToWorld(targetCell));

                StartMoveToPath(startGridPos, targetGridPos);
            }
        }

        private void OnToggleGridPerformed(InputAction.CallbackContext context)
        {
            isAiming = !isAiming;
            if (!isAiming)
            {
                UpdateAnimation(Vector2.zero);
            }
            if (GridManager.Instance != null && GridManager.Instance.GetComponent<GridRenderer>() != null)
            {
                GridManager.Instance.GetComponent<GridRenderer>().ToggleAimingLine(isAiming);
            }
        }

        private void OnDiagonalMovePerformed(InputAction.CallbackContext context)
        {
            isDiagonalOnly = true;
            Debug.Log("Diagonal-only movement ENABLED.");
        }

        private void OnDiagonalMoveCanceled(InputAction.CallbackContext context)
        {
            isDiagonalOnly = false;
            Debug.Log("Diagonal-only movement CANCELED.");
        }

        public void SetSpeedMultiplier(float newMultiplier)
        {
            speedMultiplier = newMultiplier;
            Debug.Log($"Movement speed multiplier updated to: {speedMultiplier}");
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsGameReadyForInput || gameManager.currentGameState != GameState.Playing)
            {
                StopAllMovement();
                return;
            }

            if (isAiming)
            {
                StopAllMovement();
                RotatePlayerToMouse();
                return;
            }

            Vector2 movementInput = currentInputDirection;

            if (isDiagonalOnly)
            {
                // Force input to be diagonal. If a diagonal isn't being pressed, movement is zero.
                if (Mathf.Abs(currentInputDirection.x) > 0.1f && Mathf.Abs(currentInputDirection.y) > 0.1f)
                {
                    movementInput = new Vector2(Mathf.Sign(currentInputDirection.x), Mathf.Sign(currentInputDirection.y));
                }
                else
                {
                    movementInput = Vector2.zero;
                }
            }

            if (!isMoving && movementInput != Vector2.zero)
            {
                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = startGridPos + new Vector2Int(Mathf.RoundToInt(movementInput.x), Mathf.RoundToInt(movementInput.y));

                StartMoveToPath(startGridPos, targetGridPos);
            }
            else if (!isMoving)
            {
                UpdateAnimation(Vector2.zero);
            }
        }

        private void RotatePlayerToMouse()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
            Vector2 direction = (worldMousePosition - transform.position).normalized;
            UpdateAnimation(direction);
        }

        private void StartMoveToPath(Vector2Int startPos, Vector2Int targetPos)
        {
            if (isMoving) return;

            if (AStarPathfinding.Instance == null)
            {
                Debug.LogError("StartMoveToPath: AStarPathfinding.Instance is not found. Cannot start movement.");
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
                Debug.Log("No valid path found to the target. Movement blocked.");
            }
        }

        private IEnumerator FollowPathRoutine()
        {
            isMoving = true;
            Debug.Log($"FollowPathRoutine: Player is starting to move along a path of {currentPath.Count} tiles.");

            while (currentPath.Count > 0)
            {
                Vector2Int nextTilePos = currentPath.Dequeue();

                Vector3 targetWorldPosition = gridManager.GetMainGameGrid().CellToWorld(new Vector3Int(nextTilePos.x + gridManager.GetMapGridBounds().x, nextTilePos.y + gridManager.GetMapGridBounds().y, 0)) + new Vector3(sceneGrid.cellSize.x / 2f, sceneGrid.cellSize.y / 2f, 0);

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
            Debug.Log("FollowPathRoutine: Player has reached the end of the path.");
        }

        private void StopAllMovement()
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