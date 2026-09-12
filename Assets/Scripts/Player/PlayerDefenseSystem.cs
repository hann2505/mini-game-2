using System;
using UnityEngine;
using KinematicsGame.Enemy;
using KinematicsGame.Audio;

namespace KinematicsGame.Player
{
    /// <summary>
    /// Manages player defensive systems: Energy Shield Barrier (3 hits or 8s duration, 12s cooldown)
    /// and EMP Stun Wave (halts enemy kinematics for 3s, 15s cooldown).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerDefenseSystem : MonoBehaviour
    {
        [Header("Energy Shield")]
        [SerializeField] private float shieldDuration = 8.0f;
        [SerializeField] private int maxShieldHits = 3;
        [SerializeField] private float shieldCooldown = 12.0f;
        [SerializeField] private GameObject shieldVisual;
        [SerializeField] private AudioClip shieldActivateClip;
        [SerializeField] private AudioClip shieldHitClip;

        [Header("EMP Stun Wave")]
        [SerializeField] private float empDuration = 3.0f;
        [SerializeField] private float empCooldown = 15.0f;
        [SerializeField] private AudioClip empClip;

        public event Action<bool, int> OnShieldStateChanged;
        public event Action<float, float> OnDefenseCooldownsChanged;
        public event Action OnEmpTriggered;

        public bool IsShieldActive { get; private set; }
        public int RemainingShieldHits { get; private set; }
        public float ShieldTimeRemaining { get; private set; }
        public float ShieldCooldownRemaining { get; private set; }
        public float EmpCooldownRemaining { get; private set; }

        public float ShieldDuration => shieldDuration;
        public int MaxShieldHits => maxShieldHits;
        public float ShieldCooldown => shieldCooldown;
        public float EmpDuration => empDuration;
        public float EmpCooldown => empCooldown;

        public GameObject ShieldVisual
        {
            get => shieldVisual;
            set => shieldVisual = value;
        }

        public AudioClip ShieldActivateClip { get => shieldActivateClip; set => shieldActivateClip = value; }
        public AudioClip ShieldHitClip { get => shieldHitClip; set => shieldHitClip = value; }
        public AudioClip EmpClip { get => empClip; set => empClip = value; }

        public bool CanActivateShield => !IsShieldActive && ShieldCooldownRemaining <= 0f;
        public bool CanTriggerEmp => EmpCooldownRemaining <= 0f;

        private void Awake()
        {
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(false);
            }
        }

        private void Update()
        {
            UpdateDefense(Time.deltaTime);
        }

        public bool ActivateShield(int hits = 3, float duration = 8.0f)
        {
            if (!CanActivateShield) return false;

            IsShieldActive = true;
            RemainingShieldHits = hits > 0 ? hits : maxShieldHits;
            ShieldTimeRemaining = duration > 0 ? duration : shieldDuration;
            ShieldCooldownRemaining = shieldCooldown;

            if (shieldVisual != null)
            {
                shieldVisual.SetActive(true);
            }

            if (AudioManager.Instance != null && shieldActivateClip != null)
            {
                AudioManager.Instance.PlaySfx(shieldActivateClip);
            }

            OnShieldStateChanged?.Invoke(true, RemainingShieldHits);
            return true;
        }

        public bool TryAbsorbDamage()
        {
            if (!IsShieldActive) return false;

            RemainingShieldHits--;

            if (AudioManager.Instance != null && shieldHitClip != null)
            {
                AudioManager.Instance.PlaySfx(shieldHitClip);
            }

            if (RemainingShieldHits <= 0)
            {
                DeactivateShield();
            }
            else
            {
                OnShieldStateChanged?.Invoke(true, RemainingShieldHits);
            }

            return true;
        }

        public void DeactivateShield()
        {
            IsShieldActive = false;
            RemainingShieldHits = 0;
            ShieldTimeRemaining = 0f;

            if (shieldVisual != null)
            {
                shieldVisual.SetActive(false);
            }

            OnShieldStateChanged?.Invoke(false, 0);
        }

        public bool TriggerEmpStun()
        {
            if (!CanTriggerEmp) return false;

            EmpCooldownRemaining = empCooldown;

            TargetController[] targets = FindObjectsByType<TargetController>(FindObjectsSortMode.None);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
                {
                    targets[i].ApplyStun(empDuration);
                }
            }

            if (AudioManager.Instance != null && empClip != null)
            {
                AudioManager.Instance.PlaySfx(empClip);
            }

            OnEmpTriggered?.Invoke();
            return true;
        }

        public void UpdateDefense(float deltaTime)
        {
            if (ShieldCooldownRemaining > 0f)
            {
                ShieldCooldownRemaining = Mathf.Max(0f, ShieldCooldownRemaining - deltaTime);
            }

            if (EmpCooldownRemaining > 0f)
            {
                EmpCooldownRemaining = Mathf.Max(0f, EmpCooldownRemaining - deltaTime);
            }

            if (IsShieldActive)
            {
                ShieldTimeRemaining -= deltaTime;
                if (ShieldTimeRemaining <= 0f)
                {
                    DeactivateShield();
                }
            }

            OnDefenseCooldownsChanged?.Invoke(ShieldCooldownRemaining, EmpCooldownRemaining);
        }
    }
}
