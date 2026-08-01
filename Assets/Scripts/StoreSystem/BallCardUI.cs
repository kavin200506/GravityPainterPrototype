using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BallCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBg;
    public Image ballImage;
    public TextMeshProUGUI ballNameText;
    public GameObject lockIcon;
    public Button infoButton;

    [Header("Status & Button Controls")]
    public Button actionButton;
    public Image actionButtonImage;
    public TextMeshProUGUI actionText;
    public Image coinIconImage;
    public GameObject coinIconObj;

    [Header("Sprites")]
    public Sprite pricePanelSprite;
    public Sprite ownedSprite;
    public Sprite equipSprite;
    public Sprite coinSprite;
    public Sprite defaultBallSprite;

    private BallData currentBall;
    private Action<BallData> onClickCallback;

    private void Awake()
    {
        LoadSpritesIfMissing();
    }

    private void LoadSpritesIfMissing()
    {
        if (pricePanelSprite == null)
            pricePanelSprite = Resources.Load<Sprite>("UI/Store_Page/price_panel");
        if (ownedSprite == null)
            ownedSprite = Resources.Load<Sprite>("UI/Store_Page/owned");
        if (equipSprite == null)
            equipSprite = Resources.Load<Sprite>("UI/Store_Page/equip");
        if (coinSprite == null)
            coinSprite = Resources.Load<Sprite>("UI/Store_Page/coin_icon_32");
        if (defaultBallSprite == null)
            defaultBallSprite = Resources.Load<Sprite>("UI/Store_Page/White_ball");
    }

    public void Setup(BallData data, Action<BallData> onClick)
    {
        LoadSpritesIfMissing();
        currentBall = data;
        onClickCallback = onClick;

        if (actionButton != null)
        {
            actionButton.transition = Selectable.Transition.None;

            Navigation nav = actionButton.navigation;
            nav.mode = Navigation.Mode.None;
            actionButton.navigation = nav;

            ColorBlock cb = actionButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;
            actionButton.colors = cb;

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);

                onClickCallback?.Invoke(currentBall);
            });
        }

        EnsureRectTransformLayout();
        EnsureInfoButton();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        EnsureRectTransformLayout();
        EnsureInfoButton();
        LoadSpritesIfMissing();
        if (currentBall == null) return;

        if (ballNameText != null)
        {
            ballNameText.text = currentBall.ballName.ToUpper();
            ballNameText.raycastTarget = false;
        }

        if (ballImage != null)
        {
            if (currentBall.icon != null)
            {
                ballImage.sprite = currentBall.icon;
            }
            else if (defaultBallSprite != null)
            {
                ballImage.sprite = defaultBallSprite;
            }

            ballImage.color = Color.white;
            ballImage.enabled = ballImage.sprite != null;
            ballImage.preserveAspect = true;
            ballImage.raycastTarget = false;
        }

        if (coinIconImage != null && coinSprite != null)
        {
            coinIconImage.sprite = coinSprite;
            coinIconImage.color = Color.white;
            coinIconImage.preserveAspect = true;
            coinIconImage.raycastTarget = false;
        }

        bool isOwned = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(currentBall.ballId, currentBall.isDefault);
        bool isEquipped = InventoryManager.Instance != null && InventoryManager.Instance.IsEquipped(currentBall.ballId);

        // Lock icon display
        if (lockIcon != null)
        {
            lockIcon.SetActive(!isOwned);
            foreach (Graphic g in lockIcon.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
        }

        // State Pill/Button formatting using exact visual sprites
        if (isEquipped)
        {
            if (coinIconObj != null) coinIconObj.SetActive(false);
            if (actionText != null) actionText.text = "EQUIPPED";
            if (actionButtonImage != null)
            {
                if (equipSprite != null)
                {
                    actionButtonImage.sprite = equipSprite;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = new Color(0.15f, 0.7f, 0.25f, 1f);
                }
            }
        }
        else if (isOwned)
        {
            if (coinIconObj != null) coinIconObj.SetActive(false);
            if (actionText != null) actionText.text = "EQUIP";
            if (actionButtonImage != null)
            {
                if (ownedSprite != null)
                {
                    actionButtonImage.sprite = ownedSprite;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    if (pricePanelSprite != null)
                    {
                        actionButtonImage.sprite = pricePanelSprite;
                        actionButtonImage.color = Color.white;
                    }
                    else
                    {
                        actionButtonImage.color = new Color(0.2f, 0.25f, 0.35f, 1f);
                    }
                }
            }
        }
        else
        {
            if (coinIconObj != null) coinIconObj.SetActive(true);
            if (actionText != null) actionText.text = currentBall.price.ToString();
            if (actionButtonImage != null)
            {
                if (pricePanelSprite != null)
                {
                    actionButtonImage.sprite = pricePanelSprite;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = new Color(0.15f, 0.45f, 0.75f, 1f);
                }
            }
        }

        // Center alignment of text when coin icon is hidden
        if (actionText != null)
        {
            actionText.alignment = TextAlignmentOptions.Center;
            actionText.raycastTarget = false;
            if (coinIconObj != null && coinIconObj.activeSelf)
            {
                actionText.rectTransform.offsetMin = new Vector2(40f, 0f);
                actionText.rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                actionText.rectTransform.offsetMin = Vector2.zero;
                actionText.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }

    private void EnsureInfoButton()
    {
        if (infoButton == null)
        {
            Transform existing = transform.Find("InfoButton");
            if (existing != null)
            {
                infoButton = existing.GetComponent<Button>();
            }
            else
            {
                GameObject infoObj = new GameObject("InfoButton", typeof(RectTransform), typeof(Image), typeof(Button));
                infoObj.transform.SetParent(transform, false);

                infoButton = infoObj.GetComponent<Button>();
            }
        }

        if (infoButton != null)
        {
            RectTransform infoRt = infoButton.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(1f, 1f); // Top Right corner
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.pivot = new Vector2(1f, 1f);
            float xOffset = -12f;
            if (currentBall != null)
            {
                if (currentBall.skinId == "yellow")
                    xOffset = 5f; // Shifted further right for Nova Yellow
                else if (currentBall.skinId == "white")
                    xOffset = -3f; // Shifted right for Nova White
            }
            infoRt.anchoredPosition = new Vector2(xOffset, -12f);
            infoRt.sizeDelta = new Vector2(40f, 40f);

            Image infoImg = infoButton.GetComponent<Image>();
            if (infoImg != null)
            {
                infoImg.color = new Color(0.15f, 0.65f, 0.95f, 0.95f);
            }

            Transform textChild = infoButton.transform.Find("Text");
            if (textChild == null)
            {
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(infoButton.transform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
                tmp.text = "i";
                tmp.fontSize = 26f;
                tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }

            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() =>
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);

                ShowBallInfoModal(currentBall);
            });
        }
    }

    public void ShowBallInfoModal(BallData data)
    {
        if (data == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("BallInfoModal");
        if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

        // Glassmorphism Overlay
        GameObject modalObj = new GameObject("BallInfoModal", typeof(RectTransform), typeof(Image));
        modalObj.transform.SetParent(canvas.transform, false);

        RectTransform modalRt = modalObj.GetComponent<RectTransform>();
        modalRt.anchorMin = Vector2.zero;
        modalRt.anchorMax = Vector2.one;
        modalRt.sizeDelta = Vector2.zero;

        Image bgImg = modalObj.GetComponent<Image>();
        bgImg.color = new Color(0f, 0.02f, 0.05f, 0.65f); // Translucent Dark Overlay

        // Card Container
        GameObject cardObj = new GameObject("InfoCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(modalObj.transform, false);

        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(720f, 920f);

        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.06f, 0.12f, 0.22f, 0.95f); // Translucent Dark Blue Glass

        // Border Frame
        GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(cardObj.transform, false);
        RectTransform borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = Vector2.zero;
        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.2f, 0.85f, 1f, 0.4f);

        // Header Title
        CreateText("Title", cardObj.transform, data.ballName.ToUpper(), 56f, new Vector2(0f, 380f), new Color(0.3f, 0.9f, 1f));

        // Ball Icon
        if (data.icon != null)
        {
            GameObject iconObj = new GameObject("BallIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0f, 220f);
            iconRt.sizeDelta = new Vector2(160f, 160f);

            Image img = iconObj.GetComponent<Image>();
            img.sprite = data.icon;
            img.preserveAspect = true;
        }

        // Speed Multiplier Badge
        int speedBonusPercent = Mathf.RoundToInt((data.speedMultiplier - 1f) * 100f);
        string speedBadgeText = speedBonusPercent > 0 ? $"⚡ SPEED BOOST: +{speedBonusPercent}%" : "⚡ SPEED: STANDARD (1.0x)";
        Color speedBadgeColor = speedBonusPercent > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.8f, 0.9f);
        CreateText("SpeedBadge", cardObj.transform, speedBadgeText, 36f, new Vector2(0f, 100f), speedBadgeColor);

        // Ability Name & Description
        CreateText("AbilityHeader", cardObj.transform, "⭐ ABILITY", 34f, new Vector2(0f, 30f), new Color(0.3f, 0.9f, 1f));
        CreateText("AbilityName", cardObj.transform, string.IsNullOrEmpty(data.abilityName) ? "Balanced Control" : data.abilityName, 40f, new Vector2(0f, -20f), Color.white);
        CreateText("AbilityDesc", cardObj.transform, string.IsNullOrEmpty(data.abilityDescription) ? "Standard baseline speed & handling." : data.abilityDescription, 30f, new Vector2(0f, -70f), new Color(0.85f, 0.9f, 1f));

        // Why To Buy Section
        CreateText("WhyHeader", cardObj.transform, "💡 WHY BUY THIS BALL?", 34f, new Vector2(0f, -140f), new Color(1f, 0.75f, 0.2f));
        CreateText("WhyDesc", cardObj.transform, string.IsNullOrEmpty(data.whyToBuy) ? "Essential upgrade for faster level progression!" : data.whyToBuy, 30f, new Vector2(0f, -200f), Color.white);

        // Price Section
        string priceText = data.isDefault || data.price <= 0 ? "STATUS: FREE (UNLOCKED)" : $"PRICE: 💰 {data.price} COINS";
        CreateText("PriceText", cardObj.transform, priceText, 38f, new Vector2(0f, -280f), new Color(0.2f, 0.95f, 0.5f));

        // Close Button
        GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(cardObj.transform, false);

        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchoredPosition = new Vector2(0f, -370f);
        closeRt.sizeDelta = new Vector2(300f, 80f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.18f, 0.55f, 0.95f, 1f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        CreateText("Text", closeBtnObj.transform, "GOT IT!", 38f, Vector2.zero, Color.white);

        closeBtn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(modalObj);
        });
    }

    private GameObject CreateText(string name, Transform parent, string text, float fontSize, Vector2 anchoredPos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(640f, 80f);
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;

        return go;
    }

    private void EnsureRectTransformLayout()
    {
        if (actionButton != null)
        {
            RectTransform btnRt = actionButton.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(0.5f, 0f);
                btnRt.anchorMax = new Vector2(0.5f, 0f);
                btnRt.pivot = new Vector2(0.5f, 0f);
                btnRt.anchoredPosition = new Vector2(0f, 2.936005f);
                btnRt.sizeDelta = new Vector2(235f, 70f);
                btnRt.localScale = new Vector3(0.8146776f, 1.4875f, 1f);
                btnRt.localRotation = Quaternion.identity;
            }
        }

        if (ballImage != null)
        {
            RectTransform bRt = ballImage.GetComponent<RectTransform>();
            if (bRt != null)
            {
                bRt.anchorMin = new Vector2(0.5f, 0.5f);
                bRt.anchorMax = new Vector2(0.5f, 0.5f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.anchoredPosition = new Vector2(0f, 15f);
                bRt.sizeDelta = new Vector2(170f, 170f);
                bRt.localScale = Vector3.one;
                bRt.localRotation = Quaternion.identity;
            }
        }
    }
}
