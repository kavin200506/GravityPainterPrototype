using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Editor tool: Automatically finds all screen-space Canvases in all game scenes
/// and wraps their UI contents under a SafeArea panel to handle notches/punch-holes.
/// Run via: Tools → Gravity Painter → Apply Safe Area (All Scenes)
/// </summary>
public static class ApplySafeArea
{
    [MenuItem("Tools/Gravity Painter/Apply Safe Area (All Scenes)")]
    public static void ApplyAll()
    {
        // Save the current scene first
        string currentScene = EditorSceneManager.GetActiveScene().path;
        bool savedCurrent   = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (!savedCurrent) return;

        string[] scenePaths = {
            "Assets/Scenes/LoadingScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/Menus/MainMenu.unity",
            "Assets/Scenes/Menus/DeveloperProceduralLevelSelect.unity",
            "Assets/Scenes/Levels/Level 1.unity",
            "Assets/Scenes/Levels/Level 2.unity",
            "Assets/Scenes/Levels/Level 3.unity",
            "Assets/Scenes/Levels/Level 4.unity",
            "Assets/Scenes/Levels/Level 5.unity",
            "Assets/Procedural(test).unity"
        };

        int totalApplied = 0;

        foreach (string path in scenePaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int appliedInScene = 0;

            // Find all Canvas components in the scene
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                // Skip world-space canvases
                if (canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                // Check if SafeArea is already applied under this canvas
                SafeArea existingSafeArea = canvas.GetComponentInChildren<SafeArea>(true);
                if (existingSafeArea != null)
                {
                    Debug.Log($"[ApplySafeArea] SafeArea already exists under Canvas '{canvas.name}' in '{path}'. Skipping.");
                    continue;
                }

                // Create a new child GameObject for the SafeArea panel
                GameObject safeAreaObj = new GameObject("SafeAreaPanel", typeof(RectTransform), typeof(SafeArea));
                Undo.RegisterCreatedObjectUndo(safeAreaObj, "Create SafeAreaPanel");

                RectTransform safeAreaRect = safeAreaObj.GetComponent<RectTransform>();
                safeAreaRect.SetParent(canvas.transform, false);

                // Set it to full stretch/fill
                safeAreaRect.anchorMin = Vector2.zero;
                safeAreaRect.anchorMax = Vector2.one;
                safeAreaRect.offsetMin = Vector2.zero;
                safeAreaRect.offsetMax = Vector2.zero;

                // Move all pre-existing UI children under SafeAreaPanel
                List<Transform> childrenToMove = new List<Transform>();
                foreach (Transform child in canvas.transform)
                {
                    // Skip the newly created safeAreaObj itself
                    if (child == safeAreaRect) continue;

                    // Only move RectTransforms
                    if (child is RectTransform)
                    {
                        childrenToMove.Add(child);
                    }
                }

                foreach (Transform child in childrenToMove)
                {
                    Undo.SetTransformParent(child, safeAreaRect, "Move child to SafeAreaPanel");
                }

                EditorUtility.SetDirty(canvas);
                appliedInScene++;
                totalApplied++;
                Debug.Log($"[ApplySafeArea] Added SafeAreaPanel to Canvas '{canvas.name}' in scene: {path}");
            }

            if (appliedInScene > 0)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        // Re-open the original scene
        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Safe Area Applied",
            $"Done! Automatically wrapped UI contents with a SafeAreaPanel under {totalApplied} Canvas(es) across all scenes.\n\n" +
            "This will dynamically adjust all UI components away from hardware notches and punch holes.",
            "Awesome!");
    }
}
