using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    private const string CoinsKey = "TotalCoins";
    private const string EquippedBallKey = "SelectedSkinId";
    private const string OwnedBallsKey = "OwnedBallsList";
    private const string SkinPurchasedPrefix = "SkinPurchased_";

    public static int LoadCoins()
    {
        return PlayerPrefs.GetInt(CoinsKey, 0);
    }

    public static void SaveCoins(int amount)
    {
        PlayerPrefs.SetInt(CoinsKey, amount);
        PlayerPrefs.Save();
    }

    public static string LoadEquippedBall(string defaultId = "default")
    {
        return PlayerPrefs.GetString(EquippedBallKey, defaultId);
    }

    public static void SaveEquippedBall(string ballId)
    {
        PlayerPrefs.SetString(EquippedBallKey, ballId);
        PlayerPrefs.Save();
    }

    public static List<string> LoadOwnedBalls(string defaultId = "default")
    {
        List<string> owned = new List<string>();
        string raw = PlayerPrefs.GetString(OwnedBallsKey, "");

        if (!string.IsNullOrEmpty(raw))
        {
            string[] split = raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in split)
            {
                if (!owned.Contains(s.Trim()))
                    owned.Add(s.Trim());
            }
        }

        if (!owned.Contains(defaultId))
            owned.Add(defaultId);

        return owned;
    }

    public static void SaveOwnedBalls(List<string> ownedList)
    {
        if (ownedList == null) return;
        string raw = string.Join(",", ownedList);
        PlayerPrefs.SetString(OwnedBallsKey, raw);

        // Sync legacy keys so existing systems stay compatible
        foreach (string id in ownedList)
        {
            PlayerPrefs.SetInt(SkinPurchasedPrefix + id, 1);
        }

        PlayerPrefs.Save();
    }

    public static bool IsBallPurchasedLegacy(string ballId)
    {
        return PlayerPrefs.GetInt(SkinPurchasedPrefix + ballId, 0) == 1;
    }
}
