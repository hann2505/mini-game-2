using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Enemy;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Periodically spawns Objects X (Hazard Mine), Y (Tech Supply Crate), and Z (Gem Core)
    /// at the entry perimeter of the viewport, drifting along the primary kinematic axis.
    /// </summary>
    [DisallowMultipleComponent]
    public class HazardSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject hazardMinePrefab;
        [SerializeField] private GameObject supplyCratePrefab;
        [SerializeField] private GameObject gemCorePrefab;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 3.0f;
        [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.8f;
        [SerializeField] private float minSpawnInterval = 2.0f;
        [SerializeField] private bool autoSpawn = false;
        [SerializeField] private float spawnTimer = 0f;

        [Header("Spawn Volume & Prewarm")]
        [SerializeField] [Range(1, 4)] private int maxSpawnPerCycle = 1;
        [SerializeField] [Range(0f, 1f)] private float multiSpawnChance = 0.0f;
        [SerializeField] private bool prewarmViewport = false;
        [SerializeField] [Range(0, 8)] private int initialPrewarmCount = 0;

        [Header("Enemy Defeat Drop Chance")]
        [SerializeField] [Range(0f, 1f)] private float enemyDropChance = 1.0f;

        [Header("Spawn Weights (Objects X, Y, Z)")]
        [SerializeField] private float hazardMineWeight = 0.25f;
        [SerializeField] private float supplyCrateWeight = 0.25f;
        [SerializeField] private float gemCoreWeight = 0.50f;

        [Header("Orientation")]
        [SerializeField] private bool isHorizontal = true;

        public GameObject HazardMinePrefab { get => hazardMinePrefab; set => hazardMinePrefab = value; }
        public GameObject SupplyCratePrefab { get => supplyCratePrefab; set => supplyCratePrefab = value; }
        public GameObject GemCorePrefab { get => gemCorePrefab; set => gemCorePrefab = value; }
        public float SpawnInterval { get => spawnInterval; set => spawnInterval = Mathf.Max(0.1f, value); }
        public float SpawnChance { get => spawnChance; set => spawnChance = Mathf.Clamp01(value); }
        public float MinSpawnInterval { get => minSpawnInterval; set => minSpawnInterval = Mathf.Max(0.1f, value); }
        public int MaxSpawnPerCycle { get => maxSpawnPerCycle; set => maxSpawnPerCycle = Mathf.Max(1, value); }
        public float MultiSpawnChance { get => multiSpawnChance; set => multiSpawnChance = Mathf.Clamp01(value); }
        public bool PrewarmViewport { get => prewarmViewport; set => prewarmViewport = value; }
        public int InitialPrewarmCount { get => initialPrewarmCount; set => initialPrewarmCount = Mathf.Max(0, value); }
        public float EnemyDropChance { get => enemyDropChance; set => enemyDropChance = Mathf.Clamp01(value); }
        public float HazardMineWeight { get => hazardMineWeight; set => hazardMineWeight = Mathf.Max(0f, value); }
        public float SupplyCrateWeight { get => supplyCrateWeight; set => supplyCrateWeight = Mathf.Max(0f, value); }
        public float GemCoreWeight { get => gemCoreWeight; set => gemCoreWeight = Mathf.Max(0f, value); }
        public bool AutoSpawn { get => autoSpawn; set => autoSpawn = value; }
        public bool IsHorizontal { get => isHorizontal; set => isHorizontal = value; }

        public void EnsurePrefabsLoaded()
        {
#if UNITY_EDITOR
            if (hazardMinePrefab == null)
            {
                hazardMinePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hazard_Mine.prefab");
            }
            if (supplyCratePrefab == null)
            {
                supplyCratePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Supply_Crate.prefab");
            }
            if (gemCorePrefab == null)
            {
                gemCorePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gem_Core.prefab");
            }
#endif
        }

        private void OnEnable()
        {
            GameController.OnOrientationChanged += HandleOrientationChanged;
            TargetController.OnTargetHit += HandleTargetHit;
            if (GameController.Instance != null)
            {
                isHorizontal = GameController.Instance.Orientation == GameOrientation.Horizontal;
            }
            EnsurePrefabsLoaded();
            spawnTimer = Mathf.Min(0.1f, spawnInterval * 0.25f);
        }

        private void Start()
        {
            if (autoSpawn && prewarmViewport)
            {
                PrewarmSpawn();
            }
        }

        /// <summary>
        /// Pre-populates the viewport with an initial spread of hazards and pickups
        /// so the player immediately experiences active gameplay upon starting.
        /// </summary>
        public void PrewarmSpawn()
        {
            if (ViewportManager.Instance == null || initialPrewarmCount <= 0) return;
            EnsurePrefabsLoaded();

            for (int i = 0; i < initialPrewarmCount; i++)
            {
                Vector3 pos;
                if (isHorizontal)
                {
                    float t = (i + 1f) / (initialPrewarmCount + 1f);
                    float x = Mathf.Lerp(ViewportManager.Instance.MinX + 3.0f, ViewportManager.Instance.MaxX - 0.5f, t);
                    float y = Random.Range(ViewportManager.Instance.MinY + 1.0f, ViewportManager.Instance.MaxY - 1.0f);
                    pos = new Vector3(x, y, 0f);
                }
                else
                {
                    float t = (i + 1f) / (initialPrewarmCount + 1f);
                    float x = Random.Range(ViewportManager.Instance.MinX + 1.0f, ViewportManager.Instance.MaxX - 1.0f);
                    float y = Mathf.Lerp(ViewportManager.Instance.MinY + 0.5f, ViewportManager.Instance.MaxY - 3.0f, t);
                    pos = new Vector3(x, y, 0f);
                }

                EntityType type = GetRandomEntityType();
                SpawnHazard(type, pos);
            }
        }

        private void OnDisable()
        {
            GameController.OnOrientationChanged -= HandleOrientationChanged;
            TargetController.OnTargetHit -= HandleTargetHit;
        }

        private void HandleTargetHit(TargetController target, TargetProfile profile, Vector3 hitPosition)
        {
            if (enemyDropChance > 0f && (enemyDropChance >= 1f || Random.value <= enemyDropChance))
            {
                EntityType type = GetRandomEntityType();
                SpawnHazard(type, hitPosition, armingDelay: 0.75f);
            }
        }

        private void HandleOrientationChanged(GameOrientation orientation)
        {
            isHorizontal = orientation == GameOrientation.Horizontal;
        }

        private void Update()
        {
            if (autoSpawn)
            {
                UpdateSpawner(Time.deltaTime);
            }
        }

        public void UpdateSpawner(float deltaTime)
        {
            if (!autoSpawn) return;

            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                if (spawnChance >= 1f || Random.value <= spawnChance)
                {
                    int spawnCount = 1;
                    if (maxSpawnPerCycle > 1 && multiSpawnChance > 0f && Random.value <= multiSpawnChance)
                    {
                        spawnCount = Random.Range(2, maxSpawnPerCycle + 1);
                    }

                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnRandomHazard();
                    }
                }
            }
        }

        public EntityType GetRandomEntityType()
        {
            float totalWeight = hazardMineWeight + supplyCrateWeight + gemCoreWeight;
            if (totalWeight <= 0.0001f) totalWeight = 1f;

            float roll = Random.value * totalWeight;
            if (roll < hazardMineWeight)
            {
                return EntityType.HazardMine; // Object X
            }
            else if (roll < hazardMineWeight + supplyCrateWeight)
            {
                return EntityType.SupplyCrate; // Object Y
            }
            else
            {
                return EntityType.GemCore; // Object Z
            }
        }

        public GameObject SpawnRandomHazard()
        {
            EnsurePrefabsLoaded();
            return SpawnHazard(GetRandomEntityType());
        }

        public GameObject SpawnHazard(EntityType type, Vector3? explicitPosition = null, float armingDelay = 0f)
        {
            EnsurePrefabsLoaded();
            GameObject prefab = null;
            switch (type)
            {
                case EntityType.HazardMine:
                    prefab = hazardMinePrefab;
                    break;
                case EntityType.SupplyCrate:
                    prefab = supplyCratePrefab;
                    break;
                case EntityType.GemCore:
                    prefab = gemCorePrefab;
                    break;
            }

            Vector3 spawnPos = explicitPosition ?? CalculateSpawnPosition();
            GameObject go = null;

            if (prefab != null)
            {
                go = Instantiate(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                go = new GameObject($"Hazard_{type}");
                go.transform.position = spawnPos;
                CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
                cc.isTrigger = true;
                InteractiveEntity ie = go.AddComponent<InteractiveEntity>();
                ie.Type = type;
                ie.EnsureComponents();
            }

            InteractiveEntity entity = go.GetComponent<InteractiveEntity>();
            if (entity != null)
            {
                entity.IsHorizontal = isHorizontal;
                if (type == EntityType.HazardMine && armingDelay > 0f)
                {
                    entity.SetArmingDelay(armingDelay);
                }
            }

            return go;
        }

        private Vector3 CalculateSpawnPosition()
        {
            if (ViewportManager.Instance == null)
            {
                return isHorizontal ? new Vector3(10f, Random.Range(-3f, 3f), 0f) : new Vector3(Random.Range(-3f, 3f), -10f, 0f);
            }

            if (isHorizontal)
            {
                // Enter from Right edge, spread across vertical lanes
                float x = ViewportManager.Instance.MaxX + 1.0f + Random.Range(0f, 0.6f);
                float y = Random.Range(ViewportManager.Instance.MinY + 1.0f, ViewportManager.Instance.MaxY - 1.0f);
                return new Vector3(x, y, 0f);
            }
            else
            {
                // Enter from Bottom edge, spread across horizontal lanes
                float x = Random.Range(ViewportManager.Instance.MinX + 1.0f, ViewportManager.Instance.MaxX - 1.0f);
                float y = ViewportManager.Instance.MinY - 1.0f - Random.Range(0f, 0.6f);
                return new Vector3(x, y, 0f);
            }
        }
    }
}
