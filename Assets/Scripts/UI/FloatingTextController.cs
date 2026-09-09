using UnityEngine;
using TMPro;

namespace KinematicsGame.UI
{
    /// <summary>
    /// A single floating combat text instance that ascends and fades over a fixed duration.
    /// Driven by manual Update (no coroutines) for EditMode test compatibility.
    /// Calls OnComplete when the animation finishes, signalling the pool to reclaim it.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingTextController : MonoBehaviour
    {
        public const float DefaultDuration = 0.8f;
        public const float AscentSpeed = 1.5f;
        public const float PeakScaleMultiplier = 1.4f;
        public const float ScalePopDuration = 0.15f;

        [SerializeField] private TextMeshPro tmpText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fontSize = 3.5f;

        private float elapsed = 0f;
        private float duration = DefaultDuration;
        private bool isPlaying = false;
        private Vector3 baseScale;
        private System.Action onComplete;

        public bool IsPlaying => isPlaying;

        public float FontSize
        {
            get => fontSize;
            set
            {
                fontSize = value;
                if (tmpText != null) tmpText.fontSize = value;
            }
        }

        public TextMeshPro TmpText
        {
            get => tmpText;
            set => tmpText = value;
        }

        public static void ClearEventSubscribers() { }

        public void EnsureComponents()
        {
            if (tmpText == null)
            {
                tmpText = GetComponent<TextMeshPro>();
                if (tmpText == null)
                {
                    tmpText = gameObject.AddComponent<TextMeshPro>();
                }
            }

            if (tmpText != null)
            {
                tmpText.fontSize = fontSize;
                tmpText.alignment = TextAlignmentOptions.Center;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// Activates this FCT instance at a world position with the given text and color.
        /// </summary>
        public void Play(Vector3 worldPosition, string text, Color color, System.Action onComplete = null, float animDuration = DefaultDuration)
        {
            EnsureComponents();

            transform.position = worldPosition;
            gameObject.SetActive(true);

            if (tmpText != null)
            {
                tmpText.fontSize = fontSize;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.text = text;
                tmpText.color = color;
                if (tmpText.color.a < 1f)
                {
                    Color c = tmpText.color;
                    c.a = 1f;
                    tmpText.color = c;
                }
            }

            elapsed = 0f;
            duration = animDuration;
            baseScale = Vector3.one;
            transform.localScale = baseScale;
            isPlaying = true;
            this.onComplete = onComplete;
        }

        private void Update()
        {
            TickAnimation(Time.deltaTime);
        }

        /// <summary>
        /// Drives the ascend-and-fade animation. Public for deterministic test stepping.
        /// </summary>
        public void TickAnimation(float deltaTime)
        {
            if (!isPlaying) return;

            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ascend
            Vector3 pos = transform.position;
            pos.y += AscentSpeed * deltaTime;
            transform.position = pos;

            // Scale pop: quick overshoot at start then back to 1x
            if (elapsed < ScalePopDuration)
            {
                float popT = elapsed / ScalePopDuration;
                float scale = Mathf.Lerp(1f, PeakScaleMultiplier, Mathf.Sin(popT * Mathf.PI));
                transform.localScale = baseScale * scale;
            }
            else
            {
                transform.localScale = baseScale;
            }

            // Alpha fade-out in the second half of the animation
            float fadeStartT = 0.5f;
            if (t > fadeStartT)
            {
                float fadeProgress = (t - fadeStartT) / (1f - fadeStartT);
                float alpha = 1f - fadeProgress;

                if (tmpText != null)
                {
                    Color c = tmpText.color;
                    c.a = alpha;
                    tmpText.color = c;
                }
            }

            if (t >= 1f)
            {
                Finish();
            }
        }

        private void Finish()
        {
            isPlaying = false;
            gameObject.SetActive(false);
            transform.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Immediately stops and deactivates this instance (pool reclaim).
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            gameObject.SetActive(false);
            transform.localScale = Vector3.one;
        }
    }
}
