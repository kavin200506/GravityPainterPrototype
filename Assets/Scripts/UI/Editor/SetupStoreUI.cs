#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class SetupStoreUI
{
    private const string ScenePath = "Assets/Scenes/Menus/MainMenu.unity";

    [MenuItem("Gravity Painter/Overhaul Store UI")]
    public static void OverhaulStore()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        bool openedTempScene = false;
        if (scene.path != ScenePath)
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("SetupStoreUI: scene not found at " + ScenePath);
                return;
            }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            openedTempScene = true;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform storePanel = canvas.transform.Find("StorePanel");
        if (storePanel == null)
        {
            Debug.LogError("StorePanel not found under Canvas.");
            return;
        }

        StoreUI storeUI = Object.FindFirstObjectByType<StoreUI>(FindObjectsInactive.Include);
        if (storeUI == null)
        {
            Debug.LogError("StoreUI component not found anywhere in scene.");
            return;
        }

        // 1. Wipe existing Store Panel
        for (int i = storePanel.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(storePanel.GetChild(i).gameObject);
        }

        // 2. Setup Panel Background (Dark Blue, but highly transparent!)
        Image bgImg = storePanel.GetComponent<Image>();
        if (bgImg == null) bgImg = storePanel.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.05f, 0.15f, 0.4f); // Much more transparent

        // 3. Add Back Button (Top Left)
        GameObject backBtnObj = new GameObject("BackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(storePanel, false);
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(60f, -60f);
        backRect.sizeDelta = new Vector2(100f, 100f);
        Image backImg = backBtnObj.GetComponent<Image>();
        Sprite backSprite = Resources.Load<Sprite>("UI/back_button");
        if (backSprite != null)
        {
            backImg.sprite = backSprite;
            backImg.color = Color.white;
            backRect.sizeDelta = new Vector2(240f, 96f);
        }
        else
        {
            backImg.color = new Color(0.1f, 0.3f, 0.6f, 1f);
        }
        Button backBtn = backBtnObj.GetComponent<Button>();

        // 4. Add Title "STORE"
        GameObject titleObj = CreateTextObj("Title", storePanel, "STORE", 120);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(600f, 150f);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.fontStyle = FontStyles.Bold;

        // 5. Add Tab "BALLS"
        GameObject tabObj = CreateTextObj("TabLabel", storePanel, "BALLS", 50);
        RectTransform tabRect = tabObj.GetComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0.5f, 1f);
        tabRect.anchorMax = new Vector2(0.5f, 1f);
        tabRect.pivot = new Vector2(0.5f, 1f);
        tabRect.anchoredPosition = new Vector2(0f, -280f);
        tabRect.sizeDelta = new Vector2(400f, 60f);
        TextMeshProUGUI tabText = tabObj.GetComponent<TextMeshProUGUI>();
        tabText.color = new Color(0.6f, 0.8f, 1f, 1f);

        // 6. Create Grid Container
        GameObject gridObj = new GameObject("GridParent", typeof(RectTransform));
        gridObj.transform.SetParent(storePanel, false);
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -50f);
        gridRect.sizeDelta = new Vector2(980f, 1200f);

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(300f, 400f);
        grid.spacing = new Vector2(30f, 40f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        // 7. Create Skin Card Prefab
        GameObject prefabObj = CreateSkinCardPrefab();

        // 8. Wire up StoreUI.cs
        SerializedObject serialized = new SerializedObject(storeUI);
        serialized.FindProperty("storePanel").objectReferenceValue = storePanel.gameObject;
        serialized.FindProperty("gridParent").objectReferenceValue = gridObj;
        serialized.FindProperty("skinCardPrefab").objectReferenceValue = prefabObj;
        
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Fix missing Confirm Popup if it was accidentally destroyed
        if (storeUI.confirmPopup == null)
        {
            Debug.LogWarning("ConfirmPopup missing. You may need to wire it up or rebuild it manually if it was deleted.");
        }

        EditorUtility.SetDirty(storeUI);
        EditorUtility.SetDirty(storePanel);
        EditorSceneManager.MarkSceneDirty(scene);

        if (openedTempScene)
            EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Store UI Overhaul", "The store layout has been successfully updated to a 3-column sci-fi grid!", "Awesome!");
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
        return obj;
    }

    private static GameObject CreateSkinCardPrefab()
    {
        GameObject cardRoot = new GameObject("StoreSkinCard", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = cardRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(300f, 400f);

        Image bgImg = cardRoot.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.1f, 0.25f, 1f);

        GameObject nameObj = CreateTextObj("NameLabel", cardRoot.transform, "Ball Name", 40f);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -20f);
        nameRect.sizeDelta = new Vector2(0f, 60f);

        GameObject previewObj = new GameObject("PreviewSlot", typeof(RectTransform), typeof(Image));
        previewObj.transform.SetParent(cardRoot.transform, false);
        RectTransform prevRect = previewObj.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0.5f, 0.5f);
        prevRect.anchorMax = new Vector2(0.5f, 0.5f);
        prevRect.pivot = new Vector2(0.5f, 0.5f);
        prevRect.anchoredPosition = new Vector2(0f, 20f);
        prevRect.sizeDelta = new Vector2(220f, 220f);
        Image prevImg = previewObj.GetComponent<Image>();
        prevImg.color = new Color(0f, 0f, 0f, 0.3f); 

        // We also need a "Check" and "Lock" icon so StoreUI.cs doesn't break
        GameObject checkObj = new GameObject("CheckIcon", typeof(RectTransform), typeof(Image));
        checkObj.transform.SetParent(cardRoot.transform, false);
        Image checkImg = checkObj.GetComponent<Image>();
        checkImg.color = new Color(1f, 0.8f, 0.2f, 1f); // Golden indicator for selected
        RectTransform checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 1f);
        checkRect.anchorMax = new Vector2(0.5f, 1f);
        checkRect.pivot = new Vector2(0.5f, 1f);
        checkRect.anchoredPosition = new Vector2(120f, 10f); // Top right
        checkRect.sizeDelta = new Vector2(50f, 50f);
        checkObj.SetActive(false);

        GameObject lockObj = new GameObject("LockIcon", typeof(RectTransform), typeof(Image));
        lockObj.transform.SetParent(cardRoot.transform, false);
        Image lockImg = lockObj.GetComponent<Image>();
        lockImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        RectTransform lockRect = lockObj.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0f, 1f);
        lockRect.anchorMax = new Vector2(0f, 1f);
        lockRect.pivot = new Vector2(0f, 1f);
        lockRect.anchoredPosition = new Vector2(30f, 10f); // Top left
        lockRect.sizeDelta = new Vector2(60f, 60f);
        lockObj.SetActive(false);

        GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buyBtnObj.transform.SetParent(cardRoot.transform, false);
        RectTransform buyRect = buyBtnObj.GetComponent<RectTransform>();
        buyRect.anchorMin = new Vector2(0.5f, 0f);
        buyRect.anchorMax = new Vector2(0.5f, 0f);
        buyRect.pivot = new Vector2(0.5f, 0f);
        buyRect.anchoredPosition = new Vector2(0f, 50f);
        buyRect.sizeDelta = new Vector2(240f, 80f);
        
        Image buyImg = buyBtnObj.GetComponent<Image>();
        buyImg.color = new Color(0.1f, 0.7f, 0.1f, 1f); 
        GameObject priceObj = CreateTextObj("PriceLabel", buyBtnObj.transform, "500", 34f);
        RectTransform priceRect = priceObj.GetComponent<RectTransform>();
        priceRect.offsetMin = new Vector2(40f, 0f); // Make room for coin icon on the left
        
        TextMeshProUGUI priceText = priceObj.GetComponent<TextMeshProUGUI>();
        priceText.fontStyle = FontStyles.Bold;
        priceText.enableAutoSizing = true;
        priceText.fontSizeMin = 20f;
        priceText.fontSizeMax = 40f;

        GameObject coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinObj.transform.SetParent(buyBtnObj.transform, false);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0f, 0.5f);
        coinRect.anchorMax = new Vector2(0f, 0.5f);
        coinRect.pivot = new Vector2(0f, 0.5f);
        coinRect.anchoredPosition = new Vector2(30f, 0f);
        coinRect.sizeDelta = new Vector2(50f, 50f);
        Image coinImg = coinObj.GetComponent<Image>();
        Sprite coinSprite = Resources.Load<Sprite>("UI/coin_icon");
        if (coinSprite != null) coinImg.sprite = coinSprite;
        coinImg.preserveAspect = true;

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
