using System.Collections;
using System.Collections.Generic;
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

    // Animated Visual Elements
    private readonly List<RectTransform> _musicBars = new List<RectTransform>();
    private readonly List<RectTransform> _soundBars = new List<RectTransform>();
    private TextMeshProUGUI _musicPercentText;
    private TextMeshProUGUI _soundPercentText;
    private Image _musicFillImg;
    private Image _soundFillImg;
    private RectTransform _popUpCardRt;

    private void Awake()
    {
        EnsureUI();
    }

    private void OnEnable()
    {
        EnsureUI();

        float musicVol = GetMusicVolume();
        float soundVol = GetSoundVolume();

        if (musicSlider != null)
        {
            musicSlider.value = musicVol;
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            UpdateMusicDisplay(musicVol);
        }

        if (soundSlider != null)
        {
            soundSlider.value = soundVol;
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            UpdateSoundDisplay(soundVol);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
        }

        // Animate PopUp Container Scale-in
        if (_popUpCardRt != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePopUpEntry());
        }

        // Disable SafeArea on parent if present
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

    private void Update()
    {
        // Animate Equalizer Bars in Real-time
        float musicVol = musicSlider != null ? musicSlider.value : 0f;
        float soundVol = soundSlider != null ? soundSlider.value : 0f;

        AnimateEqualizerBars(_musicBars, musicVol, 14f);
        AnimateEqualizerBars(_soundBars, soundVol, 20f);
    }

    private void AnimateEqualizerBars(List<RectTransform> bars, float volume, float speed)
    {
        for (int i = 0; i < bars.Count; i++)
        {
            if (bars[i] == null) continue;
            if (volume <= 0.01f)
            {
                bars[i].sizeDelta = new Vector2(bars[i].sizeDelta.x, 6f);
            }
            else
            {
                float phase = Time.time * speed + (i * 1.35f);
                float wave = (Mathf.Sin(phase) + 1f) * 0.5f; // 0..1
                float height = Mathf.Lerp(8f, 42f, wave * volume);
                bars[i].sizeDelta = new Vector2(bars[i].sizeDelta.x, height);
            }
        }
    }

    private IEnumerator AnimatePopUpEntry()
    {
        _popUpCardRt.localScale = Vector3.one * 0.7f;
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Elastic spring overshoot: 0 -> 1.06 -> 1.0
            float scale = Mathf.Sin(t * Mathf.PI * 0.55f) * 1.05f;
            if (t >= 0.85f) scale = Mathf.Lerp(1.05f, 1.0f, (t - 0.85f) / 0.15f);
            _popUpCardRt.localScale = Vector3.one * scale;
            yield return null;
        }

        _popUpCardRt.localScale = Vector3.one;
    }

    public void EnsureUI()
    {
        // Transparent Overlay Background
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image bgImage = GetComponent<Image>();
        if (bgImage == null) bgImage = gameObject.AddComponent<Image>();
        bgImage.sprite = null;
        // Transparent dark frosted backdrop overlay (0.45 alpha)
        bgImage.color = new Color(0f, 0.02f, 0.05f, 0.45f);
        bgImage.raycastTarget = true;

        Transform popUpTransform = transform.Find("PopUpCard");
        if (popUpTransform == null)
        {
            GenerateUI();
            return;
        }

        _popUpCardRt = popUpTransform.GetComponent<RectTransform>();
        if (musicSlider == null) musicSlider = popUpTransform.Find("MusicSection/MusicSlider")?.GetComponent<Slider>();
        if (soundSlider == null) soundSlider = popUpTransform.Find("SoundSection/SoundSlider")?.GetComponent<Slider>();
        if (closeButton == null) closeButton = popUpTransform.Find("CloseButton")?.GetComponent<Button>();

        CacheDynamicElements(popUpTransform);
    }

    private void GenerateUI()
    {
        // Clear old children if re-generating
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // PopUp Container Card (Glassmorphism Frame)
        GameObject popUpObj = new GameObject("PopUpCard", typeof(RectTransform), typeof(Image));
        popUpObj.transform.SetParent(transform, false);

        _popUpCardRt = popUpObj.GetComponent<RectTransform>();
        _popUpCardRt.anchorMin = new Vector2(0.5f, 0.5f);
        _popUpCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        _popUpCardRt.pivot = new Vector2(0.5f, 0.5f);
        _popUpCardRt.sizeDelta = new Vector2(760f, 960f);
        _popUpCardRt.anchoredPosition = Vector2.zero;

        Image popUpBg = popUpObj.GetComponent<Image>();
        popUpBg.color = new Color(0.06f, 0.1f, 0.18f, 0.88f); // Translucent Dark Glass
        popUpBg.raycastTarget = true;

        // Glowing Border Frame
        GameObject borderObj = new GameObject("BorderFrame", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(popUpObj.transform, false);
        RectTransform borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = Vector2.zero;
        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.2f, 0.85f, 1f, 0.35f); // Cyan Outer Border Glow
        borderImg.raycastTarget = false;

        // Header Title
        CreateText("Title", popUpObj.transform, "SETTINGS", 68f, new Vector2(0f, 380f), new Color(0.3f, 0.9f, 1f));

        // --- MUSIC SECTION ---
        GameObject musicGroup = new GameObject("MusicSection", typeof(RectTransform));
        musicGroup.transform.SetParent(popUpObj.transform, false);
        RectTransform musicGroupRt = musicGroup.GetComponent<RectTransform>();
        musicGroupRt.anchoredPosition = new Vector2(0f, 160f);
        musicGroupRt.sizeDelta = new Vector2(680f, 160f);

        CreateText("MusicLabel", musicGroup.transform, "MUSIC", 40f, new Vector2(-180f, 35f), Color.white, TextAlignmentOptions.Left);
        CreateEqualizerBars("MusicEqualizer", musicGroup.transform, new Vector2(-50f, 35f), _musicBars);
        _musicPercentText = CreateText("MusicPercent", musicGroup.transform, "100%", 36f, new Vector2(260f, -25f), new Color(0.3f, 0.9f, 1f), TextAlignmentOptions.Right).GetComponent<TextMeshProUGUI>();
        musicSlider = CreateSlider("MusicSlider", musicGroup.transform, new Vector2(-40f, -25f), out _musicFillImg);

        // --- SOUND SECTION ---
        GameObject soundGroup = new GameObject("SoundSection", typeof(RectTransform));
        soundGroup.transform.SetParent(popUpObj.transform, false);
        RectTransform soundGroupRt = soundGroup.GetComponent<RectTransform>();
        soundGroupRt.anchoredPosition = new Vector2(0f, -80f);
        soundGroupRt.sizeDelta = new Vector2(680f, 160f);

        CreateText("SoundLabel", soundGroup.transform, "SOUND", 40f, new Vector2(-180f, 35f), Color.white, TextAlignmentOptions.Left);
        CreateEqualizerBars("SoundEqualizer", soundGroup.transform, new Vector2(-50f, 35f), _soundBars);
        _soundPercentText = CreateText("SoundPercent", soundGroup.transform, "100%", 36f, new Vector2(260f, -25f), new Color(0.3f, 0.9f, 1f), TextAlignmentOptions.Right).GetComponent<TextMeshProUGUI>();
        soundSlider = CreateSlider("SoundSlider", soundGroup.transform, new Vector2(-40f, -25f), out _soundFillImg);

        // --- CLOSE BUTTON ---
        closeButton = CreateButton("CloseButton", popUpObj.transform, "CLOSE", new Vector2(0f, -340f));

        if (mainMenu == null)
            mainMenu = Object.FindFirstObjectByType<MainMenu>();
    }

    private void CacheDynamicElements(Transform popUpTransform)
    {
        _musicBars.Clear();
        _soundBars.Clear();

        Transform musicEq = popUpTransform.Find("MusicSection/MusicEqualizer");
        if (musicEq != null)
        {
            foreach (Transform bar in musicEq) _musicBars.Add(bar.GetComponent<RectTransform>());
        }

        Transform soundEq = popUpTransform.Find("SoundSection/SoundEqualizer");
        if (soundEq != null)
        {
            foreach (Transform bar in soundEq) _soundBars.Add(bar.GetComponent<RectTransform>());
        }

        Transform mPercent = popUpTransform.Find("MusicSection/MusicPercent");
        if (mPercent != null) _musicPercentText = mPercent.GetComponent<TextMeshProUGUI>();

        Transform sPercent = popUpTransform.Find("SoundSection/SoundPercent");
        if (sPercent != null) _soundPercentText = sPercent.GetComponent<TextMeshProUGUI>();

        Transform mFill = popUpTransform.Find("MusicSection/MusicSlider/Fill Area/Fill");
        if (mFill != null) _musicFillImg = mFill.GetComponent<Image>();

        Transform sFill = popUpTransform.Find("SoundSection/SoundSlider/Fill Area/Fill");
        if (sFill != null) _soundFillImg = sFill.GetComponent<Image>();
    }

    private void CreateEqualizerBars(string name, Transform parent, Vector2 anchoredPos, List<RectTransform> barList)
    {
        GameObject eqObj = new GameObject(name, typeof(RectTransform));
        eqObj.transform.SetParent(parent, false);
        RectTransform eqRt = eqObj.GetComponent<RectTransform>();
        eqRt.anchoredPosition = anchoredPos;
        eqRt.sizeDelta = new Vector2(100f, 50f);

        barList.Clear();
        float[] barPosX = { -30f, -10f, 10f, 30f };
        for (int i = 0; i < 4; i++)
        {
            GameObject barObj = new GameObject("Bar_" + i, typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(eqObj.transform, false);

            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0.5f);
            barRt.anchorMax = new Vector2(0.5f, 0.5f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.anchoredPosition = new Vector2(barPosX[i], 0f);
            barRt.sizeDelta = new Vector2(12f, 24f);

            Image barImg = barObj.GetComponent<Image>();
            // Neon Gradient Equalizer Colors
            barImg.color = Color.Lerp(new Color(0.2f, 0.85f, 1f), new Color(1f, 0.3f, 0.8f), i / 3f);
            barImg.raycastTarget = false;

            barList.Add(barRt);
        }
    }

    private GameObject CreateText(string name, Transform parent, string text, float fontSize, Vector2 anchoredPos, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 80f);
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;

        return go;
    }

    private Slider CreateSlider(string name, Transform parent, Vector2 anchoredPos, out Image fillImg)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(480f, 36f);
        rt.anchoredPosition = anchoredPos;

        // Background Track
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.2f);
        bgRt.anchorMax = new Vector2(1, 0.8f);
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.2f);
        fillAreaRt.anchorMax = new Vector2(1, 0.8f);
        fillAreaRt.sizeDelta = new Vector2(-20f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.sizeDelta = Vector2.zero;
        fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 1f, 1f); // Vibrant Cyan Neon

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.sizeDelta = new Vector2(-36f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(44f, 44f);
        Image handleImg = handle.GetComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = go.GetComponent<Slider>();
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
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340f, 90f);
        rt.anchoredPosition = anchoredPos;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.55f, 0.95f, 1f); // Vibrant Blue Button

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;

        ColorBlock cb = btn.colors;
        cb.normalColor = img.color;
        cb.highlightedColor = img.color * 1.25f;
        cb.pressedColor = img.color * 0.8f;
        btn.colors = cb;

        CreateText("Text", go.transform, text, 42f, Vector2.zero, Color.white);

        return btn;
    }

    private void UpdateMusicDisplay(float val)
    {
        int percent = Mathf.RoundToInt(val * 100f);
        if (_musicPercentText != null) _musicPercentText.text = percent + "%";
        if (_musicFillImg != null)
        {
            _musicFillImg.color = Color.Lerp(new Color(0.4f, 0.45f, 0.55f, 1f), new Color(0.2f, 0.85f, 1f, 1f), val);
        }
    }

    private void UpdateSoundDisplay(float val)
    {
        int percent = Mathf.RoundToInt(val * 100f);
        if (_soundPercentText != null) _soundPercentText.text = percent + "%";
        if (_soundFillImg != null)
        {
            _soundFillImg.color = Color.Lerp(new Color(0.4f, 0.45f, 0.55f, 1f), new Color(1f, 0.55f, 0.2f, 1f), val);
        }
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
        UpdateMusicDisplay(val);
        GameplayMusicController.NotifySettingsChanged();
    }

    public void OnSoundVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(SoundVolumeKey, val);
        UpdateSoundDisplay(val);
    }

    private void CloseSettings()
    {
        if (mainMenu != null)
        {
            mainMenu.CloseSettings();
        }
        else
        {
            gameObject.SetActive(false);
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
