using System.Collections;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Pathfinding;
using MyGame.Core;
using UnityEngine;
using MyGame.World;

namespace MyGame.Player
{
    public class ClosedWorldMovement : MonoBehaviour
    {
        private GameManager gameManager;
        private GridManager gridManager;
        private Grid sceneGrid;

        private PlayerMovement playerMovement;

        public event System.Action<bool> OnGridToggleChanged;

        private void Awake()
        {
            gameManager = GameManager.Instance;
            gridManager = GridManager.Instance;
            playerMovement = GetComponent<PlayerMovement>();

            if (gameManager == null) Debug.LogError("GameManager not found.");
            if (playerMovement == null) Debug.LogError("PlayerMovement not found on this GameObject.");
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
        }
        
        public void OnSceneLoaded(Vector3 initialPosition)
        {
            if (gameManager != null)
            {
                transform.position = initialPosition;
                Debug.Log($"Player instantiated and placed at: {transform.position}");
            }
        }

        public void OnMouseClick(Vector3 worldPosition)
        {
            if (!IsGameReadyForMovement() || IsGridToggled()) return;

            Vector2Int startGridPos = gridManager.GetGridCoordinates(transform.position);
            Vector2Int targetGridPos = gridManager.GetGridCoordinates(worldPosition);

            if (!gridManager.IsPositionValid(startGridPos) || !gridManager.IsPositionValid(targetGridPos))
            {
                Debug.LogWarning("Invalid start or target grid position. Cannot move.");
                return;
            }

            playerMovement.StartMoveToPath(startGridPos, targetGridPos);
        }

        public void ToggleGrid()
        {
            // playerMovement.StopAllMovement();
            OnGridToggleChanged?.Invoke(IsGridToggled());
        }

        private bool IsGameReadyForMovement()
        {
            return gameManager != null && gameManager.IsGameReadyForInput && gameManager.currentGameState == GameState.Playing &&
                   gridManager != null && gridManager.IsGridDataInitialized && sceneGrid != null;
        }

        public bool IsGridToggled()
        {
            return false;
        }

        private void OnGridReady()
        {
            sceneGrid = gridManager.GetMainGameGrid();
            if (sceneGrid == null)
            {
                Debug.LogError("Main Game Grid from GridManager is null!");
                return;
            }
            Debug.Log("Player is ready for movement.");
        }
    }
}