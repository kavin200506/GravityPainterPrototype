using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GravityPainter.UI
{
    public class GameLoader : MonoBehaviour
    {
        [Header("Scene to Load")]
        [SerializeField] private string nextSceneName = "MainMenu";

        [Header("Loading Settings")]
        [SerializeField] private float minimumLoadingTime = 2.0f;
        [SerializeField] private float fadeDuration = 0.5f;

        private AsyncOperation _asyncOperation;
        private TextMeshProUGUI _progressText;
        private CanvasGroup _overlayGroup;

        private void Start()
        {
            CreateOverlayUI();
            StartCoroutine(LoadSceneAsync());
        }

        private Image _progressLine;

        private void CreateOverlayUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Create overlay container
            GameObject overlayObj = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayObj.transform.SetParent(canvas.transform, false);

            _overlayGroup = overlayObj.GetComponent<CanvasGroup>();

            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Create progress line at bottom
            GameObject lineObj = new GameObject("ProgressLine", typeof(RectTransform));
            lineObj.transform.SetParent(overlayObj.transform, false);

            _progressLine = lineObj.AddComponent<Image>();
            _progressLine.color = Color.white;

            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0.03f);
            lineRect.anchorMax = new Vector2(0f, 0.03f);
            lineRect.pivot = new Vector2(0f, 0.5f);
            lineRect.sizeDelta = new Vector2(0f, 4f);

            // Create percentage text above the line
            GameObject textObj = new GameObject("ProgressText", typeof(RectTransform));
            textObj.transform.SetParent(overlayObj.transform, false);

            _progressText = textObj.AddComponent<TextMeshProUGUI>();
            _progressText.text = "0%";
            _progressText.alignment = TextAlignmentOptions.Bottom;
            _progressText.fontSize = 30;
            _progressText.color = new Color(1f, 1f, 1f, 0.85f);
            _progressText.fontStyle = FontStyles.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.03f);
            textRect.anchorMax = new Vector2(1f, 0.09f);
            textRect.offsetMin = new Vector2(20f, 0f);
            textRect.offsetMax = new Vector2(-20f, 0f);
        }

        private IEnumerator LoadSceneAsync()
        {
            float elapsedTime = 0f;

            _asyncOperation = SceneManager.LoadSceneAsync(nextSceneName);
            _asyncOperation.allowSceneActivation = false;

            // Clear the level select flag so MainMenu starts on the main page
            PlayerPrefs.DeleteKey("OpenLevelSelect");
            PlayerPrefs.Save();

            // Get screen width for line scaling
            float screenWidth = 1080f;
            CanvasScaler scaler = FindFirstObjectByType<CanvasScaler>();
            if (scaler != null && scaler.referenceResolution.x > 0)
                screenWidth = scaler.referenceResolution.x;

            while (!_asyncOperation.isDone)
            {
                elapsedTime += Time.deltaTime;

                // Unity async progress stops at 0.9, map to 0-1
                float loadProgress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);

                // Enforce minimum loading time for visual polish
                float timeProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);

                // Display whichever is slower
                float displayProgress = Mathf.Min(loadProgress, timeProgress);

                if (_progressText != null)
                    _progressText.text = Mathf.RoundToInt(displayProgress * 100f) + "%";

                // Update progress line width
                if (_progressLine != null)
                {
                    RectTransform lineRect = _progressLine.rectTransform;
                    float maxWidth = screenWidth - 40f;
                    lineRect.sizeDelta = new Vector2(maxWidth * displayProgress, lineRect.sizeDelta.y);
                }

                // Ready to activate
                if (_asyncOperation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
                {
                    // Fade out overlay
                    yield return StartCoroutine(FadeOverlay());

                    _asyncOperation.allowSceneActivation = true;
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator FadeOverlay()
        {
            if (_overlayGroup == null) yield break;

            float counter = 0f;
            while (counter < fadeDuration)
            {
                counter += Time.deltaTime;
                _overlayGroup.alpha = Mathf.Lerp(1f, 0f, counter / fadeDuration);
                yield return null;
            }

            _overlayGroup.alpha = 0f;
        }
    }
}
