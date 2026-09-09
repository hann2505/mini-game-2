using System;
using System.Collections.Generic;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Combat;

namespace KinematicsGame.Enemy
{
    /// <summary>
    /// Manages an object pool of targets, lane-partitioned anti-clumping entry positions,
    /// dynamic difficulty scaling based on score, and dual-orientation realignment.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private TargetController targetPrefab;
        [SerializeField] private int poolSize = 15;
        [SerializeField] private TargetProfile[] targetProfiles;
        [SerializeField] private PlayerController playerInstance;

        [Header("Kinematics Defaults")]
        [SerializeField] private float baseSpeed = 3f;
        [SerializeField] private float baseWaveFrequency = 2f;
        [SerializeField] private float baseWaveAmplitude = 1.5f;
        [SerializeField] private float targetUniformSize = 1.5f;

        [Header("Lane & Spawning Logic")]
        [SerializeField] private int laneCount = 6;
        [SerializeField] private float laneCooldownDuration = 1.2f;
        [SerializeField] private GameOrientation currentOrientation = GameOrientation.Horizontal;
        [SerializeField] private bool autoSpawn = true;

        private TargetController[] pool;
        private float[] laneCooldowns;
        private float spawnTimer = 0f;
        private int currentScore = 0;
        private int currentQuota = 2;
        private float currentSpawnInterval = 2.0f;
        private bool isInitialized = false;

        public int PoolSize
        {
            get => poolSize;
            set => poolSize = value;
        }

        public TargetProfile[] TargetProfiles
        {
            get => targetProfiles;
            set => targetProfiles = value;
        }

        public PlayerController PlayerInstance
        {
            get => playerInstance;
            set => playerInstance = value;
        }

        public TargetController TargetPrefab
        {
            get => targetPrefab;
            set => targetPrefab = value;
        }

        public GameOrientation CurrentOrientation
        {
            get => currentOrientation;
            set => SetOrientation(value);
        }

        public bool AutoSpawn
        {
            get => autoSpawn;
            set => autoSpawn = value;
        }

        public int LaneCount => laneCount;
        public float LaneCooldownDuration => laneCooldownDuration;
        public int CurrentQuota => currentQuota;
        public float CurrentSpawnInterval => currentSpawnInterval;
        public TargetController[] Pool => pool;
        public bool IsInitialized => isInitialized;

