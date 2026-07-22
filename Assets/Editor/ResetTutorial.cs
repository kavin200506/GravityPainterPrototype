#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Quick dev tool to reset the Level 1 tutorial flag so it shows again.
/// Tools → Gravity Painter → Reset Tutorial (Test)
/// </summary>
public static class ResetTutorial
{
    [MenuItem("Tools/Gravity Painter/Reset Tutorial (Test)")]
    public static void Reset()
    {
        for (int i = 1; i <= 20; i++)
        {
            PlayerPrefs.DeleteKey("TutorialShown_Level" + i);
        }
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] ✅ All tutorial flags reset (Levels 1-20) — play Level 1 or Level 6 to see them again.");
    }
}
#endif
