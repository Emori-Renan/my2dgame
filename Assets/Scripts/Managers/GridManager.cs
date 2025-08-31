using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using MyGame.Core;

namespace MyGame.Managers
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        public static event Action OnGridReady;

        [Header("References")]
        [Tooltip("The main Unity Grid component that contains all your Tilemaps. Assign this in the Inspector.")]
        [SerializeField] private Grid mainUnityGrid;

        private BoundsInt _mapGridBounds;
        private CustomTileData[,] _sceneMapGridData;

        public bool IsGridDataInitialized { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(transform.root.gameObject);
            }
            else
            {
                Destroy(transform.root.gameObject);
                return;
            }
            Debug.Log("GridManager: Initialized singleton in Awake.");
        }

        public void InitializeSceneGrid(BoundsInt bounds, CustomTileData[,] initialMapGrid)
        {
            if (mainUnityGrid == null)
            {
                Debug.LogError("GridManager: Main Unity Grid is not assigned in the Inspector! Cannot initialize scene grid.");
                IsGridDataInitialized = false;
                return;
            }

            _mapGridBounds = bounds;
            _sceneMapGridData = initialMapGrid;
            IsGridDataInitialized = true;

            Debug.Log($"GridManager: Scene grid data initialized. Bounds: {_mapGridBounds}, Dimensions: {_sceneMapGridData.GetLength(0)}x{_sceneMapGridData.GetLength(1)}.");

            OnGridReady?.Invoke();
            GameManager.Instance?.OnGridReadyFromGridManager();
        }

        public Grid GetMainGameGrid()
        {
            if (mainUnityGrid == null)
            {
                Debug.LogError("GridManager: Request for Main Unity Grid, but it's not assigned! Please assign it in the Inspector.");
            }
            return mainUnityGrid;
        }

        public BoundsInt GetMapGridBounds()
        {
            return _mapGridBounds;
        }

        public Vector2Int GetGridCoordinates(Vector3 worldPosition)
        {
            if (mainUnityGrid == null)
            {
                Debug.LogError("GridManager: Main Unity Grid is null when calling GetGridCoordinates.");
                return Vector2Int.zero;
            }
            Vector3Int cellPos = mainUnityGrid.WorldToCell(worldPosition);
            return new Vector2Int(cellPos.x - _mapGridBounds.xMin, cellPos.y - _mapGridBounds.yMin);
        }

        public Vector3 GetWorldPosition(Vector2Int gridCoords)
        {
            if (mainUnityGrid == null)
            {
                Debug.LogError("GridManager: Main Unity Grid is null when calling GetWorldPosition.");
                return Vector3.zero;
            }
            Vector3Int cellPos = new Vector3Int(gridCoords.x + _mapGridBounds.xMin, gridCoords.y + _mapGridBounds.yMin, 0);
            return mainUnityGrid.CellToWorld(cellPos) + mainUnityGrid.cellSize / 2f;
        }

        public bool IsWalkable(Vector2Int gridCoords)
        {
            if (mainUnityGrid == null)
            {
                Debug.LogWarning("GridManager: sceneGrid is not initialized when checking IsWalkable.");
                return false;
            }
            if (!IsGridDataInitialized)
            {
                Debug.LogWarning("GridManager: Grid data not initialized. All positions considered not walkable.");
                return false;
            }
            if (gridCoords.x >= 0 && gridCoords.x < _sceneMapGridData.GetLength(0) &&
                gridCoords.y >= 0 && gridCoords.y < _sceneMapGridData.GetLength(1))
            {
                return _sceneMapGridData[gridCoords.x, gridCoords.y].isWalkable;
            }
            return false;
        }

        public bool IsPositionValid(Vector2Int gridCoords)
        {
            if (!IsGridDataInitialized)
            {
                Debug.LogWarning("GridManager: Grid data not initialized. Positions cannot be validated.");
                return false;
            }
            return gridCoords.x >= 0 && gridCoords.x < _sceneMapGridData.GetLength(0) &&
                   gridCoords.y >= 0 && gridCoords.y < _sceneMapGridData.GetLength(1);
        }

        public void SetTileWalkability(Vector2Int gridCoords, bool isWalkable)
        {
            if (!IsGridDataInitialized)
            {
                Debug.LogWarning("GridManager: Grid data not initialized. Cannot set tile walkability.");
                return;
            }
            if (gridCoords.x >= 0 && gridCoords.x < _sceneMapGridData.GetLength(0) &&
                gridCoords.y >= 0 && gridCoords.y < _sceneMapGridData.GetLength(1))
            {
                _sceneMapGridData[gridCoords.x, gridCoords.y].isWalkable = isWalkable;
            }
            else
            {
                Debug.LogWarning($"GridManager: Attempted to set walkability for out-of-bounds gridCoords: {gridCoords}");
            }
        }

        public CustomTileData GetTileData(Vector2Int gridCoords)
        {
            if (!IsGridDataInitialized)
            {
                Debug.LogWarning("GridManager: Grid data not initialized. Returning default tile data.");
                return new CustomTileData { type = TileType.Undefined, isWalkable = false };
            }
            if (gridCoords.x >= 0 && gridCoords.x < _sceneMapGridData.GetLength(0) &&
                gridCoords.y >= 0 && gridCoords.y < _sceneMapGridData.GetLength(1))
            {
                return _sceneMapGridData[gridCoords.x, gridCoords.y];
            }
            return new CustomTileData { type = TileType.Undefined, isWalkable = false };
        }
    }
}
