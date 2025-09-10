using System;
using System.Collections;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Pathfinding;
using MyGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using MyGame.World;

namespace MyGame.Player
{
    public class ClosedWorldMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float speedMultiplier = 1.0f;

        [Header("Interaction Settings")]
        [SerializeField] private LayerMask interactableLayer;

        private Animator animator;
        private GridManager gridManager;
        private Grid sceneGrid;
        private GameManager gameManager;
        private Camera mainCamera;

        private bool isMoving = false;
        private Vector2 lastDirection = Vector2.down;
        private Queue<Vector2Int> currentPath;
        private bool isGridReady = false;
        private bool isDiagonalOnlyActive = false;
        private bool isGridToggled = false;

        public event Action<bool> OnGridToggleChanged;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            gameManager = GameManager.Instance;
            gridManager = GridManager.Instance;
            mainCamera = Camera.main;

            if (gameManager == null) Debug.LogError("GameManager not found.");
            if (gameManager != null)
            {
                // Get the initial position from the GameManager.
                Vector3 initialPosition = gameManager.GetPlayerSpawnPosition();

                // Place the player at that position.
                transform.position = initialPosition;

                Debug.Log($"Player instantiated and placed at: {transform.position}");
            }
            else
            {
                Debug.LogError("GameManager instance not found. Player cannot be positioned correctly.");
            }
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found.");
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
                Debug.LogError("Main camera not found! Please ensure your scene has a camera tagged as 'MainCamera'.");
            }
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
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
                InputManager.Instance.onSelectPerformed -= OnSelectPerformed;
                InputManager.Instance.onToggleGridPerformed -= OnToggleGridPerformed;
                InputManager.Instance.onDiagonalMovePerformed -= HandleDiagonalMovePerformed;
                InputManager.Instance.onDiagonalMoveCanceled -= HandleDiagonalMoveCanceled;
                InputManager.Instance.onInteractPerformed -= OnInteractPerformed;
            }
            StopAllMovement();
        }

        private void Update()
        {
            if (isGridToggled)
            {
                return;
            }

            if (!IsGameReadyForMovement())
            {
                StopAllMovement();
                return;
            }

            Vector2 movementInput = InputManager.Instance.GetMovementDirection();

            if (isDiagonalOnlyActive && (Mathf.Abs(movementInput.x) < 0.1f || Mathf.Abs(movementInput.y) < 0.1f))
            {
                if (isMoving) StopAllMovement();
                UpdateAnimation(Vector2.zero);
                return;
            }

            if (!isMoving && movementInput != Vector2.zero)
            {
                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = startGridPos + new Vector2Int(Mathf.RoundToInt(movementInput.x), Mathf.RoundToInt(movementInput.y));

                if (!gridManager.IsPositionValid(startGridPos) || !gridManager.IsPositionValid(targetGridPos))
                {
                    Debug.LogWarning("Invalid start or target grid position. Cannot move.");
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

        public void SetSpeedMultiplier(float newMultiplier)
        {
            speedMultiplier = newMultiplier;
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

        private void OnSelectPerformed()
        {
            if (!IsGameReadyForMovement() || isGridToggled || isMoving)
            {
                Debug.Log($"Movement check failed. Game ready: {IsGameReadyForMovement()}, Grid toggled: {isGridToggled}, Is moving: {isMoving}");
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCamera.nearClipPlane));
            Debug.Log($"Mouse screen position: {mousePosition}, converted to world position: {worldPosition}");

            Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
            Vector2Int targetGridPos = gridManager.GetGridCoordinates(worldPosition);

            Debug.Log($"Player's world position: {transform.position}, converted to grid position: {startGridPos}");
            Debug.Log($"Mouse click world position: {worldPosition}, converted to grid position: {targetGridPos}");

            bool isStartValid = gridManager.IsPositionValid(startGridPos);
            bool isTargetValid = gridManager.IsPositionValid(targetGridPos);

            if (!isStartValid || !isTargetValid)
            {
                Debug.LogWarning($"Invalid start or target grid position for OnSelect. Cannot move. Start valid: {isStartValid}, Target valid: {isTargetValid}");
                return;
            }

            StartMoveToPath(startGridPos, targetGridPos);
        }

        private void OnToggleGridPerformed()
        {
            StopAllMovement();
            isGridToggled = !isGridToggled;
            Debug.Log($"Grid toggled. Movement is now: {!isGridToggled}");

            if (isGridToggled)
            {
                StartCoroutine(AimAtMouseRoutine());
            }
            else
            {
                StopAllCoroutines();
                UpdateAnimation(Vector2.zero);
            }
            OnGridToggleChanged?.Invoke(isGridToggled);
        }

        private void HandleDiagonalMovePerformed()
        {
            isDiagonalOnlyActive = true;
            Debug.Log("Diagonal move active");
        }

        private void HandleDiagonalMoveCanceled()
        {
            isDiagonalOnlyActive = false;
            Debug.Log("Diagonal move inactive");
        }

        private void OnInteractPerformed()
        {
            if (!IsGameReadyForMovement())
            {
                return;
            }

            Vector2Int currentGridPos = gridManager.GetGridCoordinates(transform.position);
            Vector2Int targetGridPos = currentGridPos + new Vector2Int(Mathf.RoundToInt(lastDirection.x), Mathf.RoundToInt(lastDirection.y));
            Vector3 targetWorldPosition = gridManager.GetWorldPosition(targetGridPos);

            Collider2D[] colliders = Physics2D.OverlapPointAll(targetWorldPosition, interactableLayer);
            foreach (Collider2D collider in colliders)
            {
                Door door = collider.GetComponent<Door>();
                if (door != null)
                {
                    door.ToggleDoor();
                    return;
                }
            }
        }

        private bool IsGameReadyForMovement()
        {
            return gameManager != null && gameManager.IsGameReadyForInput && gameManager.currentGameState == GameState.Playing &&
                   gridManager != null && gridManager.IsGridDataInitialized && sceneGrid != null && isGridReady;
        }

        // Inside ClosedWorldMovement.cs
        private void OnGridReady()
        {
            // The GameManager has already placed the player.
            // The only job here is to get the grid reference and enable movement.
            sceneGrid = gridManager.GetMainGameGrid();
            if (sceneGrid == null)
            {
                Debug.LogError("Main Game Grid from GridManager is null!");
                return;
            }

            isGridReady = true;
            Debug.Log("Player is ready for movement.");
        }

        private void StartMoveToPath(Vector2Int startPos, Vector2Int targetPos)
        {
            if (isMoving) return;
            if (AStarPathfinding.Instance == null)
            {
                Debug.LogError("AStarPathfinding instance not found!");
                return;
            }
            if (!IsGameReadyForMovement())
            {
                Debug.LogWarning("GridManager or sceneGrid not ready when trying to start path! Stopping.");
                return;
            }

            var path = AStarPathfinding.Instance.FindPath(startPos, targetPos);
            if (path != null && path.Count > 0)
            {
                currentPath = new Queue<Vector2Int>(path);
                StopAllCoroutines();
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
                if (!IsGameReadyForMovement())
                {
                    Debug.LogError("GridManager or sceneGrid is null during path following! Stopping movement.");
                    isMoving = false;
                    yield break;
                }

                Vector3 targetWorldPosition = gridManager.GetWorldPosition(nextTilePos);
                Vector3 startPosition = transform.position;
                float finalSpeed = moveSpeed * speedMultiplier;

                Vector2 direction = (targetWorldPosition - startPosition).normalized;
                UpdateAnimation(direction);

                while (Vector3.Distance(transform.position, targetWorldPosition) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, finalSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetWorldPosition;
            }
            isMoving = false;
            UpdateAnimation(Vector2.zero);
        }

        private IEnumerator AimAtMouseRoutine()
        {
            while (isGridToggled)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCamera.nearClipPlane));
                Vector2 directionToMouse = (worldPosition - transform.position).normalized;

                if (Mathf.Abs(directionToMouse.x) > Mathf.Abs(directionToMouse.y))
                {
                    lastDirection = new Vector2(Mathf.Sign(directionToMouse.x), 0);
                }
                else
                {
                    lastDirection = new Vector2(0, Mathf.Sign(directionToMouse.y));
                }

                UpdateAnimation(Vector2.zero);
                yield return null;
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
