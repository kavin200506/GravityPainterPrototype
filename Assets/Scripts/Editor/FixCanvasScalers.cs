using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Editor tool: fixes all Canvas Scalers in all scenes to use
/// "Scale With Screen Size" at 1080x1920 portrait with height matching.
/// Run via: Tools → Gravity Painter → Fix Canvas Scalers (All Scenes)
/// </summary>
public static class FixCanvasScalers
{
    private const int    REF_WIDTH  = 1080;
    private const int    REF_HEIGHT = 1920;
    private const float  MATCH      = 1f; // 1 = match height (best for portrait games)

    [MenuItem("Tools/Gravity Painter/Fix Canvas Scalers (All Scenes)")]
    public static void FixAll()
    {
        // Save the current scene first
        string currentScene = EditorSceneManager.GetActiveScene().path;
        bool savedCurrent   = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (!savedCurrent) return;

        string[] scenePaths = {
            "Assets/Scenes/LoadingScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/Menus/MainMenu.unity",
            "Assets/Scenes/Levels/Level 1.unity",
            "Assets/Scenes/Levels/Level 2.unity",
            "Assets/Scenes/Levels/Level 3.unity",
            "Assets/Scenes/Levels/Level 4.unity",
            "Assets/Scenes/Levels/Level 5.unity",
        };

        int totalFixed = 0;

        foreach (string path in scenePaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int fixedInScene = 0;

            // Find every CanvasScaler in this scene
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (CanvasScaler scaler in scalers)
            {
                // Skip world-space canvases (e.g. 3D scoreboards)
                if (scaler.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace)
                    continue;

                EditorUtility.SetDirty(scaler);

                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(REF_WIDTH, REF_HEIGHT);
                scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight  = MATCH;

                fixedInScene++;
                totalFixed++;
            }

            if (fixedInScene > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[FixCanvasScalers] Fixed {fixedInScene} scaler(s) in {path}");
            }
        }

        // Re-open the original scene
        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Canvas Scalers Fixed",
            $"Done! Fixed {totalFixed} Canvas Scaler(s) across all scenes.\n\n" +
            "All canvases now use:\n" +
            $"• Scale With Screen Size\n" +
            $"• Reference: {REF_WIDTH}×{REF_HEIGHT}\n" +
            $"• Match: Height (1.0)\n\n" +
            "This ensures consistent UI across all phone sizes.",
            "Great!");
    }
}
