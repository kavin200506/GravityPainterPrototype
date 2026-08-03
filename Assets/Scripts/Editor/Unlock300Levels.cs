using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Developer Testing Tools under Tools -> Gravity Painter.
/// Includes options to Unlock / Lock 300 Levels and Unlock / Lock All Store Balls.
/// </summary>
public static class Unlock300Levels
{
    private static readonly string[] KnownSkinIds = new string[] { "default", "blue", "red", "white", "yellow" };

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
        EditorUtility.DisplayDialog("Unlock Levels", "Successfully unlocked 300 levels with 3 stars in Gravity Painter!", "OK");
    }

    [MenuItem("Tools/Gravity Painter/Lock 300 Levels")]
    public static void LockAll300Levels()
    {
        PlayerPrefs.DeleteKey("UnlockedLevel");
        PlayerPrefs.DeleteKey("ProceduralLevelsCompleted");
        PlayerPrefs.SetInt("UnlockedLevel", 1);
        PlayerPrefs.SetInt("ProceduralLevelsCompleted", 0);

        for (int i = 1; i <= 300; i++)
        {
            PlayerPrefs.DeleteKey("LevelStars_" + i);
        }

        PlayerPrefs.Save();

        Debug.Log("🔒 [Gravity Painter] Successfully locked 300 levels! Reset to Level 1.");
        EditorUtility.DisplayDialog("Lock Levels", "Successfully locked 300 levels. Progress reset to Level 1!", "OK");
    }

    [MenuItem("Tools/Gravity Painter/Unlock All Store Balls")]
    public static void UnlockAllStoreBalls()
    {
        List<string> ownedList = new List<string>(KnownSkinIds);
        SaveManager.SaveOwnedBalls(ownedList);

        foreach (string skin in KnownSkinIds)
        {
            PlayerPrefs.SetInt("SkinPurchased_" + skin, 1);
        }

        PlayerPrefs.Save();

        InventoryManager inv = Object.FindFirstObjectByType<InventoryManager>();
        if (inv != null) inv.LoadInventory();

        StoreManager store = Object.FindFirstObjectByType<StoreManager>();
        if (store != null) store.RefreshStore();

        Debug.Log("⚽ [Gravity Painter] Successfully unlocked all store balls!");
        EditorUtility.DisplayDialog("Unlock Balls", "Successfully unlocked all store balls (Default, Blue, Red, White, Yellow)!", "OK");
    }

    [MenuItem("Tools/Gravity Painter/Lock All Store Balls")]
    public static void LockAllStoreBalls()
    {
        foreach (string skin in KnownSkinIds)
        {
            PlayerPrefs.DeleteKey("SkinPurchased_" + skin);
            PlayerPrefs.SetInt("SkinPurchased_" + skin, 0);
        }

        PlayerPrefs.SetInt("SkinPurchased_default", 1);
        SaveManager.SaveOwnedBalls(new List<string> { "default" });
        SaveManager.SaveEquippedBall("default");
        PlayerPrefs.Save();

        InventoryManager inv = Object.FindFirstObjectByType<InventoryManager>();
        if (inv != null) inv.LoadInventory();

        StoreManager store = Object.FindFirstObjectByType<StoreManager>();
        if (store != null) store.RefreshStore();

        Debug.Log("🔒 [Gravity Painter] Successfully locked all store balls! Only Default ball is owned.");
        EditorUtility.DisplayDialog("Lock Balls", "Successfully locked all store balls. Only Default ball is owned/equipped!", "OK");
    }
}
