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

        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!inputDetected)
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

            TileZone tile = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            if (tile != null)
            {
                tile.CycleZone();
                break;
            }
        }
    }
}
