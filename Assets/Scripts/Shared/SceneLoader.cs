using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // The name of the scene to load. You set this in the Inspector.
    public string sceneName;

    // This method is called when another object enters the trigger collider.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player.
        // Make sure your player has a "Player" tag.
        if (other.CompareTag("Player"))
        {
            // Load the new scene by name.
            SceneManager.LoadScene(sceneName);
        }
    }
}