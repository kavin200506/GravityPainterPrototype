using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class FixMenuPages : EditorWindow
{
    [MenuItem("Tools/Gravity Painter/Fix Menu Pages Fullscreen")]
    public static void FixPages()
    {
        string scenePath = "Assets/Scenes/Menus/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find all PopUp objects or Level/Settings panels
        GameObject[] rootObjects = scene.GetRootGameObjects();
        int fixedCount = 0;

        foreach (GameObject root in rootObjects)
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas == null) continue;

            // Find popups
            Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t.name == "PopUp" || t.name.Contains("Settings") || t.name.Contains("LevelSelect"))
                {
                    RectTransform rt = t as RectTransform;
                    if (rt != null)
                    {
                        Undo.RecordObject(rt, "Stretch Panel");
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        
                        // Also check if we should remove Image margins
                        Image img = rt.GetComponent<Image>();
                        if (img != null && img.type == Image.Type.Sliced)
                        {
                            // If it's a popup window, maybe they want it to lose the rounded corners/margins if they want full screen
                            // But usually just stretching is enough
                        }
                        
                        fixedCount++;
                        EditorUtility.SetDirty(rt);
                    }
                }
            }
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FixMenuPages] Fixed {fixedCount} panels to stretch full screen.");
        }
        else
        {
            Debug.Log("[FixMenuPages] Could not find any panels to fix.");
        }
    }
}
