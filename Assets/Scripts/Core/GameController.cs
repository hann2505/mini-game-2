using System;
using UnityEngine;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;

namespace KinematicsGame.Core
{
    public enum GameOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Master orchestrator for the 2D Object Kinematics Game:
    /// - Manages entity lifecycle and spawning for Object A (Player), Object B (Target), and Object C (Projectile).
    /// - Dynamically forces exact visual size parity between Object A and Object B.
    /// - Configures and dynamically flips between Horizontal and Vertical kinematic orientations.
    /// - Scales background to encompass arbitrary camera aspect ratios with zero black bars.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }
        public static event Action<GameOrientation> OnOrientationChanged;

        [Header("Orientation Settings")]
        [SerializeField] private GameOrientation orientation = GameOrientation.Horizontal;

        [Header("Prefab References (Optional - Auto-creates if null)")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Scene References")]
        [SerializeField] private PlayerController playerInstance;
        [SerializeField] private TargetController targetInstance;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private BackgroundScroller backgroundScroller;
        [SerializeField] private TargetSpawner targetSpawner;
        [SerializeField] private ScoreManager scoreManager;

        [Header("Sizing & Kinematics Parameters")]
        [SerializeField] private float targetUniformSize = 1.5f;
        [SerializeField] private float playerMoveSpeed = 6f;
        [SerializeField] private float targetBaseSpeed = 3.5f;
        [SerializeField] private float targetWaveFrequency = 2f;
        [SerializeField] private float targetWaveAmplitude = 1.5f;
        [SerializeField] private float projectileSpeed = 12f;

        [Header("Audio Configuration")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip fireSfx;
        [SerializeField] private AudioClip explosionSfx;

        [SerializeField] private float musicVolume = 0.25f;

        [Header("Sprite Asset References (Optional Fallbacks)")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite targetSprite;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Sprite backgroundSprite;

        public Sprite PlayerSprite
        {
            get => playerSprite;
            set => playerSprite = value;
        }

        public Sprite TargetSprite
        {
            get => targetSprite;
            set => targetSprite = value;
        }

        public Sprite ProjectileSprite
        {
            get => projectileSprite;
            set => projectileSprite = value;
        }

        public Sprite BackgroundSprite
        {
            get => backgroundSprite;
            set => backgroundSprite = value;
        }

        public GameOrientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;
                ApplyOrientation(orientation);
                OnOrientationChanged?.Invoke(orientation);
            }
        }

        public PlayerController PlayerInstance
        {
            get => playerInstance;
            set => playerInstance = value;
        }

        public TargetController TargetInstance
        {
            get => targetInstance;
            set => targetInstance = value;
        }

        public SpriteRenderer BackgroundRenderer
        {
            get => backgroundRenderer;
            set => backgroundRenderer = value;
        }

        public BackgroundScroller BackgroundScroller
        {
            get => backgroundScroller;
            set => backgroundScroller = value;
        }

        public TargetSpawner TargetSpawner
        {
            get => targetSpawner;
            set => targetSpawner = value;
        }

        public ScoreManager ScoreManager
        {
            get => scoreManager;
            set => scoreManager = value;
        }

        public float TargetUniformSize
        {
            get => targetUniformSize;
            set => targetUniformSize = value;
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource != null) musicSource.volume = musicVolume;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureViewportManager();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private float lastViewportWidth;
        private float lastViewportHeight;

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (ViewportManager.Instance != null)
            {
                float w = ViewportManager.Instance.Width;
                float h = ViewportManager.Instance.Height;
                if (Mathf.Abs(w - lastViewportWidth) > 0.01f || Mathf.Abs(h - lastViewportHeight) > 0.01f)
                {
                    ScaleBackground();
                }
            }
        }

        private void EnsureViewportManager()
        {
            if (ViewportManager.Instance == null)
            {
                ViewportManager vm = FindFirstObjectByType<ViewportManager>();
                if (vm == null)
                {
                    vm = gameObject.AddComponent<ViewportManager>();
                }
                vm.Initialize();
            }
        }

        /// <summary>
        /// Bootstraps all game systems, entities, sizing parity, and orientation alignment.
        /// </summary>
        public void InitializeGame()
        {
            EnsureViewportManager();
            ViewportManager.Instance.UpdateBounds();

            ScaleBackground();
            SetupEntities();
            NormalizeEntitySizes();
            ApplyOrientation(orientation);
            StartMusic();
        }

