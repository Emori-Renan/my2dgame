using UnityEngine;
using UnityEngine.SceneManagement;
using MyGame.Core;
using MyGame.Player;
using Unity.Cinemachine;
using System.Collections;
using MyGame.World;

namespace MyGame.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameObject playerPrefab;

        private CinemachineCamera cinemachineCamera;
        private ClosedWorldMovement closedWorldMovementScript;

        public Transform playerTransform { get; private set; }
        public GameState currentGameState { get; private set; } = GameState.Loading;
        public bool IsGameReadyForInput { get; private set; } = false;

        private Vector2Int? loadedGridPosition = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            Debug.Log("GameManager: Singleton instance initialized.");
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "b")
            {
                Debug.Log("GameManager: Scene 'b' loaded. Setting up player and camera...");
                StartCoroutine(SetupPlayerAndCameraRoutine());
            }
        }

        private IEnumerator SetupPlayerAndCameraRoutine()
        {
            yield return null;

            Vector3 spawnPosition = GetPlayerSpawnPosition();
            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerTransform = playerInstance.transform;

            yield return new WaitUntil(() => playerTransform != null);

            closedWorldMovementScript = playerInstance.GetComponent<ClosedWorldMovement>();
            if (closedWorldMovementScript == null)
            {
                Debug.LogWarning("GameManager: Player prefab is missing the ClosedWorldMovement script.");
            }

            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            if (cinemachineCamera != null)
            {
                SetupCameraFollow(playerTransform);
            }
            else
            {
                Debug.LogError("GameManager: CinemachineCamera not found in scene!");
            }

            SetGameState(GameState.Playing);
        }

        public void OnGridReadyFromGridManager()
        {
            Debug.Log("GameManager: Grid is ready.");
        }

        public void SetGameState(GameState newState)
        {
            currentGameState = newState;
            Debug.Log($"GameManager: Game State changed to {currentGameState}");

            bool enablePlayerControl = (newState == GameState.Playing);
            if (closedWorldMovementScript != null)
            {
                closedWorldMovementScript.enabled = enablePlayerControl;
                Debug.Log($"GameManager: ClosedWorldMovement enabled: {closedWorldMovementScript.enabled}");
            }
            else
            {
                Debug.LogWarning("GameManager: ClosedWorldMovement script not assigned!");
            }

            IsGameReadyForInput = enablePlayerControl;
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            if (loadedGridPosition.HasValue && GridManager.Instance.IsPositionValid(loadedGridPosition.Value))
            {
                Vector3 loadedWorldPosition = GridManager.Instance.GetWorldPosition(loadedGridPosition.Value);
                Debug.Log($"GameManager: Player will spawn at saved position: {loadedWorldPosition}");
                loadedGridPosition = null;
                return loadedWorldPosition;
            }
            else
            {
                SpawnPoint defaultSpawnPoint = FindAnyObjectByType<SpawnPoint>();
                if (defaultSpawnPoint != null)
                {
                    Debug.Log($"GameManager: Player will spawn at default spawn point: {defaultSpawnPoint.transform.position}");
                    return defaultSpawnPoint.transform.position;
                }
                else
                {
                    Debug.LogError("GameManager: No SpawnPoint found in the scene! Defaulting to Vector3.zero.");
                    return Vector3.zero;
                }
            }
        }

        public void SetLoadedPosition(Vector2Int gridCoords)
        {
            loadedGridPosition = gridCoords;
        }

        private void SetupCameraFollow(Transform target)
        {
            if (cinemachineCamera == null || target == null)
            {
                Debug.LogWarning("GameManager: Cannot set camera follow — missing references.");
                return;
            }
            cinemachineCamera.Follow = target;
            cinemachineCamera.Lens.OrthographicSize = 5f;
            Debug.Log($"GameManager: Camera now following {target.name}");
        }
        
        
    }
}