using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a looping video on a UI RawImage behind the main menu buttons.
/// Wire on the BackGround object: RawImage + VideoPlayer + this component.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(VideoPlayer))]
[ExecuteAlways]
public class MainMenuVideoBackground : MonoBehaviour
{
    [Header("Video Source")]
    [SerializeField] private VideoClip videoClip;

    [Header("Render Resolution")]
    [Tooltip("Use the clip's native width and height for the render texture.")]
    [SerializeField] private bool useSourceResolution = true;
    [SerializeField] private int renderWidth = 1080;
    [SerializeField] private int renderHeight = 1920;
    [Tooltip("Longest edge cap when using source resolution (keeps mobile memory reasonable).")]
    [SerializeField] private int maxRenderDimension = 1920;

    [Header("Playback")]
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool muteAudio = true;
    [Range(0.1f, 4f)]
    [SerializeField] private float playbackSpeed = 1f;

    [Header("Display")]
    [SerializeField] private bool invertVertical = true;
    [SerializeField] private bool invertHorizontal = false;
    [SerializeField] private VideoAspectRatio aspectRatio = VideoAspectRatio.FitVertically;

    [Header("UI")]
    [SerializeField] private bool blockRaycasts = false;
    [SerializeField] private Color tint = Color.white;

    private RawImage _rawImage;
    private VideoPlayer _player;
    private RenderTexture _renderTexture;

    public VideoClip VideoClip => videoClip;

    private void Reset()
    {
        _rawImage = GetComponent<RawImage>();
        _player = GetComponent<VideoPlayer>();
        if (_rawImage != null)
        {
            _rawImage.raycastTarget = false;
        }
    }

    private void Awake()
    {
        VideoPlayer player = GetComponent<VideoPlayer>();
        if (player != null)
        {
            player.Stop();
            player.enabled = false;
        }

        RawImage rawImage = GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.enabled = false;
        }

        Image image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }

        image.enabled = true;
        image.raycastTarget = false;
        image.color = Color.white;

        Sprite menuSprite = Resources.Load<Sprite>("UI/Mainmenu");
#if UNITY_EDITOR
        if (menuSprite == null)
        {
            menuSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Mainmenu.png");
        }
