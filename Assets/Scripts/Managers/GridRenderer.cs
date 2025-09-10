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

        // Public property to read the state
        public bool IsLineVisible => isLineVisible;

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
                Debug.Log($"GridRenderer: Created LineRenderer for aiming line {i}.");
            }

            // Set the initial visibility based on the current state
            SetLineVisibility(false);
        }

        private void OnDestroy()
        {
            // No event to unsubscribe from here now!
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

        // Public method to be called by the InputManager
        public void ToggleAimingLine(bool isVisible)
        {
            Debug.Log($"GridRenderer: Toggling aiming line visibility to {isVisible}.");
            isLineVisible = isVisible;
            SetLineVisibility(isVisible);
        }

        private void SetLineVisibility(bool isVisible)
        {
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
                // Simple test line to confirm the method is being called
                DrawAimingPath();
            }
        }

        private void DrawAimingPath()
        {
            // Let's just confirm this method is being called with a simple log.
            Debug.Log("GridRenderer: DrawAimingPath() is being called successfully!");
        }
    }
}
