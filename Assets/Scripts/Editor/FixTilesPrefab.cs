using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool: Automatically re-links the mesh references inside the TilesGlbMesh prefab
/// to point to the new meshes in the optimized tiles.glb file.
/// Run via: Tools → Gravity Painter → Fix Tiles Prefab References
/// </summary>
public static class FixTilesPrefab
{
    [MenuItem("Tools/Gravity Painter/Fix Tiles Prefab References")]
    public static void FixReferences()
    {
        string prefabPath = "Assets/Resources/Visuals/Tiles/TilesGlbMesh.prefab";
        string modelPath = "Assets/Art/Models/GLB/tiles.glb";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[FixTilesPrefab] Could not find prefab at: {prefabPath}");
            return;
        }

        // Load all sub-assets from the GLB model
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        Dictionary<string, Mesh> modelMeshes = new Dictionary<string, Mesh>();
        Dictionary<string, Material> modelMaterials = new Dictionary<string, Material>();

        foreach (Object asset in subAssets)
        {
            if (asset == null) continue;
            Debug.Log($"[FixTilesPrefab] Asset in GLB: Name = '{asset.name}', Type = '{asset.GetType()}'");

            if (asset is Mesh mesh)
            {
                modelMeshes[mesh.name] = mesh;
                Debug.Log($"[FixTilesPrefab] Model Mesh Name: '{mesh.name}'");
            }
            else if (asset is Material mat)
            {
                modelMaterials[mat.name] = mat;
                Debug.Log($"[FixTilesPrefab] Model Material Name: '{mat.name}'");
            }
        }

        Debug.Log($"[FixTilesPrefab] Found {modelMeshes.Count} meshes and {modelMaterials.Count} materials in '{modelPath}'");

        // Re-link Mesh Filters
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        int fixedMeshes = 0;
        foreach (MeshFilter filter in filters)
        {
            string childName = filter.gameObject.name;
            Debug.Log($"[FixTilesPrefab] Prefab Child Name: '{childName}'");

            if (modelMeshes.TryGetValue(childName, out Mesh newMesh))
            {
                filter.sharedMesh = newMesh;
                fixedMeshes++;
                Debug.Log($"[FixTilesPrefab] Linked mesh '{childName}' to MeshFilter.");
            }
            else
            {
                // Try fuzzy match if name doesn't match exactly (e.g. ignoring case or dots)
                string fallbackName = childName.Replace(".", "_");
                if (modelMeshes.TryGetValue(fallbackName, out Mesh fallbackMesh))
                {
                    filter.sharedMesh = fallbackMesh;
                    fixedMeshes++;
                    Debug.Log($"[FixTilesPrefab] Linked mesh '{childName}' to fallback MeshFilter '{fallbackName}'.");
                }
                else
                {
                    Debug.LogWarning($"[FixTilesPrefab] No matching mesh found for child object: {childName}");
                }
            }
        }

        // Re-link Mesh Renderer Materials
        MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        int fixedMaterials = 0;
        foreach (MeshRenderer renderer in renderers)
        {
            string childName = renderer.gameObject.name;
            
            // Try to match by material name if possible, or fallback to child object name
            Material matchedMat = null;
            foreach (var kvp in modelMaterials)
            {
                if (childName.Contains(kvp.Key) || kvp.Key.Contains(childName))
                {
                    matchedMat = kvp.Value;
                    break;
                }
            }

            // Fallback: Use first available material if none matched specifically
            if (matchedMat == null && modelMaterials.Count > 0)
            {
                var enumerator = modelMaterials.Values.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    matchedMat = enumerator.Current;
                }
            }

            if (matchedMat != null)
            {
                renderer.sharedMaterial = matchedMat;
                fixedMaterials++;
                Debug.Log($"[FixTilesPrefab] Linked material '{matchedMat.name}' to MeshRenderer for '{childName}'.");
            }
        }

        if (fixedMeshes > 0 || fixedMaterials > 0)
        {
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixTilesPrefab] Successfully updated prefab! Re-linked {fixedMeshes} meshes and {fixedMaterials} materials.");
            
            EditorUtility.DisplayDialog(
                "Prefab Fixed",
                $"Successfully re-linked the Tiles prefab references:\n\n" +
                $"• Meshes Re-linked: {fixedMeshes}\n" +
                $"• Materials Re-linked: {fixedMaterials}\n\n" +
                "The tiles visual path should now render properly in-game!",
                "Great!");
        }
        else
        {
            string meshList = string.Join(", ", modelMeshes.Keys);
            string childList = "";
            foreach (var f in filters) childList += f.gameObject.name + ", ";

            EditorUtility.DisplayDialog(
                "No Changes Made",
                $"The tool ran, but could not match the meshes to the prefab.\n\n" +
                $"Prefab child names: {childList}\n" +
                $"GLB mesh names: {meshList}\n\n" +
                "Please check the Unity Console for detailed logs.",
                "OK");
            Debug.LogWarning("[FixTilesPrefab] No changes were made. Mesh names do not match child GameObject names.");
        }
    }
}
