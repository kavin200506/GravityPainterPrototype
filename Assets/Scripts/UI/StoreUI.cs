using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class StoreUI : MonoBehaviour
{
    [Header("References")]
    public BallPreviewRenderer previewRenderer;
    public TextMeshProUGUI coinDisplay;
    public TextMeshProUGUI statusMessage;
    public GameObject storePanel;

    [Header("Grid")]
    public GameObject gridParent;
    public GameObject skinCardPrefab;

    [Header("Skin Data")]
    public List<BallSkinData> allSkins;

    [Header("Messages")]
    [SerializeField] private string notEnoughCoinsMsg = "Not enough coins!";
    [SerializeField] private string skinUnlockedMsg = "New skin unlocked!";
    [SerializeField] private float statusMessageDuration = 2f;

    [Header("Confirmation Popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmText;
    public Button confirmYesBtn;
    public Button confirmNoBtn;

    private readonly List<SkinCard> _cards = new List<SkinCard>();
    private BallSkinData _pendingSkin;
    private BallSkinData _selectedSkin;

    private class SkinCard
    {
        public GameObject root;
        public BallSkinData skin;
        public Button button;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI priceLabel;
        public Image lockIcon;
        public Image checkIcon;
        public Image previewIcon;
        public GameObject coinIcon;
    }

    private void Awake()
    {
        if (allSkins == null || allSkins.Count == 0)
            allSkins = new List<BallSkinData>(Resources.LoadAll<BallSkinData>("Settings/BallSkins"));

        AutoConfigureLayoutRuntime();

        WireBackButton();
        WireConfirmButtons();
    }

    private void OnValidate()
    {
        AutoConfigureLayoutRuntime();
    }

    private void AutoConfigureLayoutRuntime()
    {
        GameObject panelObj = storePanel != null ? storePanel : gameObject;

        // Force store panel to full screen
        RectTransform storeRect = panelObj.GetComponent<RectTransform>();
        if (storeRect != null)
        {
            storeRect.anchorMin = Vector2.zero;
            storeRect.anchorMax = Vector2.one;
            storeRect.offsetMin = Vector2.zero;
            storeRect.offsetMax = Vector2.zero;
        }

        Image bgImg = panelObj.GetComponent<Image>();
        if (bgImg != null)
        {
            Sprite bgSprite = Resources.Load<Sprite>("UI/Store_Page/BackgroundLevels");
            if (bgSprite != null) bgImg.sprite = bgSprite;
            bgImg.color = Color.white;
        }

        // Configure Grid Parent and Scroll Rect natively
        if (gridParent != null)
        {
            GridLayoutGroup grid = gridParent.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = gridParent.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(400f, 550f);
            grid.spacing = new Vector2(50f, 50f);
            grid.padding = new RectOffset(50, 50, 50, 50);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2; // balanced 2 columns

            ContentSizeFitter fitter = gridParent.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = gridParent.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Ensure ScrollRect doesn't overlap top area (Title/Back Button)
            ScrollRect scroll = panelObj.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
            {
                RectTransform scrollRect = scroll.GetComponent<RectTransform>();
                scrollRect.anchorMin = new Vector2(0f, 0f);
                scrollRect.anchorMax = new Vector2(1f, 1f);
                scrollRect.offsetMin = new Vector2(50f, 50f);
                scrollRect.offsetMax = new Vector2(-50f, -250f); // 250px space for Top Bar
                
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = true;
                scroll.decelerationRate = 0.135f;
                scroll.scrollSensitivity = 30f;

                Image scrollBg = scroll.GetComponent<Image>();
                if (scrollBg == null) scrollBg = scroll.gameObject.AddComponent<Image>();
                if (scrollBg.sprite == null) scrollBg.color = new Color(0f, 0f, 0f, 0f);
                scrollBg.raycastTarget = true;

                if (scroll.viewport != null)
                {
                    Mask oldMask = scroll.viewport.GetComponent<Mask>();
                    if (oldMask != null)
                    {
                        if (Application.isPlaying) Destroy(oldMask);
                        else DestroyImmediate(oldMask);
                    }
                    Image viewImg = scroll.viewport.GetComponent<Image>();
                    if (viewImg != null) viewImg.raycastTarget = false;
                    RectMask2D rMask = scroll.viewport.GetComponent<RectMask2D>();
                    if (rMask == null) scroll.viewport.gameObject.AddComponent<RectMask2D>();
                }
            }
        }

        // Enforce Back Button layout from screenshot
        Transform backBtnT = transform.Find("BackButton");
        if (backBtnT == null && panelObj != null)
            backBtnT = panelObj.transform.Find("BackButton");

        if (backBtnT != null)
        {
            RectTransform backBtnRect = backBtnT.GetComponent<RectTransform>();
            if (backBtnRect != null)
            {
                backBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
                backBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
                backBtnRect.pivot = new Vector2(0f, 1f);
                backBtnRect.anchoredPosition = new Vector2(-622f, 922f);
                backBtnRect.sizeDelta = new Vector2(540f, 275f);
                backBtnRect.localRotation = Quaternion.identity;
                backBtnRect.localScale = Vector3.one;
            }
        }
    }

    private void WireBackButton()
    {
        Transform backBtnT = transform.Find("BackButton");
        if (backBtnT == null) return;
        Button btn = backBtnT.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Close);
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

    private void OnDisable()
    {
        _cards.Clear();
    }

    private void ClearCards()
    {
        if (gridParent != null)
        {
            int childCount = gridParent.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
                DestroyImmediate(gridParent.transform.GetChild(i).gameObject);
        }
        foreach (var card in _cards)
        {
            if (card.root != null)
                DestroyImmediate(card.root);
        }
        _cards.Clear();
    }

    public void Open()
    {
        if (storePanel != null)
            storePanel.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (storePanel != null)
            storePanel.SetActive(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform mainMenu = canvas.transform.Find("MainMenu");
            if (mainMenu != null)
                mainMenu.gameObject.SetActive(true);
        }
    }

    public void Refresh()
    {
        if (allSkins == null || allSkins.Count == 0)
            allSkins = new List<BallSkinData>(Resources.LoadAll<BallSkinData>("Settings/BallSkins"));

        UpdateCoinDisplay();
        RebuildCards();
        SelectDefaultSkin();
    }

    private void UpdateCoinDisplay()
    {
        if (coinDisplay != null)
            coinDisplay.text = CoinManager.GetTotalCoins().ToString();
    }

    private void RebuildCards()
    {
        ClearCards();

        if (skinCardPrefab == null) return;
        if (gridParent == null) return;

        foreach (BallSkinData skin in allSkins)
        {
            if (skin == null) continue;

            GameObject cardObj = Instantiate(skinCardPrefab, gridParent.transform);

            SkinCard card = new SkinCard();
            card.root = cardObj;
            card.skin = skin;

            TextMeshProUGUI[] labels = cardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels)
            {
                if (label.name.Contains("Name"))
                    card.nameLabel = label;
                else if (label.name.Contains("Price"))
                    card.priceLabel = label;
            }

            Image[] images = cardObj.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.name.Contains("Lock"))
                    card.lockIcon = img;
                else if (img.name.Contains("Check"))
                    card.checkIcon = img;
                else if (img.name.Contains("PreviewSlot"))
                    card.previewIcon = img;
                else if (img.name.Contains("CoinIcon"))
                    card.coinIcon = img.gameObject;
            }

            card.button = cardObj.GetComponentInChildren<Button>(true);
            if (card.button != null)
            {
                BallSkinData captured = skin;
                card.button.onClick.AddListener(() => OnSkinCardClicked(captured));
            }

            ApplyCardState(card);
            _cards.Add(card);
        }
    }

    private void ApplyCardState(SkinCard card)
    {
        if (card == null || card.skin == null) return;

        bool purchased = BallSkinManager.IsSkinPurchased(card.skin.skinId);
        bool selected = BallSkinManager.GetSelectedSkinId() == card.skin.skinId;
        bool unlocked = purchased || card.skin.unlockedByDefault;

        if (card.previewIcon != null && card.skin.icon != null)
        {
            card.previewIcon.sprite = card.skin.icon;
            card.previewIcon.color = Color.white;
            card.previewIcon.preserveAspect = true;
        }

        if (card.nameLabel != null)
            card.nameLabel.text = card.skin.skinName;

        if (card.priceLabel != null)
        {
            if (purchased)
            {
                card.priceLabel.text = selected ? "SELECTED" : "OWNED";
                card.priceLabel.rectTransform.offsetMin = new Vector2(0f, 0f); // Reset offset to center text
            }
            else
            {
                card.priceLabel.text = card.skin.price.ToString();
            }
        }
        
        if (card.coinIcon != null)
            card.coinIcon.SetActive(!purchased);

        Image btnImg = card.button != null ? card.button.GetComponent<Image>() : null;
        if (btnImg != null)
        {
            if (purchased)
            {
                btnImg.color = new Color(0.15f, 0.15f, 0.25f, 1f); // Dark panel color for owned
            }
            else
            {
                btnImg.color = new Color(0.1f, 0.7f, 0.1f, 1f); // Bright green for buy
            }
        }

        if (card.checkIcon != null)
        {
            card.checkIcon.gameObject.SetActive(selected);
            card.checkIcon.color = new Color(0.2f, 1f, 0.2f, 1f); // Green for equipped
        }

        // Make Lock Icon semi-transparent dark overlay covering the whole box
        if (card.lockIcon != null)
        {
            card.lockIcon.gameObject.SetActive(!unlocked);
            card.lockIcon.color = new Color(0f, 0f, 0f, 0.7f);
            RectTransform lockRect = card.lockIcon.GetComponent<RectTransform>();
            if (lockRect != null)
            {
                lockRect.anchorMin = new Vector2(0.5f, 0.5f);
                lockRect.anchorMax = new Vector2(0.5f, 0.5f);
                lockRect.sizeDelta = new Vector2(240f, 240f);
            }
        }
    }

    private void OnSkinCardClicked(BallSkinData skin)
    {
        _selectedSkin = skin;

        if (previewRenderer != null)
            previewRenderer.ShowSkin(skin);

        bool purchased = BallSkinManager.IsSkinPurchased(skin.skinId) || skin.unlockedByDefault;
        bool selected = BallSkinManager.GetSelectedSkinId() == skin.skinId;

        if (selected) return;

        if (purchased)
        {
            BallSkinManager.SelectSkin(skin.skinId);
            ShowStatus("Selected " + skin.skinName);
            Refresh();
        }
        else
        {
            if (CoinManager.GetTotalCoins() < skin.price)
            {
                ShowStatus(notEnoughCoinsMsg);
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
            ShowStatus(skinUnlockedMsg + " (" + _pendingSkin.skinName + ")");
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

    private void SelectDefaultSkin()
    {
        string selectedId = BallSkinManager.GetSelectedSkinId();

        BallSkinData found = null;
        if (allSkins != null)
        {
            found = allSkins.Find(s => s.skinId == selectedId);
            if (found == null)
                found = allSkins.Find(s => s.unlockedByDefault);
        }

        _selectedSkin = found;

        if (found != null && previewRenderer != null)
            previewRenderer.ShowSkin(found);
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
        yield return new WaitForSeconds(statusMessageDuration);
        if (statusMessage != null)
            statusMessage.gameObject.SetActive(false);
    }
}
