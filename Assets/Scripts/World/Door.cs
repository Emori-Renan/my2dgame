using UnityEngine;

namespace MyGame.World
{
    // A simple script to represent a door that can be interacted with.
    [RequireComponent(typeof(SpriteRenderer))]
    public class Door : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private bool isOpen = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Toggles the state of the door between open and closed.
        /// </summary>
        public void ToggleDoor()
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
