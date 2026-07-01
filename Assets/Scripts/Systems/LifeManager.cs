using UnityEngine;

public static class LifeManager
{
    public const int MaxLives = 3;
    public static int CurrentLives { get; private set; } = MaxLives;

    public static void ResetLives()
    {
        CurrentLives = MaxLives;
    }

    public static bool LoseLife()
    {
        CurrentLives--;
        return CurrentLives > 0;
    }
}
