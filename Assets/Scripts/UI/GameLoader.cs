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
        [SerializeField] private Sprite loadingBgSprite;

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

            // Create background image
            GameObject bgObj = new GameObject("LoadingBackground", typeof(RectTransform));
            bgObj.transform.SetParent(overlayObj.transform, false);
            bgObj.transform.SetAsFirstSibling();

            Image bgImg = bgObj.AddComponent<Image>();
            if (loadingBgSprite != null)
            {
                bgImg.sprite = loadingBgSprite;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = Color.black;
            }

            RectTransform bgRect = bgImg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Create progress bar track at bottom
            GameObject trackObj = new GameObject("ProgressBarTrack", typeof(RectTransform));
            trackObj.transform.SetParent(overlayObj.transform, false);

            Image trackImg = trackObj.AddComponent<Image>();
            trackImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            RectTransform trackRect = trackObj.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0.06f);
            trackRect.anchorMax = new Vector2(0.5f, 0.06f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            
            // We'll set the actual width based on CanvasScaler later, for now set a default
            float defaultWidth = 800f;
            trackRect.sizeDelta = new Vector2(defaultWidth, 24f);

            // Create progress fill
            GameObject fillObj = new GameObject("ProgressBarFill", typeof(RectTransform));
            fillObj.transform.SetParent(trackObj.transform, false);

            _progressLine = fillObj.AddComponent<Image>();
            _progressLine.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 0f); // Height stretches to parent, width updated dynamically
            fillRect.anchoredPosition = Vector2.zero;

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
            textRect.anchorMin = new Vector2(0f, 0.08f);
            textRect.anchorMax = new Vector2(1f, 0.14f);
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

            // Get screen width for track scaling
            float screenWidth = 1080f;
            CanvasScaler scaler = FindFirstObjectByType<CanvasScaler>();
            if (scaler != null && scaler.referenceResolution.x > 0)
                screenWidth = scaler.referenceResolution.x;

            float barWidth = screenWidth * 0.8f; // 80% of screen width
            if (_progressLine != null && _progressLine.transform.parent != null)
            {
                RectTransform trackRect = _progressLine.transform.parent.GetComponent<RectTransform>();
                if (trackRect != null) trackRect.sizeDelta = new Vector2(barWidth, trackRect.sizeDelta.y);
            }

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

                // Update progress fill width
                if (_progressLine != null)
                {
                    RectTransform fillRect = _progressLine.rectTransform;
                    fillRect.sizeDelta = new Vector2(barWidth * displayProgress, 0f);
                }

                // Ready to activate
                if (_asyncOperation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
                {
                    yield return StartCoroutine(PerformSmoothTransition());
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator PerformSmoothTransition()
        {
            // Create a persistent canvas for the fade transition
            GameObject transitionCanvasObj = new GameObject("TransitionCanvas");
            DontDestroyOnLoad(transitionCanvasObj);
            Canvas tCanvas = transitionCanvasObj.AddComponent<Canvas>();
            tCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tCanvas.sortingOrder = 999;
            
            GameObject bgObj = new GameObject("BlackBG");
            bgObj.transform.SetParent(transitionCanvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = Color.black;
            
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            
            CanvasGroup tGroup = transitionCanvasObj.AddComponent<CanvasGroup>();
            tGroup.alpha = 0f;

            // Fade to black (and fade out the progress bar simultaneously)
            float counter = 0f;
            while (counter < fadeDuration)
            {
                counter += Time.deltaTime;
                float t = counter / fadeDuration;
                if (_overlayGroup != null) _overlayGroup.alpha = Mathf.Lerp(1f, 0f, t);
                tGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            // Move the rest of the transition to a persistent component
            // so it doesn't get destroyed when this scene unloads.
            FadeTransition fader = transitionCanvasObj.AddComponent<FadeTransition>();
            fader.StartCoroutine(fader.DoFade(_asyncOperation, fadeDuration, tGroup));
        }
    public class FadeTransition : MonoBehaviour
    {
        public IEnumerator DoFade(AsyncOperation asyncOp, float duration, CanvasGroup group)
        {
            asyncOp.allowSceneActivation = true;
            
            while (!asyncOp.isDone)
            {
                yield return null;
            }
            
            yield return null;

            // Wait until MainMenuVideoBackground has rendered its first frame (or max 1.5s timeout)
            // so that BOTH the video and UI buttons appear simultaneously at the exact same instant!
            float maxWait = 1.5f;
            float waitTimer = 0f;
            MainMenuVideoBackground videoBg = FindFirstObjectByType<MainMenuVideoBackground>();
            if (videoBg != null)
            {
                while (!videoBg.IsFirstFrameReady && waitTimer < maxWait)
                {
                    waitTimer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            float counter = 0f;
            while (counter < duration)
            {
                counter += Time.unscaledDeltaTime;
                float t = counter / duration;
                group.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            
            Destroy(gameObject);
        }
    }
}
}

