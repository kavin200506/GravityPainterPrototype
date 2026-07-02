using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    private Image[] _heartIcons;
    private TextMeshProUGUI _coinCountText;
    private int _lastLives = -1;
    private int _lastCoins = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level ") || LevelProgress.IsProceduralScene(scene))
        {
            LifeManager.ResetLives();
            CreateHUD();
        }
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

        UpdateFromState();
    }

    private void UpdateFromState()
    {
        UpdateLives(LifeManager.CurrentLives);
        UpdateCoins(CoinManager.SessionCoins);
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

    public void UpdateCoins(int count)
    {
        _lastCoins = count;
        if (_coinCountText != null)
            _coinCountText.text = count.ToString();
    }

    private void Update()
    {
        int lives = LifeManager.CurrentLives;
        if (lives != _lastLives)
            UpdateLives(lives);

        int coins = CoinManager.SessionCoins;
        if (coins != _lastCoins)
            UpdateCoins(coins);
    }
}
