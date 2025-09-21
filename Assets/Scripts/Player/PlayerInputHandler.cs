using UnityEngine;
using UnityEngine.InputSystem;
using MyGame.Managers;
using MyGame.Player;
using MyGame.World;

namespace MyGame.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerMovement playerMovement;
        private PlayerAnimator playerAnimator;
        private PlayerInteractor playerInteractor;
        private GridManager gridManager;
        private GridRenderer gridRenderer;
        private Camera mainCamera;

        private bool isDiagonalOnly = false;
        private Vector2 lastMoveDirection;

        private bool isAimingMode = false;


        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            playerInteractor = GetComponent<PlayerInteractor>();
            playerAnimator = GetComponent<PlayerAnimator>();
            gridManager = GridManager.Instance;
            mainCamera = Camera.main;

            gridRenderer = FindAnyObjectByType<GridRenderer>();

            if (playerMovement == null)
                Debug.LogError("PlayerMovement component not found.");
            if (playerInteractor == null)
                Debug.LogError("PlayerInteractor component not found.");
            if (gridManager == null)
                Debug.LogError("GridManager instance not found.");
            if (mainCamera == null)
                Debug.LogError("Main Camera not found.");
            if (gridRenderer == null)
                Debug.LogError("GridRenderer component not found. Aiming line will not function.");
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.onMovePerformed += OnMovePerformed;
                InputManager.Instance.onMoveCanceled += OnMoveCanceled;
                InputManager.Instance.onSelectPerformed += OnSelectPerformed;
                InputManager.Instance.onToggleGridPerformed += OnToggleGridPerformed;
                InputManager.Instance.onDiagonalMovePerformed += OnDiagonalMovePerformed;
                InputManager.Instance.onDiagonalMoveCanceled += OnDiagonalMoveCanceled;
                InputManager.Instance.onInteractPerformed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.onMovePerformed -= OnMovePerformed;
                InputManager.Instance.onMoveCanceled -= OnMoveCanceled;
                InputManager.Instance.onSelectPerformed -= OnSelectPerformed;
                InputManager.Instance.onToggleGridPerformed -= OnToggleGridPerformed;
                InputManager.Instance.onDiagonalMovePerformed -= OnDiagonalMovePerformed;
                InputManager.Instance.onDiagonalMoveCanceled -= OnDiagonalMoveCanceled;
                InputManager.Instance.onInteractPerformed -= OnInteractPerformed;
            }
        }

        private void Update()
        {
            if (mainCamera == null || playerAnimator == null || playerMovement == null) return;

            if (isAimingMode)
            {
                Vector3 mousePos = Mouse.current.position.ReadValue();
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, transform.position.z));
                Vector3 directionToMouse = (worldPos - transform.position).normalized;
                directionToMouse.z = 0;

                // Update both the animator's direction and the aiming line's direction
                playerAnimator.SetIdleDirection(directionToMouse);
                gridRenderer.SetAimingDirection(transform.position, directionToMouse);
            }
        }


        private void OnMovePerformed(Vector2 direction)
        {
            Vector2 gridDirection = Vector2.zero;
            if (isAimingMode)
                return;
            if (isDiagonalOnly)
            {
                if (Mathf.Abs(direction.x) > 0.1f && Mathf.Abs(direction.y) > 0.1f)
                {
                    gridDirection.x = Mathf.Sign(direction.x);
                    gridDirection.y = Mathf.Sign(direction.y);
                }
            }
            else
            {
                gridDirection.x = Mathf.RoundToInt(direction.x);
                gridDirection.y = Mathf.RoundToInt(direction.y);
            }

            if (gridDirection != lastMoveDirection)
            {
                lastMoveDirection = gridDirection;
                playerMovement.SetMovementDirection(gridDirection);
            }
        }

        private void OnMoveCanceled()
        {
            lastMoveDirection = Vector2.zero;
            if (isAimingMode)
                return;
            playerMovement.SetMovementDirection(Vector2.zero);
        }

        private void OnSelectPerformed()
        {
            if (playerMovement != null && gridManager != null && mainCamera != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z));
                Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
                Vector2Int targetGridPos = gridManager.GetGridCoordinates(worldPosition);
                playerMovement.StartMoveToPath(startGridPos, targetGridPos);
            }
        }

        private void OnToggleGridPerformed()
        {
            isAimingMode = !isAimingMode;

            if (isAimingMode)
            {
                playerMovement.StopCurrentMovement();
                if (gridRenderer != null)
                {
                    gridRenderer.ToggleAimingLine(true);
                }
            }
            else
            {
                if (gridRenderer != null)
                {
                    gridRenderer.ToggleAimingLine(false);
                }
            }
        }

        private void OnInteractPerformed()
        {
            if (playerInteractor != null)
            {
                playerInteractor.AttemptInteraction();
            }
        }

        private void OnDiagonalMovePerformed()
        {
            isDiagonalOnly = true;
            OnMovePerformed(InputManager.Instance.GetMovementDirection());
        }

        private void OnDiagonalMoveCanceled()
        {
            isDiagonalOnly = false;
            OnMovePerformed(InputManager.Instance.GetMovementDirection());
        }
    }
}
