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

        // Load the GLB model main asset
        Object mainAsset = AssetDatabase.LoadMainAssetAtPath(modelPath);
        if (mainAsset == null)
        {
            Debug.LogError($"[FixTilesPrefab] Could not load main asset at path: {modelPath}");
            return;
        }

        Debug.Log($"[FixTilesPrefab] Main asset type: '{mainAsset.GetType()}', Name: '{mainAsset.name}'");

        GameObject modelRoot = mainAsset as GameObject;
        if (modelRoot == null)
        {
            Debug.LogError($"[FixTilesPrefab] Main asset is not a GameObject. Type is: {mainAsset.GetType()}");
            return;
        }

        Dictionary<string, Mesh> modelMeshes = new Dictionary<string, Mesh>();
        Dictionary<string, Material> modelMaterials = new Dictionary<string, Material>();

        // Traverse the GLB hierarchy to extract meshes
        MeshFilter[] modelFilters = modelRoot.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in modelFilters)
        {
            if (filter.sharedMesh != null)
            {
                modelMeshes[filter.gameObject.name] = filter.sharedMesh;
                // Also index by mesh name just in case
                modelMeshes[filter.sharedMesh.name] = filter.sharedMesh;
                Debug.Log($"[FixTilesPrefab] Extracted Model Mesh: Object '{filter.gameObject.name}', Mesh '{filter.sharedMesh.name}'");
            }
        }

        // Traverse the GLB hierarchy to extract materials
        MeshRenderer[] modelRenderers = modelRoot.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in modelRenderers)
        {
            if (renderer.sharedMaterial != null)
            {
                modelMaterials[renderer.gameObject.name] = renderer.sharedMaterial;
                modelMaterials[renderer.sharedMaterial.name] = renderer.sharedMaterial;
                Debug.Log($"[FixTilesPrefab] Extracted Model Material: Object '{renderer.gameObject.name}', Material '{renderer.sharedMaterial.name}'");
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
