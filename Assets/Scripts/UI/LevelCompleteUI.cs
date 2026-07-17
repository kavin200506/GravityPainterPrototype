using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Level-complete overlay on LevelCompleteCanvas: stars, stats, Restart, Next Level, Home.
/// Hierarchy: Panel (background) → "Level Completed" title → StarContainer → StatsText → ActionButtons.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    private const string ButtonsRootName = "ActionButtons";
    private const string StarContainerName = "StarContainer";
    private const string StatsTextName = "StatsText";
    private const string TitleObjectName = "Text (TMP)";
    private const string RestartResource = "UI/LevelCompleteUI/restart";
    private const string NextLevelResource = "UI/LevelCompleteUI/nextlevel";
    private const string HomeResource = "UI/LevelCompleteUI/home";
    private const string StarFilledResource = "UI/LevelCompleteUI/star_filled";
    private const string StarEmptyResource = "UI/LevelCompleteUI/star_empty";

    [SerializeField] private int currentLevel;
    [SerializeField] private ProceduralLevelBuilder proceduralBuilder;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite restartButtonSprite;
    [SerializeField] private Sprite nextLevelButtonSprite;
    [SerializeField] private Sprite homeButtonSprite;

    [Header("Layout (1080×1920 portrait canvas)")]
    [SerializeField] private float buttonHeight = 200f;
    [SerializeField] private float buttonSpacing = 48f;
    [Tooltip("Y position of the button row — higher = nearer top of screen.")]
    [SerializeField] private float buttonsAnchoredY = 520f;
    [Tooltip("Gap between the title text and the button row.")]
    [SerializeField] private float titleGap = 36f;

    private Button _restartButton;
    private Button _nextLevelButton;
    private Button _homeButton;
    private Image[] _starImages;
    private TextMeshProUGUI _statsText;
    private Sprite _starFilledSprite;
    private Sprite _starEmptySprite;
    private StarEvaluator.StarResult _starResult;
    private bool _starResultReady;
    private int _collectedCoins;
    private int _totalCoins;
    private float _elapsedTime;
    private float _parTime;

    public void ConfigureProcedural(ProceduralLevelBuilder builder)
    {
        proceduralBuilder = builder;
        UpdateNextLevelButton();
    }

    public void SetStarResult(StarEvaluator.StarResult result)
    {
        _starResult = result;
        _starResultReady = true;
    }

    public void SetStatsSnapshot(int collected, int total, float elapsed, float par)
    {
        _collectedCoins = collected;
        _totalCoins = total;
        _elapsedTime = elapsed;
        _parTime = par;
    }

    private bool IsProceduralMode => proceduralBuilder != null;

    private void Awake()
    {
        Debug.Log("[LevelCompleteUI] Awake called on: " + gameObject.name);

        if (currentLevel < 1)
        {
            currentLevel = LevelProgress.GetActiveLevelNumber();
        }

        LoadDefaultSpritesIfNeeded();
        BuildStarContainer();
        BuildStatsText();
        BuildActionButtons();
        LayoutTitleText();

        Debug.Log("[LevelCompleteUI] Awake complete. StarImages=" + (_starImages != null ? _starImages.Length : 0)
            + " StatsText=" + (_statsText != null)
            + " RestartBtn=" + (_restartButton != null)
            + " NextBtn=" + (_nextLevelButton != null)
            + " HomeBtn=" + (_homeButton != null));
    }

    private void OnEnable()
    {
        Debug.Log("[LevelCompleteUI] OnEnable called. starResultReady=" + _starResultReady
            + " level=" + currentLevel
            + " procedural=" + IsProceduralMode);

        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.gameObject.SetActive(false);
        }

        if (currentLevel < 1)
        {
            currentLevel = LevelProgress.GetActiveLevelNumber();
        }

        if (!IsProceduralMode)
        {
            LevelProgress.UnlockThrough(currentLevel);
        }

        EnsureBackgroundVisible();
        EnsureActionButtonsExactLayout();
        UpdateNextLevelButton();
        UpdateProceduralTitle();
        GameplayMusicController.NotifyLevelCompleteOverlayVisible(true);
        
        if (PauseUI.Instance != null)
        {
            PauseUI.Instance.Hide();
        }

        if (_starResultReady)
        {
            Debug.Log("[LevelCompleteUI] Starting star animation. Stars="
                + _starResult.star1 + "," + _starResult.star2 + "," + _starResult.star3
                + " Coins=" + _collectedCoins + "/" + _totalCoins
                + " Time=" + LevelTimer.FormatTime(_elapsedTime) + "/" + LevelTimer.FormatTime(_parTime));
            StartCoroutine(AnimateStarsCoroutine());
        }
        else
        {
            Debug.LogWarning("[LevelCompleteUI] No star result ready! Panel will show without star animation.");
        }
    }

    private void OnDisable()
    {
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.gameObject.SetActive(true);
        }

        GameplayMusicController.NotifyLevelCompleteOverlayVisible(false);
    }

    private void EnsureBackgroundVisible()
    {
        Transform panel = transform.Find("Panel");
        if (panel == null)
        {
            panel = transform.Find("SafeAreaPanel/Panel");
        }
        if (panel == null)
        {
            // Fallback to finding the first Panel named child or any background Image inside this canvas
            foreach (Image img in GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name == "Panel" || img.gameObject.name == "Background")
                {
                    panel = img.transform;
                    break;
                }
            }
        }
        if (panel == null)
        {
            Debug.LogWarning("[LevelCompleteUI] No 'Panel' child found for background!");
            return;
        }

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning("[LevelCompleteUI] Panel has no Image component!");
            return;
        }

        Sprite bgToUse = backgroundSprite != null ? backgroundSprite : LevelCompleteCanvasFactory.LoadBackgroundSprite();
        if (bgToUse != null)
        {
            image.sprite = bgToUse;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            Debug.Log("[LevelCompleteUI] Background sprite applied: " + bgToUse.name);
        }
        else if (image.sprite != null)
        {
            image.preserveAspect = false;
            image.color = Color.white;
        }
    }

    private void LoadDefaultSpritesIfNeeded()
    {
        restartButtonSprite = EnsureSprite(restartButtonSprite, RestartResource);
        nextLevelButtonSprite = EnsureSprite(nextLevelButtonSprite, NextLevelResource);
        homeButtonSprite = EnsureSprite(homeButtonSprite, HomeResource);
        _starFilledSprite = EnsureSprite(_starFilledSprite, StarFilledResource);
        _starEmptySprite = EnsureSprite(_starEmptySprite, StarEmptyResource);

        Debug.Log("[LevelCompleteUI] Sprites loaded:"
            + " restart=" + (restartButtonSprite != null)
            + " next=" + (nextLevelButtonSprite != null)
            + " home=" + (homeButtonSprite != null)
            + " starFilled=" + (_starFilledSprite != null)
            + " starEmpty=" + (_starEmptySprite != null));
    }

    private static Sprite EnsureSprite(Sprite assigned, string resourcesPath)
    {
        if (assigned != null)
        {
            return assigned;
        }

        return Resources.Load<Sprite>(resourcesPath);
    }

    private void LayoutTitleText()
    {
        Transform title = transform.Find(TitleObjectName);
        if (title is not RectTransform titleRect)
        {
            return;
        }

        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);

        float titleHeight = titleRect.sizeDelta.y;
        float titleY = buttonsAnchoredY + buttonHeight * 0.5f + titleGap + titleHeight * 0.5f + 300f;
        titleRect.anchoredPosition = new Vector2(0f, titleY);

        titleRect.SetAsLastSibling();
    }

    private void UpdateProceduralTitle()
    {
        if (!IsProceduralMode)
        {
            return;
        }

        Transform title = transform.Find(TitleObjectName);
        TextMeshProUGUI label = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
        if (label == null)
        {
            return;
        }

        int nextMenuLevel = LevelProgress.GetSelectedMenuLevel() + 1;
        float nextDifficulty = LevelProgress.GetDifficultyForMenuLevel(nextMenuLevel);
        label.text =
            "Level Completed!\nNext: "
            + DifficultyManager.GetTierName(nextDifficulty)
            + " ("
            + nextDifficulty.ToString("F2")
            + ")";
    }

    // ── Star Container ───────────────────────────────────────────────────

    private void BuildStarContainer()
    {
        Transform existing = transform.Find(StarContainerName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject container = new GameObject(StarContainerName, typeof(RectTransform));
        container.transform.SetParent(transform, false);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(600f, 200f);
        containerRect.anchoredPosition = new Vector2(0f, 141f);

        _starImages = new Image[3];
        float starSize = 150f;
        float starSpacing = 40f;
        float totalWidth = starSize * 3 + starSpacing * 2f;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < 3; i++)
        {
            GameObject starObj = new GameObject("Star" + (i + 1), typeof(RectTransform), typeof(Image));
            starObj.transform.SetParent(container.transform, false);

            RectTransform starRect = starObj.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.pivot = new Vector2(0.5f, 0.5f);
            starRect.sizeDelta = new Vector2(starSize, starSize);
            starRect.anchoredPosition = new Vector2(startX + (starSize * 0.5f) + i * (starSize + starSpacing), 0f);

            Image starImage = starObj.GetComponent<Image>();
            starImage.sprite = _starEmptySprite;
            starImage.type = Image.Type.Simple;
            starImage.preserveAspect = true;

            if (_starEmptySprite == null)
            {
                starImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            }
            else
            {
                starImage.color = Color.white;
            }

            _starImages[i] = starImage;
        }
    }

    private void BuildStatsText()
    {
        Transform existing = transform.Find(StatsTextName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
        _statsText = null;
    }

    private void UpdateStatsText()
    {
        if (_statsText == null)
        {
            return;
        }

        string time = LevelTimer.FormatTime(_elapsedTime);
        string par = LevelTimer.FormatTime(_parTime);

        _statsText.text = _collectedCoins + "/" + _totalCoins + " coins  |  " + time + " / " + par;

        Debug.Log("[LevelCompleteUI] Stats: " + _statsText.text);
    }

    // ── Star Animation ───────────────────────────────────────────────────

    private IEnumerator AnimateStarsCoroutine()
    {
        if (_starImages == null)
        {
            yield break;
        }

        foreach (Image img in _starImages)
        {
            if (img != null)
            {
                img.transform.localScale = Vector3.zero;
            }
        }

        if (_statsText != null)
        {
            _statsText.gameObject.SetActive(false);
        }
        UpdateStatsText();

        yield return new WaitForSecondsRealtime(0.5f);

        int totalStars = _starResult.totalStars;

        for (int i = 0; i < 3; i++)
        {
            bool earned = i < totalStars;
            yield return StartCoroutine(AnimateSingleStar(i, earned));
            yield return new WaitForSecondsRealtime(0.4f);
        }

        if (_statsText != null)
        {
            _statsText.gameObject.SetActive(true);
        }
    }

    private IEnumerator AnimateSingleStar(int index, bool earned)
    {
        if (index < 0 || index >= _starImages.Length || _starImages[index] == null)
        {
            yield break;
        }

        Image starImage = _starImages[index];
        Transform starTransform = starImage.transform;

        if (earned)
        {
            if (_starFilledSprite != null)
            {
                starImage.sprite = _starFilledSprite;
            }
            else
            {
                starImage.color = new Color(1f, 0.84f, 0f, 1f);
            }
        }

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scale = Mathf.Lerp(0f, 1.3f, t);
            starTransform.localScale = new Vector3(scale, scale, 1f);

            if (!earned)
            {
                starImage.color = new Color(0.5f, 0.5f, 0.5f, Mathf.Lerp(0f, 0.6f, t));
            }

            yield return null;
        }

        elapsed = 0f;
        float bounceDuration = 0.15f;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);

            float scale = Mathf.Lerp(1.3f, 1f, t);
            starTransform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        starTransform.localScale = Vector3.one;
    }

    // ── Action Buttons ───────────────────────────────────────────────────

    private void BuildActionButtons()
    {
        Transform existing = transform.Find(ButtonsRootName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject root = new GameObject(ButtonsRootName, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 100f);
        rootRect.sizeDelta = new Vector2(1080f, 250f);

        Vector2 buttonSize = new Vector2(300f, 200f);

        _restartButton = PlaceButtonAt(root.transform, "Restart", RestartLevel, restartButtonSprite, buttonSize, new Vector2(-382f, -55f), new Vector3(1f, 1.2517f, 1f));
        _nextLevelButton = PlaceButtonAt(root.transform, "Next Level", GoToNextLevel, nextLevelButtonSprite, buttonSize, new Vector2(-2f, -65f), new Vector3(1.5373f, 1.8618f, 1f));
        _homeButton = PlaceButtonAt(root.transform, "Home", GoHome, homeButtonSprite, buttonSize, new Vector2(385f, -65f), new Vector3(1f, 1.2517f, 1f));

        UpdateNextLevelButton();
    }

    private void EnsureActionButtonsExactLayout()
    {
        if (_restartButton == null || _nextLevelButton == null || _homeButton == null)
        {
            BuildActionButtons();
        }

        if (_restartButton != null)
        {
            RectTransform r = _restartButton.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(-382f, -55f);
            r.localScale = new Vector3(1f, 1.2517f, 1f);
        }

        if (_nextLevelButton != null)
        {
            RectTransform r = _nextLevelButton.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(-2f, -65f);
            r.localScale = new Vector3(1.5373f, 1.8618f, 1f);
        }

        if (_homeButton != null)
        {
            RectTransform r = _homeButton.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(385f, -65f);
            r.localScale = new Vector3(1f, 1.2517f, 1f);
        }
    }

    private Button PlaceButtonAt(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction onClick,
        Sprite sprite,
        Vector2 size,
        Vector2 position,
        Vector3 scale)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = scale;

        Image image = buttonObject.GetComponent<Image>();
        image.raycastTarget = true;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return button;
    }

    private void UpdateNextLevelButton()
    {
        if (_nextLevelButton == null)
        {
            return;
        }

        if (IsProceduralMode)
        {
            _nextLevelButton.gameObject.SetActive(true);
            _nextLevelButton.interactable = true;
            return;
        }

        bool hasNext = LevelProgress.HasBuiltLevel(currentLevel + 1);
        _nextLevelButton.gameObject.SetActive(true);
        _nextLevelButton.interactable = hasNext;
    }

    // ── Navigation ───────────────────────────────────────────────────────

    public void RestartLevel()
    {
        LifeManager.ResetLives();
        CoinManager.ResetSessionCoins();
        CoinManager.ResetTotalCoinsInLevel();
        LevelTimer.Reset();
        _starResultReady = false;

        if (IsProceduralMode || LevelProgress.IsProceduralScene(SceneManager.GetActiveScene()))
        {
            if (proceduralBuilder == null)
            {
                proceduralBuilder = FindFirstObjectByType<ProceduralLevelBuilder>();
            }

            if (proceduralBuilder == null)
            {
                Debug.LogWarning("LevelCompleteUI: no ProceduralLevelBuilder found for Restart.");
                return;
            }

            HideAndResume();
            proceduralBuilder.RebuildSameSeed();
            return;
        }

        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
        {
            SceneManager.LoadScene(active.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(active.name);
        }
    }

    public void GoToNextLevel()
    {
        LifeManager.ResetLives();
        CoinManager.ResetSessionCoins();
        CoinManager.ResetTotalCoinsInLevel();
        LevelTimer.Reset();
        _starResultReady = false;

        if (IsProceduralMode || LevelProgress.IsProceduralScene(SceneManager.GetActiveScene()))
        {
            if (proceduralBuilder == null)
            {
                proceduralBuilder = FindFirstObjectByType<ProceduralLevelBuilder>();
            }

            if (proceduralBuilder == null)
            {
                Debug.LogWarning("LevelCompleteUI: no ProceduralLevelBuilder found for Next Level.");
                return;
            }

            HideAndResume();
            int nextMenuLevel = LevelProgress.GetSelectedMenuLevel() + 1;
            LevelProgress.SetSelectedMenuLevel(nextMenuLevel);
            currentLevel = nextMenuLevel;
            proceduralBuilder.RebuildNextSeed();
            UpdateProceduralTitle();
            return;
        }

        Time.timeScale = 1f;
        int next = currentLevel + 1;

        if (!LevelProgress.HasBuiltLevel(next))
        {
            GoHome();
            return;
        }

        LevelProgress.SetSelectedMenuLevel(next);
        if (LevelProgress.IsProceduralMenuLevel(next))
        {
            ProceduralSession.MarkFreshRunFromMenu();
        }

        SceneManager.LoadScene(LevelProgress.GetSceneNameForLevel(next));
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        LevelTimer.Reset();
        _starResultReady = false;
        MainMenu.RequestOpenLevelSelect();
        SceneManager.LoadScene("MainMenu");
    }

    private void HideAndResume()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        if (PauseUI.Instance != null)
        {
            PauseUI.Instance.Show();
        }
    }

    private Vector2 GetButtonSize(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Vector2(buttonHeight, buttonHeight);
        }

        float w = Mathf.Max(sprite.rect.width, 1f);
        float h = Mathf.Max(sprite.rect.height, 1f);
        float aspect = w / h;
        aspect = Mathf.Clamp(aspect, 0.65f, 1.35f);
        return new Vector2(buttonHeight * aspect, buttonHeight);
    }
}
