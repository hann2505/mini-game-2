using UnityEngine;
using KinematicsGame.Core;

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
        [SerializeField] private float spawnInterval = 8.0f;
        [SerializeField] private bool autoSpawn = true;
        [SerializeField] private float spawnTimer = 0f;

        [Header("Orientation")]
        [SerializeField] private bool isHorizontal = true;

        public GameObject HazardMinePrefab { get => hazardMinePrefab; set => hazardMinePrefab = value; }
        public GameObject SupplyCratePrefab { get => supplyCratePrefab; set => supplyCratePrefab = value; }
        public GameObject GemCorePrefab { get => gemCorePrefab; set => gemCorePrefab = value; }
        public float SpawnInterval { get => spawnInterval; set => spawnInterval = value; }
        public bool IsHorizontal { get => isHorizontal; set => isHorizontal = value; }

        private void OnEnable()
        {
            GameController.OnOrientationChanged += HandleOrientationChanged;
            if (GameController.Instance != null)
            {
                isHorizontal = GameController.Instance.Orientation == GameOrientation.Horizontal;
            }
            spawnTimer = spawnInterval * 0.5f;
        }

        private void OnDisable()
        {
            GameController.OnOrientationChanged -= HandleOrientationChanged;
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
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                SpawnRandomHazard();
            }
        }

        public GameObject SpawnRandomHazard()
        {
            float roll = Random.value;
            EntityType selectedType = EntityType.HazardMine;

            if (roll < 0.50f)
            {
                selectedType = EntityType.HazardMine; // 50%
            }
            else if (roll < 0.80f)
            {
                selectedType = EntityType.SupplyCrate; // 30%
            }
            else
            {
                selectedType = EntityType.GemCore; // 20%
            }

            return SpawnHazard(selectedType);
        }

        public GameObject SpawnHazard(EntityType type, Vector3? explicitPosition = null)
        {
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
            }

            InteractiveEntity entity = go.GetComponent<InteractiveEntity>();
            if (entity != null)
            {
                entity.IsHorizontal = isHorizontal;
            }

            return go;
        }

        private Vector3 CalculateSpawnPosition()
        {
            if (ViewportManager.Instance == null)
            {
                return isHorizontal ? new Vector3(10f, 0f, 0f) : new Vector3(0f, -10f, 0f);
            }

            if (isHorizontal)
            {
                // Enter from Right edge
                float x = ViewportManager.Instance.MaxX + 1.0f;
                float y = Random.Range(ViewportManager.Instance.MinY + 1.0f, ViewportManager.Instance.MaxY - 1.0f);
                return new Vector3(x, y, 0f);
            }
            else
            {
                // Enter from Bottom edge
                float x = Random.Range(ViewportManager.Instance.MinX + 1.0f, ViewportManager.Instance.MaxX - 1.0f);
                float y = ViewportManager.Instance.MinY - 1.0f;
                return new Vector3(x, y, 0f);
            }
        }
    }
}
