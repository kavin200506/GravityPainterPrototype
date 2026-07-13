using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool: Automatically compresses the separate JPG textures exported from Blender
/// for the tiles model, creates native URP materials, and builds the TilesGlbMesh prefab
/// to use these native URP materials so they render perfectly in the URP pipeline.
/// Run via: Tools → Gravity Painter → Optimize Tiles GLTF
/// </summary>
public static class OptimizeTilesGltf
{
    [MenuItem("Tools/Gravity Painter/Optimize Tiles GLTF")]
    public static void Optimize()
    {
        string tilesFolder = "Assets/Art/Models/GLB/tiles";
        string gltfPath = "Assets/Art/Models/GLB/tiles/Untitled.gltf";
        string prefabPath = "Assets/Prefabs/Visuals/Tiles/TilesGlbMesh.prefab";
        string resourcesPrefabPath = "Assets/Resources/Visuals/Tiles/TilesGlbMesh.prefab";

        if (!Directory.Exists(tilesFolder) || !File.Exists(gltfPath))
        {
            EditorUtility.DisplayDialog("Model Not Found", $"Could not find folder or model at: {gltfPath}", "OK");
            return;
        }

        // 1. Compress all JPG textures for Android (512, ASTC 8x8)
        string[] files = Directory.GetFiles(tilesFolder, "*.jpg");
        int compressedCount = 0;
        foreach (string file in files)
        {
            string unityPath = file.Replace("\\", "/");
            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer != null)
            {
                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = 256;
                androidSettings.format = TextureImporterFormat.ASTC_8x8;

                importer.SetPlatformTextureSettings(androidSettings);
                importer.SaveAndReimport();
                compressedCount++;
            }
        }
        Debug.Log($"[OptimizeTiles] Compressed {compressedCount} external textures.");

        AssetDatabase.Refresh();

        // 2. Create or load native URP materials
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            Debug.LogError("[OptimizeTiles] Universal Render Pipeline/Lit shader not found!");
            return;
        }

        Material blueMat = GetOrCreateMaterial(tilesFolder + "/Tile_Blue.mat", urpLitShader, tilesFolder + "/Image_0.jpg");
        Material redMat = GetOrCreateMaterial(tilesFolder + "/Tile_Red.mat", urpLitShader, tilesFolder + "/Image_0-1.jpg");
        Material yellowMat = GetOrCreateMaterial(tilesFolder + "/Tile_Yellow.mat", urpLitShader, tilesFolder + "/Image_0-2.jpg");

        if (blueMat == null || redMat == null || yellowMat == null)
        {
            Debug.LogError("[OptimizeTiles] Failed to create URP materials.");
            return;
        }

        // 3. Load the GLTF model root
        GameObject gltfRoot = AssetDatabase.LoadAssetAtPath<GameObject>(gltfPath);
        if (gltfRoot == null)
        {
            Debug.LogError($"[OptimizeTiles] Could not load GLTF model root at: {gltfPath}");
            return;
        }

        // 4. Instantiate a clone to build the prefab
        GameObject model = Object.Instantiate(gltfRoot);
        model.name = "TilesGlbMesh";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        // Strip colliders/physics
        foreach (Collider col in model.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(col);
        }
        foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rb);
        }

        // 5. Link the new URP materials to the mesh renderers
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in renderers)
        {
            string childName = r.gameObject.name;
            if (childName == "Mesh_0")
            {
                r.sharedMaterial = blueMat;
                Debug.Log("[OptimizeTiles] Assigned URP Blue Material to Mesh_0");
            }
            else if (childName == "Mesh_0.001")
            {
                r.sharedMaterial = redMat;
                Debug.Log("[OptimizeTiles] Assigned URP Red Material to Mesh_0.001");
            }
            else if (childName == "Mesh_0.004")
            {
                r.sharedMaterial = yellowMat;
                Debug.Log("[OptimizeTiles] Assigned URP Yellow Material to Mesh_0.004");
            }
        }

        // 6. Save the prefab
        EnsureDirectory(prefabPath);
        EnsureDirectory(resourcesPrefabPath);

        SavePrefab(model, prefabPath);
        SavePrefab(model, resourcesPrefabPath);
        Object.DestroyImmediate(model);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Tiles Optimized",
            $"Successfully compressed {compressedCount} external JPG texture(s) to Max Size 256 (ASTC 8x8) for Android!\n\n" +
            "Please select 'Gravity Painter → Build Tiles GLB Mesh Prefab' from the top menu to generate the updated prefab using this optimized model.",
            "OK");
    }

    private static Material GetOrCreateMaterial(string path, Shader shader, string texturePath)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
        }
        else
        {
            Debug.LogWarning($"[OptimizeTiles] Could not find texture at: {texturePath}");
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SavePrefab(GameObject model, string path)
    {
        GameObject clone = Object.Instantiate(model);
        clone.name = "TilesGlbMesh";
        PrefabUtility.SaveAsPrefabAsset(clone, path);
        Object.DestroyImmediate(clone);
    }

    private static void EnsureDirectory(string assetPath)
    {
        string dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
