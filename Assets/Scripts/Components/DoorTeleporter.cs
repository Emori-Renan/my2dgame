using UnityEngine;

public class DoorTeleporter : MonoBehaviour
{
    public Transform destinationDoor;

    public void TeleportPlayer(Transform playerTransform)
    {
        if (destinationDoor != null)
        {
            playerTransform.position = destinationDoor.position;
        }
    }
}