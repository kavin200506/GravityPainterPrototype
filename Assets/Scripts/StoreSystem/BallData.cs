using UnityEngine;

[CreateAssetMenu(fileName = "NewBallData", menuName = "Store System/Ball Data")]
public class BallData : ScriptableObject
{
    public string skinId = "default";
    public string skinName = "Default Ball";
    public Sprite icon;
    public int price = 0;
    public bool unlockedByDefault = false;
    public string prefabResourcePath = "";
    public float speedMultiplier = 1f;

    [Header("Ability & Value Pitch")]
    public string abilityName = "Balanced Handling";
    public string abilityDescription = "Standard speed and baseline control.";
    public string whyToBuy = "Standard starter ball.";

    // Helper Accessors
    public string ballId => skinId;
    public string ballName => skinName;
    public bool isDefault => unlockedByDefault;
}
