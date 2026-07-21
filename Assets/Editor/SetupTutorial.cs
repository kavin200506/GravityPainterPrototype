#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility: adds a TutorialManager GameObject to the Procedural level scene.
/// Runs automatically on editor domain reload (edit mode only) and via menu item.
/// Tools → Gravity Painter → Setup Tutorial
/// </summary>
[InitializeOnLoad]
public static class SetupTutorial
{
    private const string ProceduralScenePath = "Assets/Scenes/Procedural(test).unity";
    private const string TutorialGoName      = "TutorialManager";

    static SetupTutorial()
    {
        // Run after domain reload — but only in edit mode, never during play
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlaying)
                Run();
        };
    }

    [MenuItem("Tools/Gravity Painter/Setup Tutorial")]
    public static void Run()
    {
        // Hard guard: never manipulate scenes during play mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SetupTutorial] Cannot modify scene during Play Mode. Exit play mode first.");
            return;
        }

        // Check if the scene file actually exists
        if (!System.IO.File.Exists(ProceduralScenePath))
        {
            Debug.LogWarning("[SetupTutorial] Procedural scene not found at: " + ProceduralScenePath);
            return;
        }

        // Open the scene additively if not already loaded
        Scene scene = EditorSceneManager.GetSceneByPath(ProceduralScenePath);
        bool wasLoaded = scene.isLoaded;

        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(ProceduralScenePath, OpenSceneMode.Additive);
        }

        // Check if TutorialManager already exists in the scene
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.GetComponent<TutorialManager>() != null)
            {
                Debug.Log("[SetupTutorial] TutorialManager already present in scene — skipping.");
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, false);
                return;
            }
        }

        // Create and add TutorialManager
        GameObject tutorialGo = new GameObject(TutorialGoName);
        tutorialGo.AddComponent<TutorialManager>();
        SceneManager.MoveGameObjectToScene(tutorialGo, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);

        if (saved)
            Debug.Log("[SetupTutorial] ✅ TutorialManager added to " + ProceduralScenePath);
        else
            Debug.LogWarning("[SetupTutorial] Scene could not be saved. Try manually saving.");

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, false);
    }
}
#endif