#endif
        if (menuSprite != null)
        {
            image.sprite = menuSprite;
        }
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureComponents();
        ApplySettings();
    }

    public void ApplySettings()
    {
        EnsureComponents();

        if (_rawImage == null || _player == null)
        {
            return;
        }

        StretchToParent();
        _rawImage.raycastTarget = blockRaycasts;
        _rawImage.color = tint;
        ApplyUvRect();

        if (videoClip == null)
        {
            _rawImage.texture = null;
            return;
        }

        int width = renderWidth;
        int height = renderHeight;
        if (useSourceResolution)
        {
            width = Mathf.Max(1, (int)videoClip.width);
            height = Mathf.Max(1, (int)videoClip.height);
            ScaleToMaxDimension(ref width, ref height, maxRenderDimension);
        }
        else
        {
            width = Mathf.Max(1, renderWidth);
            height = Mathf.Max(1, renderHeight);
        }

        EnsureRenderTexture(width, height);
        _rawImage.texture = _renderTexture;

        _player.playOnAwake = false;
        _player.isLooping = loop;
        _player.skipOnDrop = true;
        _player.waitForFirstFrame = true;
        _player.playbackSpeed = playbackSpeed;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.targetTexture = _renderTexture;
        _player.clip = videoClip;
        _player.aspectRatio = aspectRatio;

        if (muteAudio)
        {
            _player.audioOutputMode = VideoAudioOutputMode.None;
        }
        else
        {
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
        }
    }

    public bool IsFirstFrameReady { get; private set; }

    public void Play()
    {
        EnsureComponents();
        if (_player == null || videoClip == null)
        {
            IsFirstFrameReady = true;
            return;
        }

        ApplySettings();
        _player.sendFrameReadyEvents = true;
        _player.frameReady -= OnFrameReady;
        _player.frameReady += OnFrameReady;

        if (_player.isPrepared)
        {
            _player.Play();
            StartCoroutine(FallbackFrameReadyTimeout());
            return;
        }

        _player.prepareCompleted -= OnPrepared;
        _player.prepareCompleted += OnPrepared;
        _player.Prepare();

        StartCoroutine(FallbackFrameReadyTimeout());
    }

    public void Stop()
    {
        if (_player != null && _player.isPlaying)
        {
            _player.Stop();
        }
    }

    private void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        source.frameReady -= OnFrameReady;
        IsFirstFrameReady = true;
    }

    private void OnPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnPrepared;
        source.Play();
        StartCoroutine(MarkReadyNextFrame());
    }

    private System.Collections.IEnumerator MarkReadyNextFrame()
    {
        yield return null;
        IsFirstFrameReady = true;
    }

    private System.Collections.IEnumerator FallbackFrameReadyTimeout()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        IsFirstFrameReady = true;
    }

    private void EnsureComponents()
    {
        if (_rawImage == null)
        {
            _rawImage = GetComponent<RawImage>();
        }

        if (_rawImage == null)
        {
            _rawImage = gameObject.AddComponent<RawImage>();
        }

        if (_player == null)
        {
            _player = GetComponent<VideoPlayer>();
        }

        if (_player == null)
        {
            _player = gameObject.AddComponent<VideoPlayer>();
        }
    }

    private void StretchToParent()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        // 12px overscan on all sides pushes hardware video decoder edge artifacts safely past screen boundaries
        rect.offsetMin = new Vector2(-12f, -12f);
        rect.offsetMax = new Vector2(12f, 12f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        transform.SetAsFirstSibling();
    }

    [Header("Edge Trimming")]
    [Tooltip("Crop fraction to remove Android video decoder green edge line artifacts.")]
    [SerializeField] private float edgeCrop = 0.005f;

    private void ApplyUvRect()
    {
        if (_rawImage == null)
        {
            return;
        }

        float crop = Mathf.Clamp(edgeCrop, 0f, 0.05f);
        float uMin = crop;
        float uMax = 1f - crop;
        float vMin = crop;
        float vMax = 1f - crop;

        float x = invertHorizontal ? uMax : uMin;
        float y = invertVertical ? vMax : vMin;
        float w = invertHorizontal ? -(uMax - uMin) : (uMax - uMin);
        float h = invertVertical ? -(vMax - vMin) : (vMax - vMin);

        _rawImage.uvRect = new Rect(x, y, w, h);
    }

    private void EnsureRenderTexture(int width, int height)
    {
        if (_renderTexture != null
            && _renderTexture.width == width
            && _renderTexture.height == height)
        {
            return;
        }

        ReleaseRenderTexture();
        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _renderTexture.name = "MainMenuVideo_RT";
        _renderTexture.wrapMode = TextureWrapMode.Clamp;
        _renderTexture.filterMode = FilterMode.Bilinear;
        _renderTexture.Create();

        // Clear RenderTexture to solid black so it doesn't show transparent/uninitialized pixels while VideoPlayer prepares on Android
        RenderTexture activeRt = RenderTexture.active;
        RenderTexture.active = _renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = activeRt;
    }

    private void ReleaseRenderTexture()
    {
        if (_player != null)
        {
            _player.targetTexture = null;
        }

        if (_renderTexture == null)
        {
            return;
        }

        _renderTexture.Release();
        if (Application.isPlaying)
        {
            Destroy(_renderTexture);
        }
        else
        {
            DestroyImmediate(_renderTexture);
        }
        _renderTexture = null;
    }

    private static void ScaleToMaxDimension(ref int width, ref int height, int maxDimension)
    {
        int largest = Mathf.Max(width, height);
        if (largest <= maxDimension)
        {
            return;
        }

        float scale = maxDimension / (float)largest;
        width = Mathf.Max(1, Mathf.RoundToInt(width * scale));
        height = Mathf.Max(1, Mathf.RoundToInt(height * scale));
    }
}
