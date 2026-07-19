using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    public static PauseUI Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance._isPaused;

    private bool _isPaused = false;
    private GameObject _pauseOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only show in actual gameplay levels
        if (scene.name.StartsWith("Level ") || LevelProgress.IsProceduralScene(scene))
        {
            CreatePauseCanvas();
        }
    }

    private static void CreatePauseCanvas()
    {
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        canvasObj.AddComponent<GraphicRaycaster>();
        
        PauseUI pauseUI = canvasObj.AddComponent<PauseUI>();
        pauseUI.SetupUI(canvasObj.transform);
    }

    private static Sprite LoadPauseSprite(string primaryPath, string fallbackPath = null)
    {
        Sprite sp = Resources.Load<Sprite>(primaryPath);
        if (sp == null && !string.IsNullOrEmpty(fallbackPath))
        {
            sp = Resources.Load<Sprite>(fallbackPath);
        }
#if UNITY_EDITOR
        if (sp == null)
        {
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + primaryPath + ".png");
            if (sp == null && !string.IsNullOrEmpty(fallbackPath))
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + fallbackPath + ".png");
        }
#endif
        return sp;
    }

    private void SetupUI(Transform parent)
    {
        // 1. Pause Overlay (semi-transparent backdrop so playing level remains visible in background)
        _pauseOverlay = new GameObject("PauseOverlay");
        _pauseOverlay.transform.SetParent(parent, false);
        
        RectTransform overlayRect = _pauseOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = _pauseOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
        _pauseOverlay.SetActive(false);

        // Pause Panel (Centered Modal Window using pause_page.png)
        GameObject panelObj = new GameObject("PausePanel");
        panelObj.transform.SetParent(_pauseOverlay.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900f, 650f);

        Image panelImage = panelObj.AddComponent<Image>();
        Sprite bgSprite = LoadPauseSprite("UI/Pause_Page/pause_page", "UI/PauseBackground");
        if (bgSprite != null)
        {
            panelImage.sprite = bgSprite;
            panelImage.preserveAspect = true;
        }
        else
        {
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        }

        // Center Container for the 3 Action Buttons inside the panel
        GameObject buttonGroupObj = new GameObject("ButtonGroup");
        buttonGroupObj.transform.SetParent(panelObj.transform, false);
        RectTransform groupRt = buttonGroupObj.AddComponent<RectTransform>();
        groupRt.anchorMin = new Vector2(0.5f, 0.5f);
        groupRt.anchorMax = new Vector2(0.5f, 0.5f);
        groupRt.pivot = new Vector2(0.5f, 0.5f);
        groupRt.anchoredPosition = new Vector2(0f, -20f);
        groupRt.sizeDelta = new Vector2(800f, 250f);

        // LEFT: Restart Button (reply.png)
        GameObject restartBtnObj = new GameObject("RestartButton");
        restartBtnObj.transform.SetParent(buttonGroupObj.transform, false);
        RectTransform restartRect = restartBtnObj.AddComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.pivot = new Vector2(0.5f, 0.5f);
        restartRect.sizeDelta = new Vector2(175f, 175f);
        restartRect.anchoredPosition = new Vector2(-230f, 0f);

        Image restartImg = restartBtnObj.AddComponent<Image>();
        Sprite restartSprite = LoadPauseSprite("UI/Pause_Page/reply", "UI/RestartIcon");
        if (restartSprite != null)
        {
            restartImg.sprite = restartSprite;
            restartImg.color = Color.white;
            restartImg.preserveAspect = true;
        }

        Button restartBtn = restartBtnObj.AddComponent<Button>();
        restartBtn.onClick.AddListener(RestartLevel);

        // CENTER: Resume Button (play.png)
        GameObject resumeBtnObj = new GameObject("ResumeButton");
        resumeBtnObj.transform.SetParent(buttonGroupObj.transform, false);
        RectTransform resumeRect = resumeBtnObj.AddComponent<RectTransform>();
        resumeRect.anchorMin = new Vector2(0.5f, 0.5f);
        resumeRect.anchorMax = new Vector2(0.5f, 0.5f);
        resumeRect.pivot = new Vector2(0.5f, 0.5f);
        resumeRect.sizeDelta = new Vector2(210f, 210f);
        resumeRect.anchoredPosition = new Vector2(0f, 0f);

        Image resumeImg = resumeBtnObj.AddComponent<Image>();
        Sprite resumeSprite = LoadPauseSprite("UI/Pause_Page/play", "UI/ResumeIcon");
        if (resumeSprite != null)
        {
            resumeImg.sprite = resumeSprite;
            resumeImg.color = Color.white;
            resumeImg.preserveAspect = true;
        }

        Button resumeBtn = resumeBtnObj.AddComponent<Button>();
        resumeBtn.onClick.AddListener(TogglePause);

        // RIGHT: Home Button (home.png)
        GameObject homeBtnObj = new GameObject("HomeButton");
        homeBtnObj.transform.SetParent(buttonGroupObj.transform, false);
        RectTransform homeRect = homeBtnObj.AddComponent<RectTransform>();
        homeRect.anchorMin = new Vector2(0.5f, 0.5f);
        homeRect.anchorMax = new Vector2(0.5f, 0.5f);
        homeRect.pivot = new Vector2(0.5f, 0.5f);
        homeRect.sizeDelta = new Vector2(175f, 175f);
        homeRect.anchoredPosition = new Vector2(230f, 0f);

        Image homeImg = homeBtnObj.AddComponent<Image>();
        Sprite homeSprite = LoadPauseSprite("UI/Pause_Page/home", "UI/HomeIcon");
        if (homeSprite != null)
        {
            homeImg.sprite = homeSprite;
            homeImg.color = Color.white;
            homeImg.preserveAspect = true;
        }

        Button homeBtn = homeBtnObj.AddComponent<Button>();
        homeBtn.onClick.AddListener(GoHome);

        // 2. In-Game Pause Button (Top Right HUD)
        GameObject btnObj = new GameObject("PauseButton");
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-50f, -50f);
        btnRect.sizeDelta = new Vector2(140f, 140f);

        Image btnImg = btnObj.AddComponent<Image>();
        Sprite pauseSprite = LoadPauseSprite("UI/Pause_Page/pause", "UI/PauseIcon");
        if (pauseSprite != null)
        {
            btnImg.sprite = pauseSprite;
            btnImg.color = Color.white;
            btnImg.preserveAspect = true;
        }
        else
        {
            btnImg.color = Color.white; 
        }

        Button pauseBtn = btnObj.AddComponent<Button>();
        pauseBtn.onClick.AddListener(TogglePause);
        
        EnsureEventSystem();
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        _pauseOverlay.SetActive(_isPaused);
    }
    
    private void RestartLevel()
    {
        TogglePause(); // Unpause and hide overlay
        LifeManager.ResetLives();

        Scene active = SceneManager.GetActiveScene();
        if (LevelProgress.IsProceduralScene(active))
        {
            ProceduralLevelBuilder builder = FindFirstObjectByType<ProceduralLevelBuilder>();
            if (builder != null)
            {
                builder.RebuildSameSeed();
            }
            return;
        }

        // Standard level restart
        if (active.buildIndex >= 0)
        {
            SceneManager.LoadScene(active.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(active.name);
        }
    }

    private void GoHome()
    {
        Time.timeScale = 1f;
        MainMenu.RequestOpenLevelSelect();
        SceneManager.LoadScene("MainMenu");
    }
    
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}

