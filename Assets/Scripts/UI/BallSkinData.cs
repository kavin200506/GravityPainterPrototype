using UnityEngine;

[CreateAssetMenu(menuName = "Gravity Painter/Ball Skin Data")]
public class BallSkinData : BallData
{
    [Header("Store Display")]
    [Tooltip("Flavor text describing this skin's special ability, shown as a badge on the store card.")]
    public string abilityDescription = "Balanced Handling";
}
