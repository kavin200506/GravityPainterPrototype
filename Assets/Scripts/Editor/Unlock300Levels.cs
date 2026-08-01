using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool menu item under Tools -> Gravity Painter -> Unlock 300 Levels.
/// Instantly unlocks 300 levels for testing and sets difficulty progression.
/// </summary>
public static class Unlock300Levels
{
    [MenuItem("Tools/Gravity Painter/Unlock 300 Levels")]
    public static void UnlockAll300Levels()
    {
        LevelProgress.UnlockThrough(300);
        DifficultyManager.SetLevelsCompleted(300);

        for (int i = 1; i <= 300; i++)
        {
            if (LevelProgress.GetStars(i) < 3)
            {
                LevelProgress.SaveStars(i, 3);
            }
        }

        PlayerPrefs.Save();

        Debug.Log("✅ [Gravity Painter] Successfully unlocked 300 levels with 3 stars!");
        EditorUtility.DisplayDialog("Unlock Levels", "Successfully unlocked 300 levels in Gravity Painter!", "OK");
    }
}
