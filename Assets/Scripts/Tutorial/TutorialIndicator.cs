using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A self-contained glow particle + floating label that marks
/// where the player should tap on a tile. Call ShowAt() / Hide().
/// </summary>
public class TutorialIndicator : MonoBehaviour
{
    // ── Tuning ─────────────────────────────────────────────────────────
    private const float HoverHeight      = 1.8f;   // world-units above tile surface
    private const float LabelHeight      = 2.6f;   // extra height for the text label
    private const float PulsePeriod      = 1.0f;   // seconds for one scale pulse
    private const float ParticleRate     = 20f;
    private const float ParticleLifetime = 0.9f;
    private const float ParticleSpeed    = 0.6f;
    private const float ParticleSize     = 0.18f;
    private const float RingRadius       = 0.28f;  // spawn ring radius

    // Label colours
    private static readonly Color LabelColor   = new Color(1f, 0.92f, 0.3f, 1f);  // gold
    private static readonly Color GlowColorA   = new Color(1f, 0.85f, 0.2f, 1f);  // gold warm
    private static readonly Color GlowColorB   = new Color(0.4f, 1f, 1f,  0f);   // teal → transparent

    // ── State ──────────────────────────────────────────────────────────
    private ParticleSystem _ps;
    private TextMeshPro    _label;
    private bool           _visible;
    private Coroutine      _pulseCoroutine;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        BuildParticleSystem();
        BuildLabel();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Shows the indicator at worldPos (tile centre / left / right)
    /// with the given hint message.
    /// </summary>
    public void ShowAt(Vector3 tileCenter, TapHint hint, string message)
    {
        Vector3 pos = tileCenter + Vector3.up * HoverHeight;
        transform.position = pos;
        _label.transform.position = tileCenter + Vector3.up * LabelHeight;
        _label.text = message;

        // Colour the label and particles by hint type
        Color particleColor = hint == TapHint.Forward
            ? new Color(0.3f, 1f, 0.5f)      // green = forward
            : hint == TapHint.Left
                ? new Color(0.3f, 0.7f, 1f)  // blue  = left
                : new Color(1f, 0.5f, 0.2f); // orange = right
        _label.color = particleColor;
        SetParticleColor(particleColor);

        if (!_visible)
        {
            gameObject.SetActive(true);
            _ps.Play();
            _visible = true;
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseScale());
        }
    }

    public void Hide()
    {
        if (!_visible) return;
        _visible = false;
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    // ── Build helpers ─────────────────────────────────────────────────

    private void BuildParticleSystem()
    {
        GameObject psGo = new GameObject("TutorialPS");
        psGo.transform.SetParent(transform, false);

        _ps = psGo.AddComponent<ParticleSystem>();
        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Use Unity's built-in Default-Particle material
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        // Main
        var main = _ps.main;
        main.loop            = true;
        main.duration        = 1f;
        main.startLifetime   = ParticleLifetime;
        main.startSpeed      = ParticleSpeed;
        main.startSize       = ParticleSize;
        main.startColor      = GlowColorA;
        main.gravityModifier = -0.15f;  // float upward slightly
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake     = false;

        // Emission
        var emission = _ps.emission;
        emission.rateOverTime = ParticleRate;

        // Shape: ring → taps converge in the centre visually
        var shape = _ps.shape;
        shape.enabled        = true;
        shape.shapeType      = ParticleSystemShapeType.Circle;
        shape.radius         = RingRadius;
        shape.radiusThickness = 0f;   // emit from edge only (ring)

        // Color over lifetime: warm gold → teal → fade out
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(GlowColorA, 0f),
                new GradientColorKey(GlowColorB, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Size over lifetime: grow then shrink
        var sizeOL = _ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f,   0.3f),
            new Keyframe(0.4f, 1.0f),
            new Keyframe(1f,   0.1f)
        );
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Noise: subtle wobble
        var noise = _ps.noise;
        noise.enabled   = true;
        noise.strength  = 0.1f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.4f;
        noise.quality   = ParticleSystemNoiseQuality.Low;
    }

    private void BuildLabel()
    {
        GameObject labelGo = new GameObject("TutorialLabel");
        labelGo.transform.SetParent(transform.parent ?? transform, false);

        _label = labelGo.AddComponent<TextMeshPro>();
        _label.fontSize        = 3.5f;
        _label.alignment       = TextAlignmentOptions.Center;
        _label.color           = LabelColor;
        _label.fontStyle       = FontStyles.Bold;
        _label.enableWordWrapping = true;

        // Face the main camera always
        labelGo.AddComponent<FaceCameraY>();
    }

    private void SetParticleColor(Color c)
    {
        var main = _ps.main;
        main.startColor = c;

        var col = _ps.colorOverLifetime;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(c, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(c, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    private IEnumerator PulseScale()
    {
        while (true)
        {
            float t = 0f;
            while (t < PulsePeriod)
            {
                t += Time.deltaTime;
                float s = 1f + 0.2f * Mathf.Sin((t / PulsePeriod) * Mathf.PI * 2f);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
        }
    }

    private void OnDestroy()
    {
        if (_label != null)
            Destroy(_label.gameObject);
    }
}

/// <summary>Which tap zone this hint is for.</summary>
public enum TapHint { Forward, Left, Right }

/// <summary>Simple billboard component — rotates Y toward the main camera each frame.</summary>
public class FaceCameraY : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 dir = transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
