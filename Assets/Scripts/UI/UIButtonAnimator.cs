using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Transform targetTransform;
    public float scaleFactor = 0.95f;
    public float animationSpeed = 20f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isInitialized = false;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        if (isInitialized) return;

        if (targetTransform == null)
            targetTransform = transform;

        originalScale = targetTransform.localScale;
        targetScale = originalScale;
        isInitialized = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Init();
        targetScale = originalScale * scaleFactor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Init();
        targetScale = originalScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Init();
        targetScale = originalScale;
    }

    void Update()
    {
        if (targetTransform != null && Application.isPlaying)
        {
            targetTransform.localScale = Vector3.Lerp(targetTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }
    }
}
