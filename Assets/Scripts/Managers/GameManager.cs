using UnityEngine;
using UnityEngine.SceneManagement;
using MyGame.Core;
using MyGame.Player;

namespace MyGame.Managers
{
    /// <summary>
    /// Manages the overall game state for a simplified setup (no pause, no combat, no open-world).
    /// Uses a singleton pattern.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Singleton instance
        public static GameManager Instance { get; private set; }

        [Header("Player Movement Script")]
        [Tooltip("Assign the ClosedWorldMovement script component from your player GameObject.")]
        [SerializeField] private ClosedWorldMovement closedWorldMovementScript;
        
        [Header("Game State")]
        [Tooltip("The current overall state of the game.")]
        public GameState currentGameState = GameState.Playing; // Always start in Playing for this test

        // NEW: Flag to indicate when the game is fully ready for player input
        public bool IsGameReadyForInput { get; private set; } = false;

        private void Awake()
        {
            // Enforce singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        void Start()
        {
            currentGameState = GameState.Playing;
            SetGameState(currentGameState);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Sets the current game state and updates player movement script activity.
        /// Simplified for basic walking.
        /// </summary>
        /// <param name="newState">The new GameState to transition to.</param>
        public void SetGameState(GameState newState)
        {
            currentGameState = newState;
            Debug.Log($"Game Manager: Game State changed to: {currentGameState}");

            // --- Player Movement Script Activation Control ---
            bool enablePlayerControl = (newState == GameState.Playing);

            if (closedWorldMovementScript != null)
            {
                closedWorldMovementScript.enabled = enablePlayerControl;
                Debug.Log($"Game Manager: ClosedWorldMovement enabled: {closedWorldMovementScript.enabled}");
            }
            else
            {
                Debug.LogWarning("Game Manager: ClosedWorldMovement script not assigned to GameManager!");
            }
            
            Time.timeScale = 1f;

            // NEW: Set the IsGameReadyForInput flag
            IsGameReadyForInput = (newState == GameState.Playing);
            Debug.Log($"Game Manager: IsGameReadyForInput set to: {IsGameReadyForInput}");
        }
    }
}
