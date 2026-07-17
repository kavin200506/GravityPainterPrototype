using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public static event Action<string> OnEquippedChanged;
    public static event Action<string> OnBallUnlocked;

    private List<string> ownedBallIds = new List<string>();
    private string equippedBallId = "default";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadInventory();
    }

    public void LoadInventory()
    {
        ownedBallIds = SaveManager.LoadOwnedBalls("default");
        equippedBallId = SaveManager.LoadEquippedBall("default");
    }

    public bool IsOwned(string ballId, bool unlockedByDefault = false)
    {
        if (unlockedByDefault) return true;
        if (ownedBallIds.Contains(ballId)) return true;
        if (SaveManager.IsBallPurchasedLegacy(ballId))
        {
            if (!ownedBallIds.Contains(ballId))
            {
                ownedBallIds.Add(ballId);
                SaveManager.SaveOwnedBalls(ownedBallIds);
            }
            return true;
        }
        return false;
    }

    public void AddOwned(string ballId)
    {
        if (!ownedBallIds.Contains(ballId))
        {
            ownedBallIds.Add(ballId);
            SaveManager.SaveOwnedBalls(ownedBallIds);
            OnBallUnlocked?.Invoke(ballId);
        }
    }

    public string GetEquippedId()
    {
        return SaveManager.LoadEquippedBall("default");
    }

    public bool IsEquipped(string ballId)
    {
        return GetEquippedId() == ballId;
    }

    public void Equip(string ballId)
    {
        equippedBallId = ballId;
        SaveManager.SaveEquippedBall(ballId);
        OnEquippedChanged?.Invoke(ballId);
    }
}
