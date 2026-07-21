#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility: adds a TutorialManager GameObject to the Procedural level scene
/// so the Level 1 tutorial works without manual setup.
/// Run via: Tools → Gravity Painter → Setup Tutorial
/// </summary>
public static class SetupTutorial
{
    private const string ProceduralScenePath = "Assets/Scenes/Procedural(test).unity";
    private const string TutorialGoName      = "TutorialManager";

    [MenuItem("Tools/Gravity Painter/Setup Tutorial")]
    public static void Run()
    {
        // Open (or get) the procedural scene
        Scene scene = EditorSceneManager.GetSceneByPath(ProceduralScenePath);
        bool wasLoaded = scene.isLoaded;

        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(ProceduralScenePath, OpenSceneMode.Additive);
        }

        // Check if TutorialManager already exists
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == TutorialGoName)
            {
                Debug.Log("[SetupTutorial] TutorialManager already present in scene.");
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, false);
                return;
            }
        }

        // Create TutorialManager GameObject
        GameObject tutorialGo = new GameObject(TutorialGoName);
        tutorialGo.AddComponent<TutorialManager>();
        SceneManager.MoveGameObjectToScene(tutorialGo, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupTutorial] TutorialManager added to " + ProceduralScenePath);

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, false);
    }
}
#endif