        /// <summary>
        /// Spawns or validates instances of Player, Target, and Projectile.
        /// </summary>
        private void SetupEntities()
        {
            // Setup Projectile Prefab if not assigned
            if (projectilePrefab == null)
            {
                projectilePrefab = CreateDefaultProjectilePrefab();
            }

            // Setup Player (Object A)
            if (playerInstance == null)
            {
                playerInstance = FindFirstObjectByType<PlayerController>();
                if (playerInstance == null)
                {
                    GameObject pGo = playerPrefab != null
                        ? Instantiate(playerPrefab)
                        : CreateDefaultPlayerObject();
                    pGo.name = "Player_ObjectA";
                    playerInstance = pGo.GetComponent<PlayerController>();
                }
            }

            if (playerInstance != null)
            {
                if (playerInstance.GetComponent<AudioSource>() == null)
                {
                    AudioSource a = playerInstance.gameObject.AddComponent<AudioSource>();
                    a.playOnAwake = false;
                }
                playerInstance.MoveSpeed = playerMoveSpeed;
                playerInstance.ProjectilePrefab = projectilePrefab;
                playerInstance.ProjectileSpeed = projectileSpeed;
                if (fireSfx != null)
                {
                    playerInstance.FireClip = fireSfx;
                }
            }

            // Setup Target (Object B)
            if (targetInstance == null)
            {
                targetInstance = FindFirstObjectByType<TargetController>();
                if (targetInstance == null)
                {
                    GameObject tGo = targetPrefab != null
                        ? Instantiate(targetPrefab)
                        : CreateDefaultTargetObject();
                    tGo.name = "Target_ObjectB";
                    targetInstance = tGo.GetComponent<TargetController>();
                }
            }

            if (targetInstance != null)
            {
                if (targetInstance.GetComponent<AudioSource>() == null)
                {
                    AudioSource a = targetInstance.gameObject.AddComponent<AudioSource>();
                    a.playOnAwake = false;
                }
                targetInstance.BaseSpeed = targetBaseSpeed;
                targetInstance.WaveFrequency = targetWaveFrequency;
                targetInstance.WaveAmplitude = targetWaveAmplitude;
                if (explosionSfx != null)
                {
                    targetInstance.HitClip = explosionSfx;
                }
            }

            if (scoreManager == null)
            {
                scoreManager = FindFirstObjectByType<ScoreManager>();
            }
            if (scoreManager != null)
            {
                scoreManager.EnsureComponents();
                if (explosionSfx != null && scoreManager.HitClip == null)
                {
                    scoreManager.HitClip = explosionSfx;
                }
            }
        }

        /// <summary>
        /// Enforces exact visual size parity between Object A and Object B regardless of native sprite resolution.
        /// </summary>
        public void NormalizeEntitySizes()
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            SpriteRenderer srPlayer = playerInstance != null ? playerInstance.SpriteRenderer : null;
            SpriteRenderer srTarget = targetInstance != null ? targetInstance.SpriteRenderer : null;

            if (srPlayer != null && srPlayer.sprite != null)
            {
                // Normalize Player to target uniform world unit size
                ViewportManager.Instance.MatchObjectUniformSize(srPlayer, targetUniformSize);
            }

            if (srTarget != null && srTarget.sprite != null && srPlayer != null && srPlayer.sprite != null)
            {
                // Dynamically force exact size parity: Target matches Player's bounding dimensions
                ViewportManager.Instance.MatchObjectBounds(srTarget, srPlayer, uniform: true);
            }
            else if (srTarget != null && srTarget.sprite != null)
            {
                ViewportManager.Instance.MatchObjectUniformSize(srTarget, targetUniformSize);
            }

