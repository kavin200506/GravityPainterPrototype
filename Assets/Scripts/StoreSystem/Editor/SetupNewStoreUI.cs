#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public static class SetupNewStoreUI
{
    [MenuItem("Gravity Painter/Reset Purchased Balls (Keep Coins)")]
    public static void ResetPurchasedBallsKeepCoins()
    {
        int currentCoins = SaveManager.LoadCoins();

        string[] knownSkins = new string[] { 
            "red", "blue", "white", "yellow", 
            "nova_red", "ai_nova_red", "ai_nova_white", "ai_nova_yellow", "ai_nova_blue" 
        };

        foreach (string skin in knownSkins)
        {
            PlayerPrefs.DeleteKey("SkinPurchased_" + skin);
            PlayerPrefs.SetInt("SkinPurchased_" + skin, 0);
        }

        PlayerPrefs.SetString("OwnedBallsList", "default");
        PlayerPrefs.SetInt("SkinPurchased_default", 1);
        PlayerPrefs.SetString("SelectedSkinId", "default");
        PlayerPrefs.Save();

        InventoryManager inv = Object.FindFirstObjectByType<InventoryManager>();
        if (inv != null) inv.LoadInventory();

        StoreManager store = Object.FindFirstObjectByType<StoreManager>();
        if (store != null) store.RefreshStore();

        Debug.Log($"Successfully reset purchased balls. Only Default ball is owned/equipped. Preserved {currentCoins} coins.");
    }

    [MenuItem("Gravity Painter/Overhaul Architecture Store UI")]
    public static void BuildStoreUI()
    {
        ResetPurchasedBallsKeepCoins();
        // 1. Create or Find Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Ensure EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 2. Build or Find StorePanel
        Transform existingStore = canvas.transform.Find("StorePanel");
        if (existingStore != null)
        {
            Object.DestroyImmediate(existingStore.gameObject);
        }

        GameObject storePanelObj = new GameObject("StorePanel", typeof(RectTransform), typeof(Image));
        storePanelObj.transform.SetParent(canvas.transform, false);

        RectTransform storeRt = storePanelObj.GetComponent<RectTransform>();
        storeRt.anchorMin = Vector2.zero;
        storeRt.anchorMax = Vector2.one;
        storeRt.offsetMin = Vector2.zero;
        storeRt.offsetMax = Vector2.zero;

        Image bgImg = storePanelObj.GetComponent<Image>();
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/BackgroundLevels.png");
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            bgImg.color = Color.white;
        }

        // Add System Components to StorePanel
        StoreManager storeMgr = storePanelObj.AddComponent<StoreManager>();
        storeMgr.storePanel = storePanelObj;

        CurrencyManager currencyMgr = storePanelObj.GetComponent<CurrencyManager>();
        if (currencyMgr == null) currencyMgr = storePanelObj.AddComponent<CurrencyManager>();

        InventoryManager invMgr = storePanelObj.GetComponent<InventoryManager>();
        if (invMgr == null) invMgr = storePanelObj.AddComponent<InventoryManager>();

        StoreSceneLoader sceneLoader = storePanelObj.AddComponent<StoreSceneLoader>();
        sceneLoader.storePanel = storePanelObj;

        // 3. StoreMainPanel (Sci-Fi Blue Outer Frame)
        GameObject frameObj = new GameObject("StoreMainPanel", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(storePanelObj.transform, false);

        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 0.5f);
        frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.sizeDelta = new Vector2(980f, 1480f);
        frameRt.anchoredPosition = new Vector2(0f, -90f);

        Image frameImg = frameObj.GetComponent<Image>();
        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/Store_Main.png");
        if (frameSprite != null)
        {
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;
        }

        // 4. Back Button (Top Left - EVEN BIGGER: 260x260px)
        GameObject backBtnObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(storePanelObj.transform, false);

        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0f, 1f);
        backRt.anchorMax = new Vector2(0f, 1f);
        backRt.pivot = new Vector2(0f, 1f);
        backRt.sizeDelta = new Vector2(260f, 260f);
        backRt.anchoredPosition = new Vector2(30f, -50f);

        Image backImg = backBtnObj.GetComponent<Image>();
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/LevelsBackButton.png");
        if (backSprite != null) backImg.sprite = backSprite;
        backImg.color = Color.white;
        backImg.preserveAspect = true;

        Button backBtn = backBtnObj.GetComponent<Button>();
        backBtn.transition = Selectable.Transition.None;
        Navigation navNone = backBtn.navigation;
        navNone.mode = Navigation.Mode.None;
        backBtn.navigation = navNone;

        ColorBlock backCb = backBtn.colors;
        backCb.normalColor = Color.white;
        backCb.highlightedColor = Color.white;
        backCb.pressedColor = Color.white;
        backCb.selectedColor = Color.white;
        backCb.disabledColor = Color.white;
        backBtn.colors = backCb;

        sceneLoader.backButton = backBtn;

        // 5. Coin Panel (Top Right - Using CoinPanel.png & coin_icon_32.png)
        GameObject coinPanelObj = new GameObject("CoinPanel", typeof(RectTransform), typeof(Image));
        coinPanelObj.transform.SetParent(storePanelObj.transform, false);

        RectTransform coinRt = coinPanelObj.GetComponent<RectTransform>();
        coinRt.anchorMin = new Vector2(1f, 1f);
        coinRt.anchorMax = new Vector2(1f, 1f);
        coinRt.pivot = new Vector2(1f, 1f);
        coinRt.sizeDelta = new Vector2(320f, 95f);
        coinRt.anchoredPosition = new Vector2(-50f, -80f);

        Image coinBg = coinPanelObj.GetComponent<Image>();
        Sprite coinPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/CoinPanel.png");
        if (coinPanelSprite != null)
        {
            coinBg.sprite = coinPanelSprite;
            coinBg.type = Image.Type.Sliced;
            coinBg.color = Color.white;
        }
        else
        {
            coinBg.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);
        }

        // Coin Icon inside Panel (coin_icon_32.png)
        GameObject coinIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinIconObj.transform.SetParent(coinPanelObj.transform, false);

        RectTransform cIconRt = coinIconObj.GetComponent<RectTransform>();
        cIconRt.anchorMin = new Vector2(0f, 0.5f);
        cIconRt.anchorMax = new Vector2(0f, 0.5f);
        cIconRt.pivot = new Vector2(0f, 0.5f);
        cIconRt.sizeDelta = new Vector2(60f, 60f);
        cIconRt.anchoredPosition = new Vector2(20f, 0f);

        Image cIconImg = coinIconObj.GetComponent<Image>();
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/coin_icon_32.png");
        if (coinSprite == null) coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/coin_icon.png");
        if (coinSprite != null) cIconImg.sprite = coinSprite;
        cIconImg.color = Color.white;
        cIconImg.preserveAspect = true;

        // Coin Text
        GameObject coinTextObj = new GameObject("CoinText", typeof(RectTransform), typeof(TextMeshProUGUI));
        coinTextObj.transform.SetParent(coinPanelObj.transform, false);

        RectTransform cTextRt = coinTextObj.GetComponent<RectTransform>();
        cTextRt.anchorMin = new Vector2(0f, 0f);
        cTextRt.anchorMax = new Vector2(1f, 1f);
        cTextRt.offsetMin = new Vector2(90f, 0f);
        cTextRt.offsetMax = new Vector2(-60f, 0f);

        TextMeshProUGUI coinText = coinTextObj.GetComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 38f;
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.Left;
        coinText.color = new Color(1f, 0.9f, 0.3f, 1f);

        storeMgr.coinDisplayText = coinText;

        // Plus Button
        GameObject plusBtnObj = new GameObject("PlusBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        plusBtnObj.transform.SetParent(coinPanelObj.transform, false);

        RectTransform plusRt = plusBtnObj.GetComponent<RectTransform>();
        plusRt.anchorMin = new Vector2(1f, 0.5f);
        plusRt.anchorMax = new Vector2(1f, 0.5f);
        plusRt.pivot = new Vector2(1f, 0.5f);
        plusRt.sizeDelta = new Vector2(50f, 50f);
        plusRt.anchoredPosition = new Vector2(-15f, 0f);

        Button plusBtn = plusBtnObj.GetComponent<Button>();
        plusBtn.transition = Selectable.Transition.None;
        Navigation plusNav = plusBtn.navigation;
        plusNav.mode = Navigation.Mode.None;
        plusBtn.navigation = plusNav;

        ColorBlock plusCb = plusBtn.colors;
        plusCb.normalColor = Color.white;
        plusCb.highlightedColor = Color.white;
        plusCb.pressedColor = Color.white;
        plusCb.selectedColor = Color.white;
        plusCb.disabledColor = Color.white;
        plusBtn.colors = plusCb;

        Image plusImg = plusBtnObj.GetComponent<Image>();
        plusImg.color = new Color(0.2f, 0.6f, 0.9f, 1f);

        GameObject plusTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        plusTextObj.transform.SetParent(plusBtnObj.transform, false);
        RectTransform ptRt = plusTextObj.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one;
        ptRt.offsetMin = Vector2.zero; ptRt.offsetMax = Vector2.zero;
        TextMeshProUGUI ptText = plusTextObj.GetComponent<TextMeshProUGUI>();
        ptText.text = "+"; ptText.fontSize = 32f; ptText.alignment = TextAlignmentOptions.Center;

        // 6. Scroll View & Viewport Positioning
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(frameObj.transform, false);

        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;

        Image scrollBg = scrollObj.GetComponent<Image>();
        if (scrollBg == null) scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0f);
        scrollBg.raycastTarget = true;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);

        RectTransform viewRt = viewportObj.GetComponent<RectTransform>();
        viewRt.anchorMin = new Vector2(0f, 0f);
        viewRt.anchorMax = new Vector2(1f, 1f);
        viewRt.pivot = new Vector2(0.5f, 0.5f);
        viewRt.offsetMin = new Vector2(0f, -172f);
        viewRt.offsetMax = new Vector2(0f, -172f);
        viewRt.localRotation = Quaternion.identity;
        viewRt.localScale = Vector3.one;

        scrollRect.viewport = viewRt;

        // Content Container (WITH 230PX TOP PADDING SO FIRST ROW IS WELL BELOW STORE HEADER ARCH)
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.localRotation = Quaternion.identity;
        contentRt.localScale = Vector3.one;

        GridLayoutGroup grid = contentObj.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(275f, 380f);
        grid.spacing = new Vector2(20f, 30f);
        grid.padding = new RectOffset(10, 10, 230, 10); // 230px TOP PADDING so first row is well below STORE header!
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        storeMgr.gridParent = contentRt;

        // 7. Create & Assign BallCard Prefab
        GameObject cardPrefab = CreateBallCardPrefab();
        storeMgr.ballCardPrefab = cardPrefab;

        // 8. Build Purchase Popup
        GameObject popupObj = CreatePurchasePopup(storePanelObj.transform);
        storeMgr.popupPanel = popupObj;
        storeMgr.popupMessageText = popupObj.GetComponentInChildren<TextMeshProUGUI>(true);
        Button[] popupBtns = popupObj.GetComponentsInChildren<Button>(true);
        if (popupBtns.Length > 0) storeMgr.popupConfirmBtn = popupBtns[0];
        if (popupBtns.Length > 1) storeMgr.popupCancelBtn = popupBtns[1];

        popupObj.SetActive(false);

        // Update MainMenu references if present
        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenu != null)
        {
            SerializedObject so = new SerializedObject(mainMenu);
            so.Update();
            so.FindProperty("storeUI").objectReferenceValue = null;
            so.FindProperty("testStoreUI").objectReferenceValue = null;
            so.ApplyModifiedProperties();
        }

        storePanelObj.SetActive(false);
        Selection.activeGameObject = canvas.gameObject;
        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("Successfully updated Store Architecture UI with extra large back button (260px), 230px top grid padding, and centered button text!");
    }

    private static GameObject CreateBallCardPrefab()
    {
        string dir = "Assets/Resources/Prefabs/UI";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string prefabPath = dir + "/BallCard.prefab";

        GameObject cardRoot = new GameObject("BallCard", typeof(RectTransform), typeof(Image), typeof(BallCardUI));
        RectTransform rootRt = cardRoot.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(275f, 380f);

        Image bgImg = cardRoot.GetComponent<Image>();
        Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/BallBox.png");
        if (cardSprite != null)
        {
            bgImg.sprite = cardSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
        }

        BallCardUI cardUI = cardRoot.GetComponent<BallCardUI>();
        cardUI.cardBg = bgImg;

        // Name Text (Top)
        GameObject nameObj = new GameObject("BallName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(cardRoot.transform, false);
        RectTransform nRt = nameObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0.5f, 1f);
        nRt.anchoredPosition = new Vector2(0f, -18f);
        nRt.sizeDelta = new Vector2(-20f, 45f);
        TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
        nameText.text = "RED BALL";
        nameText.fontSize = 26f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.raycastTarget = false;
        cardUI.ballNameText = nameText;

        // Lock Icon (Top Right)
        GameObject lockObj = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
        lockObj.transform.SetParent(cardRoot.transform, false);
        RectTransform lRt = lockObj.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(1f, 1f); lRt.anchorMax = new Vector2(1f, 1f);
        lRt.pivot = new Vector2(1f, 1f);
        lRt.anchoredPosition = new Vector2(-15f, -15f);
        lRt.sizeDelta = new Vector2(38f, 38f);
        Image lockImg = lockObj.GetComponent<Image>();
        Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/Padlock-removebg.png");
        if (lockSprite == null) lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Level_Page/Padlock.png");
        if (lockSprite != null) lockImg.sprite = lockSprite;
        lockImg.color = Color.white;
        lockImg.preserveAspect = true;
        lockImg.raycastTarget = false;
        cardUI.lockIcon = lockObj;

        // Ball Image (Center 3D ball)
        GameObject ballImgObj = new GameObject("BallImage", typeof(RectTransform), typeof(Image));
        ballImgObj.transform.SetParent(cardRoot.transform, false);
        RectTransform bRt = ballImgObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0.5f); bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(0f, 15f);
        bRt.sizeDelta = new Vector2(170f, 170f);
        bRt.localScale = Vector3.one;
        Image bImg = ballImgObj.GetComponent<Image>();
        Sprite ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/White_ball.png");
        if (ballSprite != null) bImg.sprite = ballSprite;
        bImg.color = Color.white;
        bImg.preserveAspect = true;
        bImg.raycastTarget = false;
        cardUI.ballImage = bImg;

        // Action Button / Price Panel (Bottom)
        GameObject btnObj = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(cardRoot.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f); btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 2.936005f);
        btnRt.sizeDelta = new Vector2(235f, 70f);
        btnRt.localScale = new Vector3(0.8146776f, 1.4875f, 1f);

        Image btnImg = btnObj.GetComponent<Image>();
        Sprite pricePanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/price_panel.png");
        if (pricePanelSprite != null)
        {
            btnImg.sprite = pricePanelSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
        }

        Button actBtn = btnObj.GetComponent<Button>();
        actBtn.transition = Selectable.Transition.None;
        Navigation actNav = actBtn.navigation;
        actNav.mode = Navigation.Mode.None;
        actBtn.navigation = actNav;

        ColorBlock actCb = actBtn.colors;
        actCb.normalColor = Color.white;
        actCb.highlightedColor = Color.white;
        actCb.pressedColor = Color.white;
        actCb.selectedColor = Color.white;
        actCb.disabledColor = Color.white;
        actBtn.colors = actCb;

        cardUI.actionButton = actBtn;
        cardUI.actionButtonImage = btnImg;

        // Coin Icon inside Button
        GameObject cIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        cIconObj.transform.SetParent(btnObj.transform, false);
        RectTransform ciRt = cIconObj.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0f, 0.5f); ciRt.anchorMax = new Vector2(0f, 0.5f);
        ciRt.pivot = new Vector2(0f, 0.5f);
        ciRt.sizeDelta = new Vector2(42f, 42f);
        ciRt.anchoredPosition = new Vector2(20f, 0f);
        Image ciImg = cIconObj.GetComponent<Image>();
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/coin_icon_32.png");
        if (coinSprite == null) coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/coin_icon.png");
        if (coinSprite != null) ciImg.sprite = coinSprite;
        ciImg.color = Color.white;
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;
        cardUI.coinIconObj = cIconObj;
        cardUI.coinIconImage = ciImg;

        // Action Text
        GameObject actTextObj = new GameObject("ActionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        actTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform atRt = actTextObj.GetComponent<RectTransform>();
        atRt.anchorMin = Vector2.zero; atRt.anchorMax = Vector2.one;
        atRt.offsetMin = Vector2.zero; atRt.offsetMax = Vector2.zero;
        TextMeshProUGUI actText = actTextObj.GetComponent<TextMeshProUGUI>();
        actText.text = "500";
        actText.fontSize = 26f;
        actText.fontStyle = FontStyles.Bold;
        actText.alignment = TextAlignmentOptions.Center;
        actText.color = Color.white;
        actText.raycastTarget = false;
        cardUI.actionText = actText;

        // Load Sprites onto Card UI component
        cardUI.pricePanelSprite = pricePanelSprite;
        cardUI.ownedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/owned.png");
        cardUI.equipSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/equip.png");
        cardUI.coinSprite = coinSprite;
        cardUI.defaultBallSprite = ballSprite;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cardRoot, prefabPath);
        Object.DestroyImmediate(cardRoot);
        return prefab;
    }

    private static GameObject CreatePurchasePopup(Transform parent)
    {
        GameObject popupObj = new GameObject("PurchasePopup", typeof(RectTransform), typeof(Image));
        popupObj.transform.SetParent(parent, false);

        RectTransform popRt = popupObj.GetComponent<RectTransform>();
        popRt.anchorMin = new Vector2(0.5f, 0.5f); popRt.anchorMax = new Vector2(0.5f, 0.5f);
        popRt.pivot = new Vector2(0.5f, 0.5f);
        popRt.sizeDelta = new Vector2(700f, 400f);

        Image popImg = popupObj.GetComponent<Image>();
        popImg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

        // Text
        GameObject textObj = new GameObject("MessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(popupObj.transform, false);
        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.4f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(30f, 0f); tRt.offsetMax = new Vector2(-30f, -30f);
        TextMeshProUGUI msgText = textObj.GetComponent<TextMeshProUGUI>();
        msgText.text = "Confirm Purchase?";
        msgText.fontSize = 32f;
        msgText.alignment = TextAlignmentOptions.Center;

        // Yes Button
        GameObject yesBtnObj = new GameObject("YesButton", typeof(RectTransform), typeof(Image), typeof(Button));
        yesBtnObj.transform.SetParent(popupObj.transform, false);
        RectTransform yRt = yesBtnObj.GetComponent<RectTransform>();
        yRt.anchorMin = new Vector2(0.2f, 0.1f); yRt.anchorMax = new Vector2(0.45f, 0.35f);
        yRt.offsetMin = Vector2.zero; yRt.offsetMax = Vector2.zero;
        Button yesBtn = yesBtnObj.GetComponent<Button>();
        yesBtn.transition = Selectable.Transition.None;
        Navigation yNav = yesBtn.navigation; yNav.mode = Navigation.Mode.None; yesBtn.navigation = yNav;
        Image yImg = yesBtnObj.GetComponent<Image>(); yImg.color = new Color(0.15f, 0.7f, 0.25f, 1f);
        GameObject yTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        yTextObj.transform.SetParent(yesBtnObj.transform, false);
        RectTransform ytRt = yTextObj.GetComponent<RectTransform>(); ytRt.anchorMin = Vector2.zero; ytRt.anchorMax = Vector2.one; ytRt.offsetMin = Vector2.zero; ytRt.offsetMax = Vector2.zero;
        TextMeshProUGUI ytText = yTextObj.GetComponent<TextMeshProUGUI>(); ytText.text = "YES"; ytText.fontSize = 28f; ytText.alignment = TextAlignmentOptions.Center;

        // No Button
        GameObject noBtnObj = new GameObject("NoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        noBtnObj.transform.SetParent(popupObj.transform, false);
        RectTransform nRt = noBtnObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0.55f, 0.1f); nRt.anchorMax = new Vector2(0.8f, 0.35f);
        nRt.offsetMin = Vector2.zero; nRt.offsetMax = Vector2.zero;
        Button noBtn = noBtnObj.GetComponent<Button>();
        noBtn.transition = Selectable.Transition.None;
        Navigation nNav = noBtn.navigation; nNav.mode = Navigation.Mode.None; noBtn.navigation = nNav;
        Image nImg = noBtnObj.GetComponent<Image>(); nImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
        GameObject nTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        nTextObj.transform.SetParent(noBtnObj.transform, false);
        RectTransform ntRt = nTextObj.GetComponent<RectTransform>(); ntRt.anchorMin = Vector2.zero; ntRt.anchorMax = Vector2.one; ntRt.offsetMin = Vector2.zero; ntRt.offsetMax = Vector2.zero;
        TextMeshProUGUI ntText = nTextObj.GetComponent<TextMeshProUGUI>(); ntText.text = "NO"; ntText.fontSize = 28f; ntText.alignment = TextAlignmentOptions.Center;

        return popupObj;
    }
}
#endif
