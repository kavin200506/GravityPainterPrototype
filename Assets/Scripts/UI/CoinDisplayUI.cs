using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image coinIcon;

    private void Awake()
    {
        if (coinText == null)
            coinText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        UpdateCoinDisplay();
    }

    public void UpdateCoinDisplay()
    {
        if (coinText != null)
            coinText.text = CoinManager.GetTotalCoins().ToString();

        if (coinIcon == null)
        {
            Transform iconT = transform.Find("CoinIcon");
            if (iconT != null)
                coinIcon = iconT.GetComponent<Image>();
        }

        if (coinIcon != null && coinIcon.sprite == null)
        {
            Sprite s = Resources.Load<Sprite>("UI/coin_icon");
            if (s != null)
                coinIcon.sprite = s;
        }
    }
}
