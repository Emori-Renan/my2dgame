using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using MyGame.Core;

namespace MyGame.Managers
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Core Grid References")]
        [SerializeField] private Grid mainGameGrid;

        [Header("Tilemaps")]
        [SerializeField] private List<Tilemap> walkableTilemaps;
        [SerializeField] private List<Tilemap> nonWalkableTilemaps;

        private Dictionary<string, TileType> tileKeywordMapping;
        private Dictionary<TileBase, TileType> tileMappings;
        private CustomTileData[,] mapGrid;
        private int width;
        private int height;
        private BoundsInt bounds;

        public Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }

            tileMappings = new Dictionary<TileBase, TileType>();
            tileKeywordMapping = new Dictionary<string, TileType>
            {
                { "grass", TileType.Grass },
                { "dirt", TileType.Dirt },
                { "water", TileType.Water },
                { "rock", TileType.Road },
                { "street", TileType.Road },
                { "cobble", TileType.Road },
            };
        }

        void Start()
        {
            if (mainGameGrid == null)
            {
                Debug.LogError("GridManager: 'Main Game Grid' is not assigned!");
                return;
            }

            BuildTileMappings();
            LoadMapFromTilemaps();
        }

        private void BuildTileMappings()
        {
            tileMappings.Clear();

            foreach (var tilemap in walkableTilemaps)
            {
                if (tilemap == null) continue;
                tilemap.CompressBounds();
                foreach (var pos in tilemap.cellBounds.allPositionsWithin)
                {
                    TileBase tile = tilemap.GetTile(pos);
                    if (tile != null && !tileMappings.ContainsKey(tile))
                    {
                        TileType parsedType = ParseTileTypeFromName(tile.name);
                        tileMappings[tile] = parsedType;
                    }
                }
            }

            foreach (var tilemap in nonWalkableTilemaps)
            {
                if (tilemap == null) continue;
                tilemap.CompressBounds();
                foreach (var pos in tilemap.cellBounds.allPositionsWithin)
                {
                    TileBase tile = tilemap.GetTile(pos);
                    if (tile != null && !tileMappings.ContainsKey(tile))
                    {
                        TileType parsedType = ParseTileTypeFromName(tile.name);
                        tileMappings[tile] = parsedType;
                    }
                }
            }
        }

        private TileType ParseTileTypeFromName(string tileName)
        {
            string lowerName = tileName.ToLower();
            foreach (var entry in tileKeywordMapping)
            {
                if (lowerName.Contains(entry.Key))
                {
                    return entry.Value;
                }
            }
            return TileType.Undefined;
        }

        public Grid GetMainGameGrid()
        {
            return mainGameGrid;
        }

        public BoundsInt GetMapGridBounds()
        {
            if (walkableTilemaps != null && walkableTilemaps.Count > 0 && walkableTilemaps[0] != null)
            {
                walkableTilemaps[0].CompressBounds();
                return walkableTilemaps[0].cellBounds;
            }
            Debug.LogWarning("GridManager: No valid walkable Tilemap found to derive map bounds from. Returning default bounds.");
            return new BoundsInt(0, 0, 0, 100, 100, 1);
        }

        public Vector3Int GetPlayerCellPosition()
        {
            if (playerTransform != null && mainGameGrid != null)
            {
                return mainGameGrid.WorldToCell(playerTransform.position);
            }
            Debug.LogWarning("GridManager: Player Transform or Main Game Grid is null. Cannot get player cell position. Returning Vector3Int.zero.");
            return Vector3Int.zero;
        }

        private void LoadMapFromTilemaps()
        {
            if (walkableTilemaps == null || walkableTilemaps.Count == 0 || walkableTilemaps[0] == null)
            {
                Debug.LogError("GridManager: No valid walkable Tilemaps assigned! Map grid will not be initialized.");
                return;
            }

            walkableTilemaps[0].CompressBounds();
            bounds = walkableTilemaps[0].cellBounds;
            width = bounds.size.x;
            height = bounds.size.y;

            mapGrid = new CustomTileData[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int cellPosition = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                    CustomTileData tileData = new CustomTileData();
                    tileData.isWalkable = false;
                    tileData.type = TileType.Undefined;
                    bool foundAnyWalkableTilemapTile = false;

                    foreach (var tilemap in walkableTilemaps)
                    {
                        if (tilemap != null && tilemap.HasTile(cellPosition))
                        {
                            tileData.isWalkable = true;
                            tileData.type = GetTileTypeFromTileBase(tilemap.GetTile(cellPosition));
                            foundAnyWalkableTilemapTile = true;
                            break;
                        }
                    }

                    if (foundAnyWalkableTilemapTile)
                    {
                        foreach (var tilemap in nonWalkableTilemaps)
                        {
                            if (tilemap != null && tilemap.HasTile(cellPosition))
                            {
                                tileData.isWalkable = false;
                                tileData.type = GetTileTypeFromTileBase(tilemap.GetTile(cellPosition));
                                break;
                            }
                        }
                    }
                    mapGrid[x, y] = tileData;
                }
            }
        }

        public Vector2Int GetGridCoordinates(Vector3 worldPosition)
        {
            if (mainGameGrid == null)
            {
                Debug.LogError("GridManager: 'Main Game Grid' is null when trying to get grid coordinates!");
                return Vector2Int.zero;
            }

            Vector3Int cellPosition = mainGameGrid.WorldToCell(worldPosition);
            int gridX = cellPosition.x - bounds.xMin;
            int gridY = cellPosition.y - bounds.yMin;

            return new Vector2Int(gridX, gridY);
        }

        public CustomTileData GetTileData(Vector2Int gridPosition)
        {
            if (mapGrid == null)
            {
                Debug.LogError("GridManager: Map grid is not initialized. Cannot retrieve tile data.");
                return new CustomTileData { type = TileType.Undefined, isWalkable = false };
            }

            if (gridPosition.x >= 0 && gridPosition.x < width &&
                gridPosition.y >= 0 && gridPosition.y < height)
            {
                return mapGrid[gridPosition.x, gridPosition.y];
            }

            return new CustomTileData { type = TileType.Undefined, isWalkable = false };
        }

        public bool IsWalkable(Vector2Int gridPosition)
        {
            CustomTileData tile = GetTileData(gridPosition);
            return tile.isWalkable;
        }

        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            if (mainGameGrid == null)
            {
                Debug.LogError("GridManager: 'Main Game Grid' is not assigned! Cannot get world position.");
                return Vector3.zero;
            }

            Vector3Int cellPosition = new Vector3Int(gridPosition.x + bounds.xMin, gridPosition.y + bounds.yMin, 0);
            Vector3 worldPosition = mainGameGrid.CellToWorld(cellPosition);

            return worldPosition;
        }

        private TileType GetTileTypeFromTileBase(TileBase tileBase)
        {
            if (tileBase == null) return TileType.Undefined;
            if (tileMappings.TryGetValue(tileBase, out TileType type))
            {
                return type;
            }

            Debug.LogWarning($"GridManager: No TileType mapping found for TileBase: {tileBase.name}. Returning Undefined.");
            return TileType.Undefined;
        }
    }
}