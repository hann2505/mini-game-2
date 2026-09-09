using UnityEngine;
using TMPro;
using KinematicsGame.Combat;

namespace KinematicsGame.UI
{
    /// <summary>
    /// Displays the live score, combo badge, and persistent high score using TextMeshPro.
    /// Listens to ScoreManager events for reactive updates.
    /// Score display uses a spring-pop scale animation (no Tweening library dependency).
    /// </summary>
    [DisallowMultipleComponent]
    public class GameHUDController : MonoBehaviour
    {
        public const float ScorePopDuration = 0.25f;
        public const float ScorePopScale = 1.35f;

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreLabel;
        [SerializeField] private TextMeshProUGUI highScoreLabel;

        [Header("Combo Display")]
        [SerializeField] private TextMeshProUGUI comboLabel;
        [SerializeField] private GameObject comboBadgeRoot;

        [Header("Score Pop Animation")]
        [SerializeField] private float scorePopDuration = ScorePopDuration;
        [SerializeField] private float scorePopScale = ScorePopScale;

        [Header("Combo Color Tiers")]
        [SerializeField] private Color comboBadgeColor1 = Color.white;
        [SerializeField] private Color comboBadgeColor2 = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color comboBadgeColor3 = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color comboBadgeColor4Plus = new Color(1f, 0.2f, 0.2f);

        // Spring-pop animation state
        private float scorePopElapsed = 0f;
        private bool isScorePopping = false;
        private Vector3 scoreLabelBaseScale = Vector3.one;

        private int displayedScore = 0;
        private int displayedHighScore = 0;
        private int displayedCombo = 1;

        public bool IsScorePopping => isScorePopping;
        public TextMeshProUGUI ScoreLabel => scoreLabel;
        public TextMeshProUGUI HighScoreLabel => highScoreLabel;
        public TextMeshProUGUI ComboLabel => comboLabel;

        private void Awake()
        {
            if (scoreLabel != null) scoreLabelBaseScale = scoreLabel.transform.localScale;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        public void SubscribeEvents()
        {
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
            ScoreManager.OnScoreChanged += HandleScoreChanged;

            ScoreManager.OnComboChanged -= HandleComboChanged;
            ScoreManager.OnComboChanged += HandleComboChanged;

            ScoreManager.OnHighScoreChanged -= HandleHighScoreChanged;
            ScoreManager.OnHighScoreChanged += HandleHighScoreChanged;
        }

        public void UnsubscribeEvents()
        {
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
            ScoreManager.OnComboChanged -= HandleComboChanged;
            ScoreManager.OnHighScoreChanged -= HandleHighScoreChanged;
        }

        private void Start()
        {
            // Initialize display from ScoreManager if already active
            if (ScoreManager.Instance != null)
            {
                UpdateScoreDisplay(ScoreManager.Instance.CurrentScore, animate: false);
                UpdateHighScoreDisplay(ScoreManager.Instance.HighScore);
                UpdateComboDisplay(ScoreManager.Instance.ComboMultiplier, 0f);
            }
            else
            {
                UpdateScoreDisplay(0, animate: false);
                UpdateHighScoreDisplay(0);
                UpdateComboDisplay(1, 0f);
            }
        }

        private void Update()
        {
            TickScorePopAnimation(Time.deltaTime);
        }

        /// <summary>
        /// Drives the score label spring-pop scale animation. Public for EditMode test stepping.
        /// </summary>
        public void TickScorePopAnimation(float deltaTime)
        {
            if (!isScorePopping) return;

            scorePopElapsed += deltaTime;
            float t = Mathf.Clamp01(scorePopElapsed / scorePopDuration);

            // Sin-curve overshoot: peak at midpoint, back to 1 at end
            float scale = 1f + (scorePopScale - 1f) * Mathf.Sin(t * Mathf.PI);
            if (scoreLabel != null)
            {
                scoreLabel.transform.localScale = scoreLabelBaseScale * scale;
            }

            if (t >= 1f)
            {
                isScorePopping = false;
                if (scoreLabel != null) scoreLabel.transform.localScale = scoreLabelBaseScale;
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            UpdateScoreDisplay(newScore, animate: true);
        }

        private void HandleComboChanged(int multiplier, float timerRatio)
        {
            UpdateComboDisplay(multiplier, timerRatio);
        }

        private void HandleHighScoreChanged(int newHighScore)
        {
            UpdateHighScoreDisplay(newHighScore);
        }

        public void UpdateScoreDisplay(int score, bool animate = false)
        {
            displayedScore = score;
            if (scoreLabel != null)
            {
                scoreLabel.text = score.ToString("N0");
            }

            if (animate)
            {
                TriggerScorePop();
            }
        }

        public void UpdateHighScoreDisplay(int highScore)
        {
            displayedHighScore = highScore;
            if (highScoreLabel != null)
            {
                highScoreLabel.text = $"Best: {highScore:N0}";
            }
        }

        public void UpdateComboDisplay(int multiplier, float timerRatio)
        {
            displayedCombo = multiplier;

            bool showBadge = multiplier > 1;

            if (comboBadgeRoot != null)
            {
                comboBadgeRoot.SetActive(showBadge);
            }

            if (comboLabel != null)
            {
                comboLabel.gameObject.SetActive(showBadge);
                if (showBadge)
                {
                    comboLabel.text = $"x{multiplier} COMBO";
                    comboLabel.color = GetComboColor(multiplier);
                }
            }
        }

        private void TriggerScorePop()
        {
            scorePopElapsed = 0f;
            isScorePopping = true;
        }

        private Color GetComboColor(int combo)
        {
            if (combo >= 4) return comboBadgeColor4Plus;
            if (combo == 3) return comboBadgeColor3;
            if (combo == 2) return comboBadgeColor2;
            return comboBadgeColor1;
        }

        /// <summary>
        /// Sets direct label references programmatically (used in tests and SceneSetupHelper).
        /// </summary>
        public void SetLabels(TextMeshProUGUI scoreLbl, TextMeshProUGUI highScoreLbl, TextMeshProUGUI comboLbl)
        {
            scoreLabel = scoreLbl;
            highScoreLabel = highScoreLbl;
            comboLabel = comboLbl;
            if (scoreLabel != null) scoreLabelBaseScale = scoreLabel.transform.localScale;
        }

        public int DisplayedScore => displayedScore;
        public int DisplayedHighScore => displayedHighScore;
        public int DisplayedCombo => displayedCombo;
    }
}
