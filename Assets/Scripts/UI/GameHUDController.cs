using UnityEngine;
using TMPro;
using UnityEngine.UI;
using KinematicsGame.Combat;
using KinematicsGame.Player;

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
        public const float VitalBarWidth = 360f;
        public const float VitalBarHeight = 56f;
        public const float VitalFontSize = 30f;

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreLabel;
        [SerializeField] private TextMeshProUGUI highScoreLabel;

        [Header("Combo Display")]
        [SerializeField] private TextMeshProUGUI comboLabel;
        [SerializeField] private GameObject comboBadgeRoot;

        [Header("Typography & Sizing")]
        [SerializeField] private float scoreFontSize = 72f;
        [SerializeField] private float highScoreFontSize = 28f;
        [SerializeField] private float comboFontSize = 54f;

        [Header("Score Pop Animation")]
        [SerializeField] private float scorePopDuration = ScorePopDuration;
        [SerializeField] private float scorePopScale = ScorePopScale;

        [Header("Combo Color Tiers")]
        [SerializeField] private Color comboBadgeColor1 = Color.white;
        [SerializeField] private Color comboBadgeColor2 = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color comboBadgeColor3 = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color comboBadgeColor4Plus = new Color(1f, 0.2f, 0.2f);

        [Header("Player Vitals Display")]
        [SerializeField] private TextMeshProUGUI hpLabel;
        [SerializeField] private UnityEngine.UI.Slider hpSlider;
        [SerializeField] private TextMeshProUGUI armorLabel;
        [SerializeField] private Slider armorSlider;
        [SerializeField] private TextMeshProUGUI shieldLabel;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI goldLabel;
        [SerializeField] private TextMeshProUGUI diamondLabel;

        [Header("Combat & Cooldowns Display")]
        [SerializeField] private TextMeshProUGUI weaponLabel;
        [SerializeField] private TextMeshProUGUI cooldownLabel;

        private PlayerStats boundStats;
        private PlayerCombatSystem boundCombat;
        private PlayerDefenseSystem boundDefense;

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

        public TextMeshProUGUI HpLabel => hpLabel;
        public UnityEngine.UI.Slider HpSlider => hpSlider;
        public TextMeshProUGUI ArmorLabel => armorLabel;
        public Slider ArmorSlider => armorSlider;
        public TextMeshProUGUI ShieldLabel => shieldLabel;
        public TextMeshProUGUI GoldLabel => goldLabel;
        public TextMeshProUGUI DiamondLabel => diamondLabel;
        public TextMeshProUGUI WeaponLabel => weaponLabel;
        public TextMeshProUGUI CooldownLabel => cooldownLabel;

        public float ScoreFontSize
        {
            get => scoreFontSize;
            set
            {
                scoreFontSize = value;
                if (scoreLabel != null) scoreLabel.fontSize = value;
            }
        }

        public float HighScoreFontSize
        {
            get => highScoreFontSize;
            set
            {
                highScoreFontSize = value;
                if (highScoreLabel != null) highScoreLabel.fontSize = value;
            }
        }

        public float ComboFontSize
        {
            get => comboFontSize;
            set
            {
                comboFontSize = value;
                if (comboLabel != null) comboLabel.fontSize = value;
            }
        }

        public void ApplyFontSizes()
        {
            if (scoreLabel != null) scoreLabel.fontSize = scoreFontSize;
            if (highScoreLabel != null) highScoreLabel.fontSize = highScoreFontSize;
            if (comboLabel != null) comboLabel.fontSize = comboFontSize;
        }

        private void Awake()
        {
            if (scoreLabel != null) scoreLabelBaseScale = scoreLabel.transform.localScale;
            ApplyFontSizes();
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
            EnsureVitalBars();
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

            if (boundStats == null)
            {
                PlayerController player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    BindPlayer(player);
                }
            }
        }

        private void Update()
        {
            UpdateHUD(Time.deltaTime);
        }

        public void UpdateHUD(float deltaTime = 0f)
        {
            TickScorePopAnimation(deltaTime);

            if (weaponLabel != null && boundCombat != null)
            {
                if (boundCombat.IsTemporaryWeaponActive)
                {
                    weaponLabel.text = $"WEAPON: {boundCombat.CurrentWeapon} ({boundCombat.TemporaryWeaponTimeRemaining:F1}s)";
                }
            }
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
            ApplyFontSizes();
        }

        public void SetStatsLabels(
            TextMeshProUGUI hpLbl,
            TextMeshProUGUI armorLbl,
            TextMeshProUGUI shieldLbl,
            TextMeshProUGUI goldLbl,
            TextMeshProUGUI diamondLbl,
            TextMeshProUGUI weaponLbl,
            UnityEngine.UI.Slider slider = null,
            TextMeshProUGUI cooldownLbl = null)
        {
            hpLabel = hpLbl;
            armorLabel = armorLbl;
            shieldLabel = shieldLbl;
            goldLabel = goldLbl;
            diamondLabel = diamondLbl;
            weaponLabel = weaponLbl;
            hpSlider = slider;
            cooldownLabel = cooldownLbl;
            EnsureVitalBars();
        }

        /// <summary>Upgrades existing scene labels without requiring a scene rebuild.</summary>
        public void EnsureVitalBars()
        {
            ApplyReadableHudLayout();
            if (hpSlider == null) hpSlider = CreateVitalBar(hpLabel, "HealthBar", new Color(0.10f, 0.38f, 0.23f));
            if (armorSlider == null) armorSlider = CreateVitalBar(armorLabel, "ArmorBar", new Color(0.08f, 0.31f, 0.52f));
            ConfigureVitalBar(hpSlider);
            ConfigureVitalBar(armorSlider);
            HandleHealthChanged(boundStats != null ? boundStats.CurrentHealth : 100,
                boundStats != null ? boundStats.MaxHealth : 100);
            HandleArmorChanged(boundStats != null ? boundStats.CurrentArmor : 0,
                boundStats != null ? boundStats.MaxArmor : 50);
        }

        private static void ConfigureVitalBar(Slider slider)
        {
            if (slider == null) return;
            RectTransform rect = slider.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, VitalBarWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, VitalBarHeight);
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.minValue = 0;
            slider.wholeNumbers = true;
            foreach (Graphic graphic in slider.GetComponentsInChildren<Graphic>())
                graphic.raycastTarget = false;
        }

        private void ApplyReadableHudLayout()
        {
            ConfigureLabel(hpLabel, new Vector2(170f, -32f), new Vector2(VitalBarWidth, VitalBarHeight), VitalFontSize,
                TextAlignmentOptions.Center, true);
            ConfigureLabel(armorLabel, new Vector2(170f, -96f), new Vector2(VitalBarWidth, VitalBarHeight), VitalFontSize,
                TextAlignmentOptions.Center, true);
            ConfigureLabel(shieldLabel, new Vector2(170f, -158f), new Vector2(360f, 38f), 24f,
                TextAlignmentOptions.Left, true);
            ConfigureLabel(goldLabel, new Vector2(550f, -30f), new Vector2(180f, 40f), 27f,
                TextAlignmentOptions.Left, true);
            ConfigureLabel(diamondLabel, new Vector2(550f, -76f), new Vector2(180f, 40f), 27f,
                TextAlignmentOptions.Left, true);
            ConfigureLabel(weaponLabel, new Vector2(550f, -122f), new Vector2(250f, 40f), 25f,
                TextAlignmentOptions.Left, true);
            ConfigureLabel(cooldownLabel, new Vector2(550f, -166f), new Vector2(250f, 38f), 21f,
                TextAlignmentOptions.Left, false);
        }

        private static void ConfigureLabel(
            TextMeshProUGUI label,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment,
            bool bold)
        {
            if (label == null || label.GetComponentInParent<Canvas>() == null) return;
            RectTransform rect = label.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.alignment = alignment;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
        }

        private static Slider CreateVitalBar(TextMeshProUGUI label, string name, Color color)
        {
            if (label == null || label.GetComponentInParent<Canvas>() == null) return null;
            RectTransform source = label.rectTransform;
            Transform existing = source.parent.Find(name);
            if (existing != null && existing.TryGetComponent<Slider>(out var existingSlider))
                return existingSlider;
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            var rect = (RectTransform)root.transform;
            rect.SetParent(source.parent, false);
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = new Vector2(VitalBarWidth, VitalBarHeight);
            rect.SetSiblingIndex(source.GetSiblingIndex());
            root.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.96f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(rect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fill.GetComponent<Image>().color = color;
            var slider = root.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;

            label.color = Color.white;
            label.fontSize = VitalFontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            source.sizeDelta = new Vector2(VitalBarWidth, VitalBarHeight);
            return slider;
        }

        public void BindPlayer(PlayerController player)
        {
            if (player == null) return;
            BindStats(player.Stats, player.CombatSystem, player.DefenseSystem);
        }

        public void BindStats(PlayerStats stats, PlayerCombatSystem combat = null, PlayerDefenseSystem defense = null)
        {
            UnbindStats();

            boundStats = stats;
            boundCombat = combat;
            boundDefense = defense;

            if (boundStats != null)
            {
                boundStats.OnHealthChanged += HandleHealthChanged;
                boundStats.OnArmorChanged += HandleArmorChanged;
                boundStats.OnCurrencyChanged += HandleCurrencyChanged;

                HandleHealthChanged(boundStats.CurrentHealth, boundStats.MaxHealth);
                HandleArmorChanged(boundStats.CurrentArmor, boundStats.MaxArmor);
                HandleCurrencyChanged(boundStats.Gold, boundStats.Diamonds);
            }

            if (boundCombat != null)
            {
                boundCombat.OnWeaponChanged += HandleWeaponChanged;
                HandleWeaponChanged(boundCombat.CurrentWeapon);
            }

            if (boundDefense != null)
            {
                boundDefense.OnShieldStateChanged += HandleShieldStateChanged;
                boundDefense.OnDefenseCooldownsChanged += HandleDefenseCooldownsChanged;
                HandleShieldStateChanged(boundDefense.IsShieldActive, boundDefense.RemainingShieldHits);
                HandleDefenseCooldownsChanged(boundDefense.ShieldCooldownRemaining, boundDefense.EmpCooldownRemaining);
            }
        }

        public void UnbindStats()
        {
            if (boundStats != null)
            {
                boundStats.OnHealthChanged -= HandleHealthChanged;
                boundStats.OnArmorChanged -= HandleArmorChanged;
                boundStats.OnCurrencyChanged -= HandleCurrencyChanged;
                boundStats = null;
            }

            if (boundCombat != null)
            {
                boundCombat.OnWeaponChanged -= HandleWeaponChanged;
                boundCombat = null;
            }

            if (boundDefense != null)
            {
                boundDefense.OnShieldStateChanged -= HandleShieldStateChanged;
                boundDefense.OnDefenseCooldownsChanged -= HandleDefenseCooldownsChanged;
                boundDefense = null;
            }
        }

        public void HandleHealthChanged(int current, int max)
        {
            if (hpLabel != null)
            {
                hpLabel.text = $"HP: {current}/{max}";
            }
            if (hpSlider != null)
            {
                hpSlider.maxValue = Mathf.Max(1, max);
                hpSlider.SetValueWithoutNotify(current);
                if (hpSlider.fillRect != null && hpSlider.fillRect.TryGetComponent<Image>(out var fill))
                    fill.color = current <= max * 0.25f
                        ? new Color(0.62f, 0.12f, 0.15f)
                        : new Color(0.10f, 0.38f, 0.23f);
            }
        }

        public void HandleArmorChanged(int current, int max)
        {
            if (armorLabel != null)
            {
                armorLabel.text = $"ARMOR: {current}/{max}";
            }
            if (armorSlider != null)
            {
                armorSlider.maxValue = Mathf.Max(1, max);
                armorSlider.SetValueWithoutNotify(current);
            }
        }

        public void HandleShieldStateChanged(bool active, int hits)
        {
            if (shieldLabel != null)
            {
                shieldLabel.text = active ? $"SHIELD: ACTIVE ({hits})" : "SHIELD: READY";
            }
        }

        public void HandleCurrencyChanged(int gold, int diamonds)
        {
            if (goldLabel != null)
            {
                goldLabel.text = $"GOLD: {gold}";
            }
            if (diamondLabel != null)
            {
                diamondLabel.text = $"GEMS: {diamonds}";
            }
        }

        public void HandleWeaponChanged(WeaponType weapon)
        {
            if (weaponLabel != null)
            {
                weaponLabel.text = $"WEAPON: {weapon}";
            }
        }

        public void HandleDefenseCooldownsChanged(float shieldCd, float empCd)
        {
            if (cooldownLabel != null)
            {
                string sText = shieldCd > 0f ? $"S: {shieldCd:F1}s" : "S: READY";
                string eText = empCd > 0f ? $"EMP: {empCd:F1}s" : "EMP: READY";
                cooldownLabel.text = $"{sText} | {eText}";
            }
        }

        public int DisplayedScore => displayedScore;
        public int DisplayedHighScore => displayedHighScore;
        public int DisplayedCombo => displayedCombo;
    }
}
