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
        btnRect.anchoredPosition = new Vector2(0f, -50f);
        btnRect.sizeDelta = new Vector2(400f, 80f);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(1f, 0.3f, 0.3f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(RestartFromGameOver);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "Restart";
        btnText.fontSize = 50f;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

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
}
