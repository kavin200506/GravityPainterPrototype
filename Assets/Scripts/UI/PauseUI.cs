using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseUI : MonoBehaviour
{
    public static PauseUI Instance { get; private set; }

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

    private void SetupUI(Transform parent)
    {
        // 1. Pause Overlay (hidden by default)
        _pauseOverlay = new GameObject("PauseOverlay");
        _pauseOverlay.transform.SetParent(parent, false);
        
        RectTransform overlayRect = _pauseOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = _pauseOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.75f);
        _pauseOverlay.SetActive(false);

        // Pause Panel (Golden Frame Background)
        GameObject panelObj = new GameObject("PausePanel");
        panelObj.transform.SetParent(_pauseOverlay.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 600f); // Landscape to fit the new scroll

        Image panelImage = panelObj.AddComponent<Image>();
        Sprite bgSprite = Resources.Load<Sprite>("UI/PauseBackground");
        if (bgSprite == null)
        {
            Texture2D bgTex = Resources.Load<Texture2D>("UI/PauseBackground");
            if (bgTex != null)
            {
                bgSprite = Sprite.Create(bgTex, new Rect(0f, 0f, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            }
        }
        
        if (bgSprite != null)
        {
            panelImage.sprite = bgSprite;
            // Optionally preserve aspect ratio if needed, but for a frame we usually slice or stretch. 
            // We'll preserve aspect so the frame doesn't look distorted.
            panelImage.preserveAspect = true;
        }
        else
        {
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        // Resume Button inside PausePanel
        GameObject resumeBtnObj = new GameObject("ResumeButton");
        resumeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform resumeRect = resumeBtnObj.AddComponent<RectTransform>();
        resumeRect.sizeDelta = new Vector2(200f, 200f);
        resumeRect.anchoredPosition = new Vector2(0f, 0f); // Dead center

        Image resumeImg = resumeBtnObj.AddComponent<Image>();
        Sprite resumeSprite = Resources.Load<Sprite>("UI/ResumeIcon");
        if (resumeSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UI/ResumeIcon");
            if (tex != null)
            {
                resumeSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (resumeSprite != null)
        {
            resumeImg.sprite = resumeSprite;
            resumeImg.color = Color.white;
            resumeImg.preserveAspect = true;
        }
        else
        {
            resumeImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        }

        Button resumeBtn = resumeBtnObj.AddComponent<Button>();
        resumeBtn.onClick.AddListener(TogglePause);


        // Restart Button inside PausePanel
        GameObject restartBtnObj = new GameObject("RestartButton");
        restartBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform restartRect = restartBtnObj.AddComponent<RectTransform>();
        restartRect.sizeDelta = new Vector2(200f, 200f); // Make it square for the icon
        restartRect.anchoredPosition = new Vector2(-280f, 0f); // Left side

        Image restartImg = restartBtnObj.AddComponent<Image>();
        Sprite restartSprite = Resources.Load<Sprite>("UI/RestartIcon");
        if (restartSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UI/RestartIcon");
            if (tex != null)
            {
                restartSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (restartSprite != null)
        {
            restartImg.sprite = restartSprite;
            restartImg.color = Color.white;
            restartImg.preserveAspect = true;
        }
        else
        {
            // Fallback
            restartImg.color = new Color(0f, 0f, 0f, 0.4f); 
        }

        Button restartBtn = restartBtnObj.AddComponent<Button>();
        restartBtn.onClick.AddListener(RestartLevel);

        // We no longer need the text object since it's an icon now!

        // Home Button inside PausePanel
        GameObject homeBtnObj = new GameObject("HomeButton");
        homeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform homeRect = homeBtnObj.AddComponent<RectTransform>();
        homeRect.sizeDelta = new Vector2(200f, 200f); // Make it square for the icon
        homeRect.anchoredPosition = new Vector2(280f, 0f); // Right side

        Image homeImg = homeBtnObj.AddComponent<Image>();
        Sprite homeSprite = Resources.Load<Sprite>("UI/HomeIcon");
        if (homeSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UI/HomeIcon");
            if (tex != null)
            {
                homeSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (homeSprite != null)
        {
            homeImg.sprite = homeSprite;
            homeImg.color = Color.white;
            homeImg.preserveAspect = true;
        }
        else
        {
            // Fallback
            homeImg.color = new Color(0f, 0f, 0f, 0.4f); 
        }

        Button homeBtn = homeBtnObj.AddComponent<Button>();
        homeBtn.onClick.AddListener(GoHome);

        // We no longer need the text object since it's an icon now!
        
        // Add nice labels underneath the three icons
        AddLabelUnderButton(resumeBtnObj, "Resume");
        AddLabelUnderButton(restartBtnObj, "Restart");
        AddLabelUnderButton(homeBtnObj, "Home");

        // 2. Pause Button (Top Right)
        GameObject btnObj = new GameObject("PauseButton");
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-50f, -50f);
        btnRect.sizeDelta = new Vector2(150f, 150f);

        Image btnImg = btnObj.AddComponent<Image>();
        Sprite pauseSprite = Resources.Load<Sprite>("UI/PauseIcon");
        if (pauseSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UI/PauseIcon");
            if (tex != null)
            {
                pauseSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (pauseSprite != null)
        {
            btnImg.sprite = pauseSprite;
        }
        else
        {
            // Fallback appearance if the sprite is missing
            btnImg.color = Color.white; 
            Debug.LogWarning("Pause icon sprite not found at Resources/UI/PauseIcon. Please place the image there and set its Texture Type to Sprite (2D and UI).");
        }

        Button pauseBtn = btnObj.AddComponent<Button>();
        pauseBtn.onClick.AddListener(TogglePause);
        
        EnsureEventSystem();
    }

    private void AddLabelUnderButton(GameObject buttonObj, string labelText)
    {
        GameObject textObj = new GameObject("Label (TMP)");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(300f, 60f);
        textRect.anchoredPosition = new Vector2(0f, -140f); // Positioned nicely below the 200x200 icon
        
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = labelText;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 50f;
        tmpText.color = Color.white; 
        tmpText.fontStyle = FontStyles.Bold;
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
