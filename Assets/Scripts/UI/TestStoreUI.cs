using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the ball skin store page: lists every skin (including the default one),
/// shows its price and a cosmetic ability badge, and lets the player buy/select skins
/// using coins earned in gameplay.
/// </summary>
public class TestStoreUI : MonoBehaviour
{
    private const string NotEnoughCoinsFormat = "Not enough coins! Need {0} more.";
    private const string SkinSelectedFormat = "Selected {0}";
    private const string SkinPurchasedFormat = "Unlocked {0}!";
    private const float StatusMessageDurationSeconds = 2f;

    public GameObject storeRoot;
    public GameObject skinCardPrefab;
    public GameObject gridParent;
    public TextMeshProUGUI coinDisplay;
    public TextMeshProUGUI statusMessage;
    public BallPreviewRenderer previewRenderer;

    [Header("Confirmation Popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmText;
    public Button confirmYesBtn;
    public Button confirmNoBtn;

    [Header("Card Colors")]
    [SerializeField] private Color buyColor = new Color(0.15f, 0.7f, 0.25f, 1f);
    [SerializeField] private Color ownedColor = new Color(0.12f, 0.4f, 0.65f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.85f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color abilityBonusColor = new Color(0.4f, 0.85f, 1f, 1f);
    [SerializeField] private Color abilityStandardColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    private List<BallSkinData> allSkins;
    private readonly List<SkinCard> cards = new List<SkinCard>();
    private BallSkinData _pendingSkin;

    private class SkinCard
    {
        public GameObject root;
        public BallSkinData skin;
        public Button button;
        public Image buttonImage;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI priceLabel;
        public TextMeshProUGUI abilityLabel;
        public Image lockIcon;
        public Image checkIcon;
        public Image ballIcon;
        public Image cardBgImage;
    }

    private void Awake()
    {
        AutoConfigureStoreLayout();
        WireBackButton();
        WireConfirmButtons();
        LoadSkins();
    }

    private void AutoConfigureStoreLayout()
    {
        GameObject root = storeRoot != null ? storeRoot : gameObject;

        // 1. Fullscreen Store Root Background (BackgroundLevels)
        Image bgImage = root.GetComponent<Image>();
        if (bgImage == null) bgImage = root.AddComponent<Image>();
        Sprite bgSprite = Resources.Load<Sprite>("UI/Store_Page/BackgroundLevels");
        if (bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.color = Color.white;
        }

        // 2. Hide any mysterious dark overlays attached to Title/Store
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Image" || child.name.Contains("Square") || child.name.Contains("Overlay"))
            {
                if (child.GetComponent<Image>() != null && child.parent != null && child.parent.name.Contains("Title"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // 3. Format Coin Display
        if (coinDisplay != null)
        {
            coinDisplay.fontSize = 36f;
            coinDisplay.color = new Color(1f, 0.9f, 0.3f, 1f);
            coinDisplay.fontStyle = FontStyles.Bold;
            coinDisplay.alignment = TextAlignmentOptions.Right;

            RectTransform coinRt = coinDisplay.GetComponent<RectTransform>();
            if (coinRt != null)
            {
                coinRt.anchorMin = new Vector2(1f, 1f);
                coinRt.anchorMax = new Vector2(1f, 1f);
                coinRt.pivot = new Vector2(1f, 1f);
                coinRt.anchoredPosition = new Vector2(-60f, -40f);
                coinRt.sizeDelta = new Vector2(400f, 60f);
            }
        }

        // 4. Configure Scroll Rect & Viewport Masking so items stay strictly inside frame
        ScrollRect scrollRect = root.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null)
        {
            RectTransform sRt = scrollRect.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = new Vector2(40f, 60f);
            sRt.offsetMax = new Vector2(-40f, -320f); // Top offset leaves space for Header / 3D Preview

            if (scrollRect.viewport != null)
            {
                Mask vMask = scrollRect.viewport.GetComponent<Mask>();
                if (vMask == null) vMask = scrollRect.viewport.gameObject.AddComponent<Mask>();
                vMask.showMaskGraphic = false;

                Image vImg = scrollRect.viewport.GetComponent<Image>();
                if (vImg == null) vImg = scrollRect.viewport.gameObject.AddComponent<Image>();
                vImg.color = new Color(1f, 1f, 1f, 0.01f);
            }

            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 40f;
        }

        // 5. Configure Grid Parent (Vertical Layout for rows)
        if (gridParent != null)
        {
            RectTransform gRt = gridParent.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0f, 1f);
            gRt.anchorMax = new Vector2(1f, 1f);
            gRt.pivot = new Vector2(0.5f, 1f);
            gRt.offsetMin = Vector2.zero;
            gRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = gridParent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                GridLayoutGroup glg = gridParent.GetComponent<GridLayoutGroup>();
                if (glg != null) DestroyImmediate(glg);
                vlg = gridParent.AddComponent<VerticalLayoutGroup>();
            }

            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 24f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = gridParent.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = gridParent.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void WireConfirmButtons()
    {
        if (confirmYesBtn != null)
        {
            confirmYesBtn.onClick.RemoveAllListeners();
            confirmYesBtn.onClick.AddListener(ConfirmPurchase);
        }
        if (confirmNoBtn != null)
        {
            confirmNoBtn.onClick.RemoveAllListeners();
            confirmNoBtn.onClick.AddListener(CancelPurchase);
        }
    }

    private void LoadSkins()
    {
        allSkins = new List<BallSkinData>(Resources.LoadAll<BallSkinData>("Settings/BallSkins"));
        allSkins.Sort((a, b) =>
        {
            if (a.unlockedByDefault != b.unlockedByDefault)
                return a.unlockedByDefault ? -1 : 1;
            return a.price.CompareTo(b.price);
        });
    }

    private void WireBackButton()
    {
        Transform backBtnT = transform.Find("BackButton");
        if (backBtnT == null && storeRoot != null) backBtnT = storeRoot.transform.Find("BackButton");
        if (backBtnT == null) return;

        Button btn = backBtnT.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (storeRoot != null)
            storeRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (storeRoot != null)
            storeRoot.SetActive(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform mainMenu = FindDeep(canvas.transform, "MainMenu");
            if (mainMenu != null)
                mainMenu.gameObject.SetActive(true);
        }
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void OnDisable()
    {
        foreach (var card in cards)
        {
            if (card.root != null)
                Destroy(card.root);
        }
        cards.Clear();
    }

    public void Refresh()
    {
        if (allSkins == null || allSkins.Count == 0)
            LoadSkins();

        UpdateCoinDisplay();
        RebuildCards();
    }

    private void UpdateCoinDisplay()
    {
        if (coinDisplay != null)
            coinDisplay.text = "Coins: " + CoinManager.GetTotalCoins();
    }

    private void RebuildCards()
    {
        foreach (var card in cards)
        {
            if (card.root != null)
                Destroy(card.root);
        }
        cards.Clear();

        if (skinCardPrefab == null || gridParent == null || allSkins == null)
            return;

        Sprite cardBgSprite = Resources.Load<Sprite>("UI/Store_Page/BallBox");

        foreach (BallSkinData skin in allSkins)
        {
            if (skin == null) continue;

            GameObject cardObj = Instantiate(skinCardPrefab, gridParent.transform);
            
            // Fix Card Container dimensions
            LayoutElement le = cardObj.GetComponent<LayoutElement>();
            if (le == null) le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 180f;
            le.preferredHeight = 180f;
            le.flexibleWidth = 1f;

            SkinCard card = new SkinCard();
            card.root = cardObj;
            card.skin = skin;

            card.cardBgImage = cardObj.GetComponent<Image>();
            if (card.cardBgImage != null && cardBgSprite != null)
            {
                card.cardBgImage.sprite = cardBgSprite;
                card.cardBgImage.type = Image.Type.Sliced;
                card.cardBgImage.color = Color.white; // Don't stain mustard yellow!
            }

            TextMeshProUGUI[] labels = cardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels)
            {
                if (label.name.Contains("Name"))
                    card.nameLabel = label;
                else if (label.name.Contains("Price"))
                    card.priceLabel = label;
                else if (label.name.Contains("Ability"))
                    card.abilityLabel = label;
            }

            Image[] images = cardObj.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject == cardObj) continue;

                if (img.name.Contains("Lock"))
                    card.lockIcon = img;
                else if (img.name.Contains("Check"))
                    card.checkIcon = img;
                else if (img.name.Contains("Icon") || img.name.Contains("Preview") || img.name.Contains("Ball"))
                    card.ballIcon = img;
            }

            card.button = cardObj.GetComponentInChildren<Button>(true);
            if (card.button != null)
            {
                card.buttonImage = card.button.GetComponent<Image>();
                BallSkinData captured = skin;
                card.button.onClick.AddListener(() => OnSkinCardClicked(captured));
            }

            LayoutCardElements(card);
            ApplyCardState(card);
            cards.Add(card);
        }
    }

    private void LayoutCardElements(SkinCard card)
    {
        if (card == null || card.root == null) return;

        // Position Ball Icon on Left side
        if (card.ballIcon != null)
        {
            RectTransform bRt = card.ballIcon.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(30f, 0f);
            bRt.sizeDelta = new Vector2(130f, 130f);
            card.ballIcon.preserveAspect = true;
        }

        // Position Name Label (Middle Top)
        if (card.nameLabel != null)
        {
            RectTransform nRt = card.nameLabel.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(180f, -25f);
            nRt.sizeDelta = new Vector2(-480f, 50f);
            card.nameLabel.alignment = TextAlignmentOptions.Left;
            card.nameLabel.fontSize = 36f;
            card.nameLabel.fontStyle = FontStyles.Bold;
            card.nameLabel.color = Color.white;
        }

        // Position Ability Description (Middle Bottom)
        if (card.abilityLabel != null)
        {
            RectTransform aRt = card.abilityLabel.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0f, 0f);
            aRt.anchorMax = new Vector2(1f, 0f);
            aRt.pivot = new Vector2(0f, 0f);
            aRt.anchoredPosition = new Vector2(180f, 30f);
            aRt.sizeDelta = new Vector2(-480f, 50f);
            card.abilityLabel.alignment = TextAlignmentOptions.Left;
            card.abilityLabel.fontSize = 24f;
        }

        // Position Action Button on Right side (PROPER SIZE, NOT SQUISHED!)
        if (card.button != null)
        {
            RectTransform btnRt = card.button.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-30f, 0f);
            btnRt.sizeDelta = new Vector2(280f, 75f);

            if (card.buttonImage != null)
                card.buttonImage.preserveAspect = false;

            if (card.priceLabel != null)
            {
                RectTransform pRt = card.priceLabel.GetComponent<RectTransform>();
                pRt.anchorMin = Vector2.zero;
                pRt.anchorMax = Vector2.one;
                pRt.offsetMin = Vector2.zero;
                pRt.offsetMax = Vector2.zero;
                card.priceLabel.alignment = TextAlignmentOptions.Center;
                card.priceLabel.fontSize = 26f;
                card.priceLabel.fontStyle = FontStyles.Bold;
                card.priceLabel.color = Color.white;
            }
        }

        // Lock Icon over the Ball Icon
        if (card.lockIcon != null)
        {
            RectTransform lRt = card.lockIcon.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0.5f, 0.5f);
            lRt.anchoredPosition = new Vector2(95f, 0f);
            lRt.sizeDelta = new Vector2(80f, 80f);
            card.lockIcon.preserveAspect = true;
        }

        // Checkmark Icon on top right of card
        if (card.checkIcon != null)
        {
            RectTransform cRt = card.checkIcon.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-15f, -15f);
            cRt.sizeDelta = new Vector2(40f, 40f);
            card.checkIcon.preserveAspect = true;
        }
    }

    private void ApplyCardState(SkinCard card)
    {
        if (card == null || card.skin == null) return;

        bool purchased = BallSkinManager.IsSkinPurchased(card.skin.skinId);
        bool selected = BallSkinManager.GetSelectedSkinId() == card.skin.skinId;
        bool unlocked = purchased || card.skin.unlockedByDefault;

        if (card.nameLabel != null)
            card.nameLabel.text = card.skin.skinName;

        if (card.ballIcon != null)
        {
            bool hasIcon = card.skin.icon != null;
            card.ballIcon.enabled = hasIcon;
            if (hasIcon)
                card.ballIcon.sprite = card.skin.icon;
        }

        if (card.abilityLabel != null)
        {
            bool hasBonus = card.skin.speedMultiplier > 1f;
            card.abilityLabel.text = hasBonus ? ("\u26A1 " + card.skin.abilityDescription) : card.skin.abilityDescription;
            card.abilityLabel.color = hasBonus ? abilityBonusColor : abilityStandardColor;
        }

        if (card.priceLabel != null)
        {
            if (selected)
                card.priceLabel.text = "SELECTED";
            else if (unlocked)
                card.priceLabel.text = "EQUIP";
            else
                card.priceLabel.text = "BUY (" + card.skin.price + ")";
        }

        if (card.buttonImage != null)
        {
            if (selected)
                card.buttonImage.color = selectedColor;
            else if (unlocked)
                card.buttonImage.color = ownedColor;
            else
                card.buttonImage.color = buyColor;
        }

        if (card.lockIcon != null)
            card.lockIcon.gameObject.SetActive(!unlocked);

        if (card.checkIcon != null)
            card.checkIcon.gameObject.SetActive(selected);
    }

    private void OnSkinCardClicked(BallSkinData skin)
    {
        if (skin == null) return;

        if (previewRenderer != null)
            previewRenderer.ShowSkin(skin);

        bool unlocked = BallSkinManager.IsSkinPurchased(skin.skinId) || skin.unlockedByDefault;
        bool selected = BallSkinManager.GetSelectedSkinId() == skin.skinId;

        if (selected) return;

        if (unlocked)
        {
            BallSkinManager.SelectSkin(skin.skinId);
            ShowStatus(string.Format(SkinSelectedFormat, skin.skinName));
            Refresh();
        }
        else
        {
            int totalCoins = CoinManager.GetTotalCoins();
            if (totalCoins < skin.price)
            {
                ShowStatus(string.Format(NotEnoughCoinsFormat, skin.price - totalCoins));
                return;
            }

            _pendingSkin = skin;
            if (confirmPopup != null && confirmText != null)
            {
                confirmText.text = "Buy " + skin.skinName + " for " + skin.price + " coins?";
                confirmPopup.SetActive(true);
            }
        }
    }

    private void ConfirmPurchase()
    {
        if (_pendingSkin == null) return;

        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        if (CoinManager.SpendCoins(_pendingSkin.price))
        {
            BallSkinManager.PurchaseSkin(_pendingSkin.skinId);
            BallSkinManager.SelectSkin(_pendingSkin.skinId);
            ShowStatus(string.Format(SkinPurchasedFormat, _pendingSkin.skinName));
            Refresh();
        }

        _pendingSkin = null;
    }

    private void CancelPurchase()
    {
        if (confirmPopup != null)
            confirmPopup.SetActive(false);
        _pendingSkin = null;
    }

    private void ShowStatus(string message)
    {
        if (statusMessage != null)
        {
            StopAllCoroutines();
            statusMessage.text = message;
            statusMessage.gameObject.SetActive(true);
            StartCoroutine(HideStatusAfterDelay());
        }
    }

    private IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSeconds(StatusMessageDurationSeconds);
        if (statusMessage != null)
            statusMessage.gameObject.SetActive(false);
    }
}

