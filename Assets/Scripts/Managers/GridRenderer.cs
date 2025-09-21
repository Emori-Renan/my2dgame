using UnityEngine;
using MyGame.Managers;
using MyGame.Player;
using System.Collections;
using System.Collections.Generic;

namespace MyGame.Managers
{
    public class GridRenderer : MonoBehaviour
    {
        private GridManager gridManager;

        private bool isLineVisible = false;
        private LineRenderer aimingLine;

        [Header("Aiming Line Settings")]
        public int aimingLineLength = 3;
        public float lineGap = 0.5f;
        [Range(-1.0f, 1.0f)]
        public float pixelNudgeOffset = 0.0f;

        // Public property to read the state
        public bool IsLineVisible => isLineVisible;

        private void Awake()
        {
            InitializeLineRenderer();
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

            ToggleAimingLine(false);
        }

        private void InitializeLineRenderer()
        {
            GameObject lineGO = new GameObject("AimingLine");
            lineGO.transform.SetParent(this.transform);
            lineGO.transform.localPosition = Vector3.zero;
            aimingLine = lineGO.AddComponent<LineRenderer>();

            Material lineMaterial = new Material(Shader.Find("Unlit/Color"));
            lineMaterial.color = Color.white;

            aimingLine.material = lineMaterial;
            aimingLine.startWidth = 0.05f;
            aimingLine.endWidth = 0.05f;
            aimingLine.loop = false;
            aimingLine.useWorldSpace = true;
            aimingLine.sortingLayerName = "Foreground";
            aimingLine.sortingOrder = 100;
        }

        public void ToggleAimingLine(bool isVisible)
        {
            isLineVisible = isVisible;
            if (aimingLine != null)
            {
                aimingLine.enabled = isVisible;
            }
        }

        public void SetAimingDirection(Vector3 playerPosition, Vector3 direction)
        {
            if (!isLineVisible || aimingLine == null || gridManager == null)
            {
                return;
            }

            // Determine the snapped grid direction based on the fluid direction
            Vector2Int snappedDirection = Vector2Int.zero;
            if (direction.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // Normalize the angle to be between 0 and 360
                angle = (angle + 360) % 360;

                // An array of the 8 possible grid directions
                Vector2Int[] gridDirections = new Vector2Int[]
                {
                    new Vector2Int(1, 0),    // Right
                    new Vector2Int(1, 1),    // Up-Right
                    new Vector2Int(0, 1),    // Up
                    new Vector2Int(-1, 1),   // Up-Left
                    new Vector2Int(-1, 0),   // Left
                    new Vector2Int(-1, -1),  // Down-Left
                    new Vector2Int(0, -1),   // Down
                    new Vector2Int(1, -1)    // Down-Right
                };

                // Determine the index of the direction by dividing the angle by 45 degrees
                int index = Mathf.RoundToInt(angle / 45.0f);
                // Ensure the index is within the bounds of the array
                index = (index + 8) % 8;

                snappedDirection = gridDirections[index];
            }

            if (snappedDirection == Vector2Int.zero)
            {
                aimingLine.enabled = false;
                return;
            }

            // Draw the line based on the snapped, grid-aligned direction
            Vector3[] linePositions = new Vector3[aimingLineLength + 1];
            linePositions[0] = playerPosition;
            Vector2Int currentGridPos = gridManager.GetGridCoordinates(playerPosition);

            for (int i = 1; i <= aimingLineLength; i++)
            {
                Vector2Int nextGridPos = currentGridPos + snappedDirection * i;

                if (!gridManager.IsWalkable(nextGridPos))
                {
                    aimingLine.positionCount = i;
                    break;
                }

                Vector3 targetWorldPos = gridManager.GetWorldPosition(nextGridPos);
                linePositions[i] = targetWorldPos;
                aimingLine.positionCount = i + 1;
            }

            aimingLine.SetPositions(linePositions);
            aimingLine.enabled = true;
        }
    }
}
