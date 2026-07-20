using UnityEngine;

/// <summary>
/// Pure logic for evaluating star rating based on level completion, coin collection, and time.
/// </summary>
public static class StarEvaluator
{
    /// <summary>Fraction of coins that must be collected to earn the coin star (70%).</summary>
    public const float CoinThreshold = 0.7f;

    public struct StarResult
    {
        public bool star1;
        public bool star2;
        public bool star3;
        public int totalStars => (star1 ? 1 : 0) + (star2 ? 1 : 0) + (star3 ? 1 : 0);

        public StarResult(bool s1, bool s2, bool s3)
        {
            star1 = s1;
            star2 = s2;
            star3 = s3;
        }
    }

    /// <summary>
    /// Evaluate stars for a completed level.
    /// </summary>
    /// <param name="collectedCoins">Coins collected by the player.</param>
    /// <param name="totalCoins">Total coins available in the level.</param>
    /// <param name="elapsedTime">Time taken to complete the level.</param>
    /// <param name="parTime">Target time for the level.</param>
    /// <returns>StarResult with earned stars.</returns>
    public static StarResult Evaluate(int collectedCoins, int totalCoins, float elapsedTime, float parTime)
    {
        bool star1 = true;
        bool star2 = EvaluateCoinStar(collectedCoins, totalCoins);
        bool star3 = EvaluateTimeStar(elapsedTime, parTime);

        return new StarResult(star1, star2, star3);
    }

    /// <summary>
    /// Evaluate coin star: collected >= 70% of total.
    /// If total is 0, auto-earn the star.
    /// </summary>
    public static bool EvaluateCoinStar(int collected, int total)
    {
        if (total <= 0)
        {
            return true;
        }

        float percentage = (float)collected / total;
        return percentage >= CoinThreshold;
    }

    /// <summary>
    /// Evaluate time star: elapsed time must be strictly less than par time.
    /// If timer expired (elapsed >= par), the star is NOT earned.
    /// </summary>
    public static bool EvaluateTimeStar(float elapsed, float par)
    {
        if (par <= 0f)
        {
            return true;
        }

        return elapsed < par;
    }

    /// <summary>
    /// Get the coin percentage as a formatted string (e.g. "75%").
    /// </summary>
    public static string GetCoinPercentage(int collected, int total)
    {
        if (total <= 0)
        {
            return "100%";
        }

        float percentage = (float)collected / total * 100f;
        return Mathf.RoundToInt(percentage) + "%";
    }
}
