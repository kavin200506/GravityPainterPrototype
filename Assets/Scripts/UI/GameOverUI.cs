using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    private static GameObject _canvas;

    private static Sprite LoadGameOverSprite(string fileName)
    {
        Sprite sp = Resources.Load<Sprite>("UI/Pause_Page/" + fileName);
#if UNITY_EDITOR
        if (sp == null)
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Sprites/UI/Pause_Page/" + fileName + ".png");
#endif
        return sp;
    }

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
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.offsetMin = new Vector2(0f, -83f);
        overlayRect.offsetMax = new Vector2(0f, -83f);
        overlayRect.localScale = new Vector3(1f, 0.3579548f, 1f);
        Image overlayImg = overlay.AddComponent<Image>();
        Sprite bgSprite = LoadGameOverSprite("Game_over");
        if (bgSprite != null)
        {
            overlayImg.sprite = bgSprite;
            overlayImg.type = Image.Type.Simple;
            overlayImg.preserveAspect = false;
            overlayImg.color = Color.white;
        }
        else
        {
            overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        }

        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-150f, -150f); // Shifted left to make room for Home
        btnRect.sizeDelta = new Vector2(250f, 250f); // Make it a large square for the circular icon
        Image btnImg = btnObj.AddComponent<Image>();
        
        Sprite restartSprite = LoadGameOverSprite("reply");
        if (restartSprite != null)
        {
            btnImg.sprite = restartSprite;
            btnImg.preserveAspect = true;
            btnImg.color = Color.white;
        }
        else
        {
            btnImg.color = new Color(1f, 0.3f, 0.3f, 1f);
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
        
        Sprite homeSprite = LoadGameOverSprite("home");
        if (homeSprite != null)
        {
            homeImg.sprite = homeSprite;
            homeImg.preserveAspect = true;
            homeImg.color = Color.white;
        }
        else
        {
            homeImg.color = new Color(0.3f, 0.3f, 1f, 1f);
        }
        
        Button homeBtn = homeObj.AddComponent<Button>();
        homeBtn.onClick.AddListener(GoHomeFromGameOver);
        // -----------------------

        if (PauseUI.Instance != null)
            PauseUI.Instance.Hide();

        if (GameHUD.Instance != null)
            GameHUD.Instance.SetGameOverMode(true);

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

        if (GameHUD.Instance != null)
            GameHUD.Instance.SetGameOverMode(false);

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
                if (PauseUI.Instance != null)
                    PauseUI.Instance.Show();
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
