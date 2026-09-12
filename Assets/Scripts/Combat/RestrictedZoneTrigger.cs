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
        [SerializeField, Range(3, 6)] private int pulseCount = 4;
        [SerializeField] private float pulseInterval = 0.35f;
        [SerializeField] private float debounceTime = 3.0f;
        [SerializeField, Range(0.05f, 0.5f)] private float zoneViewportRatio = 0.2f;

        [Header("Components")]
        [SerializeField] private BoxCollider2D boxCollider;

        private float lastTriggerTime = -100f;
        private float simulatedTime = 0f;
        private bool useSimulatedTime = false;

        public int TriggerCount { get; private set; }
        public int PulseCount => pulseCount;
        public float PulseInterval => pulseInterval;
        public float DebounceTime => debounceTime;
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
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            TargetController target = other.GetComponent<TargetController>();
            if (target == null)
            {
                target = other.GetComponentInParent<TargetController>();
            }

            if (target != null)
            {
                TryTriggerAlarm();
            }
        }

        public bool TryTriggerAlarm()
        {
            float currentTime = useSimulatedTime ? simulatedTime : Time.time;
            if (currentTime - lastTriggerTime < debounceTime)
            {
                return false;
            }

            lastTriggerTime = currentTime;
            TriggerCount++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWarningAlarm(warningClip, pulseCount, pulseInterval);
            }

            OnWarningTriggered?.Invoke();
            return true;
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
            lastTriggerTime = -100f;
            simulatedTime = 0f;
        }

        #endregion
    }
}
