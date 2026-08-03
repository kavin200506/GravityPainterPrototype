using UnityEngine;

/// <summary>
/// Spawns a GLB model as a child visual while pickup collider + logic stay on the root.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-55)]
public class PowerUpVisual : MonoBehaviour
{
    public const string VisualRootName = "PowerUpVisualRoot";
    public const string DefaultResourcePath = "Prefabs/MagnetVisual";

    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private Vector3 targetLocalBoundsSize = new Vector3(1.2f, 1.2f, 1.2f);

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureVisual();
        }
    }

    public void ConfigurePrefab(GameObject prefab)
    {
        modelPrefab = prefab;
    }

    public void EnsureVisual()
    {
        RemovePrimitiveMeshFromRoot();

        Transform existing = transform.Find(VisualRootName);
        if (existing != null)
        {
            if (!UsesBrokenVisual(existing))
            {
                Debug.Log($"[PowerUpVisual] '{name}' already has a valid visual child '{existing.name}'.");
                return;
            }

            Debug.Log($"[PowerUpVisual] Destroying stale/primitive visual for '{name}' to rebuild 3D model.");
            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        if (!TryResolvePrefab(out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[PowerUpVisual] ❌ Failed to resolve 3D model prefab for '{name}'. Check GlbModelPaths.SpeedUp and Resources/Prefabs/SpeedUpVisual.");
            return;
        }

        GameObject root = new GameObject(VisualRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject model = InstantiateGameObject(prefab, root.transform);
        if (model == null)
        {
            Debug.LogError($"[PowerUpVisual] ❌ InstantiateGameObject returned null for prefab '{prefab.name}' on '{name}'.");
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
            return;
        }

        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        StripPhysics(model);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(model);
        FitModelToTargetBounds(model, targetLocalBoundsSize);

        Debug.Log($"[PowerUpVisual] ✅ Successfully created 3D visual '{model.name}' for pickup '{name}'.");
    }

    private static GameObject InstantiateGameObject(UnityEngine.Object source, Transform parent)
    {
        if (source == null) return null;

        try
        {
            if (source is GameObject go)
            {
                return Instantiate(go, parent);
            }

            UnityEngine.Object instantiated = Instantiate(source, parent);
            if (instantiated is GameObject instantiatedGo)
            {
                return instantiatedGo;
            }
            if (instantiated is Component comp)
            {
                return comp.gameObject;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PowerUpVisual] Safe instantiate failed for {source.name}: {ex.Message}");
        }

        return null;
    }

    public void RebuildVisual()
    {
        Transform existing = transform.Find(VisualRootName);
        if (existing != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        EnsureVisual();
    }

    private bool TryResolvePrefab(out GameObject prefab)
    {
        prefab = modelPrefab;
        if (prefab != null && prefab is GameObject)
        {
            Debug.Log($"[PowerUpVisual] Resolved modelPrefab from SerializedField for '{name}' -> '{prefab.name}'.");
            return true;
        }

        bool isSpeed = name.Contains("Speed") || 
                       (transform.parent != null && transform.parent.name.Contains("Speed"));

        PowerUpPickup pickup = GetComponent<PowerUpPickup>();
        if (pickup != null && pickup.powerUpType == PowerUpType.Speed)
        {
            isSpeed = true;
        }

        string resourcePath = isSpeed ? "Prefabs/SpeedUpVisual" : DefaultResourcePath;
        prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null)
        {
            Debug.Log($"[PowerUpVisual] Resolved model prefab from Resources: '{resourcePath}' for '{name}'.");
            return true;
        }

#if UNITY_EDITOR
        string fallbackPath = isSpeed ? GlbModelPaths.SpeedUp : GlbModelPaths.Magnet;
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
        if (prefab != null)
        {
            Debug.Log($"[PowerUpVisual] Resolved model prefab from AssetDatabase: '{fallbackPath}' for '{name}'.");
            return true;
        }
#endif

        Debug.LogError($"[PowerUpVisual] ❌ Could NOT resolve any model prefab for '{name}' (isSpeed={isSpeed}).");
        return false;
    }

    private static bool UsesBrokenVisual(Transform visualRoot)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return true; // Force rebuild if no renderer
        }

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return true;
            }

            foreach (Material material in materials)
            {
                if (material == null || material.shader == null)
                {
                    return true;
                }

                string shaderName = material.shader.name;
                if (shaderName.Contains("Hidden") || shaderName.Contains("Error"))
                {
                    return true;
                }

                if (material.name.StartsWith("MagnetMaterial") || 
                    material.name.StartsWith("SpeedCoreMaterial") ||
                    material.name.Contains("SpeedCore"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RemovePrimitiveMeshFromRoot()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            if (Application.isPlaying)
            {
                Destroy(meshFilter);
            }
            else
            {
                DestroyImmediate(meshFilter);
            }
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(meshRenderer);
            }
            else
            {
                DestroyImmediate(meshRenderer);
            }
        }
    }

    private static void FitModelToTargetBounds(GameObject model, Vector3 targetSize)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[PowerUpVisual] No renderers found in model '{model.name}'.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 size = bounds.size;
        float scaleX = targetSize.x / Mathf.Max(size.x, 0.0001f);
        float scaleY = targetSize.y / Mathf.Max(size.y, 0.0001f);
        float scaleZ = targetSize.z / Mathf.Max(size.z, 0.0001f);
        float uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);
        model.transform.localScale = Vector3.one * uniformScale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = model.transform.InverseTransformPoint(bounds.center);
        model.transform.localPosition -= localCenter;
    }

    private static void StripPhysics(GameObject root)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (Application.isPlaying)
            {
                Destroy(body);
            }
            else
            {
                DestroyImmediate(body);
            }
        }
    }
}
