using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenu : MonoBehaviour
{
    public const string OpenLevelSelectKey = "OpenLevelSelect";

    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private GameObject howToPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button storeButton;
    public StoreUI storeUI;

    [Header("Menu Root Layout")]
    [SerializeField] private Vector2 menuRootPosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 menuRootSize = new Vector2(1100f, 820f);

    [Header("Button Layout")]
    [SerializeField] private bool useManualButtonLayout = true;
    private MainMenuButtonLayout playButtonLayout = new MainMenuButtonLayout("Play", new Vector2(-6f, -85f), new Vector2(800f, 400f), new Vector3(0.7754443f, 1.9291f, 1f));
    private Vector2 playClickZoneSize = new Vector2(600f, 140f);
    private MainMenuButtonLayout levelsButtonLayout = new MainMenuButtonLayout("Levels", new Vector2(-364f, -534f), new Vector2(300f, 550f), new Vector3(1.509404f, 1.2171f, 1f));
    private MainMenuButtonLayout howToPlayButtonLayout = new MainMenuButtonLayout("HowToPlay", new Vector2(-131f, -534f), new Vector2(300f, 550f), new Vector3(1.509404f, 1.2171f, 1f));
    private MainMenuButtonLayout storeButtonLayout = new MainMenuButtonLayout("Store", new Vector2(114f, -534f), new Vector2(300f, 550f), new Vector3(1.509404f, 1.2171f, 1f));
    private MainMenuButtonLayout settingsButtonLayout = new MainMenuButtonLayout("Settings", new Vector2(363f, -534f), new Vector2(300f, 550f), new Vector3(1.509404f, 1.2171f, 1f));
    private Vector2 bottomClickZoneSize = new Vector2(220f, 440f);

    [Header("Coin UI Layout")]
    [SerializeField] private Vector2 coinUIPosition = new Vector2(50f, -50f);
    [SerializeField] private Vector2 coinUISize = new Vector2(250, 110f);
    private void OnValidate()
    {
        if (!useManualButtonLayout)
            return;

        ApplyButtonLayout();
    }


    private void Start()
    {
        if (mainMenuRoot == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform menu = canvas.transform.Find("MainMenu");
                if (menu == null) menu = FindInChildren(canvas.transform, "MainMenu");
                if (menu != null)
                    mainMenuRoot = menu.gameObject;
            }
        }

        if (levelsPanel == null || levelsPanel.name == "Levels Panel")
        {
            // Try direct find first, then search under SafeAreaPanel
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("LevelSelectMenu");
                if (panel == null) panel = FindInChildren(canvas.transform, "LevelSelectMenu");
                if (panel == null)
                {
                    panel = canvas.transform.Find("Levels Panel");
                    if (panel == null) panel = FindInChildren(canvas.transform, "Levels Panel");
                }
                if (panel != null)
                {
                    levelsPanel = panel.gameObject;

                    if (levelsPanel.name == "LevelSelectMenu")
                    {
                        LevelMenu menuScript = levelsPanel.GetComponent<LevelMenu>();
                        if (menuScript == null)
                        {
                            menuScript = levelsPanel.AddComponent<LevelMenu>();
                            Debug.Log("Automatically attached LevelMenu to LevelSelectMenu!");
                        }

#if UNITY_EDITOR
                        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(menuScript);
                        so.Update();
                        
                        Sprite holderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Level_Page/LevelNumberHolder.png");
                        if (holderSprite != null)
                        {
                            so.FindProperty("buttonSprite").objectReferenceValue = holderSprite;
                        }

                        Sprite lockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Level_Page/Padlock.png");
                        if (lockSprite != null)
                        {
                            so.FindProperty("lockIconSprite").objectReferenceValue = lockSprite;
                        }
                        
                        so.FindProperty("completedColor").colorValue = Color.white;
                        so.FindProperty("focusColor").colorValue = new Color(0.8f, 0.9f, 1f, 1f);
                        so.FindProperty("lockedColor").colorValue = new Color(0.4f, 0.4f, 0.4f, 1f);
                        so.FindProperty("minimumLevels").intValue = 20;
                        so.FindProperty("cellSize").vector2Value = new Vector2(120f, 120f);
                        so.FindProperty("gridSpacing").vector2Value = new Vector2(16f, 16f);
                        so.FindProperty("scrollPadding").floatValue = 16f;
                        
                        so.ApplyModifiedProperties();
#endif
                        
                        Transform oldPanel = canvas.transform.Find("Levels Panel");
                        if (oldPanel == null) oldPanel = FindInChildren(canvas.transform, "Levels Panel");
                        if (oldPanel != null && oldPanel.gameObject != levelsPanel)
                        {
                            if (Application.isPlaying)
                                Destroy(oldPanel.gameObject);
                            else
                                DestroyImmediate(oldPanel.gameObject);
                                
                            Debug.Log("Cleaned up old Levels Panel.");
                        }
                    }
                }
            }
        }

        if (howToPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("HowTo");
                if (panel == null) panel = FindInChildren(canvas.transform, "HowTo");
                if (panel == null) panel = FindInChildren(canvas.transform, "HowToPlay");
                if (panel != null)
                    howToPanel = panel.gameObject;
            }
        }

        if (settingsPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("SettingsPanel");
                if (panel == null) panel = FindInChildren(canvas.transform, "SettingsPanel");
                if (panel == null)
                {
                    GameObject newPanel = new GameObject("SettingsPanel");
                    newPanel.transform.SetParent(canvas.transform, false);
                    panel = newPanel.transform;
                }
                settingsPanel = panel.gameObject;
            }
        }

        if (settingsPanel != null)
        {
            if (settingsPanel.GetComponent<SettingsUI>() == null)
            {
                settingsPanel.AddComponent<SettingsUI>();
            }
        }

        if (storeUI == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform store = canvas.transform.Find("StorePanel");
                if (store == null) store = FindInChildren(canvas.transform, "StorePanel");
                if (store != null)
                    storeUI = store.GetComponent<StoreUI>();
            }
        }

        if (storeUI != null && storeUI.storePanel != null)
            storeUI.storePanel.SetActive(false);

        if (howToPanel != null)
            howToPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        ApplyButtonLayout();
        SetupButtonClickZones(ResolveMainMenuRoot());
        WireMenuButtons();

        if (ConsumeOpenLevelSelectFlag())
        {
            ShowLevelSelect();
        }
        else
        {
            CloseLevelSelect();
        }
    }

    private static Transform FindInChildren(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }

    [ContextMenu("Apply Button Layout")]
    public void ApplyButtonLayout()
    {
        if (!useManualButtonLayout)
            return;

        Transform root = ResolveMainMenuRoot();
        if (root == null)
            return;

        RectTransform menuRect = root as RectTransform;
        if (menuRect != null)
        {
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.anchoredPosition = menuRootPosition;
            menuRect.sizeDelta = menuRootSize;
        }

        if (root.TryGetComponent(out VerticalLayoutGroup layoutGroup))
            layoutGroup.enabled = false;

        EnsureChildOfRoot(root, "Store");
        EnsureChildOfRoot(root, "Settings");

        ApplyButtonLayoutEntry(root, playButtonLayout);
        ApplyButtonLayoutEntry(root, levelsButtonLayout);
        ApplyButtonLayoutEntry(root, howToPlayButtonLayout);
        ApplyButtonLayoutEntry(root, storeButtonLayout);
        ApplyButtonLayoutEntry(root, settingsButtonLayout);

        string[] stretchButtons = { "Play", "Levels", "HowToPlay", "Store", "Settings" };
        foreach (string name in stretchButtons)
            SetButtonStretch(root, name);

        ApplyCoinUILayout();
    }

    private void ApplyCoinUILayout()
    {
        CoinDisplayUI coinDisplay = FindFirstObjectByType<CoinDisplayUI>(FindObjectsInactive.Include);
        if (coinDisplay != null && coinDisplay.TryGetComponent(out RectTransform coinRt))
        {
            coinRt.anchorMin = new Vector2(0f, 1f);
            coinRt.anchorMax = new Vector2(0f, 1f);
            coinRt.pivot = new Vector2(0f, 1f);
            coinRt.anchoredPosition = coinUIPosition;
            coinRt.sizeDelta = coinUISize;
        }
    }

    private Transform ResolveMainMenuRoot()
    {
        if (mainMenuRoot != null)
            return mainMenuRoot.transform;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return null;

        Transform menu = canvas.transform.Find("MainMenu");
        return menu;
    }

    private static void ApplyButtonLayoutEntry(Transform root, MainMenuButtonLayout layout)
    {
        if (layout == null || string.IsNullOrWhiteSpace(layout.buttonName))
            return;

        Transform buttonTransform = root.Find(layout.buttonName);
        if (buttonTransform == null || !buttonTransform.TryGetComponent(out RectTransform rect))
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = layout.anchoredPosition;
        rect.sizeDelta = layout.sizeDelta;
        rect.localScale = layout.localScale;

        // preserveAspect handled by stretchButtons loop below
    }

    private static void EnsureChildOfRoot(Transform root, string childName)
    {
        Transform existing = root.Find(childName);
        if (existing != null) return;

        Transform canvas = root.parent;
        if (canvas == null) return;

        Transform childOnCanvas = canvas.Find(childName);
        if (childOnCanvas != null)
            childOnCanvas.SetParent(root, false);
    }

    private void SetupButtonClickZones(Transform root)
    {
        SetupClickZoneForButton(root, "Play", playClickZoneSize, new Vector3(1f, 0.71119f, 1f));
        Vector3 bottomClickZoneScale = new Vector3(0.61381f, 0.79509f, 1f);
        SetupClickZoneForButton(root, "Levels", bottomClickZoneSize, bottomClickZoneScale);
        SetupClickZoneForButton(root, "HowToPlay", bottomClickZoneSize, bottomClickZoneScale);
        SetupClickZoneForButton(root, "Store", bottomClickZoneSize, bottomClickZoneScale);
        SetupClickZoneForButton(root, "Settings", bottomClickZoneSize, bottomClickZoneScale);
    }

    private void SetupClickZoneForButton(Transform root, string buttonName, Vector2 clickZoneSize, Vector3 clickZoneScale)
    {
        Transform btnTransform = root.Find(buttonName);
        if (btnTransform == null || !btnTransform.TryGetComponent(out RectTransform btnRect))
            return;

        Transform czTransform = btnRect.Find("ClickZone");
        if (czTransform == null)
        {
            GameObject cz = new GameObject("ClickZone", typeof(RectTransform));
            cz.transform.SetParent(btnRect, false);
            czTransform = cz.transform;
        }

        RectTransform czRect = czTransform as RectTransform;
        czRect.anchorMin = new Vector2(0.5f, 0.5f);
        czRect.anchorMax = new Vector2(0.5f, 0.5f);
        czRect.pivot = new Vector2(0.5f, 0.5f);
        czRect.anchoredPosition = Vector2.zero;
        czRect.sizeDelta = clickZoneSize;
        czRect.localScale = clickZoneScale;

        Button czBtn = czTransform.GetComponent<Button>();
        Button parentBtn = btnRect.GetComponent<Button>();
        if (czBtn == null)
        {
            czBtn = czTransform.gameObject.AddComponent<Button>();
            if (parentBtn != null)
                czBtn.interactable = parentBtn.interactable;
        }

        if (parentBtn != null)
            DestroyImmediate(parentBtn);

        Image czImage = czTransform.GetComponent<Image>();
        if (czImage == null)
            czImage = czTransform.gameObject.AddComponent<Image>();
        czImage.sprite = null;
        czImage.color = new Color(1f, 1f, 1f, 0f);
        czImage.raycastTarget = true;
    }

    private static void SetButtonStretch(Transform root, string buttonName)
    {
        Transform child = root.Find(buttonName);
        if (child == null || !child.TryGetComponent(out Image image))
            return;

        image.preserveAspect = false;
    }

    private static void ApplyCornerButtonLayout(Transform searchRoot, Transform canvas, MainMenuButtonLayout layout, bool leftCorner)
    {
        if (layout == null || string.IsNullOrWhiteSpace(layout.buttonName))
            return;

        Transform child = searchRoot.Find(layout.buttonName);
        if (child == null)
            child = canvas.Find(layout.buttonName);

        if (child == null || !child.TryGetComponent(out RectTransform rt))
            return;

        rt.SetParent(canvas, false);

        if (leftCorner)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
        }

        rt.anchoredPosition = layout.anchoredPosition;
        rt.sizeDelta = layout.sizeDelta;

        if (child.TryGetComponent(out Image image))
            image.preserveAspect = true;
    }

    private void WireMenuButtons()
    {
        Transform root = mainMenuRoot != null ? mainMenuRoot.transform : null;
        if (root == null)
            return;

        WireButton(root, "Play", PlayGame);
        WireButton(root, "Levels", ShowLevelSelect);
        WireButton(root, "HowToPlay", OpenHowToPlay);
        WireButton(root, "Store", OpenStore);
        WireButton(root, "Settings", OpenSettings);

        // Wire HowTo back/cancel buttons
        if (howToPanel != null)
        {
            Button[] buttons = howToPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                string btnName = b.name.ToLower();
                if (btnName.Contains("back") || btnName.Contains("close") || btnName.Contains("cancle") || btnName.Contains("cancel"))
                {
                    b.onClick.RemoveListener(CloseHowToPlay);
                    b.onClick.AddListener(CloseHowToPlay);
                }
            }
        }

        // Wire Settings back button
        if (settingsPanel != null)
        {
            Transform backBtn = settingsPanel.transform.Find("Back");
            if (backBtn != null)
                WireSubPageButton(backBtn, CloseSettings);
        }

        // Wire Store back button
        if (storeUI != null && storeUI.storePanel != null)
        {
            Button[] buttons = storeUI.storePanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                string btnName = b.name.ToLower();
                if (btnName.Contains("back") || btnName.Contains("close") || btnName.Contains("cancle") || btnName.Contains("cancel"))
                {
                    b.onClick.RemoveListener(CloseStore);
                    b.onClick.AddListener(CloseStore);
                }
            }
        }

        // Wire LevelSelect back button
        if (levelsPanel != null)
        {
            Transform backBtn = levelsPanel.transform.Find("BackButton");
            if (backBtn != null)
                WireSubPageButton(backBtn, CloseLevelSelect);
        }
    }

    private void WireSubPageButton(Transform buttonParent, UnityEngine.Events.UnityAction action)
    {
        // Check ClickZone child first, then parent
        Transform cz = buttonParent.Find("ClickZone");
        Button btn = null;
        if (cz != null)
            btn = cz.GetComponent<Button>();
        if (btn == null)
            buttonParent.TryGetComponent(out btn);
        if (btn == null) return;

        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    private void WireButton(Transform root, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = root.Find(childName);
        if (child == null)
            return;

        Transform cz = child.Find("ClickZone");
        Button button;
        if (cz != null)
            button = cz.GetComponent<Button>();
        else
            child.TryGetComponent(out button);

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public static void RequestOpenLevelSelect()
    {
        PlayerPrefs.SetInt(OpenLevelSelectKey, 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeOpenLevelSelectFlag()
    {
        if (PlayerPrefs.GetInt(OpenLevelSelectKey, 0) != 1)
            return false;

        PlayerPrefs.DeleteKey(OpenLevelSelectKey);
        PlayerPrefs.Save();
        return true;
    }

    public void ShowLevelSelect()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        CoinDisplayUI coinDisplay = FindFirstObjectByType<CoinDisplayUI>(FindObjectsInactive.Include);
        if (coinDisplay != null)
            coinDisplay.gameObject.SetActive(false);

        if (levelsPanel != null)
        {
            levelsPanel.SetActive(true);
            LevelMenu levelMenu = levelsPanel.GetComponent<LevelMenu>();
            if (levelMenu != null)
                levelMenu.RefreshLevelList();
        }
    }

    public void CloseLevelSelect()
    {
        if (levelsPanel != null)
            levelsPanel.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        CoinDisplayUI coinDisplay = FindFirstObjectByType<CoinDisplayUI>(FindObjectsInactive.Include);
        if (coinDisplay != null)
            coinDisplay.gameObject.SetActive(true);
    }

    public void OpenHowToPlay()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        CoinDisplayUI coinDisplay = FindFirstObjectByType<CoinDisplayUI>(FindObjectsInactive.Include);
        if (coinDisplay != null)
            coinDisplay.gameObject.SetActive(false);

        if (howToPanel != null)
            howToPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPanel != null)
            howToPanel.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        CoinDisplayUI coinDisplay = FindFirstObjectByType<CoinDisplayUI>(FindObjectsInactive.Include);
        if (coinDisplay != null)
            coinDisplay.gameObject.SetActive(true);
    }

    public void OpenSettings()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        ShowCoinDisplay();
    }

    public void OpenStore()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (storeUI != null)
            storeUI.Open();
    }

    public void CloseStore()
    {
        if (storeUI != null)
            storeUI.Close();

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        ShowCoinDisplay();
    }

    private void ShowCoinDisplay()
    {
        CoinDisplayUI coinDisplay = FindFirstObjectByType<CoinDisplayUI>(FindObjectsInactive.Include);
        if (coinDisplay != null)
            coinDisplay.gameObject.SetActive(true);
    }


    public void PlayGame()
    {
        int level = LevelProgress.GetFocusLevel();
        if (!LevelProgress.IsLevelUnlocked(level))
            level = 1;

        string sceneName = LevelProgress.GetSceneNameForLevel(level);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            ShowLevelSelect();
            return;
        }

        LevelProgress.SetSelectedMenuLevel(level);
        if (LevelProgress.IsProceduralMenuLevel(level))
            ProceduralSession.MarkFreshRunFromMenu();

        SceneManager.LoadScene(sceneName);
    }
}
