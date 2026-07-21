#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates Assets/Resources/Prefabs/BallSkins/Default.prefab from the existing
/// Sci-Fi Ball 3D model GLTF with a custom silver chrome URP material applied.
/// Runs automatically on editor domain reload (edit mode only) and via:
///   Tools → Gravity Painter → Setup Default Ball
/// </summary>
[InitializeOnLoad]
public static class SetupDefaultBall
{
    // The GLTF model that already exists in the project
    private const string GltfPath    = "Assets/Art/Models/GLB/Sci-Fi Ball 3D Model/Untitled.gltf";
    private const string PrefabPath  = "Assets/Resources/Prefabs/BallSkins/Default.prefab";
    private const string MatPath     = "Assets/Art/Materials/SilverBallMaterial.mat";

    static SetupDefaultBall()
    {
        // Auto-run after domain reload — only in edit mode
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlaying)
                RunSetup();
        };
    }

    [MenuItem("Tools/Gravity Painter/Setup Default Ball")]
    public static void RunSetup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SetupDefaultBall] Exit play mode first.");
            return;
        }

        // ── 1. Find GLTF model ─────────────────────────────────────────
        GameObject gltfAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GltfPath);
        if (gltfAsset == null)
        {
            // Fallback: search AssetDatabase for any imported ball mesh
            string[] guids = AssetDatabase.FindAssets("Untitled t:GameObject", new[] { "Assets/Art/Models/GLB/Sci-Fi Ball 3D Model" });
            if (guids.Length > 0)
                gltfAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (gltfAsset == null)
        {
            Debug.LogWarning("[SetupDefaultBall] Sci-Fi Ball GLTF not found at: " + GltfPath);
            return;
        }

        // ── 2. Create or refresh silver chrome URP material ────────────
        EnsureFolder("Assets/Art/Materials");
        Material silverMat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogWarning("[SetupDefaultBall] URP/Lit shader not found.");
            return;
        }

        if (silverMat == null)
        {
            silverMat = new Material(urpLit);
            AssetDatabase.CreateAsset(silverMat, MatPath);
        }

        // Silver chrome: bright enough to be visible even without reflection probes
        silverMat.shader = urpLit;
        silverMat.SetColor("_BaseColor",     new Color(0.85f, 0.87f, 0.92f, 1f));
        silverMat.SetFloat("_Metallic",      0.75f);
        silverMat.SetFloat("_Smoothness",    0.88f);
        // Subtle emissive so it's never pure black in dark scenes
        silverMat.SetColor("_EmissionColor", new Color(0.10f, 0.10f, 0.14f, 1f));
        silverMat.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(silverMat);
        AssetDatabase.SaveAssets();

        // ── 3. Build the prefab ────────────────────────────────────────
        EnsureFolder("Assets/Resources/Prefabs/BallSkins");

        GameObject temp = Object.Instantiate(gltfAsset);
        temp.name = "Default";

        // Strip physics (BallController owns the collider)
        foreach (Collider col in temp.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        foreach (Rigidbody rb in temp.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(rb);

        // Apply silver material to every renderer
        foreach (MeshRenderer mr in temp.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = silverMat;
            mr.sharedMaterials = mats;
        }
        foreach (SkinnedMeshRenderer smr in temp.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mats = new Material[smr.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = silverMat;
            smr.sharedMaterials = mats;
        }

        PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
        Object.DestroyImmediate(temp);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupDefaultBall] ✅ Default ball prefab saved at: " + PrefabPath);
    }

    private static void EnsureFolder(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }
}
#endif
