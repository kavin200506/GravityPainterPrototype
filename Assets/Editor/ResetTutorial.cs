#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Quick dev tool to reset the Level 1 tutorial flag so it shows again.
/// Tools → Gravity Painter → Reset Tutorial (Test)
/// </summary>
public static class ResetTutorial
{
    private const string TutorialShownKey = "TutorialShown_Level1";

    [MenuItem("Tools/Gravity Painter/Reset Tutorial (Test)")]
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(TutorialShownKey);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] ✅ Tutorial flag reset — play Level 1 to see it again.");
    }
}
#endif
