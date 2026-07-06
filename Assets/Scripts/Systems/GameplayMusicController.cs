using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene object that plays gameplay background music only while the player is actively in a level.
/// Place one "GameplayMusic" object in each scene so it stays visible in the Hierarchy.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameplayMusicController : MonoBehaviour
{
    public const string ObjectName = "GameplayMusic";
    private const string MusicResourcePath = "Audio/Beyond_the_Golden_Ridge"; // Updated music

    private static GameplayMusicController _instance;

    [SerializeField] [Range(0f, 1f)] private float volume = 0.45f;
    [SerializeField] private AudioClip musicClip;

    private AudioSource _source;

    public static GameplayMusicController Instance => _instance;

    private void Reset()
    {
        ConfigureAudioSource(GetComponent<AudioSource>());
        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>(MusicResourcePath);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Keep music playing across scenes

        _source = GetComponent<AudioSource>();
        ConfigureAudioSource(_source);

        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>(MusicResourcePath);

        _source.clip = musicClip;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RefreshPlayback();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_instance == this)
            _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshPlayback();
    }

    public static void NotifyLevelCompleteOverlayVisible(bool visible)
    {
        // No longer pausing music on level complete
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = volume;
    }

    public static void NotifySettingsChanged()
    {
        if (_instance == null)
            return;

        _instance.RefreshPlayback();
    }

    private void RefreshPlayback()
    {
        if (_source == null || _source.clip == null)
            return;

        float globalVol = 1.0f;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        // Call reflection or just link directly since it's in the same assembly
        globalVol = SettingsUI.GetMusicVolume();
#endif

        _source.volume = volume * globalVol;

        if (!_source.isPlaying)
            _source.Play();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source != null)
            ConfigureAudioSource(_source);
    }
#endif
}
