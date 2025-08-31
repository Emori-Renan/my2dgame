    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.Tilemaps;

    namespace MyGame.World
    {
        public class LayerTransparencyController : MonoBehaviour
        {
            [Header("Layer to Control")]
            [Tooltip("Drag the parent GameObject of the layer you want to make transparent (e.g., the '_Roofs' parent GameObject, or '_ForegroundTrees').")]
            public GameObject layerRootGameObject;

            [Header("Transparency Settings")]
            [Tooltip("The alpha value when the player is inside the trigger (e.g., 0.5 for semi-transparent, 0.0 for completely invisible).")]
            [Range(0f, 1f)]
            public float transparentAlpha = 0.5f;

            [Tooltip("How quickly the layer fades in/out.")]
            public float fadeSpeed = 3f;

            [Tooltip("Optional: Delay before the layer fully reappears after player exits the trigger.")]
            public float reappearDelay = 0.5f;

            private List<Renderer> renderersInLayer = new List<Renderer>();
            private Coroutine fadeCoroutine;

            // NEW: MaterialPropertyBlock for efficient material property changes
            private MaterialPropertyBlock propBlock;
            // The shader property ID for the color (cached for performance)
            private int colorPropertyID;


            private void Awake()
            {
                Collider2D col = GetComponent<Collider2D>();
                if (col == null || !col.isTrigger)
                {
                    Debug.LogError($"LayerTransparencyController on {gameObject.name}: Requires a Collider2D with 'Is Trigger' set to true. Disabling script.");
                    this.enabled = false;
                    return;
                }
                if (GetComponent<Rigidbody2D>() == null)
                {
                    Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }

                if (layerRootGameObject == null)
                {
                    Debug.LogError($"LayerTransparencyController on {gameObject.name}: 'Layer Root GameObject' is not assigned. Disabling script.");
                    this.enabled = false;
                    return;
                }

                renderersInLayer.AddRange(layerRootGameObject.GetComponentsInChildren<SpriteRenderer>());
                renderersInLayer.AddRange(layerRootGameObject.GetComponentsInChildren<TilemapRenderer>());

                if (renderersInLayer.Count == 0)
                {
                    Debug.LogWarning($"LayerTransparencyController on {gameObject.name}: No SpriteRenderers or TilemapRenderers found in the 'Layer Root GameObject' or its children. Cannot control transparency. Disabling script.");
                    this.enabled = false;
                    return;
                }

                // Initialize MaterialPropertyBlock and cache the color property ID
                propBlock = new MaterialPropertyBlock();
                colorPropertyID = Shader.PropertyToID("_Color"); // "_Color" is the default tint property for most sprites/tilemaps

                foreach (Renderer r in renderersInLayer)
                {
                    if (r.sharedMaterial == null)
                    {
                        Debug.LogWarning($"LayerTransparencyController: Renderer on '{r.name}' has no sharedMaterial. Cannot control transparency for it.");
                        continue;
                    }

                    // Ensure the material supports transparency.
                    // This warning is still relevant, but MaterialPropertyBlock won't break material instances.
                    if (r.sharedMaterial.renderQueue < 3000) // 3000 is typically the start of Transparent queue
                    {
                        Debug.LogWarning($"LayerTransparencyController: Renderer on '{r.name}' material '{r.sharedMaterial.name}' may not fully support transparency. Ensure its render mode is 'Fade' or 'Transparent'.");
                    }
                }
            }

            private void Start()
            {
                if (renderersInLayer.Count == 0)
                {
                    Debug.LogError($"LayerTransparencyController on {gameObject.name}: Script disabled due to missing Renderers in the layer root. Transparency control failed.");
                    this.enabled = false;
                    return;
                }

                SetLayerAlpha(1.0f);
                Debug.Log($"LayerTransparencyController on {gameObject.name}: Initial alpha set to 1.0f.");
            }

            private void OnTriggerEnter2D(Collider2D other)
            {
                if (other.CompareTag("Player"))
                {
                    Debug.Log($"Player entered transparency zone for layer '{layerRootGameObject.name}'. Fading out.");
                    if (fadeCoroutine != null)
                    {
                        StopCoroutine(fadeCoroutine);
                    }
                    fadeCoroutine = StartCoroutine(FadeLayerRoutine(transparentAlpha));
                }
            }

            private void OnTriggerExit2D(Collider2D other)
            {
                if (other.CompareTag("Player"))
                {
                    Debug.Log($"Player exited transparency zone for layer '{layerRootGameObject.name}'. Fading in after delay.");
                    if (fadeCoroutine != null)
                    {
                        StopCoroutine(fadeCoroutine);
                    }
                    fadeCoroutine = StartCoroutine(FadeLayerRoutine(1.0f, reappearDelay));
                }
            }

            private IEnumerator FadeLayerRoutine(float targetAlpha, float delay = 0f)
            {
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
                
                // Give Unity a couple frames to distribute work and process event
                yield return null; 
                yield return null; 

                if (renderersInLayer.Count == 0) yield break;

                // Get initial alpha using the MaterialPropertyBlock to avoid creating material instance
                // We need to get the block first, then read the color
                renderersInLayer[0].GetPropertyBlock(propBlock);
                float initialAlpha = propBlock.GetColor(colorPropertyID).a;

                float timer = 0f;

                while (timer < 1f)
                {
                    timer += Time.deltaTime * fadeSpeed;
                    float currentAlpha = Mathf.Lerp(initialAlpha, targetAlpha, timer);
                    SetLayerAlpha(currentAlpha);
                    yield return null;
                }
                SetLayerAlpha(targetAlpha); // Ensure final alpha is exact
            }

            private void SetLayerAlpha(float alpha)
            {
                for (int i = 0; i < renderersInLayer.Count; i++)
                {
                    Renderer r = renderersInLayer[i];
                    if (r != null && r.sharedMaterial != null)
                    {
                        // Get the current property block for this renderer (or an empty one if none set yet)
                        r.GetPropertyBlock(propBlock);
                        // Modify only the alpha component of the color
                        Color currentColor = propBlock.HasColor(colorPropertyID) ? propBlock.GetColor(colorPropertyID) : r.sharedMaterial.color;
                        currentColor.a = alpha;
                        propBlock.SetColor(colorPropertyID, currentColor);
                        // Apply the modified property block back to the renderer
                        r.SetPropertyBlock(propBlock);
                    }
                }
            }
        }
    }
    