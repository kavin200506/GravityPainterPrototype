#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports speed/Untitled.gltf, builds a runtime prefab, and wires PowerUpVisual on the SpeedCore pickup prefab.
/// </summary>
public static class ApplySpeedUpModel
{
    private const string GlbAssetPath = GlbModelPaths.SpeedUp;
    private const string SpeedCorePrefabPath = "Assets/Prefabs/PowerUps/SpeedCore.prefab";
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/SpeedUpVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/SpeedUpVisual.prefab";

    [InitializeOnLoadMethod]
    private static void AutoApplyOnLoad()
    {
        // Only run if the visual prefab hasn't been created yet or needs update
        if (!File.Exists(ResourcesPrefabPath) && File.Exists(GlbAssetPath))
        {
            ApplyToSpeedUpPrefab(silent: true);
        }
    }

    [MenuItem("Gravity Painter/Apply SpeedUp GLB To Prefab")]
    public static void ApplyToSpeedUpPrefabMenu()
    {
        ApplyToSpeedUpPrefab(silent: false);
    }

    public static void ApplyToSpeedUpPrefab(bool silent = false)
    {
        GameObject visualPrefab = BuildOrUpdateVisualPrefab(silent);
        if (visualPrefab == null)
        {
            return;
        }

        if (!File.Exists(SpeedCorePrefabPath))
        {
            if (!silent) EditorUtility.DisplayDialog("Missing prefab", "Could not find:\n" + SpeedCorePrefabPath, "OK");
            return;
        }

        GameObject speedCoreRoot = PrefabUtility.LoadPrefabContents(SpeedCorePrefabPath);
        RemovePrimitiveMesh(speedCoreRoot);
        WirePowerUpVisual(speedCoreRoot, visualPrefab);

        Transform rootTransform = speedCoreRoot.transform;
        rootTransform.localPosition = Vector3.zero;
        rootTransform.localRotation = Quaternion.identity;
        rootTransform.localScale = Vector3.one * 2f;

        PrefabUtility.SaveAsPrefabAsset(speedCoreRoot, SpeedCorePrefabPath);
        PrefabUtility.UnloadPrefabContents(speedCoreRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ApplySpeedUpModel] ✅ SpeedUp GLTF model applied to SpeedCore prefab.");

        if (!silent)
        {
            EditorUtility.DisplayDialog(
                "SpeedUp model applied",
                "Created/updated:\n" + ResourcesPrefabPath + "\n\n" +
                "Updated gameplay prefab:\n" + SpeedCorePrefabPath,
                "OK");
        }
    }

    private static void WirePowerUpVisual(GameObject root, GameObject visualPrefab)
    {
        PowerUpVisual visual = root.GetComponent<PowerUpVisual>();
        if (visual == null)
        {
            visual = root.AddComponent<PowerUpVisual>();
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("modelPrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        Transform staleVisual = root.transform.Find(PowerUpVisual.VisualRootName);
        if (staleVisual != null)
        {
            Object.DestroyImmediate(staleVisual.gameObject);
        }

        EditorUtility.SetDirty(root);
    }

    private static void RemovePrimitiveMesh(GameObject root)
    {
        MeshFilter meshFilter = root.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
        }

        MeshRenderer meshRenderer = root.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Object.DestroyImmediate(meshRenderer);
        }
    }

    private static GameObject BuildOrUpdateVisualPrefab(bool silent = false)
    {
        if (!File.Exists(GlbAssetPath))
        {
            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "Missing GLB",
                    "Place model in:\n" + GlbAssetPath,
                    "OK");
            }
            return null;
        }

        GameObject glbRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GlbAssetPath);
        if (glbRoot == null)
        {
            AssetDatabase.ImportAsset(GlbAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            glbRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GlbAssetPath);
        }

        if (glbRoot == null)
        {
            if (!silent) EditorUtility.DisplayDialog("GLB import failed", "Could not load " + GlbAssetPath, "OK");
            return null;
        }

        Directory.CreateDirectory("Assets/Prefabs/Visuals");
        Directory.CreateDirectory("Assets/Resources/Prefabs");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (instance == null)
        {
            instance = Object.Instantiate(glbRoot);
        }

        instance.name = "SpeedUpVisual";
        StripPhysics(instance);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(instance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, VisualPrefabPath);
        Object.DestroyImmediate(instance);

        GameObject resourcesCopy = PrefabUtility.SaveAsPrefabAsset(prefab, ResourcesPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return resourcesCopy != null ? resourcesCopy : prefab;
    }

    private static void StripPhysics(GameObject root)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(col);
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(body);
        }
    }
}
#endif
