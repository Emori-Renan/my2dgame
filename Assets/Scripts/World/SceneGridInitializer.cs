using UnityEngine;
using UnityEngine.Tilemaps;
using MyGame.Managers;
using MyGame.Core;
using System.Collections.Generic;
using System.Linq;

namespace MyGame.World
{
    public class SceneGridInitializer : MonoBehaviour
    {
        [Header("Scene Grid References")]
        [Tooltip("Drag the Unity Grid GameObject for this scene here.")]
        [SerializeField] private Grid sceneUnityGrid;

        [Tooltip("List of Tilemaps containing walkable tiles (e.g., 'Ground', 'Floor').")]
        [SerializeField] private List<Tilemap> walkableTilemaps;

        // Removed nonWalkableTilemaps for simplicity
        // [Tooltip("List of Tilemaps containing non-walkable tiles (e.g., 'Walls', 'Obstacles').")]
        // [SerializeField] private List<Tilemap> nonWalkableTilemaps;

        private Dictionary<TileBase, TileType> tileTypeMappings = new Dictionary<TileBase, TileType>();
        private Dictionary<string, TileType> tileKeywordMapping = new Dictionary<string, TileType>();

        private void Awake()
        {
            tileKeywordMapping = new Dictionary<string, TileType>
            {
                { "grass", TileType.Grass },
                { "dirt", TileType.Dirt },
                { "floor", TileType.Floor }
                // Removed other specific tile type mappings for simplicity
            };
        }

        private void OnEnable()
        {
            // REMOVED: No longer attempt to set GridManager.Instance.IsGridDataInitialized = false;
            // This flag is managed internally by GridManager.
        }

        private void Start()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogError($"SceneGridInitializer on {gameObject.scene.name}: GridManager.Instance is null. Ensure GridManager is initialized and persistent, and check Script Execution Order.");
                this.enabled = false;
                return;
            }

            if (sceneUnityGrid == null)
            {
                Debug.LogError($"SceneGridInitializer on {gameObject.name}: 'Scene Unity Grid' is not assigned! Cannot initialize scene grid. Please assign the Unity Grid GameObject in the Inspector.");
                this.enabled = false;
                return;
            }
            if (walkableTilemaps == null || walkableTilemaps.Count == 0)
            {
                Debug.LogWarning($"SceneGridInitializer on {gameObject.name}: No walkable Tilemaps assigned. Grid might be entirely empty. Ensure tilemaps are assigned.");
            }

            Debug.Log($"SceneGridInitializer: Starting grid initialization for scene '{gameObject.scene.name}'...");

            BoundsInt combinedBounds = GetCombinedTilemapBounds();
            Debug.Log($"SceneGridInitializer: Combined bounds for '{gameObject.scene.name}': {combinedBounds}");

            if (combinedBounds.size.x <= 0 || combinedBounds.size.y <= 0)
            {
                Debug.LogError($"SceneGridInitializer on {gameObject.name}: Combined Tilemap bounds are zero or invalid ({combinedBounds.size.x}x{combinedBounds.size.y}). Cannot initialize grid. Check if tilemaps are assigned and contain tiles.");
                this.enabled = false;
                return;
            }

            CustomTileData[,] initialMapGrid = new CustomTileData[combinedBounds.size.x, combinedBounds.size.y];
            
            BuildTileTypeMappings();

            InitializeWalkableTiles(initialMapGrid, combinedBounds);
            // Removed ApplyNonWalkableTiles for simplicity

            // Pass the fully built custom grid to the GridManager, which then sets its own IsGridDataInitialized flag.
            gridManager.InitializeSceneGrid(combinedBounds, initialMapGrid);

            Debug.Log($"SceneGridInitializer: Grid initialization complete for scene '{gameObject.scene.name}'. Final Dimensions: {combinedBounds.size.x}x{combinedBounds.size.y}.");
        }

        private BoundsInt GetCombinedTilemapBounds()
        {
            BoundsInt combined = new BoundsInt();
            bool firstBound = true;

            IEnumerable<Tilemap> allTilemaps = (walkableTilemaps ?? Enumerable.Empty<Tilemap>())
                                                .Where(tm => tm != null);

            Debug.Log("--- Debugging Individual Tilemap Bounds ---");
            foreach (var tm in allTilemaps)
            {
                tm.CompressBounds();
                Debug.Log($"Tilemap: {tm.name}, Cell Bounds: {tm.cellBounds}");
                if (firstBound)
                {
                    combined = tm.cellBounds;
                    firstBound = false;
                }
                else
                {
                    combined.xMin = Mathf.Min(combined.xMin, tm.cellBounds.xMin);
                    combined.yMin = Mathf.Min(combined.yMin, tm.cellBounds.yMin);
                    combined.xMax = Mathf.Max(combined.xMax, tm.cellBounds.xMax);
                    combined.yMax = Mathf.Max(combined.yMax, tm.cellBounds.yMax);
                }
            }
            Debug.Log("--- End Individual Tilemap Bounds Debug ---");

            return combined;
        }

        private void BuildTileTypeMappings()
        {
            tileTypeMappings.Clear();

            foreach (var tilemap in (walkableTilemaps ?? Enumerable.Empty<Tilemap>()).Where(tm => tm != null))
            {
                foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
                {
                    TileBase tile = tilemap.GetTile(pos);
                    if (tile != null && !tileTypeMappings.ContainsKey(tile))
                    {
                        tileTypeMappings[tile] = ParseTileTypeFromName(tile.name);
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

        private void InitializeWalkableTiles(CustomTileData[,] mapGrid, BoundsInt bounds)
        {
            foreach (var tilemap in (walkableTilemaps ?? Enumerable.Empty<Tilemap>()).Where(tm => tm != null))
            {
                foreach (Vector3Int cellPosition in tilemap.cellBounds.allPositionsWithin)
                {
                    TileBase tile = tilemap.GetTile(cellPosition);
                    if (tile != null)
                    {
                        Vector2Int gridCoords = new Vector2Int(cellPosition.x - bounds.xMin, cellPosition.y - bounds.yMin);
                        if (gridCoords.x >= 0 && gridCoords.x < mapGrid.GetLength(0) &&
                            gridCoords.y >= 0 && gridCoords.y < mapGrid.GetLength(1))
                        {
                            mapGrid[gridCoords.x, gridCoords.y] = new CustomTileData
                            {
                                type = GetTileTypeFromTileBase(tile),
                                isWalkable = true
                            };
                        }
                    }
                }
            }
        }

        private TileType GetTileTypeFromTileBase(TileBase tileBase)
        {
            if (tileBase == null) return TileType.Undefined;
            if (tileTypeMappings.TryGetValue(tileBase, out TileType type))
            {
                return type;
            }
            return ParseTileTypeFromName(tileBase.name);
        }
    }
}
