using UnityEngine;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;

namespace KinematicsGame.UI
{
    /// <summary>
    /// Manages a pool of world-space FloatingTextController instances.
    /// Listens to TargetController.OnTargetHit to spawn score pop-ups at impact positions.
    /// All 20 instances are pre-warmed and recycled — zero runtime allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingTextPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float animDuration = FloatingTextController.DefaultDuration;

        [Header("Color Tiers by Combo")]
        [SerializeField] private Color colorCombo1 = Color.white;
        [SerializeField] private Color colorCombo2 = new Color(1f, 0.9f, 0.2f); // Yellow
        [SerializeField] private Color colorCombo3 = new Color(1f, 0.55f, 0.1f); // Orange
        [SerializeField] private Color colorCombo4Plus = new Color(1f, 0.2f, 0.2f); // Red

        private FloatingTextController[] pool;
        private bool isInitialized = false;

        public int PoolSize => poolSize;
        public FloatingTextController[] Pool => pool;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            if (!isInitialized) Initialize();
        }

        private void OnEnable()
        {
            TargetController.OnTargetHit += HandleTargetHit;
        }

        private void OnDisable()
        {
            TargetController.OnTargetHit -= HandleTargetHit;
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

        /// <summary>
        /// Pre-warms the pool with inactive FloatingTextController instances.
        /// </summary>
        public void Initialize()
        {
            pool = new FloatingTextController[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = new GameObject($"FCT_{i}");
                go.transform.SetParent(transform);
                FloatingTextController fct = go.AddComponent<FloatingTextController>();
                fct.EnsureComponents();
                go.SetActive(false);
                pool[i] = fct;
            }

            isInitialized = true;
        }

        private void HandleTargetHit(TargetController target, TargetProfile profile, Vector3 hitPosition)
        {
            int points = profile != null ? profile.PointValue : 10;
            int combo = ScoreManager.Instance != null ? ScoreManager.Instance.ComboMultiplier : 1;

            // The combo at time of hit is already incremented by ScoreManager.ProcessHit,
            // so we display the multiplier that was applied (combo - 1, clamped to 1)
            int displayCombo = Mathf.Max(1, combo - 1);
            string text = displayCombo > 1
                ? $"+{points * displayCombo} x{displayCombo}"
                : $"+{points}";

            Color color = GetComboColor(displayCombo);
            Spawn(hitPosition + Vector3.up * 0.3f, text, color);
        }

        private Color GetComboColor(int combo)
        {
            if (combo >= 4) return colorCombo4Plus;
            if (combo == 3) return colorCombo3;
            if (combo == 2) return colorCombo2;
            return colorCombo1;
        }

        /// <summary>
        /// Retrieves an inactive FCT from the pool and starts the animation.
        /// If the pool is exhausted, the oldest active instance is reclaimed.
        /// </summary>
        public FloatingTextController Spawn(Vector3 worldPosition, string text, Color color)
        {
            if (pool == null) return null;

            FloatingTextController fct = GetInactiveInstance();
            if (fct == null) return null;

            fct.Play(worldPosition, text, color, null, animDuration);
            return fct;
        }

        private FloatingTextController GetInactiveInstance()
        {
            // First pass: find an inactive instance
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                {
                    return pool[i];
                }
            }

            // Fallback: reclaim the first active instance (oldest)
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null)
                {
                    pool[i].Stop();
                    return pool[i];
                }
            }

            return null;
        }

        public int ActiveCount
        {
            get
            {
                if (pool == null) return 0;
                int count = 0;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i] != null && pool[i].gameObject.activeSelf) count++;
                }
                return count;
            }
        }
    }
}
