using UnityEngine;

// An enum to define the different types of tiles
public enum TileType { Grass, Water, Forest, Mountain }

// A simple class to hold the data for a single tile
[System.Serializable] // Makes it visible in the Inspector (optional but good practice)
public class TileData
{
    public TileType type;
    public bool isWalkable;
    // We could add more properties here as the game evolves, e.g.,
    // public int movementCost;
    // public string biomeName;
}