        public int ActiveTargetCount
        {
            get
            {
                if (pool == null) return 0;
                int count = 0;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i] != null && pool[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private void Awake()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            GameController.OnOrientationChanged += HandleOrientationChanged;
            ScoreManager.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            GameController.OnOrientationChanged -= HandleOrientationChanged;
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(int newScore)
        {
            UpdateDifficulty(newScore);
        }

        private void HandleOrientationChanged(GameOrientation newOrientation)
        {
            SetOrientation(newOrientation);
        }

        /// <summary>
        /// Bootstraps spawner, allocates object pool, and initializes lane tracking.
        /// </summary>
        public void Initialize(PlayerController player = null, TargetProfile[] profiles = null, TargetController prefab = null)
        {
            if (player != null)
            {
                playerInstance = player;
            }

            if (prefab != null)
            {
                targetPrefab = prefab;
            }

            if (profiles != null && profiles.Length > 0)
            {
                targetProfiles = profiles;
            }
            else if (targetProfiles == null || targetProfiles.Length == 0)
            {
                targetProfiles = TargetProfile.CreateDefaultPresets();
            }

            laneCooldowns = new float[laneCount];

            InitializePool();
            UpdateDifficulty(0);

            isInitialized = true;
        }

        /// <summary>
        /// Pre-warms the fixed pool array of 15 TargetControllers.
        /// </summary>
        public void InitializePool()
        {
            if (pool != null && pool.Length == poolSize)
            {
                return;
            }

            pool = new TargetController[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                TargetController target = null;
                if (targetPrefab != null)
                {
                    target = Instantiate(targetPrefab, transform);
                }
                else
                {
                    GameObject go = new GameObject($"PooledTarget_{i}");
                    go.transform.SetParent(transform);
                    go.AddComponent<SpriteRenderer>();
                    go.AddComponent<CircleCollider2D>();
                    target = go.AddComponent<TargetController>();
                }

                target.IsPooled = true;
                target.gameObject.SetActive(false);
                pool[i] = target;
            }
        }

        private void Update()
        {
            UpdateSpawner(Time.deltaTime);
        }

        /// <summary>
        /// Advances cooldown timers and spawns targets up to current quota.
        /// Public for deterministic testing in EditMode.
        /// </summary>
        public void UpdateSpawner(float deltaTime)
        {
            if (laneCooldowns != null)
            {
                for (int i = 0; i < laneCooldowns.Length; i++)
                {
                    if (laneCooldowns[i] > 0f)
                    {
                        laneCooldowns[i] -= deltaTime;
                    }
                }
            }

            if (!autoSpawn || pool == null)
            {
                return;
            }

            spawnTimer += deltaTime;
            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                if (ActiveTargetCount < currentQuota)
                {
                    SpawnTarget();
                }
            }
        }

        /// <summary>
        /// Dynamically scales concurrency quota and spawn frequency based on score.
        /// </summary>
        public void UpdateDifficulty(int score)
        {
            currentScore = score;
            currentQuota = Mathf.Clamp(2 + (score / 100) * 2, 2, poolSize);
            currentSpawnInterval = Mathf.Max(0.65f, 2.0f - (score / 1000f) * 1.35f);
        }

        /// <summary>
        /// Retrieves an inactive target from the pool, assigns an anti-clumped lane,
        /// weighted archetype profile, and normalizes visual size.
        /// </summary>
        public TargetController SpawnTarget()
        {
            if (pool == null)
            {
                InitializePool();
            }

            TargetController target = null;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                {
                    target = pool[i];
                    break;
                }
            }

            if (target == null)
            {
                return null;
            }

            TargetProfile profile = SelectWeightedProfile();
            int selectedLane = SelectLane();

            Vector2 extents = Vector2.one * 0.5f;
            if (profile != null && profile.Sprite != null)
            {
                extents = (profile.Sprite.rect.size / profile.Sprite.pixelsPerUnit) * 0.5f;
            }

            Vector3 spawnPos = CalculateLanePosition(selectedLane, extents, target.BoundaryBuffer);

            // Configure target before activating
            target.transform.position = spawnPos;
            target.IsHorizontal = (currentOrientation == GameOrientation.Horizontal);
            target.transform.rotation = target.IsHorizontal ? Quaternion.identity : Quaternion.Euler(0f, 0f, 90f);
            target.Initialize(profile, target.IsHorizontal, baseSpeed, baseWaveFrequency, baseWaveAmplitude);

            // Size parity
            if (playerInstance != null && playerInstance.SpriteRenderer != null && ViewportManager.Instance != null)
            {
                ViewportManager.Instance.MatchObjectBounds(target.SpriteRenderer, playerInstance.SpriteRenderer, uniform: true);
            }
            else if (ViewportManager.Instance != null)
            {
                ViewportManager.Instance.MatchObjectUniformSize(target.SpriteRenderer, targetUniformSize);
            }

            target.gameObject.SetActive(true);
            return target;
        }

