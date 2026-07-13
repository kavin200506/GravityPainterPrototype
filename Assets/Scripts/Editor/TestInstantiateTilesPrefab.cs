using UnityEngine;
using UnityEditor;

public static class TestInstantiateTilesPrefab
{
    [MenuItem("Tools/Gravity Painter/Test Instantiate Tiles Prefab")]
    public static void InstantiateDebugPrefab()
    {
        string prefabPath = "Assets/Resources/Visuals/Tiles/TilesGlbMesh.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[DebugInstantiate] Prefab not found at: {prefabPath}");
            return;
        }

        // Clean up previous debug instances
        GameObject oldDebug = GameObject.Find("_DEBUG_TilesGlbMesh");
        if (oldDebug != null)
        {
            Object.DestroyImmediate(oldDebug);
        }

        GameObject debugInstance = Object.Instantiate(prefab);
        debugInstance.name = "_DEBUG_TilesGlbMesh";
        debugInstance.transform.position = new Vector3(0f, 2f, 0f);
        debugInstance.transform.localScale = Vector3.one;

        // Ping the object in hierarchy so user can inspect it
        Selection.activeGameObject = debugInstance;
        EditorGUIUtility.PingObject(debugInstance);

        Debug.Log("[DebugInstantiate] Spawned '_DEBUG_TilesGlbMesh' at (0, 2, 0). Inspect its transform, MeshFilters, and Materials in the Inspector.");
    }
}
