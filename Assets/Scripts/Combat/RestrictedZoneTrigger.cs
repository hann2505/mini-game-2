using System;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Enemy;
using KinematicsGame.Audio;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Orientation-aware 2D trigger zone that fires 3-6 warning pulses with a 3.0s anti-spam debounce
    /// whenever an enemy (Object B) breaches the defensive perimeter (left 20% in Horizontal, top 20% in Vertical).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public class RestrictedZoneTrigger : MonoBehaviour
    {
        public static event Action OnWarningTriggered;

        [Header("Warning Configuration")]
        [SerializeField] private AudioClip warningClip;
        [SerializeField, Range(3, 6)] private int minPulseCount = 3;
        [SerializeField, Range(3, 6)] private int maxPulseCount = 6;
        [SerializeField] private float pulseInterval = 0.35f;
        [SerializeField] private float debounceTime = 3.0f;
        [SerializeField] private float cooldownTime = 4.0f;
        [SerializeField, Range(0.05f, 0.5f)] private float zoneViewportRatio = 0.2f;

        [Header("Components")]
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private Rigidbody2D rb;

        private float lastTriggerTime = -100f;
        private float simulatedTime = 0f;
        private bool useSimulatedTime = false;

        public int TriggerCount { get; private set; }
        public int LastPulseCount { get; private set; }
        public int MinPulseCount => Mathf.Min(minPulseCount, maxPulseCount);
        public int MaxPulseCount => Mathf.Max(minPulseCount, maxPulseCount);
        public float PulseInterval => pulseInterval;
        public float DebounceTime
        {
            get => debounceTime;
            set => debounceTime = Mathf.Max(0f, value);
        }
        public float CooldownTime
        {
            get => cooldownTime;
            set => cooldownTime = Mathf.Max(0f, value);
        }
        public float EffectiveCooldown => Mathf.Max(debounceTime, cooldownTime);
        public float ZoneViewportRatio => zoneViewportRatio;
        public BoxCollider2D BoxCollider => boxCollider;

        public AudioClip WarningClip
        {
            get => warningClip;
            set => warningClip = value;
        }

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            if (ViewportManager.Instance != null)
            {
                AlignToViewport(GameController.Instance != null ? GameController.Instance.Orientation : GameOrientation.Horizontal);
            }
        }

        private void OnEnable()
        {
            EnsureComponents();
            GameController.OnOrientationChanged += AlignToViewport;

            if (ViewportManager.Instance != null)
            {
                AlignToViewport(GameController.Instance != null ? GameController.Instance.Orientation : GameOrientation.Horizontal);
            }
        }

        private void OnDisable()
        {
            GameController.OnOrientationChanged -= AlignToViewport;
        }

        private void Update()
        {
            CheckZoneBreach();
        }

        public void EnsureComponents()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    boxCollider = gameObject.AddComponent<BoxCollider2D>();
                }
            }
            boxCollider.isTrigger = true;

            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody2D>();
                }
            }
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
            }

#if UNITY_EDITOR
            if (warningClip == null)
            {
                warningClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/warning.mp3");
            }
#endif
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            TargetController target = other.GetComponent<TargetController>();
            if (target == null)
            {
                target = other.GetComponentInParent<TargetController>();
            }

            if (target != null && target.gameObject.activeInHierarchy)
            {
                TryTriggerAlarm();
            }
        }

        /// <summary>
        /// Proximity check ensuring immediate breach detection in Update,
        /// independent of kinematic trigger dispatch timing.
        /// </summary>
        public void CheckZoneBreach()
        {
            if (boxCollider == null) return;

            // Fast exit if still on cooldown or if warning alarm is actively playing
            if (!IsCooldownElapsed()) return;

            if (!useSimulatedTime && AudioManager.Instance != null)
            {
                if (AudioManager.Instance.IsWarningPlaying || AudioManager.Instance.IsWarningOnCooldown)
                {
                    return;
                }
            }

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCollider.bounds.center, boxCollider.bounds.size, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.gameObject == gameObject) continue;

                TargetController target = hit.GetComponent<TargetController>() ?? hit.GetComponentInParent<TargetController>();
                if (target != null && target.gameObject.activeInHierarchy)
                {
                    if (TryTriggerAlarm())
                    {
                        break;
                    }
                }
            }
        }

        public bool IsCooldownElapsed()
        {
            float currentTime = useSimulatedTime ? simulatedTime : Time.time;
            float cd = useSimulatedTime ? debounceTime : EffectiveCooldown;
            return currentTime - lastTriggerTime >= cd;
        }

        public bool TryTriggerAlarm()
        {
            if (!IsCooldownElapsed())
            {
                return false;
            }

            if (!useSimulatedTime && AudioManager.Instance != null)
            {
                if (AudioManager.Instance.IsWarningPlaying || AudioManager.Instance.IsWarningOnCooldown)
                {
                    return false;
                }
            }

            float currentTime = useSimulatedTime ? simulatedTime : Time.time;
            lastTriggerTime = currentTime;
            TriggerCount++;
            LastPulseCount = SelectPulseCount();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWarningAlarm(warningClip, LastPulseCount, pulseInterval);
            }

            OnWarningTriggered?.Invoke();
            return true;
        }

        public int SelectPulseCount()
        {
            return UnityEngine.Random.Range(MinPulseCount, MaxPulseCount + 1);
        }

        public void AlignToViewport(GameOrientation orientation)
        {
            if (ViewportManager.Instance == null) return;
            EnsureComponents();

            float totalWidth = ViewportManager.Instance.Width;
            float totalHeight = ViewportManager.Instance.Height;

            if (orientation == GameOrientation.Horizontal)
            {
                // Left 20% of viewport
                float zoneWidth = totalWidth * zoneViewportRatio;
                float zoneHeight = totalHeight;
                float centerX = ViewportManager.Instance.MinX + (zoneWidth * 0.5f);
                float centerY = ViewportManager.Instance.Center.y;

                transform.position = new Vector3(centerX, centerY, 0f);
                boxCollider.size = new Vector2(zoneWidth, zoneHeight);
            }
            else
            {
                // Top 20% of viewport
                float zoneWidth = totalWidth;
                float zoneHeight = totalHeight * zoneViewportRatio;
                float centerX = ViewportManager.Instance.Center.x;
                float centerY = ViewportManager.Instance.MaxY - (zoneHeight * 0.5f);

                transform.position = new Vector3(centerX, centerY, 0f);
                boxCollider.size = new Vector2(zoneWidth, zoneHeight);
            }
        }

        #region EditMode Test Helpers

        public void EnableSimulatedTime(float initialTime = 0f)
        {
            useSimulatedTime = true;
            simulatedTime = initialTime;
            lastTriggerTime = -100f;
        }

        public void AdvanceSimulatedTime(float deltaTime)
        {
            simulatedTime += deltaTime;
        }

        public void ResetTriggerState()
        {
            TriggerCount = 0;
            LastPulseCount = 0;
            lastTriggerTime = -100f;
            simulatedTime = 0f;
        }

        #endregion
    }
}
