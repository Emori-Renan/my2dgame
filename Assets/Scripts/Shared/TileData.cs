using UnityEngine;

namespace MyGame.Core
{
    /// <summary>
    /// A simple class to hold the data for a single tile.
    /// This is used to create the mapGrid array in the GridManager.
    /// </summary>
    [System.Serializable]
    public class CustomTileData
    {
        public TileType type;
        public bool isWalkable;
    }
}
