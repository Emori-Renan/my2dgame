using UnityEngine;

public class DoorColliderManager : MonoBehaviour
{
    public Collider2D solidCollider;

    void Start()
    {
        if (solidCollider == null)
        {
            Debug.LogError("DoorColliderManager: Solid Collider not assigned.");
            return;
        }

        solidCollider.enabled = true; // Door starts closed and blocks movement.
    }

    public void EnableCollider()
    {
        if (solidCollider != null)
        {
            solidCollider.enabled = true;
        }
    }

    public void DisableCollider()
    {
        if (solidCollider != null)
        {
            solidCollider.enabled = false;
        }
    }
}