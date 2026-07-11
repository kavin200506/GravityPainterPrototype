using UnityEngine;

/// <summary>
/// Static timer that counts down from a par time calculated from tile count.
/// Par time = tileCount * SecondsPerTile (3.5s default).
/// </summary>
public static class LevelTimer
{
    private const float SecondsPerTile = 3.5f;
    private const float MinParTime = 15f;
    private const float MaxParTime = 300f;

    public static float ParTime { get; private set; }
    public static float ElapsedTime { get; private set; }
    public static float RemainingTime => Mathf.Max(0f, ParTime - ElapsedTime);
    public static bool IsRunning { get; private set; }
    public static bool IsComplete { get; private set; }
    public static bool TimeExpired => ElapsedTime >= ParTime;

    /// <summary>
    /// Calculate par time from tile count.
    /// </summary>
    public static float CalculateParTime(int tileCount)
    {
        float par = tileCount * SecondsPerTile;
        return Mathf.Clamp(par, MinParTime, MaxParTime);
    }

    /// <summary>
    /// Start the timer with a given par time.
    /// </summary>
    public static void Start(float parTime)
    {
        ParTime = Mathf.Clamp(parTime, MinParTime, MaxParTime);
        ElapsedTime = 0f;
        IsRunning = true;
        IsComplete = false;
    }

    /// <summary>
    /// Start the timer by calculating par from tile count.
    /// </summary>
    public static void Start(int tileCount)
    {
        Start(CalculateParTime(tileCount));
    }

    /// <summary>
    /// Tick the timer (call in Update). Uses Time.deltaTime so it respects timeScale.
    /// </summary>
    public static void Tick()
    {
        if (!IsRunning || IsComplete)
        {
            return;
        }

        ElapsedTime += Time.deltaTime;

        if (ElapsedTime >= ParTime)
        {
            ElapsedTime = ParTime;
            IsRunning = false;
            IsComplete = true;
        }
    }

    /// <summary>
    /// Stop the timer. Call when the player crosses the finish line.
    /// </summary>
    public static void Stop()
    {
        IsRunning = false;
        IsComplete = true;
    }

    /// <summary>
    /// Reset the timer completely.
    /// </summary>
    public static void Reset()
    {
        ParTime = 0f;
        ElapsedTime = 0f;
        IsRunning = false;
        IsComplete = false;
    }

    /// <summary>
    /// Format time as M:SS (e.g. "1:05").
    /// </summary>
    public static string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return minutes + ":" + seconds.ToString("D2");
    }

    /// <summary>
    /// Get formatted remaining time for HUD display.
    /// </summary>
    public static string GetRemainingTimeString()
    {
        return FormatTime(RemainingTime);
    }

    /// <summary>
    /// Get formatted par time string.
    /// </summary>
    public static string GetParTimeString()
    {
        return FormatTime(ParTime);
    }
}
