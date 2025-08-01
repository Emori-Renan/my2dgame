using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    // A static reference to the GridManager instance (Singleton pattern)
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 40;
    public int height = 40;

    [Header("Tile Prefabs")]
    public GameObject grassPrefab;
    public GameObject waterPrefab;

    // The data structure to hold information about each tile.
    [HideInInspector] // Hides this from the Inspector since we generate it in code
    public TileData[,] mapGrid;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Generate the map data using our MapGenerator
        mapGrid = MapGenerator.GenerateWeightedRandomMap(width, height);
        
        // Use the generated data to instantiate the visual tiles
        InstantiateMapVisuals();
    }

    /// <summary>
    /// This method is only responsible for instantiating the visual prefabs
    /// based on the data provided by the MapGenerator.
    /// </summary>
    public void InstantiateMapVisuals()
    {
        // Parent the tiles under the GridManager for a cleaner Hierarchy
        Transform parent = transform; 
        
        // --- NEW CODE: Calculate the offset to center the grid ---
        int halfWidth = width / 2;
        int halfHeight = height / 2;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = mapGrid[x, y];
                GameObject tilePrefab = GetPrefabForTileType(tile.type);
                
                if (tilePrefab != null)
                {
                    // --- CHANGED LINE: Adjust the position to center the grid ---
                    Vector3 position = new Vector3(x - halfWidth, y - halfHeight, 0);
                    Instantiate(tilePrefab, position, Quaternion.identity, parent);
                }
            }
        }
    }

    /// <summary>
    /// Helper method to get the correct prefab for a given TileType.
    /// </summary>
    private GameObject GetPrefabForTileType(TileType type)
    {
        switch (type)
        {
            case TileType.Grass:
                return grassPrefab;
            case TileType.Water:
                return waterPrefab;
            default:
                return null;
        }
    }

    /// <summary>
    /// Helper method to convert a world position to grid coordinates.
    /// This is useful for getting the tile data from a mouse click.
    /// --- THIS METHOD IS UPDATED TO HANDLE NEGATIVE COORDINATES ---
    /// </summary>
    public Vector2Int GetGridCoordinates(Vector3 worldPosition)
    {
        // --- NEW CODE: Calculate the offset to correctly map negative world coordinates ---
        int halfWidth = width / 2;
        int halfHeight = height / 2;
        
        int x = Mathf.FloorToInt(worldPosition.x + halfWidth);
        int y = Mathf.FloorToInt(worldPosition.y + halfHeight);
        
        return new Vector2Int(x, y);
    }
    
    /// <summary>
    /// Returns the TileData for a specific grid coordinate.
    /// Other scripts will use this to check for walkability.
    /// --- THIS METHOD REMAINS THE SAME, as it uses the internal map array's coordinates ---
    /// </summary>
    public TileData GetTileData(Vector2Int gridPosition)
    {
        // Check for out-of-bounds access
        if (gridPosition.x >= 0 && gridPosition.x < width &&
            gridPosition.y >= 0 && gridPosition.y < height)
        {
            return mapGrid[gridPosition.x, gridPosition.y];
        }
        return null;
    }
}