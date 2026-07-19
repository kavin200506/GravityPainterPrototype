using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CoinDisplayUI : MonoBehaviour
{
    public Button Button { get; private set; }

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image shineImage;
    [SerializeField] private Image pillBgImage;

    private static Sprite _cachedGlowSprite;
    private static Sprite _cachedShineSprite;

    private int _lastDisplayedCoins = -1;
    private float _popTimer = 0f;
    private const float PopDuration = 0.25f;

    private Vector3 _initialCoinLocalPos;
    private RectTransform _coinIconRt;
    private RectTransform _textRt;
    private RectTransform _glowRt;

    private void Awake()
    {
        Button = GetComponent<Button>();
        SetupHierarchy();
    }

    private void OnEnable()
    {
        UpdateCoinDisplay();
    }

    private void SetupHierarchy()
    {
        // 0. Hide old metal box frame on root component and legacy children
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.enabled = false;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            string n = child.name;
            if (n != "PillBackground" && n != "Glow" && n != "CoinIcon" && n != "CoinText")
            {
                child.gameObject.SetActive(false);
            }
        }

        RectTransform rootRt = transform as RectTransform;
        if (rootRt != null)
        {
            rootRt.sizeDelta = new Vector2(230f, 68f);
        }

        // 1. Pill Background
        Transform pillT = transform.Find("PillBackground");
        if (pillT == null)
        {
            GameObject pillObj = new GameObject("PillBackground", typeof(RectTransform), typeof(Image));
            pillObj.transform.SetParent(transform, false);
            pillObj.transform.SetAsFirstSibling();
            pillT = pillObj.transform;
        }
        pillBgImage = pillT.GetComponent<Image>();
        RectTransform pillRt = pillT as RectTransform;
        pillRt.anchorMin = new Vector2(0f, 0f);
        pillRt.anchorMax = new Vector2(1f, 1f);
        pillRt.pivot = new Vector2(0.5f, 0.5f);
        pillRt.offsetMin = new Vector2(-147f, 0f);
        pillRt.offsetMax = new Vector2(3f, 0f);
        pillRt.localScale = new Vector3(0.4426213f, 0.54583f, 1f);
        pillRt.localRotation = Quaternion.identity;

        pillBgImage.sprite = GetPillSprite();
        pillBgImage.type = Image.Type.Simple;
        pillBgImage.preserveAspect = false;
        pillBgImage.color = Color.white;
        pillBgImage.raycastTarget = false;

        // 2. Cyan Glow behind coin
        Transform glowT = transform.Find("Glow");
        if (glowT == null)
        {
            GameObject glowObj = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(transform, false);
            glowT = glowObj.transform;
        }
        glowImage = glowT.GetComponent<Image>();
        glowImage.sprite = GetSoftGlowSprite();
        glowImage.color = new Color(0.2f, 0.9f, 1f, 0.75f); // Vibrant cyan #38E8FF
        glowImage.raycastTarget = false;
        _glowRt = glowT as RectTransform;
        _glowRt.anchorMin = new Vector2(0f, 0.5f);
        _glowRt.anchorMax = new Vector2(0f, 0.5f);
        _glowRt.pivot = new Vector2(0.5f, 0.5f);
        _glowRt.anchoredPosition = new Vector2(34f, 0f);
        _glowRt.sizeDelta = new Vector2(95f, 95f);

        // 3. Coin Icon
        Transform coinT = transform.Find("CoinIcon");
        if (coinT == null)
        {
            GameObject coinObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinObj.transform.SetParent(transform, false);
            coinT = coinObj.transform;
        }
        coinIcon = coinT.GetComponent<Image>();
        Sprite cSprite = Resources.Load<Sprite>("UI/Store_Page/coin_icon_32");
        if (cSprite == null)
            cSprite = Resources.Load<Sprite>("UI/coin_icon");
#if UNITY_EDITOR
        if (cSprite == null)
            cSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Store_Page/coin_icon_32.png");
#endif
        if (cSprite != null)
        {
            coinIcon.sprite = cSprite;
        }
        coinIcon.preserveAspect = true;
        coinIcon.raycastTarget = false;
        _coinIconRt = coinT as RectTransform;
        _coinIconRt.anchorMin = new Vector2(0f, 0.5f);
        _coinIconRt.anchorMax = new Vector2(0f, 0.5f);
        _coinIconRt.pivot = new Vector2(0.5f, 0.5f);
        _coinIconRt.anchoredPosition = new Vector2(34f, 0f);
        _coinIconRt.sizeDelta = new Vector2(58f, 58f);
        _initialCoinLocalPos = _coinIconRt.anchoredPosition;

        // 4. Coin Shine (Specular accent on coin top-left)
        Transform shineT = coinT.Find("CoinShine");
        if (shineT == null)
        {
            GameObject shineObj = new GameObject("CoinShine", typeof(RectTransform), typeof(Image));
            shineObj.transform.SetParent(coinT, false);
            shineT = shineObj.transform;
        }
        shineImage = shineT.GetComponent<Image>();
        shineImage.sprite = GetShineSprite();
        shineImage.color = new Color(1f, 1f, 1f, 0.8f);
        shineImage.raycastTarget = false;
        RectTransform shineRt = shineT as RectTransform;
        shineRt.anchorMin = new Vector2(0.5f, 0.5f);
        shineRt.anchorMax = new Vector2(0.5f, 0.5f);
        shineRt.pivot = new Vector2(0.5f, 0.5f);
        shineRt.anchoredPosition = new Vector2(-12f, 12f);
        shineRt.sizeDelta = new Vector2(18f, 18f);

        // 5. Coin Text (TextMeshProUGUI)
        Transform textT = transform.Find("CoinText");
        if (textT == null)
        {
            GameObject textObj = new GameObject("CoinText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(transform, false);
            textT = textObj.transform;
        }
        coinText = textT.GetComponent<TextMeshProUGUI>();
        _textRt = textT as RectTransform;
        _textRt.anchorMin = new Vector2(0f, 0f);
        _textRt.anchorMax = new Vector2(1f, 1f);
        _textRt.pivot = new Vector2(0.5f, 0.5f);
        _textRt.offsetMin = new Vector2(17f, 0f);
        _textRt.offsetMax = new Vector2(-83f, 0f);
        _textRt.localScale = Vector3.one;
        _textRt.localRotation = Quaternion.identity;

        coinText.fontSize = 38f;
        coinText.fontStyle = FontStyles.Bold;
        coinText.color = Color.white;
        coinText.alignment = TextAlignmentOptions.MidlineLeft;
        coinText.outlineWidth = 0.2f;
        coinText.outlineColor = new Color32(0, 0, 0, 255);
        coinText.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            coinText.font = font;
        }
    }

    private void Update()
    {
        int currentCoins = CoinManager.GetTotalCoins();
        if (_lastDisplayedCoins != currentCoins)
        {
            if (_lastDisplayedCoins >= 0 && currentCoins > _lastDisplayedCoins)
            {
                TriggerPopEffect();
            }
            _lastDisplayedCoins = currentCoins;
            if (coinText != null)
            {
                coinText.text = currentCoins.ToString();
            }
        }

        AnimateJuice();
    }

    private void TriggerPopEffect()
    {
        _popTimer = PopDuration;
    }

    private void AnimateJuice()
    {
        float time = Time.unscaledTime;

        // 1. Glow pulse (scale 1.0 -> 1.15, alpha 0.65 -> 0.90)
        float glowWave = (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f;
        if (_glowRt != null && glowImage != null)
        {
            float scale = Mathf.Lerp(1.0f, 1.15f, glowWave);
            _glowRt.localScale = Vector3.one * scale;

            float alpha = Mathf.Lerp(0.65f, 0.90f, glowWave);
            if (_popTimer > 0f)
            {
                alpha = 1.0f;
            }
            glowImage.color = new Color(0.2f, 0.9f, 1f, alpha);
        }

        // 2. Floating coin (subtle Y bobbing: 0 -> 3.5 -> 0 over 1.5s)
        float floatWave = (Mathf.Sin(time * (Mathf.PI * 2f / 1.5f)) + 1f) * 0.5f;
        if (_coinIconRt != null)
        {
            float yOffset = floatWave * 3.5f;
            _coinIconRt.anchoredPosition = _initialCoinLocalPos + new Vector3(0f, yOffset, 0f);

            if (_popTimer > 0f)
            {
                float rotAngle = Mathf.Sin((PopDuration - _popTimer) / PopDuration * Mathf.PI * 2f) * 12f;
                _coinIconRt.localRotation = Quaternion.Euler(0f, 0f, rotAngle);
            }
            else
            {
                _coinIconRt.localRotation = Quaternion.identity;
            }
        }

        // 3. Shine pulse (alpha 0.4 -> 0.9)
        if (shineImage != null)
        {
            float shineWave = (Mathf.Sin(time * 2.5f) + 1f) * 0.5f;
            float shineAlpha = Mathf.Lerp(0.4f, 0.9f, shineWave);
            shineImage.color = new Color(1f, 1f, 1f, shineAlpha);
        }

        // 4. Text Pop effect (scale 1.25 -> 1.0)
        if (_textRt != null)
        {
            if (_popTimer > 0f)
            {
                _popTimer -= Time.unscaledDeltaTime;
                float progress = 1f - Mathf.Clamp01(_popTimer / PopDuration);
                float scale = Mathf.Lerp(1.25f, 1.0f, progress);
                _textRt.localScale = Vector3.one * scale;
            }
            else
            {
                _textRt.localScale = Vector3.one;
            }
        }
    }

    public void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            int currentCoins = CoinManager.GetTotalCoins();
            _lastDisplayedCoins = currentCoins;
            coinText.text = currentCoins.ToString();
        }
    }

    private static Sprite _cachedPillSprite;

    private static Sprite GetPillSprite()
    {
        if (_cachedPillSprite != null) return _cachedPillSprite;

        int w = 256;
        int h = 80;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float radius = h / 2f;
        Color fillDark = new Color(0.04f, 0.10f, 0.22f, 0.88f);
        Color fillLight = new Color(0.08f, 0.18f, 0.35f, 0.88f);
        Color borderBright = new Color(0.22f, 0.85f, 1.0f, 0.95f);
        Color borderGlow = new Color(0.0f, 0.65f, 1.0f, 0.45f);

        for (int y = 0; y < h; y++)
        {
            float verticalGradient = (float)y / h;
            Color currentFill = Color.Lerp(fillDark, fillLight, verticalGradient);

            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius);
                float cy = radius;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                if (dist <= radius)
                {
                    float distFromEdge = radius - dist;

                    if (distFromEdge < 2.5f)
                    {
                        float t = distFromEdge / 2.5f;
                        Color borderCol = Color.Lerp(borderBright, currentFill, 1f - t);
                        tex.SetPixel(x, y, borderCol);
                    }
                    else if (distFromEdge < 6.0f)
                    {
                        float t = (distFromEdge - 2.5f) / 3.5f;
                        Color glowCol = Color.Lerp(borderGlow, currentFill, t);
                        tex.SetPixel(x, y, glowCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, currentFill);
                    }
                }
                else if (dist <= radius + 3.0f)
                {
                    float outerDist = dist - radius;
                    float alpha = (1f - (outerDist / 3.0f)) * 0.6f;
                    Color outerCol = new Color(borderBright.r, borderBright.g, borderBright.b, alpha);
                    tex.SetPixel(x, y, outerCol);
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
        tex.Apply();
        _cachedPillSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _cachedPillSprite;
    }

    private static Sprite GetSoftGlowSprite()
    {
        if (_cachedGlowSprite != null) return _cachedGlowSprite;

        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color brightCyan = new Color(0.2f, 0.9f, 1.0f, 1.0f);
        float center = (res - 1) / 2f;
        float maxDist = center;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float normalized = Mathf.Clamp01(dist / maxDist);
                float alpha = Mathf.Pow(1f - normalized, 2.2f);
                tex.SetPixel(x, y, new Color(brightCyan.r, brightCyan.g, brightCyan.b, alpha));
            }
        }
        tex.Apply();
        _cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return _cachedGlowSprite;
    }

    private static Sprite GetShineSprite()
    {
        if (_cachedShineSprite != null) return _cachedShineSprite;

        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (res - 1) / 2f;
        float maxDist = center;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float normalized = Mathf.Clamp01(dist / maxDist);
                float alpha = Mathf.Pow(1f - normalized, 3f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        _cachedShineSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return _cachedShineSprite;
    }
}

