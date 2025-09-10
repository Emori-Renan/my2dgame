using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// This script manages the Cinemachine camera properties, such as setting the follow target and adjusting lens settings.
/// </summary>
public class CinemachinePlayerFollow : MonoBehaviour
{
    // A reference to the CinemachineCamera component on this GameObject.
    private CinemachineCamera cinemachineCamera;

    private void Awake()
    {
        // Get the CinemachineCamera component once when the script starts.
        cinemachineCamera = GetComponent<CinemachineCamera>();
        if (cinemachineCamera == null)
        {
            Debug.LogError("CinemachinePlayerFollow: No CinemachineCamera component found on this GameObject!");
        }

        // Initially disable this script as per the user's request.
        // It will be enabled by the GameManager when the player is ready.
        this.enabled = false;
    }

    /// <summary>
    /// Sets the Cinemachine camera to follow the specified player transform.
    /// This method is designed to be called by the GameManager.
    /// </summary>
    /// <param name="target">The transform of the object to follow.</param>
    public void SetFollowTarget(Transform target)
    {
        if (cinemachineCamera != null)
        {
            // Assign the target to the Follow property.
            cinemachineCamera.Follow = target;
            
            // Set the camera's orthographic size for a good view of the game world.
            cinemachineCamera.Lens.OrthographicSize = 5f;

            // Set the camera's offset to keep it behind the player in Z.
            var followComponent = cinemachineCamera.GetComponent<CinemachineFollow>();
            if (followComponent != null)
            {
                followComponent.FollowOffset = new Vector3(0, 0, -10);
            }

            Debug.Log("CinemachinePlayerFollow: Camera is now following the player.");
        }
    }
}
