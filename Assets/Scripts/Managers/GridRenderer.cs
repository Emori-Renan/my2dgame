using UnityEngine;
using MyGame.Managers;
using Unity.Mathematics;
using UnityEngine.U2D;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace MyGame.Managers
{
    public class GridRenderer : MonoBehaviour
    {
        private GridManager gridManager;
        
        private bool isLineVisible = false;
        private Material lineMaterial;
        private readonly List<LineRenderer> aimingLines = new List<LineRenderer>();

        [Header("Aiming Line Settings")]
        public int aimingLineLength = 3;
        [Range(-1.0f, 1.0f)]
        public float pixelNudgeOffset = 0.0f;

        private void Awake()
        {
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Unlit/Color"));
                lineMaterial.color = Color.white;
            }
        }

        private void Start()
        {
            gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogError("GridRenderer: No GridManager instance found. Aiming line will not function.");
                return;
            }

            for (int i = 0; i < aimingLineLength; i++)
            {
                GameObject lineGO = new GameObject("AimingLine_" + i);
                lineGO.transform.SetParent(this.transform);
                lineGO.transform.localPosition = Vector3.zero;
                LineRenderer lr = lineGO.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lr);
                aimingLines.Add(lr);
            }

            SetLineVisibility(false);
        }

        private void ConfigureLineRenderer(LineRenderer lr)
        {
            lr.material = lineMaterial;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 5;
            lr.loop = true;
            lr.useWorldSpace = true;
            lr.textureMode = LineTextureMode.Stretch;
        }

        public void ToggleAimingLine(bool isVisible)
        {
            SetLineVisibility(isVisible);
        }

        private void SetLineVisibility(bool isVisible)
        {
            isLineVisible = isVisible;
            foreach (var lr in aimingLines)
            {
                if (lr != null)
                {
                    lr.enabled = isVisible;
                }
            }
        }

        private void Update()
        {
            if (isLineVisible)
            {
                DrawAimingPath();
            }
        }

        private void DrawAimingPath()
        {
            if (gridManager == null || Camera.main == null)
            {
                Debug.LogError("DrawAimingPath: Missing critical references.");
                return;
            }
            
            Grid mainGrid = gridManager.GetMainGameGrid();
            Vector3Int playerCell = gridManager.GetPlayerCellPosition();
            Vector3 mouseScreenPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            Vector3Int mouseCell = mainGrid.WorldToCell(mouseWorldPos);

            Vector3Int rawDirection = mouseCell - playerCell;
            Vector3Int normalizedDirection = Vector3Int.zero;
            if (rawDirection.magnitude > 0)
            {
                int signX = (rawDirection.x == 0) ? 0 : (int)math.sign(rawDirection.x);
                int signY = (rawDirection.y == 0) ? 0 : (int)math.sign(rawDirection.y);
                if (Mathf.Abs(rawDirection.x) > Mathf.Abs(rawDirection.y))
                {
                    normalizedDirection.x = signX;
                    if (Mathf.Abs(rawDirection.y) * 2 > Mathf.Abs(rawDirection.x))
                    {
                        normalizedDirection.y = signY;
                    }
                }
                else if (Mathf.Abs(rawDirection.y) > 0)
                {
                    normalizedDirection.y = signY;
                    if (Mathf.Abs(rawDirection.x) * 2 > Mathf.Abs(rawDirection.y))
                    {
                        normalizedDirection.x = signX;
                    }
                }
                else if (rawDirection.x != 0 && rawDirection.y == 0)
                {
                    normalizedDirection.x = signX;
                }
                else if (rawDirection.y != 0 && rawDirection.x == 0)
                {
                    normalizedDirection.y = signY;
                }
            }

            foreach (var lr in aimingLines)
            {
                lr.positionCount = 0;
            }

            if (normalizedDirection == Vector3Int.zero)
            {
                return;
            }

            for (int i = 0; i < aimingLineLength; i++)
            {
                Vector3Int pathCell = playerCell + normalizedDirection * (i + 1);
                
                Vector2Int gridCoords = gridManager.GetGridCoordinates(mainGrid.CellToWorld(pathCell));
                if (gridManager.GetTileData(gridCoords).type == MyGame.Core.TileType.Undefined)
                {
                    break;
                }
                
                DrawCellOutline(aimingLines[i], mainGrid, pathCell);
            }
        }

        private void DrawCellOutline(LineRenderer lr, Grid mainGrid, Vector3Int cellPosition)
        {
            if (lr == null) return;
            
            Vector3 cellWorldCenter = mainGrid.CellToWorld(cellPosition) + mainGrid.cellSize / 2.0f;
            Vector3 halfCellSize = mainGrid.cellSize / 2f;
            
            float finalNudge = 0;
            PixelPerfectCamera ppc = Camera.main.GetComponent<PixelPerfectCamera>();
            if (ppc != null)
            {
                finalNudge = pixelNudgeOffset / ppc.assetsPPU;
            }
            Vector3 nudge = new Vector3(finalNudge, finalNudge, 0);

            Vector3 bl = cellWorldCenter - halfCellSize + nudge;
            Vector3 br = new Vector3(cellWorldCenter.x + halfCellSize.x, cellWorldCenter.y - halfCellSize.y, 0) + nudge;
            Vector3 tr = cellWorldCenter + halfCellSize + nudge;
            Vector3 tl = new Vector3(cellWorldCenter.x - halfCellSize.x, cellWorldCenter.y + halfCellSize.y, 0) + nudge;
            
            Vector3[] points = new Vector3[5];
            points[0] = bl;
            points[1] = br;
            points[2] = tr;
            points[3] = tl;
            points[4] = bl;
            
            lr.positionCount = points.Length;
            lr.SetPositions(points);
        }
    }
}