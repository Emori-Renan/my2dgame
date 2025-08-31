using UnityEngine;

namespace MyGame.World 
{
    public class Door : MonoBehaviour
    {
        // Public method called by the player script when they press 'E'
        public void Interact()
        {
            // Log a message to confirm the interaction is working
            Debug.Log($"Door '{gameObject.name}': Interact (E) pressed.");
        }
    }
}