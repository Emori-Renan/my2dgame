using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using MyGame.Managers;

namespace MyGame.Managers
{
    public class InitialSceneLoader : MonoBehaviour
    {
        [Tooltip("The name of the first actual gameplay scene to load.")]
        public string firstGameplaySceneName = "NewGameScene"; // Changed default name

        private void Start()
        {
            // Give managers a frame to finish their Awake/Start calls
            StartCoroutine(LoadGameplaySceneAsyncRoutine());
        }

        private IEnumerator LoadGameplaySceneAsyncRoutine()
        {
            yield return null; // Wait one frame for managers to Awake/Start

            if (string.IsNullOrEmpty(firstGameplaySceneName))
            {
                Debug.LogError("InitialSceneLoader: 'First Gameplay Scene Name' is not set! Cannot load scene.");
                yield break;
            }

            Debug.Log($"InitialSceneLoader: Loading gameplay scene '{firstGameplaySceneName}' directly.");

            // Directly load the gameplay scene (no additive loading or progress bar for this simple setup)
            SceneManager.LoadScene(firstGameplaySceneName);

            // This script's job is done; it can destroy itself.
            Destroy(this.gameObject);
        }
    }
}
