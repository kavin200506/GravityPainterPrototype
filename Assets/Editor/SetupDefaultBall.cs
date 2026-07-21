#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the Default ball prefab from Silver Ball.glb with a proper URP silver chrome material.
/// Runs automatically on editor load (edit mode only) and via menu item.
/// </summary>
[InitializeOnLoad]
public static class SetupDefaultBall
{
    private const string GlbPath      = "Assets/Art/Models/GLB/Silver Ball.glb";
    private const string PrefabPath   = "Assets/Resources/Prefabs/BallSkins/Default.prefab";
    private const string MaterialPath = "Assets/Art/Materials/SilverBallMaterial.mat";

    static SetupDefaultBall()
    {
        // Only run in edit mode, never during play
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
            Debug.LogWarning("[SetupDefaultBall] Cannot run during Play Mode.");
            return;
        }

        // ── Step 1: Force the GLB to ignore its embedded dark material ──
        ModelImporter importer = AssetImporter.GetAtPath(GlbPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[SetupDefaultBall] GLB not found at: " + GlbPath);
            return;
        }

        if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            // Delay the rest until after reimport completes
            EditorApplication.delayCall += FinishSetup;
            return;
        }

        FinishSetup();
    }

    private static void FinishSetup()
    {
        // ── Step 2: Create or update the silver chrome URP material ──
        EnsureMaterialsFolder();
        Material silverMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("[SetupDefaultBall] URP/Lit shader not found — is URP installed?");
            return;
        }

        if (silverMat == null)
        {
            silverMat = new Material(urpLit);
            AssetDatabase.CreateAsset(silverMat, MaterialPath);
        }

        // Silver chrome: bright base so ambient light makes it visible even without reflection probes
        silverMat.shader = urpLit;
        silverMat.SetColor("_BaseColor",    new Color(0.85f, 0.87f, 0.92f, 1f));
        silverMat.SetFloat("_Metallic",     0.75f);
        silverMat.SetFloat("_Smoothness",   0.88f);
        silverMat.SetColor("_EmissionColor", new Color(0.10f, 0.10f, 0.13f));
        silverMat.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(silverMat);

        // ── Step 3: Load GLB and create prefab ──
        GameObject glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
        if (glbAsset == null)
        {
            Debug.LogWarning("[SetupDefaultBall] Cannot load GLB after reimport: " + GlbPath);
            return;
        }

        EnsurePrefabFolder();

        GameObject temp = Object.Instantiate(glbAsset);
        temp.name = "Default";

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

        Debug.Log("[SetupDefaultBall] ✅ Default ball prefab saved with silver chrome material.");
    }

    private static void EnsureMaterialsFolder()
    {
        string dir = System.IO.Path.GetDirectoryName(MaterialPath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
    }

    private static void EnsurePrefabFolder()
    {
        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
    }
}
#endif
