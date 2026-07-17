using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;

public class PrintHierarchy
{
    [MenuItem("Tools/Gravity Painter/Print MainMenu Hierarchy")]
    public static void Print()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menus/MainMenu.unity", OpenSceneMode.Single);
        StringBuilder sb = new StringBuilder();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PrintRecursive(root.transform, sb, 0);
        }
        System.IO.File.WriteAllText("MainMenu_Hierarchy.txt", sb.ToString());
        Debug.Log("Hierarchy saved to MainMenu_Hierarchy.txt");
    }

    private static void PrintRecursive(Transform t, StringBuilder sb, int depth)
    {
        sb.AppendLine(new string('-', depth * 2) + t.name);
        foreach (Transform child in t)
        {
            PrintRecursive(child, sb, depth + 1);
        }
    }
}
