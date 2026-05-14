using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private const float RaycastDistance = 500f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            return;
        }

        if (!TryGetPrimaryPointerDown(out Vector2 inputPosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            RaycastDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
            {
                continue;
            }

            if (col.GetComponentInParent<BallController>() != null)
            {
                continue;
            }

            TileZone hitZone = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            TileZone tile = TileZone.GetPrimaryZone(hitZone != null ? hitZone.gameObject : null);
            if (tile != null)
            {
                tile.CycleZone();
                break;
            }
        }
    }

    /// <summary>
    /// Works with New Input System, legacy Input Manager, mouse and touch (including Android).
    /// </summary>
    private static bool TryGetPrimaryPointerDown(out Vector2 screenPosition)
    {
        screenPosition = default;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touch.primaryTouch.position.ReadValue();
            return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0)
        {
            UnityEngine.Touch t = Input.GetTouch(0);
            if (t.phase == UnityEngine.TouchPhase.Began)
            {
                screenPosition = t.position;
                return true;
            }
        }
#endif

        return false;
    }
}
