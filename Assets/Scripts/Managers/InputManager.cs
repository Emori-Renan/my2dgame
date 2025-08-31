using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace MyGame.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private PlayerControls playerControls;

        public event Action<Vector2> onMovePerformed;
        public event Action onMoveCanceled;
        public event Action onSelectPerformed;
        // Removed events for Interact, ToggleGrid, DiagonalMove for simplicity
        // public event Action onInteractPerformed;
        // public event Action onToggleGridPerformed;
        // public event Action onDiagonalMovePerformed;
        // public event Action onDiagonalMoveCanceled;

        private Vector2 _currentMovementInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(transform.root.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            playerControls = new PlayerControls();

            // Movement
            playerControls.Player.Move.performed += ctx => _currentMovementInput = ctx.ReadValue<Vector2>();
            playerControls.Player.Move.canceled += ctx => _currentMovementInput = Vector2.zero;
            
            playerControls.Player.Move.performed += ctx => onMovePerformed?.Invoke(ctx.ReadValue<Vector2>());
            playerControls.Player.Move.canceled += ctx => onMoveCanceled?.Invoke();

            // Select (Mouse Click)
            playerControls.Player.Select.performed += ctx => onSelectPerformed?.Invoke();

            // Removed bindings for Interact, ToggleGrid, DiagonalMove for simplicity
            // playerControls.Player.Interact.performed += ctx => onInteractPerformed?.Invoke();
            // playerControls.Player.ToggleGrid.performed += ctx => onToggleGridPerformed?.Invoke();
            // playerControls.Player.DiagonalMove.performed += ctx => onDiagonalMovePerformed?.Invoke();
            // playerControls.Player.DiagonalMove.canceled += ctx => onDiagonalMoveCanceled?.Invoke();

            Debug.Log("InputManager: Initialized singleton in Awake.");
        }

        private void OnEnable()
        {
            playerControls.Enable();
        }

        private void OnDisable()
        {
            playerControls.Disable();
        }

        public Vector2 GetMovementDirection()
        {
            return _currentMovementInput;
        }
    }
}
