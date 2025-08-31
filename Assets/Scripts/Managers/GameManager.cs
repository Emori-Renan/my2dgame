using UnityEngine;
using UnityEngine.SceneManagement;
using MyGame.Core;
using MyGame.Player;
using System;

namespace MyGame.Managers
{
    /// <summary>
    /// Manages the overall game state and provides access to persistent objects.
    /// Uses a singleton pattern to ensure only one instance exists.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Singleton instance
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Drag and drop your player prefab here.")]
        [SerializeField] private GameObject playerPrefab;
        [Tooltip("Reference to the player's movement script.")]
        [SerializeField] private ClosedWorldMovement closedWorldMovementScript;

        // Public property to hold the player's Transform
        public Transform playerTransform { get; private set; }

        [Header("Game State")]
        [Tooltip("The current overall state of the game.")]
        public GameState currentGameState = GameState.Loading;

        // Flag to indicate when the game is fully ready for player input
        public bool IsGameReadyForInput { get; private set; } = false;

        private void Awake()
        {
            // Enforce singleton pattern and handle persistence
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            // Make the root parent object persistent across scenes
            DontDestroyOnLoad(transform.root.gameObject);
        }

        /// <summary>
        /// Sets the current game state and updates player movement script activity.
        /// </summary>
        /// <param name="newState">The new GameState to transition to.</param>
        public void SetGameState(GameState newState)
        {
            currentGameState = newState;
            Debug.Log($"Game Manager: Game State changed to: {currentGameState}");

            bool enablePlayerControl = (newState == GameState.Playing);

            // Get a reference to the player script if one exists
            if (playerTransform != null)
            {
                closedWorldMovementScript = playerTransform.GetComponent<ClosedWorldMovement>();
            }

            if (closedWorldMovementScript != null)
            {
                closedWorldMovementScript.enabled = enablePlayerControl;
                Debug.Log($"Game Manager: ClosedWorldMovement enabled: {closedWorldMovementScript.enabled}");
            }
            else
            {
                Debug.LogWarning("Game Manager: ClosedWorldMovement script not assigned or player not spawned!");
            }

            IsGameReadyForInput = (newState == GameState.Playing);
            Debug.Log($"Game Manager: IsGameReadyForInput set to: {IsGameReadyForInput}");
        }

        /// <summary>
        /// Spawns the player object at a given position.
        /// </summary>
        public void SpawnPlayerAtPosition(Vector3 position)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned to the GameManager!");
                return;
            }
            Vector3 spawnPoint = new Vector3(37.5f, 12.5f, 0);
            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
            playerTransform = playerInstance.transform;

            Debug.Log($"GameManager: Player instantiated at position ({position.x:F2}, {position.y:F2}, {position.z:F2})");
        }

        /// <summary>
        /// This method is called by the GridManager when the scene grid is ready.
        /// It handles the player spawning at a valid grid location.
        /// </summary>
        public void OnGridReadyFromGridManager()
        {
            Debug.Log("GameManager: Received OnGridReady from GridManager. Proceeding with player setup.");

            // Get the world position of the grid's (0,0) coordinate.
            // This ensures the player always spawns on a valid tile.
            Vector3 spawnPosition = GridManager.Instance.GetWorldPosition(Vector2Int.zero);

            // Spawn the player at the valid grid position.
            SpawnPlayerAtPosition(spawnPosition);

            // Set the game state to playing.
            SetGameState(GameState.Playing);
        }
    }
}