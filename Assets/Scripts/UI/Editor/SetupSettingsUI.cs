using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class SetupSettingsUI
{
    [MenuItem("Tools/Setup Settings UI")]
    public static void Setup()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        Transform settingsPanelTransform = canvas.transform.Find("SettingsPanel");
        GameObject settingsPanel;
        if (settingsPanelTransform == null)
        {
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(canvas.transform, false);
        }
        else
        {
            settingsPanel = settingsPanelTransform.gameObject;
        }

        RectTransform rect = settingsPanel.GetComponent<RectTransform>();
        if (rect == null) rect = settingsPanel.AddComponent<RectTransform>();
        
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Image bgImage = settingsPanel.GetComponent<Image>();
        if (bgImage == null) bgImage = settingsPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.95f);

        SettingsUI settingsUI = settingsPanel.GetComponent<SettingsUI>();
        if (settingsUI == null) settingsUI = settingsPanel.AddComponent<SettingsUI>();

        GameObject titleObj = CreateText("Title", settingsPanel.transform, "SETTINGS", 80, new Vector2(0f, 600f));
        titleObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        titleObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        CreateText("MusicLabel", settingsPanel.transform, "Music", 50, new Vector2(-200f, 200f));
        Slider musicSlider = CreateSlider("MusicSlider", settingsPanel.transform, new Vector2(150f, 200f));
        settingsUI.musicSlider = musicSlider;

        CreateText("SoundLabel", settingsPanel.transform, "Sound", 50, new Vector2(-200f, 0f));
        Slider soundSlider = CreateSlider("SoundSlider", settingsPanel.transform, new Vector2(150f, 0f));
        settingsUI.soundSlider = soundSlider;

        Button closeButton = CreateButton("CloseButton", settingsPanel.transform, "Close", new Vector2(0f, -400f));
        settingsUI.closeButton = closeButton;

        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>();
        settingsUI.mainMenu = mainMenu;

        EditorUtility.SetDirty(settingsPanel);
        Debug.Log("Settings UI Setup Complete!");
    }

    private static GameObject CreateText(string name, Transform parent, string text, float fontSize, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 100f);
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 40f);
        rt.anchoredPosition = anchoredPos;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.25f);
        bgRt.anchorMax = new Vector2(1, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1, 0.75f);
        fillAreaRt.sizeDelta = new Vector2(-20f, 0f); // padding

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.1f, 0.8f, 0.1f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0, 0);
        handleAreaRt.anchorMax = new Vector2(1, 1);
        handleAreaRt.sizeDelta = new Vector2(-40f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(40f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        return slider;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 100f);
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.8f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        CreateText("Text", go.transform, text, 50, Vector2.zero);

        return btn;
    }
}
