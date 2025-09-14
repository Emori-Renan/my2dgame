using UnityEngine;
using UnityEngine.InputSystem;
using MyGame.Managers;

namespace MyGame.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerMovement playerMovement;
        private PlayerInteractor playerInteractor;
        private GridManager gridManager;
        private Camera mainCamera;

        private bool isDiagonalOnly = false;
        private Vector2 lastMoveDirection;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            playerInteractor = GetComponent<PlayerInteractor>();
            gridManager = GridManager.Instance;
            mainCamera = Camera.main;

            if (playerMovement == null)
                Debug.LogError("PlayerMovement component not found.");
            if (playerInteractor == null)
                Debug.LogError("PlayerInteractor component not found.");
            if (gridManager == null)
                Debug.LogError("GridManager instance not found.");
            if (mainCamera == null)
                Debug.LogError("Main Camera not found.");
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

        private void OnMovePerformed(Vector2 direction)
        {
            Vector2 gridDirection = Vector2.zero;

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
                if (Mathf.Abs(direction.x) > 0.1f)
                    gridDirection.x = Mathf.Sign(direction.x);
                if (Mathf.Abs(direction.y) > 0.1f)
                    gridDirection.y = Mathf.Sign(direction.y);
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
            // Optional: Add logic to toggle grid visibility or snapping
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
