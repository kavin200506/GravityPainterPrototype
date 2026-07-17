using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    public const string MusicVolumeKey = "MusicVolume";
    public const string SoundVolumeKey = "SoundVolume";

    [Header("UI References")]
    public Slider musicSlider;
    public Slider soundSlider;
    public Button closeButton;
    public MainMenu mainMenu;

    private void Awake()
    {
        if (musicSlider == null || soundSlider == null)
        {
            GenerateUI();
        }
    }

    private void OnEnable()
    {
        if (musicSlider != null)
        {
            musicSlider.value = GetMusicVolume();
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (soundSlider != null)
        {
            soundSlider.value = GetSoundVolume();
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
        }

        // Disable SafeArea on parent if present to ensure truly full screen
        SafeArea safeArea = GetComponentInParent<SafeArea>();
        if (safeArea != null)
        {
            safeArea.enabled = false;
            RectTransform saRect = safeArea.GetComponent<RectTransform>();
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.offsetMin = Vector2.zero;
            saRect.offsetMax = Vector2.zero;
        }
    }

    private void GenerateUI()
    {
        // Clean slate: remove any pre-existing buttons (like "Back" or old layouts)
        foreach (Transform child in transform)
        {
            child.name = "Deleted";
            Destroy(child.gameObject);
        }

        // Dark Background
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image bgImage = GetComponent<Image>();
        if (bgImage == null) bgImage = gameObject.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.95f);

        // Title
        CreateText("Title", transform, "SETTINGS", 80, new Vector2(0f, 600f));

        // Music
        CreateText("MusicLabel", transform, "Music", 50, new Vector2(-200f, 200f));
        musicSlider = CreateSlider("MusicSlider", transform, new Vector2(150f, 200f));

        // Sound
        CreateText("SoundLabel", transform, "Sound", 50, new Vector2(-200f, 0f));
        soundSlider = CreateSlider("SoundSlider", transform, new Vector2(150f, 0f));

        // Close Button
        closeButton = CreateButton("CloseButton", transform, "Close", new Vector2(0f, -400f));

        if (mainMenu == null)
            mainMenu = Object.FindFirstObjectByType<MainMenu>();
    }

    private GameObject CreateText(string name, Transform parent, string text, float fontSize, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Destroy(existing.gameObject);

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

    private Slider CreateSlider(string name, Transform parent, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Destroy(existing.gameObject);

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
        fillAreaRt.sizeDelta = new Vector2(-20f, 0f);

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

    private Button CreateButton(string name, Transform parent, string text, Vector2 anchoredPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Destroy(existing.gameObject);

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

    private void OnDisable()
    {
        PlayerPrefs.Save();

        // Re-enable SafeArea when closing this menu so other menus get safe area
        SafeArea safeArea = GetComponentInParent<SafeArea>();
        if (safeArea != null)
        {
            safeArea.enabled = true;
        }
    }

    public void OnMusicVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, val);
        GameplayMusicController.NotifySettingsChanged();
    }

    public void OnSoundVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(SoundVolumeKey, val);
    }

    private void CloseSettings()
    {
        if (mainMenu != null)
        {
            mainMenu.CloseSettings();
        }
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
    }

    public static float GetSoundVolume()
    {
        return PlayerPrefs.GetFloat(SoundVolumeKey, 0.5f);
    }
}
