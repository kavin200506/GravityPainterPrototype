using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Creates the same level-complete overlay used in campaign levels (Level 1/2).
/// </summary>
public static class LevelCompleteCanvasFactory
{
    public const string CanvasObjectName = "LevelCompleteCanvas";
    private const string BackgroundResource = "UI/LevelCompleteUI/complete";
    private const string BackgroundResourceFallback = "UI/LevelCompleteUI/Level_Completed";
    private const string BackgroundAssetPath = "Assets/Art/Sprites/UI/Level_Complete/complete.png";
    private const string BackgroundAssetPathFallback = "Assets/Resources/UI/LevelCompleteUI/Level_Completed.png";

    public static GameObject EnsureCanvas(ProceduralLevelBuilder builder)
    {
        GameObject existing = GameObject.Find(CanvasObjectName);
        if (existing != null)
        {
            WireProceduralBuilder(existing, builder);
            existing.SetActive(false);
            return existing;
        }

        EnsureEventSystem();
        GameObject canvasObject = CreateCanvasHierarchy();
        WireProceduralBuilder(canvasObject, builder);
        canvasObject.SetActive(false);
        return canvasObject;
    }

    private static void WireProceduralBuilder(GameObject canvasObject, ProceduralLevelBuilder builder)
    {
        LevelCompleteUI ui = canvasObject.GetComponent<LevelCompleteUI>();
        if (ui == null)
        {
            ui = canvasObject.AddComponent<LevelCompleteUI>();
        }

        ui.ConfigureProcedural(builder);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject CreateCanvasHierarchy()
    {
        GameObject canvasObject = new GameObject(
            CanvasObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CreateBackgroundPanel(canvasRect);
        CreateTitleText(canvasRect);

        return canvasObject;
    }

    private static void CreateBackgroundPanel(RectTransform parent)
    {
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.raycastTarget = true;
        image.preserveAspect = false;
        image.type = Image.Type.Simple;

        image.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);

        Sprite background = LoadBackgroundSprite();
        if (background != null)
        {
            image.sprite = background;
            image.color = Color.white;
            Debug.Log("[LevelCompleteCanvasFactory] Background panel set with sprite: " + background.name);
        }
        else
        {
            Debug.LogWarning("[LevelCompleteCanvasFactory] No background sprite — using dark fallback color");
        }
    }

    private static void CreateTitleText(RectTransform parent)
    {
        GameObject title = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        title.transform.SetParent(parent, false);

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 140f);
        rect.anchoredPosition = new Vector2(0f, 658f);

        TextMeshProUGUI text = title.GetComponent<TextMeshProUGUI>();
        text.text = "Level Completed";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 72f;
        text.color = new Color(1f, 0.84f, 0f, 1f);
        text.raycastTarget = true;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            text.font = font;
        }
    }

    public static Sprite LoadBackgroundSprite()
    {
        Debug.Log("[LevelCompleteCanvasFactory] Loading background from: " + BackgroundResource);
        Sprite sprite = Resources.Load<Sprite>(BackgroundResource);
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(BackgroundResourceFallback);
        }

        if (sprite != null)
        {
            Debug.Log("[LevelCompleteCanvasFactory] Background sprite loaded successfully: " + sprite.name
                + " size=" + sprite.rect.width + "x" + sprite.rect.height);
            return sprite;
        }

        Debug.LogWarning("[LevelCompleteCanvasFactory] Resources.Load<Sprite> returned null. Trying Texture2D...");
        Texture2D texture = Resources.Load<Texture2D>(BackgroundResource);
        if (texture == null)
        {
            texture = Resources.Load<Texture2D>(BackgroundResourceFallback);
        }

        if (texture != null)
        {
            Debug.Log("[LevelCompleteCanvasFactory] Texture2D loaded: " + texture.name
                + " size=" + texture.width + "x" + texture.height);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

#if UNITY_EDITOR
        Debug.Log("[LevelCompleteCanvasFactory] Trying editor asset path: " + BackgroundAssetPath);
        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPath);
        if (sprite == null)
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPathFallback);
        }

        if (sprite == null)
        {
            Texture2D editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundAssetPath);
            if (editorTex == null)
            {
                editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundAssetPathFallback);
            }
            if (editorTex != null)
            {
                sprite = Sprite.Create(
                    editorTex,
                    new Rect(0f, 0f, editorTex.width, editorTex.height),
                    new Vector2(0.5f, 0.5f));
            }
        }

        if (sprite != null)
        {
            Debug.Log("[LevelCompleteCanvasFactory] Editor sprite loaded: " + sprite.name);
            return sprite;
        }
#endif

        Debug.LogWarning("[LevelCompleteCanvasFactory] FAILED to load background sprite! Checked: "
            + BackgroundResource + " and " + BackgroundAssetPath);
        return null;
    }
}