            if (targetSpawner != null && targetSpawner.Pool != null)
            {
                for (int i = 0; i < targetSpawner.Pool.Length; i++)
                {
                    TargetController tc = targetSpawner.Pool[i];
                    if (tc != null && tc.SpriteRenderer != null && tc.SpriteRenderer.sprite != null)
                    {
                        if (srPlayer != null && srPlayer.sprite != null)
                        {
                            ViewportManager.Instance.MatchObjectBounds(tc.SpriteRenderer, srPlayer, uniform: true);
                        }
                        else
                        {
                            ViewportManager.Instance.MatchObjectUniformSize(tc.SpriteRenderer, targetUniformSize);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Positions and orients Object A and Object B based on the selected game axis.
        /// </summary>
        public void ApplyOrientation(GameOrientation newOrientation)
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            ViewportManager.Instance.UpdateBounds();

            SpriteRenderer srA = playerInstance != null ? playerInstance.SpriteRenderer : null;
            SpriteRenderer srB = targetInstance != null ? targetInstance.SpriteRenderer : null;

            if (newOrientation == GameOrientation.Horizontal)
            {
                if (playerInstance != null)
                {
                    playerInstance.transform.rotation = Quaternion.identity;
                    playerInstance.ProjectileDirection = Vector2.right;
                    Vector2 extentsA = srA != null && srA.sprite != null ? (Vector2)srA.bounds.extents : Vector2.one * 0.5f;
                    Vector3 posA = ViewportManager.Instance.GetViewportWorldPosition(0f, 0.5f);
                    posA.x += extentsA.x; // flush against left edge
                    playerInstance.transform.position = posA;
                }

                if (targetInstance != null)
                {
                    targetInstance.IsHorizontal = true;
                    Vector2 extentsB = srB != null && srB.sprite != null ? (Vector2)srB.bounds.extents : Vector2.one * 0.5f;
                    Vector3 posB = ViewportManager.Instance.GetViewportWorldPosition(1f, 0.5f);
                    posB.x -= extentsB.x; // flush against right edge
                    targetInstance.TeleportTo(posB, resetAnchor: true);
                }
            }
            else
            {
                if (playerInstance != null)
                {
                    playerInstance.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                    playerInstance.ProjectileDirection = Vector2.down;
                    Vector2 extentsA = srA != null && srA.sprite != null ? (Vector2)srA.bounds.extents : Vector2.one * 0.5f;
                    Vector3 posA = ViewportManager.Instance.GetViewportWorldPosition(0.5f, 1f);
                    posA.y -= extentsA.y; // flush against top edge
                    playerInstance.transform.position = posA;
                }

                if (targetInstance != null)
                {
                    targetInstance.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    targetInstance.IsHorizontal = false;
                    Vector2 extentsB = srB != null && srB.sprite != null ? (Vector2)srB.bounds.extents : Vector2.one * 0.5f;
                    Vector3 posB = ViewportManager.Instance.GetViewportWorldPosition(0.5f, 0f);
                    posB.y += extentsB.y; // flush against bottom edge
                    targetInstance.TeleportTo(posB, resetAnchor: true);
                }
            }

            if (backgroundScroller != null)
            {
                backgroundScroller.SetOrientation(newOrientation);
            }

            if (targetSpawner != null)
            {
                targetSpawner.SetOrientation(newOrientation);
            }
        }

        /// <summary>
        /// Scales background sprite to encompass camera view with zero black bars across any aspect ratio.
        /// When BackgroundScroller is active, delegates to dual-segment scaling and disables parent renderer.
        /// </summary>
        public void ScaleBackground()
        {
            if (ViewportManager.Instance != null)
            {
                lastViewportWidth = ViewportManager.Instance.Width;
                lastViewportHeight = ViewportManager.Instance.Height;
            }

            if (backgroundRenderer == null)
            {
                GameObject bgGo = GameObject.Find("Background");
                if (bgGo != null)
                {
                    backgroundRenderer = bgGo.GetComponent<SpriteRenderer>();
                }
            }

            if (backgroundScroller == null)
            {
                backgroundScroller = FindFirstObjectByType<BackgroundScroller>();
                if (backgroundScroller == null && backgroundRenderer != null)
                {
                    backgroundScroller = backgroundRenderer.GetComponent<BackgroundScroller>();
                }
            }

            Sprite bgSprite = backgroundSprite != null
                ? backgroundSprite
                : (backgroundRenderer != null ? backgroundRenderer.sprite : null);

            if (bgSprite == null && backgroundScroller != null && backgroundScroller.SegmentA != null)
            {
                bgSprite = backgroundScroller.SegmentA.sprite;
            }

            if (bgSprite == null || ViewportManager.Instance == null)
            {
                return;
            }

            Vector2 spriteSize = bgSprite.rect.size / bgSprite.pixelsPerUnit;
            if (spriteSize.x <= 0.001f || spriteSize.y <= 0.001f)
            {
                return;
            }

            float viewWidth = ViewportManager.Instance.Width;
            float viewHeight = ViewportManager.Instance.Height;
            float scaleFactor = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);

            if (backgroundScroller != null)
            {
                // Disable parent renderer to prevent Z-fighting and double-rendering
                if (backgroundRenderer != null)
                {
                    backgroundRenderer.enabled = false;
                    backgroundRenderer.transform.localScale = Vector3.one;
                }

                if (!backgroundScroller.IsInitialized)
                {
                    backgroundScroller.Initialize(bgSprite, scaleFactor, ViewportManager.Instance, orientation);
                }
                else
                {
                    backgroundScroller.RefreshScale(scaleFactor);
                }
            }
            else if (backgroundRenderer != null)
            {
                backgroundRenderer.enabled = true;
                backgroundRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
                backgroundRenderer.transform.position = new Vector3(ViewportManager.Instance.Center.x, ViewportManager.Instance.Center.y, 5f);
            }

            lastViewportWidth = viewWidth;
            lastViewportHeight = viewHeight;
        }

        private void StartMusic()
        {
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            if (musicSource != null && backgroundMusic != null && !musicSource.isPlaying)
            {
                musicSource.volume = musicVolume;
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        #region Default Entity Factories

        private GameObject CreateDefaultProjectilePrefab()
        {
            GameObject go = new GameObject("DefaultProjectilePrefab");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (projectileSprite != null) sr.sprite = projectileSprite;
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            go.AddComponent<Projectile>();
            go.SetActive(false);
            return go;
        }

        private GameObject CreateDefaultPlayerObject()
        {
            GameObject go = new GameObject("Player_ObjectA");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (playerSprite != null) sr.sprite = playerSprite;
            AudioSource audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            go.AddComponent<PlayerController>();
            return go;
        }

        private GameObject CreateDefaultTargetObject()
        {
            GameObject go = new GameObject("Target_ObjectB");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (targetSprite != null) sr.sprite = targetSprite;
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            AudioSource audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            go.AddComponent<TargetController>();
            return go;
        }

        #endregion
    }
}
