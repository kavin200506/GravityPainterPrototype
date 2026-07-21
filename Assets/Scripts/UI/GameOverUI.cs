using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    private static GameObject _canvas;

    public static void Show()
    {
        if (_canvas != null) return;

        Time.timeScale = 0f;

        GameObject canvasObj = new GameObject("GameOverCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent<GameOverUI>();

        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(canvasObj.transform, false);
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(canvasObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 100f);
        textRect.sizeDelta = new Vector2(600f, 100f);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Game Over";
        text.fontSize = 80f;
        text.color = Color.red;
        text.alignment = TextAlignmentOptions.Center;

        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-150f, -150f); // Shifted left to make room for Home
        btnRect.sizeDelta = new Vector2(250f, 250f); // Make it a large square for the circular icon
        Image btnImg = btnObj.AddComponent<Image>();
        
        Sprite restartSprite = Resources.Load<Sprite>("UI/restart_icon");
        if (restartSprite != null)
        {
            btnImg.sprite = restartSprite;
            btnImg.color = Color.white;
        }
        else
        {
            btnImg.color = new Color(1f, 0.3f, 0.3f, 1f); // Fallback color
        }
        
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(RestartFromGameOver);

        // --- Add Home Button ---
        GameObject homeObj = new GameObject("HomeButton");
        homeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform homeRect = homeObj.AddComponent<RectTransform>();
        homeRect.anchorMin = new Vector2(0.5f, 0.5f);
        homeRect.anchorMax = new Vector2(0.5f, 0.5f);
        homeRect.pivot = new Vector2(0.5f, 0.5f);
        homeRect.anchoredPosition = new Vector2(150f, -150f); // Shifted right
        homeRect.sizeDelta = new Vector2(250f, 250f); 
        Image homeImg = homeObj.AddComponent<Image>();
        
        Sprite homeSprite = Resources.Load<Sprite>("UI/HomeIcon");
        if (homeSprite != null)
        {
            homeImg.sprite = homeSprite;
            homeImg.color = Color.white;
        }
        else
        {
            homeImg.color = new Color(0.3f, 0.3f, 1f, 1f); // Fallback color
        }
        
        Button homeBtn = homeObj.AddComponent<Button>();
        homeBtn.onClick.AddListener(GoHomeFromGameOver);
        // -----------------------

        _canvas = canvasObj;
    }

    private static void RestartFromGameOver()
    {
        if (_canvas != null)
        {
            Destroy(_canvas);
            _canvas = null;
        }

        Time.timeScale = 1f;
        LifeManager.ResetLives();
        CoinManager.ResetSessionCoins();

        PowerUpManager pm = Object.FindFirstObjectByType<PowerUpManager>();
        if (pm != null)
            pm.ClearAllPowerUps();

        Scene active = SceneManager.GetActiveScene();
        if (LevelProgress.IsProceduralScene(active))
        {
            ProceduralLevelBuilder builder = Object.FindFirstObjectByType<ProceduralLevelBuilder>();
            if (builder != null)
            {
                builder.RebuildSameSeed();
                return;
            }
        }

        if (active.buildIndex >= 0)
            SceneManager.LoadScene(active.buildIndex);
        else
            SceneManager.LoadScene(active.name);
    }

    private static void GoHomeFromGameOver()
    {
        if (_canvas != null)
        {
            Destroy(_canvas);
            _canvas = null;
        }

        Time.timeScale = 1f;
        LifeManager.ResetLives();
        CoinManager.ResetSessionCoins();

        MainMenu.RequestOpenLevelSelect();
        SceneManager.LoadScene("MainMenu");
    }
}
