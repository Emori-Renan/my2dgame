using UnityEngine;
using MyGame.Managers;

public class DoorGridUpdater : MonoBehaviour
{
    private GridManager gridManager;
    private Vector2Int doorGridPosition;

    void Start()
    {
        gridManager = GridManager.Instance;
        if (gridManager == null)
        {
            Debug.LogError("DoorGridUpdater: GridManager not found.");
            return;
        }

        // Get the grid coordinates of the door's world position.
        doorGridPosition = gridManager.GetGridCoordinates(transform.position);

        // Set the door to be unwalkable at the start.
        gridManager.SetTileWalkability(doorGridPosition, false);
    }

    public void SetWalkable()
    {
        if (gridManager == null) return;
        gridManager.SetTileWalkability(doorGridPosition, true);
    }

    public void SetUnwalkable()
    {
        if (gridManager == null) return;
        gridManager.SetTileWalkability(doorGridPosition, false);
    }
}