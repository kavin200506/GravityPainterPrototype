using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor tool: Automatically compresses textures to 256 for all newly imported GLTF folders,
/// creates native URP materials, and rebuilds/re-links all prefabs for gameplay obstacles, pickups,
/// and ball skins to point to the new separate GLTF models.
/// Run via: Tools → Gravity Painter → Optimize And Link All Game Assets
/// </summary>
public static class OptimizeAllGameAssets
{
    [MenuItem("Tools/Gravity Painter/Optimize And Link All Game Assets")]
    public static void OptimizeAndLinkAll()
    {
        string glbFolder = "Assets/Art/Models/GLB";
        if (!Directory.Exists(glbFolder))
        {
            EditorUtility.DisplayDialog("Error", $"GLB folder not found at: {glbFolder}", "OK");
            return;
        }

        // 0. Correct relative texture paths inside GLTF files before importing
        FixGltfTexturePaths(glbFolder);

        // 1. Walk through all folders and compress textures for Android
        List<string> targetFolders = new List<string> {
            "Assets/Art/Models/GLB",
            "Assets/Art/Sprites",
            "Assets/Art/Icons",
            "Assets/ThirdParty/Fantasy Skybox FREE"
        };

        int compressedCount = 0;
        foreach (string folder in targetFolders)
        {
            if (!Directory.Exists(folder)) continue;

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".jpg" || ext == ".png" || ext == ".tga")
                {
                    string unityPath = file.Replace("\\", "/");
                    TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                    if (importer != null)
                    {
                        TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");

                        if (unityPath.Contains("Sprites") || unityPath.Contains("Icons"))
                        {
                            // Force high resolution (2048) and Uncompressed (RGBA32) to guarantee perfect UI quality
                            androidSettings.overridden = true;
                            androidSettings.maxTextureSize = 2048;
                            androidSettings.format = TextureImporterFormat.RGBA32;
                        }
                        else if (unityPath.Contains("Fantasy Skybox FREE"))
                        {
                            // Compress Skybox
                            androidSettings.overridden = true;
                            androidSettings.maxTextureSize = 1024;
                            androidSettings.format = TextureImporterFormat.ASTC_6x6;
                        }
                        else
                        {
                            // Compress heavy 3D Models
                            androidSettings.overridden = true;
                            androidSettings.maxTextureSize = 256;
                            androidSettings.format = TextureImporterFormat.ASTC_8x8;
                        }

                        importer.SetPlatformTextureSettings(androidSettings);
                        importer.SaveAndReimport();
                        compressedCount++;
                    }
                }
            }
        }
        Debug.Log($"[OptimizeAll] Processed {compressedCount} total textures (Compressed GLB models & Skybox, restored original quality for Sprites/Icons).");

        // 1.5 Transcode Video for Android (fixes black screen/playback issues on mobile)
        string videoPath = "Assets/Art/Video/Mainmenu.mp4";
        if (File.Exists(videoPath))
        {
            VideoClipImporter videoImporter = AssetImporter.GetAtPath(videoPath) as VideoClipImporter;
            if (videoImporter != null)
            {
                VideoImporterTargetSettings videoSettings = videoImporter.GetTargetSettings("Android");
                if (videoSettings == null)
                {
                    videoSettings = new VideoImporterTargetSettings();
                }
                videoSettings.enableTranscoding = true;
                videoSettings.codec = VideoCodec.H264;
                videoSettings.resizeMode = VideoResizeMode.HalfRes;
                videoSettings.spatialQuality = VideoSpatialQuality.MediumSpatialQuality;

                videoImporter.SetTargetSettings("Android", videoSettings);
                videoImporter.SaveAndReimport();
                Debug.Log("[OptimizeAll] Transcoded Mainmenu.mp4 to Android-compatible H.264 format.");
            }
        }

        AssetDatabase.Refresh();

        // 2. Rebuild all standard gameplay prefabs via existing editor scripts
        Debug.Log("[OptimizeAll] Rebuilding gameplay prefabs...");
        
        // Tiles
        OptimizeTilesGltf.Optimize();
        
        // Coins
        ApplyCoinModel.ApplyToCoinPrefab();



        // Magnet
        ApplyMagnetModel.ApplyToMagnetPrefab();

        // Finish Line
        ApplyFinishLineModel.ApplyToLevels();

        // Sci-Fi Ball (Standard Ball Visual)
        ApplySciFiBall3DModel.ApplyToLevels();

        // Laser Gate (Korrath Beam)
        SetupLaserGateObstacle.SetupInLevel2();

        // Hammer
        SetupHammerObstacle.SetupInLevel2();

        // 3. Re-link the 4 Ball Skin Prefabs
        Debug.Log("[OptimizeAll] Re-linking Ball Skin Prefabs...");
        UpdateBallSkinPrefab(
            "Assets/Resources/Prefabs/BallSkins/Ai_Nova_Blue.prefab",
            "Assets/Art/Models/GLB/Ai_Nova_Blue/Untitled.gltf",
            "Assets/Art/Models/GLB/Ai_Nova_Blue"
        );
        UpdateBallSkinPrefab(
            "Assets/Resources/Prefabs/BallSkins/Ai_Nova_Red.prefab",
            "Assets/Art/Models/GLB/Ai_Nova_Red/Untitled.gltf",
            "Assets/Art/Models/GLB/Ai_Nova_Red"
        );
        UpdateBallSkinPrefab(
            "Assets/Resources/Prefabs/BallSkins/Ai_Nova_White.prefab",
            "Assets/Art/Models/GLB/Ai_Nova_White/Untitled.gltf",
            "Assets/Art/Models/GLB/Ai_Nova_White"
        );
        UpdateBallSkinPrefab(
            "Assets/Resources/Prefabs/BallSkins/Ai_Nova_Yellow.prefab",
            "Assets/Art/Models/GLB/Ai_Nova_Yellow/Untitled.gltf",
            "Assets/Art/Models/GLB/Ai_Nova_Yellow"
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Assets Linked & Optimized",
            "All game assets, obstacles, pickups, and ball skins have been linked to the new GLTF separate models, and all textures are set to Max Size 256!\n\n" +
            "You can now safely delete the old heavy .glb files from disk.",
            "Awesome!");
    }

    private static void UpdateBallSkinPrefab(string prefabPath, string gltfPath, string folderPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[OptimizeAll] Prefab not found at: {prefabPath}");
            return;
        }

        GameObject gltfRoot = AssetDatabase.LoadAssetAtPath<GameObject>(gltfPath);
        if (gltfRoot == null)
        {
            Debug.LogError($"[OptimizeAll] GLTF Model not found at: {gltfPath}");
            return;
        }

        MeshFilter modelFilter = gltfRoot.GetComponentInChildren<MeshFilter>(true);
        MeshRenderer modelRenderer = gltfRoot.GetComponentInChildren<MeshRenderer>(true);
        if (modelFilter == null || modelRenderer == null)
        {
            Debug.LogError($"[OptimizeAll] MeshFilter/MeshRenderer not found inside: {gltfPath}");
            return;
        }

        // Create/Get native URP material
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        Material sourceMat = modelRenderer.sharedMaterial;
        Material uMat = GetOrCreateMaterial(folderPath + "/BallMaterial.mat", urpLitShader, sourceMat);

        // Update prefab components
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        MeshFilter pfFilter = prefabRoot.GetComponent<MeshFilter>();
        if (pfFilter != null)
        {
            pfFilter.sharedMesh = modelFilter.sharedMesh;
        }

        MeshRenderer pfRenderer = prefabRoot.GetComponent<MeshRenderer>();
        if (pfRenderer != null)
        {
            pfRenderer.sharedMaterial = uMat;
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log($"[OptimizeAll] Re-linked ball skin: {Path.GetFileName(prefabPath)} using URP material.");
    }

    private static Material GetOrCreateMaterial(string path, Shader shader, Material sourceMat)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (sourceMat != null)
        {
            // Find base map texture from source material
            Texture baseMap = null;
            if (sourceMat.HasProperty("_BaseMap")) baseMap = sourceMat.GetTexture("_BaseMap");
            else if (sourceMat.HasProperty("_BaseColorMap")) baseMap = sourceMat.GetTexture("_BaseColorMap");
            else if (sourceMat.HasProperty("baseColorTexture")) baseMap = sourceMat.GetTexture("baseColorTexture");
            else baseMap = sourceMat.mainTexture;

            if (baseMap != null)
            {
                mat.SetTexture("_BaseMap", baseMap);
                mat.SetColor("_BaseColor", Color.white);
            }

            if (sourceMat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", sourceMat.GetTexture("_EmissionMap"));
                mat.EnableKeyword("_EMISSION");
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void FixGltfTexturePaths(string glbFolder)
    {
        string[] gltfFiles = Directory.GetFiles(glbFolder, "*.gltf", SearchOption.AllDirectories);
        foreach (string gltfFile in gltfFiles)
        {
            string content = File.ReadAllText(gltfFile);
            string newContent = System.Text.RegularExpressions.Regex.Replace(
                content,
                "\"uri\"\\s*:\\s*\"[^\"]*/(Image_[^\"]+)\"",
                "\"uri\":\"$1\""
            );
            if (content != newContent)
            {
                File.WriteAllText(gltfFile, newContent);
                Debug.Log($"[OptimizeAll] Fixed texture relative paths inside: {gltfFile}");
            }
        }
        AssetDatabase.Refresh();
    }
}
