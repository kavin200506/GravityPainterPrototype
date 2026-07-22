using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }

    [Header("UI & Containers")]
    public GameObject storePanel;
    public GameObject ballCardPrefab;
    public Transform gridParent;
    public TextMeshProUGUI coinDisplayText;

    [Header("Popups & Messaging")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupMessageText;
    public Button popupConfirmBtn;
    public Button popupCancelBtn;

    private List<BallData> ballDatabase = new List<BallData>();
    private List<BallCardUI> activeCards = new List<BallCardUI>();
    private BallData pendingSkinToBuy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadDatabase();
    }

    private void OnEnable()
    {
        CurrencyManager.OnBalanceChanged += UpdateCoinDisplay;
        InventoryManager.OnEquippedChanged += OnInventoryStateChanged;
        InventoryManager.OnBallUnlocked += OnInventoryStateChanged;
    }

    private void OnDisable()
    {
        CurrencyManager.OnBalanceChanged -= UpdateCoinDisplay;
        InventoryManager.OnEquippedChanged -= OnInventoryStateChanged;
        InventoryManager.OnBallUnlocked -= OnInventoryStateChanged;
    }

    private void Start()
    {
        if (ballCardPrefab == null)
            ballCardPrefab = Resources.Load<GameObject>("Prefabs/UI/BallCard");

        ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
        if (scroll != null)
        {
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;

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
                if (viewImg != null)
                {
                    viewImg.raycastTarget = false;
                }
                RectMask2D rectMask = scroll.viewport.GetComponent<RectMask2D>();
                if (rectMask == null)
                {
                    rectMask = scroll.viewport.gameObject.AddComponent<RectMask2D>();
                }

                RectTransform viewRt = scroll.viewport;
                viewRt.anchorMin = new Vector2(0f, 0f);
                viewRt.anchorMax = new Vector2(1f, 1f);
                viewRt.pivot = new Vector2(0.5f, 0.5f);
                viewRt.offsetMin = new Vector2(0f, -172f);
                viewRt.offsetMax = new Vector2(0f, -172f);
            }
            if (gridParent == null && scroll.content != null)
            {
                gridParent = scroll.content;
            }
        }

        WirePopupButtons();
        RefreshCoinDisplay();
        BuildStoreGrid();
    }

    public void LoadDatabase()
    {
        ballDatabase = new List<BallData>(Resources.LoadAll<BallData>("Settings/BallSkins"));
        
        // Sort: default skin first, then by price
        ballDatabase.Sort((a, b) =>
        {
            if (a.isDefault != b.isDefault)
                return a.isDefault ? -1 : 1;
            return a.price.CompareTo(b.price);
        });
    }

    public void BuildStoreGrid()
    {
        if (gridParent == null || ballCardPrefab == null) return;

        // Clear existing cards
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
        activeCards.Clear();

        // Enforce Content RectTransform properties (Top / Stretch)
        RectTransform contentRt = gridParent as RectTransform;
        if (contentRt != null)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.localRotation = Quaternion.identity;
            contentRt.localScale = Vector3.one;
        }

        // Enforce 3-Column Grid Layout
        GridLayoutGroup grid = gridParent.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(275f, 380f);
        grid.spacing = new Vector2(20f, 30f);
        grid.padding = new RectOffset(10, 10, 230, 10);
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = gridParent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = gridParent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Instantiate cards from database
        foreach (BallData ball in ballDatabase)
        {
            if (ball == null) continue;

            GameObject cardObj = Instantiate(ballCardPrefab, gridParent);
            BallCardUI cardUI = cardObj.GetComponent<BallCardUI>();
            if (cardUI == null) cardUI = cardObj.AddComponent<BallCardUI>();

            cardUI.Setup(ball, OnCardClicked);
            activeCards.Add(cardUI);
        }

        RefreshStore();
    }

    public void OnCardClicked(BallData ball)
    {
        if (ball == null) return;

        bool isOwned = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(ball.ballId, ball.isDefault);
        bool isEquipped = InventoryManager.Instance != null && InventoryManager.Instance.IsEquipped(ball.ballId);

        if (isEquipped) return;

        if (isOwned)
        {
            // Equip Ball
            InventoryManager.Instance?.Equip(ball.ballId);
            RefreshStore();
        }
        else
        {
            // Try Purchasing Ball
            if (CurrencyManager.Instance != null && !CurrencyManager.Instance.CanAfford(ball.price))
            {
                ShowPopup("Not Enough Coins!", "You need " + (ball.price - CurrencyManager.Instance.GetBalance()) + " more coins to buy " + ball.ballName + ".", false);
                return;
            }

            pendingSkinToBuy = ball;
            ShowPopup("Confirm Purchase", "Do you want to buy " + ball.ballName + " for " + ball.price + " coins?", true);
        }
    }

    private void ConfirmPendingPurchase()
    {
        HidePopup();
        if (pendingSkinToBuy == null) return;

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.Spend(pendingSkinToBuy.price))
        {
            InventoryManager.Instance?.AddOwned(pendingSkinToBuy.ballId);
            InventoryManager.Instance?.Equip(pendingSkinToBuy.ballId);
            RefreshStore();
        }

        pendingSkinToBuy = null;
    }

    private void WirePopupButtons()
    {
        if (popupConfirmBtn != null)
        {
            popupConfirmBtn.onClick.RemoveAllListeners();
            popupConfirmBtn.onClick.AddListener(ConfirmPendingPurchase);
        }

        if (popupCancelBtn != null)
        {
            popupCancelBtn.onClick.RemoveAllListeners();
            popupCancelBtn.onClick.AddListener(HidePopup);
        }
    }

    private void ShowPopup(string title, string message, bool showCancel)
    {
        if (popupPanel == null) return;

        if (popupMessageText != null)
            popupMessageText.text = message;

        if (popupCancelBtn != null)
            popupCancelBtn.gameObject.SetActive(showCancel);

        popupPanel.SetActive(true);
    }

    private void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void RefreshStore()
    {
        foreach (var card in activeCards)
        {
            if (card != null)
                card.RefreshDisplay();
        }
        RefreshCoinDisplay();
    }

    public void OnInventoryStateChanged(string ballId = null)
    {
        RefreshStore();
    }

    private void UpdateCoinDisplay(int balance)
    {
        if (coinDisplayText != null)
            coinDisplayText.text = balance.ToString();
    }

    private void RefreshCoinDisplay()
    {
        if (coinDisplayText != null && CurrencyManager.Instance != null)
            coinDisplayText.text = CurrencyManager.Instance.GetBalance().ToString();
    }
}
