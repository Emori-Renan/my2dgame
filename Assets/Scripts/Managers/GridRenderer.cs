using UnityEngine;
using MyGame.Managers;
using MyGame.Core;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using MyGame.Player;
using UnityEngine.Rendering.Universal;

namespace MyGame.Managers
{
    public class GridRenderer : MonoBehaviour
    {
        private GridManager gridManager;
        private ClosedWorldMovement playerMovement;

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
                this.enabled = false;
                return;
            }
            
            StartCoroutine(InitializeGridRenderer());
            SetLineVisibility(false);
        }

        private IEnumerator InitializeGridRenderer()
        {
            // Wait until GameManager has spawned the player
            yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.playerTransform != null);

            // Get the player's movement script from the spawned player object
            playerMovement = GameManager.Instance.playerTransform.GetComponent<ClosedWorldMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("GridRenderer: Player (ClosedWorldMovement) component not found on the spawned player object.");
                yield break; // Stop the coroutine if no player movement script is found
            }
            Debug.Log("GridRenderer: Player found. Aiming line will now function correctly.");

            // Now that we have the player reference, initialize the line renderers
            for (int i = 0; i < aimingLineLength; i++)
            {
                GameObject lineGO = new GameObject("AimingLine_" + i);
                lineGO.transform.SetParent(this.transform);
                lineGO.transform.localPosition = Vector3.zero;
                LineRenderer lr = lineGO.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lr);
                aimingLines.Add(lr);
            }
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
            lr.sortingLayerName = "Foreground";
            lr.sortingOrder = 100;
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
            if (isLineVisible && playerMovement != null && gridManager != null && gridManager.IsGridDataInitialized)
            {
                DrawAimingPath();
            }
            else if (!isLineVisible)
            {
                SetLineVisibility(false);
            }
        }

        private void DrawAimingPath()
        {
            if (gridManager == null || Camera.main == null || playerMovement == null)
            {
                Debug.LogError("DrawAimingPath: Missing critical references (GridManager, Main Camera, or PlayerMovement).");
                return;
            }

            Grid mainGrid = gridManager.GetMainGameGrid();
            if (mainGrid == null)
            {
                Debug.LogError("DrawAimingPath: Main Unity Grid is null from GridManager.");
                return;
            }

            Vector2Int playerGridCoords = gridManager.GetGridCoordinates(playerMovement.transform.position);
            Vector3Int playerCell = new Vector3Int(playerGridCoords.x + gridManager.GetMapGridBounds().xMin,
                                                    playerGridCoords.y + gridManager.GetMapGridBounds().yMin, 0);

            Vector3 mouseScreenPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            Vector3Int mouseCell = mainGrid.WorldToCell(mouseWorldPos);

            Vector3Int rawDirection = mouseCell - playerCell;
            Vector3Int normalizedDirection = Vector3Int.zero;
            if (rawDirection.magnitude > 0)
            {
                int signX = (rawDirection.x == 0) ? 0 : (rawDirection.x > 0 ? 1 : -1);
                int signY = (rawDirection.y == 0) ? 0 : (rawDirection.y > 0 ? 1 : -1);

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
                else if (rawDirection.x != 0)
                {
                    normalizedDirection.x = signX;
                }
                else if (rawDirection.y != 0)
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
                Vector2Int currentGridCoords = gridManager.GetGridCoordinates(mainGrid.CellToWorld(pathCell));

                if (!gridManager.IsPositionValid(currentGridCoords) || gridManager.GetTileData(currentGridCoords).type == TileType.Undefined)
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
            Vector3 br = new Vector3(cellWorldCenter.x + halfCellSize.x, cellWorldCenter.y - halfCellSize.y, cellWorldCenter.z) + nudge;
            Vector3 tr = cellWorldCenter + halfCellSize + nudge;
            Vector3 tl = new Vector3(cellWorldCenter.x - halfCellSize.x, cellWorldCenter.y + halfCellSize.y, cellWorldCenter.z) + nudge;

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