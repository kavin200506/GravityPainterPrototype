using UnityEngine;
using UnityEditor;
using System.Linq;

public static class CheckGltfImportErrors
{
    [MenuItem("Tools/Gravity Painter/Debug GLTF Import")]
    public static void DebugImport()
    {
        string gltfPath = "Assets/Art/Models/GLB/tiles/Untitled.gltf";
        var importer = AssetImporter.GetAtPath(gltfPath);
        if (importer == null)
        {
            Debug.LogError($"Asset not found at: {gltfPath}");
            return;
        }

        Debug.Log($"Importer Type: {importer.GetType().FullName}");
        
        // Use reflection to find reportItems in GltfImporter
        var field = importer.GetType().GetField("reportItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            var items = field.GetValue(importer) as System.Array;
            if (items != null)
            {
                Debug.Log($"Found {items.Length} report items:");
                foreach (var item in items)
                {
                    Debug.Log($"- Item: {item.ToString()}");
                }
            }
            else
            {
                Debug.Log("reportItems array is null.");
            }
        }
        else
        {
            Debug.Log("Could not find 'reportItems' field on importer.");
        }
    }
}
