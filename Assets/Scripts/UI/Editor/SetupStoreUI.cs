#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupStoreUI
{
    [MenuItem("Gravity Painter/Overhaul Store UI")]
    public static void OverhaulStore()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the active scene!");
            return;
        }

        StoreUI storeUI = Object.FindFirstObjectByType<StoreUI>(FindObjectsInactive.Include);
        if (storeUI == null)
        {
            storeUI = canvas.gameObject.AddComponent<StoreUI>();
        }

        Transform storePanel = canvas.transform.Find("StorePanel");
        if (storePanel == null && storeUI.storePanel != null)
        {
            storePanel = storeUI.storePanel.transform;
        }

        if (storePanel == null)
        {
            GameObject panelObj = new GameObject("StorePanel", typeof(RectTransform));
            panelObj.transform.SetParent(canvas.transform, false);
            storePanel = panelObj.transform;
        }

        // 1. Wipe existing Store Panel
        for (int i = storePanel.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(storePanel.GetChild(i).gameObject);
        }

        // Setup StorePanel Rect to fill Canvas
        RectTransform storeRect = storePanel.GetComponent<RectTransform>();
        storeRect.anchorMin = Vector2.zero;
        storeRect.anchorMax = Vector2.one;
        storeRect.offsetMin = Vector2.zero;
        storeRect.offsetMax = Vector2.zero;

        // 2. Setup Panel Background
        Image bgImg = storePanel.GetComponent<Image>();
        if (bgImg == null) bgImg = storePanel.gameObject.AddComponent<Image>();
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/BackgroundLevels.png");
        if (bgSprite != null) {
            bgImg.sprite = bgSprite;
            bgImg.color = Color.white;
        } else {
            bgImg.color = new Color(0.05f, 0.05f, 0.15f, 1f);
        }

        // 3. Create Top Bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform));
        topBar.transform.SetParent(storePanel, false);
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = Vector2.zero;
        topBarRect.sizeDelta = new Vector2(0f, 250f);

        // Title
        GameObject titleObj = CreateTextObj("Title", topBar.transform, "STORE", 100);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(600f, 150f);
        titleObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Back Button
        GameObject backBtnObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(topBar.transform, false);
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 0.5f);
        backRect.anchorMax = new Vector2(0f, 0.5f);
        backRect.pivot = new Vector2(0f, 0.5f);
        backRect.anchoredPosition = new Vector2(120f, 0f);
        backRect.sizeDelta = new Vector2(180f, 180f);
        Image backImg = backBtnObj.GetComponent<Image>();
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/LevelsBackButton.png");
        if (backSprite != null) {
            backImg.sprite = backSprite;
            backImg.preserveAspect = true;
        } else {
            backImg.color = Color.red;
        }

        // Coins Text
        GameObject coinsObj = CreateTextObj("TotalCoins", topBar.transform, "0", 80);
        RectTransform coinsRect = coinsObj.GetComponent<RectTransform>();
        coinsRect.anchorMin = new Vector2(1f, 0.5f);
        coinsRect.anchorMax = new Vector2(1f, 0.5f);
        coinsRect.pivot = new Vector2(1f, 0.5f);
        coinsRect.anchoredPosition = new Vector2(-150f, 0f);
        coinsRect.sizeDelta = new Vector2(300f, 100f);
        TextMeshProUGUI coinsText = coinsObj.GetComponent<TextMeshProUGUI>();
        coinsText.alignment = TextAlignmentOptions.Right;
        storeUI.coinDisplay = coinsText;

        // Coin Icon next to Total Coins
        GameObject topCoinIcon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        topCoinIcon.transform.SetParent(topBar.transform, false);
        RectTransform tcRect = topCoinIcon.GetComponent<RectTransform>();
        tcRect.anchorMin = new Vector2(1f, 0.5f);
        tcRect.anchorMax = new Vector2(1f, 0.5f);
        tcRect.pivot = new Vector2(1f, 0.5f);
        tcRect.anchoredPosition = new Vector2(-50f, 0f);
        tcRect.sizeDelta = new Vector2(80f, 80f);
        Image tcImg = topCoinIcon.GetComponent<Image>();
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/coin_icon.png") ?? Resources.Load<Sprite>("UI/coin_icon");
        if (coinSprite != null) tcImg.sprite = coinSprite;
        tcImg.preserveAspect = true;

        // 4. Create Scroll View
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollView.transform.SetParent(storePanel, false);
        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(50f, 50f);
        scrollRect.offsetMax = new Vector2(-50f, -250f); // Leave room for top bar

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        // Content / GridParent
        GameObject gridObj = new GameObject("GridParent", typeof(RectTransform));
        gridObj.transform.SetParent(viewport.transform, false);
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(400f, 550f);
        grid.spacing = new Vector2(50f, 50f);
        grid.padding = new RectOffset(50, 50, 50, 50);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2; // 2 columns for a balanced mobile look

        ContentSizeFitter fitter = gridObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollComp = scrollView.GetComponent<ScrollRect>();
        scrollComp.content = gridRect;
        scrollComp.viewport = viewportRect;
        scrollComp.horizontal = false;
        scrollComp.vertical = true;
        scrollComp.movementType = ScrollRect.MovementType.Clamped;
        scrollComp.inertia = true;
        scrollComp.decelerationRate = 0.135f;
        scrollComp.scrollSensitivity = 30f;

        // 7. Create Skin Card Prefab
        GameObject prefabObj = CreateSkinCardPrefab(coinSprite);

        // 8. Wire up StoreUI.cs
        SerializedObject serialized = new SerializedObject(storeUI);
        serialized.FindProperty("storePanel").objectReferenceValue = storePanel.gameObject;
        serialized.FindProperty("gridParent").objectReferenceValue = gridObj;
        serialized.FindProperty("skinCardPrefab").objectReferenceValue = prefabObj;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(storeUI);
        EditorUtility.SetDirty(storePanel);
        
        Debug.Log("Store UI Overhaul Complete! Make sure to save the scene.");
    }

    private static GameObject CreateTextObj(string name, Transform parent, string textStr, float fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = textStr;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return obj;
    }

    private static GameObject CreateSkinCardPrefab(Sprite coinSprite)
    {
        GameObject cardRoot = new GameObject("StoreSkinCard", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = cardRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400f, 550f);

        Image bgImg = cardRoot.GetComponent<Image>();
        Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/BallBox.png");
        if (cardSprite != null) {
            bgImg.sprite = cardSprite;
            bgImg.type = Image.Type.Sliced; // Prevent stretching if it has borders
        } else {
            bgImg.color = new Color(0.1f, 0.15f, 0.3f, 1f);
        }

        // Title at Top
        GameObject nameObj = CreateTextObj("NameLabel", cardRoot.transform, "Ball Name", 45f);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -30f);
        nameRect.sizeDelta = new Vector2(0f, 60f);

        // Preview Icon in Center
        GameObject previewObj = new GameObject("PreviewSlot", typeof(RectTransform), typeof(Image));
        previewObj.transform.SetParent(cardRoot.transform, false);
        RectTransform prevRect = previewObj.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0.5f, 0.5f);
        prevRect.anchorMax = new Vector2(0.5f, 0.5f);
        prevRect.pivot = new Vector2(0.5f, 0.5f);
        prevRect.anchoredPosition = new Vector2(0f, 20f);
        prevRect.sizeDelta = new Vector2(240f, 240f);
        Image prevImg = previewObj.GetComponent<Image>();
        prevImg.preserveAspect = true;
        prevImg.raycastTarget = false;

        // "Equipped" Indicator
        GameObject checkObj = new GameObject("CheckIcon", typeof(RectTransform), typeof(Image));
        checkObj.transform.SetParent(cardRoot.transform, false);
        Image checkImg = checkObj.GetComponent<Image>();
        checkImg.color = new Color(0.2f, 1f, 0.2f, 1f); 
        RectTransform checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(1f, 1f);
        checkRect.anchorMax = new Vector2(1f, 1f);
        checkRect.pivot = new Vector2(1f, 1f);
        checkRect.anchoredPosition = new Vector2(-20f, -20f); 
        checkRect.sizeDelta = new Vector2(60f, 60f);
        checkImg.raycastTarget = false;
        checkObj.SetActive(false);

        // Buy Button at Bottom
        GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buyBtnObj.transform.SetParent(cardRoot.transform, false);
        RectTransform buyRect = buyBtnObj.GetComponent<RectTransform>();
        buyRect.anchorMin = new Vector2(0.5f, 0f);
        buyRect.anchorMax = new Vector2(0.5f, 0f);
        buyRect.pivot = new Vector2(0.5f, 0f);
        buyRect.anchoredPosition = new Vector2(0f, 40f);
        buyRect.sizeDelta = new Vector2(300f, 100f);
        Image buyImg = buyBtnObj.GetComponent<Image>();
        buyImg.color = new Color(0.1f, 0.7f, 0.2f, 1f); 
        
        GameObject priceObj = CreateTextObj("PriceLabel", buyBtnObj.transform, "500", 40f);
        RectTransform priceRect = priceObj.GetComponent<RectTransform>();
        priceRect.offsetMin = new Vector2(60f, 0f); // Room for coin icon
        TextMeshProUGUI priceText = priceObj.GetComponent<TextMeshProUGUI>();
        priceText.fontStyle = FontStyles.Bold;

        // Coin Icon inside Buy Button
        GameObject coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinObj.transform.SetParent(buyBtnObj.transform, false);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0f, 0.5f);
        coinRect.anchorMax = new Vector2(0f, 0.5f);
        coinRect.pivot = new Vector2(0f, 0.5f);
        coinRect.anchoredPosition = new Vector2(50f, 0f);
        coinRect.sizeDelta = new Vector2(60f, 60f);
        Image coinImg = coinObj.GetComponent<Image>();
        if (coinSprite != null) coinImg.sprite = coinSprite;
        coinImg.preserveAspect = true;
        coinImg.raycastTarget = false;

        // Lock Overlay over the Preview
        GameObject lockObj = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
        lockObj.transform.SetParent(cardRoot.transform, false);
        Image lockImg = lockObj.GetComponent<Image>();
        lockImg.color = new Color(0f, 0f, 0f, 0.7f); // Dark semi-transparent lock overlay
        RectTransform lockRect = lockObj.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0.5f, 0.5f);
        lockRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockRect.pivot = new Vector2(0.5f, 0.5f);
        lockRect.anchoredPosition = new Vector2(0f, 20f); 
        lockRect.sizeDelta = new Vector2(240f, 240f); // Match preview icon size
        lockImg.raycastTarget = false;
        lockObj.SetActive(false);

        if (!System.IO.Directory.Exists("Assets/Resources/Prefabs/UI"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs/UI");
        }
        string prefabPath = "Assets/Resources/Prefabs/UI/StoreSkinCard.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(cardRoot, prefabPath);
        Object.DestroyImmediate(cardRoot);

        return savedPrefab;
    }
}
#endif

