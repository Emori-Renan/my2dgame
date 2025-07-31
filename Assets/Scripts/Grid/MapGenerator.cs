using UnityEngine;
using System.Collections.Generic;

// We'll create this class to handle the map generation logic.
public static class MapGenerator
{
    /// <summary>
    /// Generates and returns a 2D array of TileData based on a simple weighted random algorithm.
    /// This method is static, so it can be called without creating an instance of MapGenerator.
    /// </summary>
    public static TileData[,] GenerateWeightedRandomMap(int width, int height)
    {
        // The data structure to hold information about each tile.
        TileData[,] mapGrid = new TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create a new TileData object for this square
                mapGrid[x, y] = new TileData();

                // Simple weighted randomness to determine tile type
                int roll = Random.Range(0, 100);

                if (roll < 80) // 80% chance for Grass
                {
                    mapGrid[x, y].type = TileType.Grass;
                    mapGrid[x, y].isWalkable = true;
                }
                else // 20% chance for Water
                {
                    mapGrid[x, y].type = TileType.Water;
                    mapGrid[x, y].isWalkable = false;
                }
            }
        }

        return mapGrid;
    }
}