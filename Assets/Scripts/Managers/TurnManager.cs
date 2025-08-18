using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks; // Required for Task
using MyGame.Core; // To access GameState and ITurnTaker

namespace MyGame.Managers
{
    /// <summary>
    /// Manages the turn-based flow of the game.
    /// It cycles through registered ITurnTaker entities, but only operates during combat states.
    /// Optimized for fast, responsive turn processing.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Game State")]
        [Tooltip("The current granular state of the turn manager (e.g., PlayerTurn, EnemyTurn).")]
        public GameState CurrentState = GameState.Playing; // Initial state, will be updated by GameManager

        private List<ITurnTaker> turnTakers = new List<ITurnTaker>();
        private int currentTurnTakerIndex = 0;
        private bool isProcessingTurn = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep the TurnManager alive across scenes
            }
        }

        private void Start()
        {
            // Start a coroutine to wait for GameManager to be ready.
            // This is the most robust way to ensure singletons are initialized across varying execution orders.
            StartCoroutine(InitializeTurnManagerRoutine());
        }

        /// <summary>
        /// Coroutine to wait for the GameManager to be initialized before starting TurnManager logic.
        /// This ensures GameManager.Instance is available before any critical checks.
        /// </summary>
        private IEnumerator InitializeTurnManagerRoutine()
        {
            Debug.Log("TurnManager: Waiting for GameManager to initialize...");
            // Wait until GameManager.Instance is no longer null.
            yield return new WaitUntil(() => GameManager.Instance != null);
            Debug.Log("TurnManager: GameManager found! Initializing turn manager logic.");

            // Now that GameManager is ready, sync initial state.
            CurrentState = GameManager.Instance.currentGameState;

            // TurnManager will now ONLY be started/stopped by GameManager.SetGameStateExternally.
            // It does NOT initiate its routine automatically at Start() to avoid running during exploration.
            // This prevents "No turn takers registered" warnings in non-combat states.
            Debug.Log("TurnManager: Initializing. Will start turn sequence only when GameState transitions to Combat.");
        }

        /// <summary>
        /// Registers an entity that will take turns in the game.
        /// </summary>
        /// <param name="turnTaker">The ITurnTaker entity to register.</param>
        public void RegisterTurnTaker(ITurnTaker turnTaker)
        {
            if (!turnTakers.Contains(turnTaker))
            {
                turnTakers.Add(turnTaker);
                Debug.Log($"TurnManager: Registered new turn taker: {((MonoBehaviour)turnTaker).name}");
            }
        }

        /// <summary>
        /// Deregisters an entity from taking turns.
        /// </summary>
        /// <param name="turnTaker">The ITurnTaker entity to deregister.</param>
        public void DeregisterTurnTaker(ITurnTaker turnTaker)
        {
            if (turnTakers.Contains(turnTaker))
            {
                turnTakers.Remove(turnTaker);
                Debug.Log($"TurnManager: Deregistered turn taker: {((MonoBehaviour)turnTaker).name}");
            }
        }

        /// <summary>
        /// The main coroutine that processes turns for all registered entities.
        /// This routine only runs when the GameState is a Combat state.
        /// It processes turns quickly without artificial delays.
        /// </summary>
        private IEnumerator TurnSequenceRoutine()
        {
            if (isProcessingTurn) yield break; // Prevent re-entry if already running

            isProcessingTurn = true;
            Debug.Log("TurnManager: Turn sequence started for combat.");

            // Loop as long as GameManager indicates we are in a combat state (Player or Enemy turn)
            while (GameManager.Instance != null && 
                   (GameManager.Instance.currentGameState == GameState.CombatPlayerTurn ||
                    GameManager.Instance.currentGameState == GameState.CombatEnemyTurn))
            {
                if (turnTakers.Count == 0)
                {
                    Debug.LogWarning("TurnManager: No turn takers registered for combat. Waiting for entities.");
                    yield return new WaitForSeconds(0.1f); // Short wait to prevent constant logging
                    continue; 
                }

                ITurnTaker currentTaker = turnTakers[currentTurnTakerIndex];
                MonoBehaviour currentTakerMono = (MonoBehaviour)currentTaker;

                // Update granular state based on whose turn it is
                if (currentTakerMono.CompareTag("Player"))
                {
                    CurrentState = GameState.CombatPlayerTurn;
                    Debug.Log($"TurnManager: It's Player's turn! ({currentTakerMono.name})");
                }
                else
                {
                    CurrentState = GameState.CombatEnemyTurn; // Future: handle different mob types
                    Debug.Log($"TurnManager: It's {currentTakerMono.name}'s turn.");
                }

                // If the game is paused, wait until unpaused
                if (GameManager.Instance != null && GameManager.Instance.currentGameState == GameState.Paused)
                {
                    Debug.Log("TurnManager: Game is paused. Combat turn sequence waiting.");
                    yield return new WaitUntil(() => GameManager.Instance.currentGameState != GameState.Paused);
                    Debug.Log("TurnManager: Game unpaused. Resuming combat turn sequence.");
                }

                // Call TakeTurn() on the current entity and wait for it to signal completion.
                // This is the main "blocking" point of a turn.
                Task turnTask = currentTaker.TakeTurn();
                while (!turnTask.IsCompleted)
                {
                    yield return null; // Wait one frame at a time for task completion
                }

                Debug.Log($"TurnManager: {currentTakerMono.name}'s turn finished.");

                // After turn completion, sync the TurnManager's CurrentState with GameManager's global combat state
                CurrentState = GameManager.Instance.currentGameState;
                
                // Move to the next turn taker for the next cycle.
                currentTurnTakerIndex = (currentTurnTakerIndex + 1) % turnTakers.Count;

                // NO artificial WaitForSeconds between turns here.
                // This makes turns feel instant-succession.
            }

            isProcessingTurn = false;
            Debug.Log("TurnManager: Turn sequence stopped (no longer in combat state).");
        }

        /// <summary>
        /// This method is called by the GameManager to update TurnManager's internal state
        /// and to start/stop the turn sequence based on global game state changes.
        /// </summary>
        /// <param name="newState">The new overall game state.</param>
        public void SetGameStateExternally(GameState newState)
        {
            // Only update if there's a real change to avoid unnecessary logs/processing
            if (CurrentState != newState)
            {
                CurrentState = newState;
                Debug.Log($"TurnManager: External game state updated to {newState}.");

                // If entering a combat state and not already processing turns, start the routine
                if ((newState == GameState.CombatPlayerTurn || newState == GameState.CombatEnemyTurn) && !isProcessingTurn)
                {
                    StartCoroutine(TurnSequenceRoutine());
                }
                // If exiting combat state, the 'while' loop in TurnSequenceRoutine will naturally handle stopping itself.
                // No explicit StopCoroutine is needed here unless you want an immediate, hard halt.
            }
        }
    }
}
