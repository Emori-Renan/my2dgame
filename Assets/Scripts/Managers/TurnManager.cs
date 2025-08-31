using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using MyGame.Core;

namespace MyGame.Managers
{
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Game State")]
        [Tooltip("The current granular state of the turn manager (e.g., PlayerTurn, EnemyTurn).")]
        public GameState CurrentState = GameState.Playing;

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
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(InitializeTurnManagerRoutine());
        }

        private IEnumerator InitializeTurnManagerRoutine()
        {
            yield return new WaitUntil(() => GameManager.Instance != null);
            CurrentState = GameManager.Instance.currentGameState;
        }

        public void RegisterTurnTaker(ITurnTaker turnTaker)
        {
            if (!turnTakers.Contains(turnTaker))
            {
                turnTakers.Add(turnTaker);
                Debug.Log($"TurnManager: Registered new turn taker: {((MonoBehaviour)turnTaker).name}");
            }
        }

        public void DeregisterTurnTaker(ITurnTaker turnTaker)
        {
            if (turnTakers.Contains(turnTaker))
            {
                turnTakers.Remove(turnTaker);
                Debug.Log($"TurnManager: Deregistered turn taker: {((MonoBehaviour)turnTaker).name}");
            }
        }

        private IEnumerator TurnSequenceRoutine()
        {
            if (isProcessingTurn) yield break;

            isProcessingTurn = true;

            while (GameManager.Instance != null &&
                   (GameManager.Instance.currentGameState == GameState.CombatPlayerTurn ||
                    GameManager.Instance.currentGameState == GameState.CombatEnemyTurn))
            {
                if (turnTakers.Count == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                ITurnTaker currentTaker = turnTakers[currentTurnTakerIndex];
                MonoBehaviour currentTakerMono = (MonoBehaviour)currentTaker;

                if (currentTakerMono.CompareTag("Player"))
                {
                    CurrentState = GameState.CombatPlayerTurn;
                }
                else
                {
                    CurrentState = GameState.CombatEnemyTurn;
                }

                if (GameManager.Instance != null && GameManager.Instance.currentGameState == GameState.Paused)
                {
                    yield return new WaitUntil(() => GameManager.Instance.currentGameState != GameState.Paused);
                }

                Task turnTask = currentTaker.TakeTurn();
                while (!turnTask.IsCompleted)
                {
                    yield return null;
                }

                CurrentState = GameManager.Instance.currentGameState;
                currentTurnTakerIndex = (currentTurnTakerIndex + 1) % turnTakers.Count;
            }

            isProcessingTurn = false;
        }

        public void SetGameStateExternally(GameState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;

                if ((newState == GameState.CombatPlayerTurn || newState == GameState.CombatEnemyTurn) && !isProcessingTurn)
                {
                    StartCoroutine(TurnSequenceRoutine());
                }
            }
        }
    }
}