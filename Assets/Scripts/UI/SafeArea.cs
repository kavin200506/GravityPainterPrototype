using UnityEngine;

/// <summary>
/// Attach to any panel RectTransform that should respect the device's safe area
/// (avoids iPhone notches, Android punch-hole cameras, rounded corners, home bar).
/// Works automatically — just add this component to your root UI panel.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform _panel;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        _panel = GetComponent<RectTransform>();
        Apply();
    }


    private void Update()
    {
        // Re-apply if resolution or safe area changes (e.g. rotation, split-screen)
        if (Screen.safeArea != _lastSafeArea || 
            Screen.width != _lastScreenSize.x || 
            Screen.height != _lastScreenSize.y)
        {
            Apply();
        }
    }

    private void Apply()
    {
        if (_panel == null)
        {
            _panel = GetComponent<RectTransform>();
            if (_panel == null) return;
        }

        Rect safeArea = Screen.safeArea;

        if (safeArea == _lastSafeArea && 
            Screen.width == _lastScreenSize.x && 
            Screen.height == _lastScreenSize.y)
            return;

        _lastSafeArea    = safeArea;
        _lastScreenSize  = new Vector2Int(Screen.width, Screen.height);

        // Convert safe area pixel rect to anchor coordinates (0–1)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _panel.anchorMin = anchorMin;
        _panel.anchorMax = anchorMax;
    }
}
