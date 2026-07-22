using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor tool: Automatically extracts all embedded textures from all GLB files
/// in the project, compresses them for Android, and remaps the GLB models to use
/// the compressed external PNG textures.
/// Run via: Tools → Gravity Painter → Optimize All GLB Textures
/// </summary>
public static class CompressGlbTextures
{
    [MenuItem("Tools/Gravity Painter/Optimize All GLB Textures")]
    public static void OptimizeAll()
    {
        // Find all .glb files under Assets/
        string[] guids = AssetDatabase.FindAssets("t:Object", new[] { "Assets" });
        List<string> glbPaths = new List<string>();

        foreach (string guid in guids)
            {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!glbPaths.Contains(path))
                {
                    glbPaths.Add(path);
                }
            }
        }

        if (glbPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("No GLBs Found", "No .glb files found in the Assets folder.", "OK");
            return;
        }

        int totalTexturesExtracted = 0;
        int totalGlbsProcessed = 0;

        foreach (string glbPath in glbPaths)
        {
            AssetImporter glbImporter = AssetImporter.GetAtPath(glbPath);
            if (glbImporter == null) continue;

            // Load all sub-assets of the GLB file
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(glbPath);
            List<Texture> embeddedTextures = new List<Texture>();

            foreach (Object asset in subAssets)
            {
                if (asset is Texture tex && !string.IsNullOrEmpty(tex.name))
                {
                    embeddedTextures.Add(tex);
                }
            }

            if (embeddedTextures.Count == 0)
            {
                continue; // No textures embedded in this GLB
            }

            // Create target folder for this GLB's extracted textures next to the GLB
            string glbDir = Path.GetDirectoryName(glbPath);
            string glbName = Path.GetFileNameWithoutExtension(glbPath);
            string targetFolder = Path.Combine(glbDir, "ExtractedTextures_" + glbName).Replace("\\", "/");

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            totalGlbsProcessed++;
            bool needsReimport = false;

            foreach (Texture tex in embeddedTextures)
            {
                string targetPath = Path.Combine(targetFolder, tex.name + ".png").Replace("\\", "/");
                
                // Extract using RenderTexture Blit to bypass read/write limitations
                if (!File.Exists(targetPath))
                {
                    if (ExtractTextureToDisk(tex, targetPath))
                    {
                        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
                    }
                    else
                    {
                        continue;
                    }
                }

                // Load the extracted texture
                Texture2D externalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
                if (externalTex == null) continue;

                // Compress the extracted texture for Android
                TextureImporter texImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (texImporter != null)
                {
                    TextureImporterPlatformSettings androidSettings = texImporter.GetPlatformTextureSettings("Android");
                    androidSettings.overridden = true;
                    androidSettings.maxTextureSize = 256;
                    androidSettings.format = TextureImporterFormat.ASTC_8x8;
                    
                    texImporter.SetPlatformTextureSettings(androidSettings);
                    texImporter.SaveAndReimport();
                }

                // Remap the GLB sub-asset to use the external PNG texture
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Texture2D), tex.name);
                glbImporter.AddRemap(identifier, externalTex);
                
                totalTexturesExtracted++;
                needsReimport = true;
                
                Debug.Log($"[OptimizeGLB] Extracted and compressed '{tex.name}' for GLB: '{glbName}'");
            }

            if (needsReimport)
            {
                glbImporter.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Optimization Complete",
            $"Successfully processed {totalGlbsProcessed} GLB model(s) and extracted/compressed {totalTexturesExtracted} texture(s) to Max Size 256 (ASTC 8x8) for Android.",
            "Awesome!");
    }

    /// <summary>
    /// Blits any Texture (compressed, read-disabled, etc.) into a RenderTexture and saves it as a PNG on disk.
    /// </summary>
    private static bool ExtractTextureToDisk(Texture source, string targetPath)
    {
        try
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = readableTex.EncodeToPNG();
            Object.DestroyImmediate(readableTex);

            File.WriteAllBytes(targetPath, bytes);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OptimizeGLB] Failed to extract texture '{source.name}' to path '{targetPath}': {e.Message}");
            return false;
        }
    }
}
