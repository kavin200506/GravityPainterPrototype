using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public static event Action<int> OnBalanceChanged;

    private int currentBalance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadCurrency();
    }

    public void LoadCurrency()
    {
        currentBalance = SaveManager.LoadCoins();
        OnBalanceChanged?.Invoke(currentBalance);
    }

    public int GetBalance()
    {
        return SaveManager.LoadCoins();
    }

    public bool CanAfford(int amount)
    {
        return GetBalance() >= amount;
    }

    public bool Spend(int amount)
    {
        int balance = GetBalance();
        if (balance < amount) return false;

        currentBalance = balance - amount;
        SaveManager.SaveCoins(currentBalance);
        OnBalanceChanged?.Invoke(currentBalance);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        currentBalance = GetBalance() + amount;
        SaveManager.SaveCoins(currentBalance);
        OnBalanceChanged?.Invoke(currentBalance);
    }
}
