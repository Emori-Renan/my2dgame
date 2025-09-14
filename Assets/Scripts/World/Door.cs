using UnityEngine;
using MyGame.Core; // Import the namespace where IInteractable is defined.

namespace MyGame.World
{
    // A simple script to represent a door that can be interacted with.
    [RequireComponent(typeof(SpriteRenderer))]
    public class Door : MonoBehaviour, IInteractable // Implement the IInteractable interface.
    {
        private SpriteRenderer spriteRenderer;
        private bool isOpen = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// This method is called by the PlayerInteractor when the door is interacted with.
        /// </summary>
        public void Interact() // Rename the method to match the interface's contract.
        {
            isOpen = !isOpen;
            if (isOpen)
            {
                Debug.Log("Door: Opening door.");
                spriteRenderer.color = Color.green; // Visual cue for "open"
            }
            else
            {
                Debug.Log("Door: Closing door.");
                spriteRenderer.color = Color.white; // Visual cue for "closed"
            }
        }
    }
}