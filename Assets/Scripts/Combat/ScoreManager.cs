using System;
using UnityEngine;
using KinematicsGame.Enemy;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Decoupled scoring manager that tracks score points, combo multipliers with a decay window,
    /// high score persistence via PlayerPrefs, and centralized 50ms audio throttling.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScoreManager : MonoBehaviour
    {
        public const string HighScoreKey = "KinematicsGame_HighScore";

        public static ScoreManager Instance { get; private set; }

        public static event Action<int> OnScoreChanged;
        public static event Action<int, float> OnComboChanged; // (multiplier, timerRatio)
        public static event Action<int> OnHighScoreChanged;

        [Header("Audio Configuration")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip comboClip;
        [SerializeField] private AudioClip highScoreClip;

        [Header("Combo Configuration")]
        [SerializeField] private float comboDuration = 2.0f;
        [SerializeField] private float audioThrottleWindow = 0.050f; // 50ms

        private int currentScore = 0;
        private int highScore = 0;
        private int comboMultiplier = 1;
        private float comboTimer = 0f;
        private float lastExplosionTime = -1f;
        private int explosionSoundPlayCount = 0;
        private bool hasPlayedHighScoreSoundInSession = false;
        private bool isInitialized = false;

        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public int ComboMultiplier => comboMultiplier;
        public float ComboTimer => comboTimer;
        public float ComboDuration => comboDuration;
        public int ExplosionSoundPlayCount => explosionSoundPlayCount;
        public float AudioThrottleWindow
        {
            get => audioThrottleWindow;
            set => audioThrottleWindow = value;
        }

        public AudioSource AudioSource
        {
            get => audioSource;
            set => audioSource = value;
        }

        public AudioClip HitClip
        {
            get => hitClip;
            set => hitClip = value;
        }

        public AudioClip ComboClip
        {
            get => comboClip;
            set => comboClip = value;
        }

        public AudioClip HighScoreClip
        {
            get => highScoreClip;
            set => highScoreClip = value;
        }

        public static void ClearEventSubscribers()
        {
            OnScoreChanged = null;
            OnComboChanged = null;
            OnHighScoreChanged = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            SaveHighScore();
        }

        public void SubscribeEvents()
        {
            TargetController.OnTargetHit -= HandleTargetHit;
            TargetController.OnTargetHit += HandleTargetHit;
        }

        public void UnsubscribeEvents()
        {
            TargetController.OnTargetHit -= HandleTargetHit;
        }

        private void OnApplicationQuit()
        {
            SaveHighScore();
        }

        /// <summary>
        /// Initializes score state, loads high score from PlayerPrefs cache, and configures audio.
        /// </summary>
        public void Initialize()
        {
            EnsureComponents();
            SubscribeEvents();
            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            currentScore = 0;
            comboMultiplier = 1;
            comboTimer = 0f;
            lastExplosionTime = -1f;
            explosionSoundPlayCount = 0;
            hasPlayedHighScoreSoundInSession = false;
            isInitialized = true;
        }

        public void EnsureComponents()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        private void Update()
        {
            UpdateCombo(Time.deltaTime);
        }

        /// <summary>
        /// Advances combo decay timer. Public for deterministic test verification.
        /// </summary>
        public void UpdateCombo(float deltaTime)
        {
            if (comboMultiplier > 1)
            {
                comboTimer -= deltaTime;
                if (comboTimer <= 0f)
                {
                    comboMultiplier = 1;
                    comboTimer = 0f;
                    OnComboChanged?.Invoke(1, 0f);
                }
                else
                {
                    float ratio = Mathf.Clamp01(comboTimer / comboDuration);
                    OnComboChanged?.Invoke(comboMultiplier, ratio);
                }
            }
        }

        private void HandleTargetHit(TargetController target, TargetProfile profile, Vector3 hitPosition)
        {
            ProcessHit(profile, hitPosition, Time.unscaledTime);
        }

        /// <summary>
        /// Processes a target hit, calculates score with combo multiplier, updates high score,
        /// and applies 50ms audio throttling.
        /// </summary>
        public void ProcessHit(TargetProfile profile, Vector3 hitPosition, float currentTime = -1f)
        {
            if (currentTime < 0f)
            {
                currentTime = Time.unscaledTime;
            }

            int basePoints = profile != null ? profile.PointValue : 10;
            int pointsAwarded = basePoints * comboMultiplier;
            currentScore += pointsAwarded;
            OnScoreChanged?.Invoke(currentScore);

            // Check high score
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt(HighScoreKey, highScore);
                OnHighScoreChanged?.Invoke(highScore);

                if (!hasPlayedHighScoreSoundInSession && highScoreClip != null && audioSource != null)
                {
                    hasPlayedHighScoreSoundInSession = true;
                    audioSource.PlayOneShot(highScoreClip);
                }
            }

            // Audio throttling gate (50ms)
            PlayExplosionSfx(currentTime);

            // Combo sound
            if (comboMultiplier > 1 && comboClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(comboClip);
            }

            // Advance combo streak
            comboMultiplier++;
            comboTimer = comboDuration;
            OnComboChanged?.Invoke(comboMultiplier, 1.0f);
        }

        /// <summary>
        /// Evaluates whether an explosion audio clip may be played according to the 50ms acoustic gate.
        /// </summary>
        public bool CanPlayExplosion(float currentTime)
        {
            if (currentTime - lastExplosionTime >= audioThrottleWindow || lastExplosionTime < 0f)
            {
                lastExplosionTime = currentTime;
                explosionSoundPlayCount++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Plays explosion SFX with explicit timestamp gate check.
        /// </summary>
        public void PlayExplosionSfx(float currentTime)
        {
            if (CanPlayExplosion(currentTime))
            {
                if (audioSource != null && hitClip != null)
                {
                    audioSource.PlayOneShot(hitClip);
                }
            }
        }

        public void PlayExplosionSfx()
        {
            PlayExplosionSfx(Time.unscaledTime);
        }

        /// <summary>
        /// Flushes high score to persistent disk storage.
        /// </summary>
        public void SaveHighScore()
        {
            if (PlayerPrefs.HasKey(HighScoreKey))
            {
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Resets current game score and combo for a fresh session.
        /// </summary>
        public void ResetScore()
        {
            currentScore = 0;
            comboMultiplier = 1;
            comboTimer = 0f;
            hasPlayedHighScoreSoundInSession = false;
            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(1, 0f);
        }

        /// <summary>
        /// Clears high score from memory and PlayerPrefs (used in tests and dev tools).
        /// </summary>
        public void ResetHighScore()
        {
            highScore = 0;
            PlayerPrefs.DeleteKey(HighScoreKey);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(0);
        }
    }
}
