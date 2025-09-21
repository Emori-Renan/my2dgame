using System.Threading.Tasks;
using UnityEngine;
using MyGame.Managers;
using MyGame.Core;

namespace MyGame.Player
{
    /// <summary>
    /// This component allows the player to participate in the turn-based system.
    /// It implements the ITurnTaker interface and handles the player's turn logic.
    /// </summary>
    public class PlayerTurnTaker : MonoBehaviour, ITurnTaker
    {
        private TaskCompletionSource<bool> turnCompletionSource;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement component not found. PlayerTurnTaker requires a PlayerMovement script on the same GameObject.");
            }
        }

        private void Start()
        {
            // Register this player with the TurnManager
            TurnManager.Instance.RegisterTurnTaker(this);
        }

        /// <summary>
        /// This method is called by the TurnManager to start the player's turn.
        /// It returns a Task that completes when the player's turn is finished.
        /// </summary>
        public async Task TakeTurn()
        {
            Debug.Log("Player's turn has started. Waiting for player action.");
            
            // Create a TaskCompletionSource to await the player's action
            turnCompletionSource = new TaskCompletionSource<bool>();
            
            // The turn is completed when the player's movement is complete
            await playerMovement.MoveCompletedTask;
            
            // Let the turn manager know the turn is over
            turnCompletionSource.SetResult(true);

            await turnCompletionSource.Task;
        }

        // This method can be called by an input script to end the player's turn after an action
        public void EndTurn()
        {
            turnCompletionSource?.TrySetResult(true);
        }
    }
}
