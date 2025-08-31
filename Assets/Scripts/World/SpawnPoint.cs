using UnityEngine;

namespace MyGame.World
{
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("A unique ID for this spawn point. The GameManager will look for this ID.")]
        public string spawnId = "default";

        // You might want to visualize this in the Editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawIcon(transform.position, "d_TransformTool", true); // Unity icon for transform
        }
    }
}
