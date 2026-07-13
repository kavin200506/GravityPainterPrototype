using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool: Diagnoses the exact mesh and material state of the TilesGlbMesh prefab.
/// Run via: Tools → Gravity Painter → Diagnose Tiles Prefab
/// </summary>
public static class DiagnoseTilesPrefab
{
    [MenuItem("Tools/Gravity Painter/Diagnose Tiles Prefab")]
    public static void Diagnose()
    {
        string prefabPath = "Assets/Resources/Visuals/Tiles/TilesGlbMesh.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Diagnose] Could not find prefab at: {prefabPath}");
            return;
        }

        Debug.Log($"=== Prefab Diagnosis for '{prefab.name}' ===");
        LogHierarchyDiagnosis(prefab);
        
        string modelPath = "Assets/Art/Models/GLB/tiles.glb";
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            Debug.LogError($"[Diagnose] Could not find raw GLB model at: {modelPath}");
            return;
        }

        Debug.Log($"=== Raw Model Diagnosis for '{model.name}' ===");
        LogHierarchyDiagnosis(model);
        
        Debug.Log("=========================================");
    }

    private static void LogHierarchyDiagnosis(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            string objName = filter.gameObject.name;
            string meshName = filter.sharedMesh != null ? filter.sharedMesh.name : "MISSING (null)";
            
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            string matName = "NONE";
            string shaderName = "NONE";
            string colorInfo = "NONE";

            if (renderer != null && renderer.sharedMaterial != null)
            {
                matName = renderer.sharedMaterial.name;
                if (renderer.sharedMaterial.shader != null)
                {
                    shaderName = renderer.sharedMaterial.shader.name;
                }
                
                if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                {
                    colorInfo = $"BaseColor={renderer.sharedMaterial.GetColor("_BaseColor")}";
                }
                else if (renderer.sharedMaterial.HasProperty("_Color"))
                {
                    colorInfo = $"Color={renderer.sharedMaterial.GetColor("_Color")}";
                }
            }
            else if (renderer != null)
            {
                matName = "MISSING MATERIAL (null)";
            }

            Debug.Log($"[Diagnose] Object: '{objName}' | Mesh: '{meshName}' | Material: '{matName}' | Shader: '{shaderName}' | Color: {colorInfo}");
        }
    }
}