        public TargetProfile SelectWeightedProfile()
        {
            if (targetProfiles == null || targetProfiles.Length == 0)
            {
                targetProfiles = TargetProfile.CreateDefaultPresets();
            }

            float totalWeight = 0f;
            for (int i = 0; i < targetProfiles.Length; i++)
            {
                if (targetProfiles[i] != null)
                {
                    totalWeight += targetProfiles[i].SpawnWeight;
                }
            }

            if (totalWeight <= 0.001f)
            {
                return targetProfiles[0];
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float currentSum = 0f;

            for (int i = 0; i < targetProfiles.Length; i++)
            {
                if (targetProfiles[i] != null)
                {
                    currentSum += targetProfiles[i].SpawnWeight;
                    if (roll <= currentSum)
                    {
                        return targetProfiles[i];
                    }
                }
            }

            return targetProfiles[0];
        }

        public int SelectLane()
        {
            if (laneCooldowns == null || laneCooldowns.Length != laneCount)
            {
                laneCooldowns = new float[laneCount];
            }

            // Find all eligible lanes with cooldown <= 0
            List<int> available = new List<int>();
            int minCooldownLane = 0;
            float minCooldown = float.MaxValue;

            for (int i = 0; i < laneCount; i++)
            {
                if (laneCooldowns[i] <= 0f)
                {
                    available.Add(i);
                }
                if (laneCooldowns[i] < minCooldown)
                {
                    minCooldown = laneCooldowns[i];
                    minCooldownLane = i;
                }
            }

            int pickedLane;
            if (available.Count > 0)
            {
                pickedLane = available[UnityEngine.Random.Range(0, available.Count)];
            }
            else
            {
                // Fallback: pick the lane closest to cooling down to prevent deadlock
                pickedLane = minCooldownLane;
            }

            laneCooldowns[pickedLane] = laneCooldownDuration;
            return pickedLane;
        }

        public Vector3 CalculateLanePosition(int laneIndex, Vector2 extents, float buffer)
        {
            float vmMinX = -8.88f;
            float vmMaxX = 8.88f;
            float vmMinY = -5.0f;
            float vmMaxY = 5.0f;

            if (ViewportManager.Instance != null)
            {
                vmMinX = ViewportManager.Instance.MinX;
                vmMaxX = ViewportManager.Instance.MaxX;
                vmMinY = ViewportManager.Instance.MinY;
                vmMaxY = ViewportManager.Instance.MaxY;
            }

            if (currentOrientation == GameOrientation.Horizontal)
            {
                // Spawn past Right edge
                float entryX = vmMaxX + extents.x + buffer;
                float usableHeight = (vmMaxY - extents.y) - (vmMinY + extents.y);
                if (usableHeight < 0.1f) usableHeight = 0.1f;

                float entryY = (vmMinY + extents.y) + ((laneIndex + 0.5f) / laneCount) * usableHeight;
                return new Vector3(entryX, entryY, 0f);
            }
            else
            {
                // Spawn past Bottom edge (drifts Up)
                float entryY = vmMinY - extents.y - buffer;
                float usableWidth = (vmMaxX - extents.x) - (vmMinX + extents.x);
                if (usableWidth < 0.1f) usableWidth = 0.1f;

                float entryX = (vmMinX + extents.x) + ((laneIndex + 0.5f) / laneCount) * usableWidth;
                return new Vector3(entryX, entryY, 0f);
            }
        }

        /// <summary>
        /// Adapts all active targets and lane coordinates to the new game orientation.
        /// </summary>
        public void SetOrientation(GameOrientation newOrientation)
        {
            currentOrientation = newOrientation;

            if (laneCooldowns != null)
            {
                for (int i = 0; i < laneCooldowns.Length; i++)
                {
                    laneCooldowns[i] = 0f;
                }
            }

            if (pool == null)
            {
                return;
            }

            for (int i = 0; i < pool.Length; i++)
            {
                TargetController target = pool[i];
                if (target != null && target.gameObject.activeSelf)
                {
                    target.IsHorizontal = (currentOrientation == GameOrientation.Horizontal);
                    target.transform.rotation = target.IsHorizontal ? Quaternion.identity : Quaternion.Euler(0f, 0f, 90f);

                    Vector3 pos = target.transform.position;
                    if (ViewportManager.Instance != null)
                    {
                        pos.x = Mathf.Clamp(pos.x, ViewportManager.Instance.MinX, ViewportManager.Instance.MaxX);
                        pos.y = Mathf.Clamp(pos.y, ViewportManager.Instance.MinY, ViewportManager.Instance.MaxY);
                    }

                    target.TeleportTo(pos, resetAnchor: true);
                }
            }
        }
    }
}