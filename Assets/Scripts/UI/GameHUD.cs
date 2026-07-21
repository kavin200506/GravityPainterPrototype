using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    private Image[] _heartIcons;
    private TextMeshProUGUI _coinCountText;
    private TextMeshProUGUI _timerText;
    private TextMeshProUGUI _levelText;
    private int _lastLives = -1;
    private int _lastCoins = -1;
    private int _lastTotalCoins = -1;
    private int _lastLevelNumber = -1;
    private string _lastTimerString = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level ") || LevelProgress.IsProceduralScene(scene))
        {
            LifeManager.ResetLives();
            CreateHUD();
            InitializeLevelTimer(scene);
        }
    }

    private static void InitializeLevelTimer(Scene scene)
    {
        LevelTimer.Reset();
        CoinManager.ResetSessionCoins();

        Debug.Log("[GameHUD] InitializeLevelTimer for scene: " + scene.name);

        if (LevelProgress.IsProceduralScene(scene))
        {
            ProceduralLevelBuilder builder = FindFirstObjectByType<ProceduralLevelBuilder>();
            if (builder != null)
            {
                Debug.Log("[GameHUD] Procedural level found. LastBuiltSeed=" + builder.LastBuiltSeed
                    + " TileCount=" + builder.LastBuiltTileCount
                    + " CoinCount=" + builder.LastBuiltCoinCount);

                // Unsubscribe first to prevent duplicate subscriptions
                builder.OnLevelBuilt -= OnProceduralLevelBuilt;
                builder.OnLevelBuilt += OnProceduralLevelBuilt;

                // ProceduralLevelBuilder fires OnLevelBuilt in Awake() with DefaultExecutionOrder(-200),
                // which runs BEFORE OnSceneLoaded. If the level is already built, start the timer now.
                if (builder.LastBuiltTileCount > 0)
                {
                    Debug.Log("[GameHUD] Builder already built! Starting timer immediately with "
                        + builder.LastBuiltTileCount + " tiles, " + builder.LastBuiltCoinCount + " coins");
                    OnProceduralLevelBuilt(builder.LastBuiltSeed, builder.LastBuiltTileCount, builder.LastBuiltCoinCount);
                }
            }
            else
            {
                Debug.LogWarning("[GameHUD] No ProceduralLevelBuilder found! Timer will not start.");
            }
        }
        else
        {
            Debug.Log("[GameHUD] Campaign level - starting timer immediately");
            StartTimerForCampaignLevel();
        }
    }

    private static void OnProceduralLevelBuilt(int seed, int tileCount, int coinCount)
    {
        CoinManager.SetTotalCoinsInLevel(coinCount);
        LevelTimer.Start(tileCount);

        Debug.Log("[GameHUD] Level built! Seed=" + seed + " Tiles=" + tileCount
            + " Coins=" + coinCount + " ParTime=" + LevelTimer.FormatTime(LevelTimer.ParTime));
    }

    private static void StartTimerForCampaignLevel()
    {
        TileZone[] tiles = FindObjectsByType<TileZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int tileCount = tiles.Length;

        int coinCount = CountCoinsInLevel();
        CoinManager.SetTotalCoinsInLevel(coinCount);
        LevelTimer.Start(tileCount);

        Debug.Log("[GameHUD] Campaign level started! Tiles=" + tileCount
            + " Coins=" + coinCount + " ParTime=" + LevelTimer.FormatTime(LevelTimer.ParTime));
    }

    private static int CountCoinsInLevel()
    {
        Coin[] coins = FindObjectsByType<Coin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return coins.Length;
    }

    private static void CreateHUD()
    {
        GameObject canvasObj = new GameObject("GameHUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        canvasObj.AddComponent<GraphicRaycaster>();

        GameHUD hud = canvasObj.AddComponent<GameHUD>();
        hud.SetupUI();
    }

    private void SetupUI()
    {
        Sprite heartSprite = Resources.Load<Sprite>("UI/heart_full");
        Sprite coinSprite = Resources.Load<Sprite>("UI/coin_icon");

        GameObject heartsObj = new GameObject("Hearts");
        heartsObj.transform.SetParent(transform, false);
        RectTransform heartsRect = heartsObj.AddComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(1f, 1f);
        heartsRect.anchorMax = new Vector2(1f, 1f);
        heartsRect.pivot = new Vector2(1f, 1f);
        heartsRect.anchoredPosition = new Vector2(-30f, -180f);

        _heartIcons = new Image[LifeManager.MaxLives];
        for (int i = 0; i < LifeManager.MaxLives; i++)
        {
            GameObject heartObj = new GameObject("Heart" + (i + 1));
            heartObj.transform.SetParent(heartsObj.transform, false);
            RectTransform heartRect = heartObj.AddComponent<RectTransform>();
            heartRect.sizeDelta = new Vector2(75f, 75f);
            heartRect.anchorMin = new Vector2(1f, 0.5f);
            heartRect.anchorMax = new Vector2(1f, 0.5f);
            heartRect.pivot = new Vector2(1f, 0.5f);
            heartRect.anchoredPosition = new Vector2(-i * 85f, 0f);

            Image heartImg = heartObj.AddComponent<Image>();
            if (heartSprite != null)
                heartImg.sprite = heartSprite;
            else
                heartImg.color = Color.red;

            _heartIcons[i] = heartImg;
        }

        GameObject coinObj = new GameObject("CoinDisplay");
        coinObj.transform.SetParent(transform, false);
        RectTransform coinRect = coinObj.AddComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(1f, 1f);
        coinRect.anchorMax = new Vector2(1f, 1f);
        coinRect.pivot = new Vector2(1f, 1f);
        coinRect.anchoredPosition = new Vector2(-30f, -270f);

        GameObject coinIconObj = new GameObject("CoinIcon");
        coinIconObj.transform.SetParent(coinObj.transform, false);
        RectTransform coinIconRect = coinIconObj.AddComponent<RectTransform>();
        coinIconRect.sizeDelta = new Vector2(55f, 55f);
        coinIconRect.anchorMin = new Vector2(1f, 0.5f);
        coinIconRect.anchorMax = new Vector2(1f, 0.5f);
        coinIconRect.pivot = new Vector2(1f, 0.5f);
        coinIconRect.anchoredPosition = new Vector2(0f, 0f);

        Image coinIcon = coinIconObj.AddComponent<Image>();
        if (coinSprite != null)
            coinIcon.sprite = coinSprite;
        else
            coinIcon.color = Color.yellow;

        GameObject coinTextObj = new GameObject("CoinCount");
        coinTextObj.transform.SetParent(coinObj.transform, false);
        RectTransform coinTextRect = coinTextObj.AddComponent<RectTransform>();
        coinTextRect.sizeDelta = new Vector2(200f, 60f);
        coinTextRect.anchorMin = new Vector2(1f, 0.5f);
        coinTextRect.anchorMax = new Vector2(1f, 0.5f);
        coinTextRect.pivot = new Vector2(1f, 0.5f);
        coinTextRect.anchoredPosition = new Vector2(-65f, 0f);

        _coinCountText = coinTextObj.AddComponent<TextMeshProUGUI>();
        _coinCountText.fontSize = 44f;
        _coinCountText.color = Color.white;
        _coinCountText.alignment = TextAlignmentOptions.MidlineRight;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            _coinCountText.font = font;
        }

        CreateLevelDisplay();
        CreateTimerDisplay();

        UpdateFromState();
    }

    private void CreateLevelDisplay()
    {
        GameObject levelObj = new GameObject("LevelDisplay");
        levelObj.transform.SetParent(transform, false);
        RectTransform levelRt = levelObj.AddComponent<RectTransform>();
        levelRt.anchorMin = new Vector2(0f, 1f);
        levelRt.anchorMax = new Vector2(0f, 1f);
        levelRt.pivot = new Vector2(0f, 1f);
        levelRt.anchoredPosition = new Vector2(35f, -35f);
        levelRt.sizeDelta = new Vector2(300f, 95f);

        Image bgImage = levelObj.AddComponent<Image>();
        Sprite holderSprite = Resources.Load<Sprite>("UI/owned");
        if (holderSprite == null)
            holderSprite = Resources.Load<Sprite>("UI/Store_Page/owned");
#if UNITY_EDITOR
        if (holderSprite == null)
            holderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Store_Page/owned.png");
#endif
        if (holderSprite != null)
        {
            bgImage.sprite = holderSprite;
            bgImage.preserveAspect = false; // Allow horizontal & vertical expansion
            bgImage.color = Color.white;
        }
        else
        {
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);
        }

        GameObject textObj = new GameObject("LevelText");
        textObj.transform.SetParent(levelObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI levelText = textObj.AddComponent<TextMeshProUGUI>();
        int levelNum = LevelProgress.GetActiveLevelNumber();
        levelText.text = "LEVEL " + levelNum;
        levelText.fontSize = 32f;
        levelText.color = Color.white;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.fontStyle = FontStyles.Bold;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            levelText.font = font;
        }

        _levelText = levelText;
        _lastLevelNumber = levelNum;
    }

    private void UpdateFromState()
    {
        UpdateLives(LifeManager.CurrentLives);
        UpdateCoins(CoinManager.SessionCoins, CoinManager.TotalCoinsInLevel);
    }

    public void UpdateLives(int lives)
    {
        _lastLives = lives;
        for (int i = 0; i < _heartIcons.Length; i++)
        {
            if (_heartIcons[i] != null)
                _heartIcons[i].gameObject.SetActive(i < lives);
        }
    }

    public void UpdateCoins(int count, int totalInLevel)
    {
        _lastCoins = count;
        _lastTotalCoins = totalInLevel;
        if (_coinCountText != null)
        {
            if (totalInLevel > 0)
            {
                // Show collected / required, where required = ceil(total * 0.70)
                // so the player always sees exactly how many coins they need for the star.
                int required = Mathf.CeilToInt(totalInLevel * StarEvaluator.CoinThreshold);
                _coinCountText.text = count + " / " + required;
            }
            else
            {
                _coinCountText.text = count.ToString();
            }
        }
    }

    private void CreateTimerDisplay()
    {
        GameObject timerObj = new GameObject("TimerDisplay");
        timerObj.transform.SetParent(transform, false);
        RectTransform timerRect = timerObj.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0f, 1f);
        timerRect.anchorMax = new Vector2(0f, 1f);
        timerRect.pivot = new Vector2(0f, 1f);
        timerRect.anchoredPosition = new Vector2(676f, -85f);
        timerRect.sizeDelta = new Vector2(300f, 80f);

        GameObject iconObj = new GameObject("TimerIcon");
        iconObj.transform.SetParent(timerObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(50f, 50f);

        Image iconImg = iconObj.AddComponent<Image>();
        Sprite timerSprite = Resources.Load<Sprite>("UI/Pause_Page/timer");
        if (timerSprite == null)
            timerSprite = Resources.Load<Sprite>("UI/timer");
        if (timerSprite != null)
            iconImg.sprite = timerSprite;
        else
            iconImg.color = Color.white;

        GameObject textObj = new GameObject("TimerText");
        textObj.transform.SetParent(timerObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(60f, 0f);
        textRt.offsetMax = Vector2.zero;

        _timerText = textObj.AddComponent<TextMeshProUGUI>();
        _timerText.fontSize = 56f;
        _timerText.color = Color.white;
        _timerText.alignment = TextAlignmentOptions.MidlineLeft;
        _timerText.fontStyle = FontStyles.Bold;
        _timerText.text = "";

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            _timerText.font = font;
        }
    }

    private void UpdateTimerDisplay()
    {
        if (_timerText == null)
        {
            return;
        }

        if (!LevelTimer.IsRunning)
        {
            _timerText.text = "0:00";
            _timerText.color = Color.red;
            return;
        }

        string timerString = LevelTimer.GetRemainingTimeString();
        if (timerString != _lastTimerString)
        {
            _lastTimerString = timerString;
            _timerText.text = timerString;

            if (LevelTimer.RemainingTime <= 10f)
            {
                _timerText.color = Color.red;
            }
            else if (LevelTimer.RemainingTime <= 20f)
            {
                _timerText.color = Color.yellow;
            }
            else
            {
                _timerText.color = Color.white;
            }
        }
    }

    private void Update()
    {
        LevelTimer.Tick();

        int lives = LifeManager.CurrentLives;
        if (lives != _lastLives)
            UpdateLives(lives);

        int coins = CoinManager.SessionCoins;
        int totalCoins = CoinManager.TotalCoinsInLevel;
        if (coins != _lastCoins || totalCoins != _lastTotalCoins)
            UpdateCoins(coins, totalCoins);

        UpdateTimerDisplay();
        UpdateLevelDisplay();
    }

    private void UpdateLevelDisplay()
    {
        if (_levelText == null) return;

        int level = LevelProgress.GetActiveLevelNumber();
        if (level != _lastLevelNumber)
        {
            _lastLevelNumber = level;
            _levelText.text = "LEVEL " + level;
        }
    }
}
