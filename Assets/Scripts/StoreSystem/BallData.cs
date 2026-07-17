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

    // Helper Accessors to match the user's exact specification
    public string ballId => skinId;
    public string ballName => skinName;
    public bool isDefault => unlockedByDefault;
}
