using UnityEngine;

/// <summary>
/// Attach this script to a Coin prefab with a Trigger Collider.
/// Handles the visual rotation and collection logic.
/// </summary>
[DefaultExecutionOrder(-40)]
public class Coin : MonoBehaviour
{
    private Transform _spinTarget;
    private CoinAppearanceProfile _profile;

    private void Awake()
    {
        Transform visualRoot = transform.Find(CoinVisual.VisualRootName);
        _spinTarget = visualRoot != null ? visualRoot : transform;

        CoinAppearance appearance = GetComponent<CoinAppearance>();
        _profile = appearance != null && appearance.Profile != null
            ? appearance.Profile
            : CoinAppearanceProfile.LoadOrDefault();
    }

    private void Update()
    {
        if (_profile == null)
        {
            _profile = CoinAppearanceProfile.LoadOrDefault();
        }

        float rotationSpeed = _profile != null ? _profile.rotationSpeed : 0f;
        if (rotationSpeed <= 0f || _spinTarget == null)
        {
            return;
        }

        _spinTarget.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    [SerializeField] private AudioClip collectSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<BallController>() != null)
        {
            if (collectSound != null)
            {
                PlayCollectSound();
            }

            CoinManager.AddSessionCoin();
            Destroy(gameObject);
        }
    }

    private void PlayCollectSound()
    {
        GameObject audioObj = new GameObject("CoinCollectSound");
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = collectSound;
        audioSource.spatialBlend = 0f; // 2D Sound
        audioSource.volume = SettingsUI.GetSoundVolume();
        audioSource.Play();
        
        Destroy(audioObj, collectSound.length);
    }
}
