// File: PlayerInteractor.cs
using UnityEngine;
using MyGame.Managers;
using MyGame.Core; // Important: include this using statement
using MyGame.World;

namespace MyGame.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask interactableLayer;
        private PlayerAnimator playerAnimator;
        private GridManager gridManager;

        private void Awake()
        {
            playerAnimator = GetComponent<PlayerAnimator>();
            gridManager = GridManager.Instance;

            if (playerAnimator == null)
            {
                Debug.LogError("PlayerInteractor requires a PlayerAnimator component on the same GameObject.");
            }
        }

        public void AttemptInteraction()
        {
            if (!IsGameReadyForInteraction())
            {
                return;
            }

            Vector2 lastDirection = playerAnimator.GetLastDirection();
            Vector2Int currentGridPos = gridManager.GetGridCoordinates(transform.position);
            Vector2Int targetGridPos = currentGridPos + new Vector2Int(Mathf.RoundToInt(lastDirection.x), Mathf.RoundToInt(lastDirection.y));
            Vector3 targetWorldPosition = gridManager.GetWorldPosition(targetGridPos);
            
            Collider2D[] colliders = Physics2D.OverlapPointAll(targetWorldPosition, interactableLayer);
            
            foreach (Collider2D collider in colliders)
            {
                // Get the component using the IInteractable interface
                IInteractable interactable = collider.GetComponent<IInteractable>();
                
                if (interactable != null)
                {
                    // Call the generic Interact() method on any object that implements the interface
                    interactable.Interact();
                    return; // Return after the first successful interaction
                }
            }
        }
        
        private bool IsGameReadyForInteraction()
        {
            return GameManager.Instance != null && GameManager.Instance.IsGameReadyForInput;
        }
    }
}