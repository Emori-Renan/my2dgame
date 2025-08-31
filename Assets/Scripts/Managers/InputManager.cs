using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace MyGame.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private PlayerControls playerControls;
        private Vector2 _currentMovementInput;

        // Events for other systems to subscribe to
        public event Action<Vector2> onMovePerformed;
        public event Action onMoveCanceled;
        public event Action onSelectPerformed;
        public event Action onInteractPerformed;
        public event Action onToggleGridPerformed;
        public event Action onDiagonalMovePerformed;
        public event Action onDiagonalMoveCanceled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy the new instance
                return;
            }
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject); // Keep the first instance

            playerControls = new PlayerControls();

            // Movement
            playerControls.Player.Move.performed += ctx =>
            {
                _currentMovementInput = ctx.ReadValue<Vector2>();
                onMovePerformed?.Invoke(_currentMovementInput);
            };
            playerControls.Player.Move.canceled += ctx =>
            {
                _currentMovementInput = Vector2.zero;
                onMoveCanceled?.Invoke();
            };

            // Select
            playerControls.Player.Select.performed += ctx => onSelectPerformed?.Invoke();

            // Other actions
            playerControls.Player.Interact.performed += ctx => onInteractPerformed?.Invoke();
            playerControls.Player.ToggleGrid.performed += ctx => onToggleGridPerformed?.Invoke();
            playerControls.Player.DiagonalMove.performed += ctx => onDiagonalMovePerformed?.Invoke();
            playerControls.Player.DiagonalMove.canceled += ctx => onDiagonalMoveCanceled?.Invoke();
            
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
