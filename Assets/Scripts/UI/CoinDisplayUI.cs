using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CoinDisplayUI : MonoBehaviour
{
    public Button Button { get; private set; }
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image coinIcon;

    private void Awake()
    {
        Button = GetComponent<Button>();
        if (coinText == null)
        {
            coinText = GetComponent<TextMeshProUGUI>();
            if (coinText == null)
                coinText = GetComponentInChildren<TextMeshProUGUI>();
        }
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
