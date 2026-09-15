using System;
using UnityEngine;

namespace KinematicsGame.Player
{
    /// <summary>
    /// Manages player vitals (Health, Armor), currencies (Gold, Diamonds),
    /// and dynamic timed kinematics speed modifiers with decoupled C# events.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStats : MonoBehaviour
    {
        public const float MinSpeedMultiplier = 0.2f;
        public const float MaxSpeedMultiplier = 3.0f;

        [Header("Vitals")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int maxArmor = 50;
        [SerializeField] private int currentArmor = 0;

        [Header("Currencies")]
        [SerializeField] private int gold = 0;
        [SerializeField] private int diamonds = 0;

        [Header("Kinematics Modifiers")]
        [SerializeField] private float speedMultiplier = 1.0f;
        [SerializeField] private float speedModifierDuration = 0f;

        public event Action<int, int> OnHealthChanged;
        public event Action<int, int> OnArmorChanged;
        public event Action<int, int> OnCurrencyChanged;
        public event Action<float> OnSpeedModifierChanged;
        public event Action OnPlayerDied;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int MaxArmor => maxArmor;
        public int CurrentArmor => currentArmor;
        public int Gold => gold;
        public int Diamonds => diamonds;
        public bool IsAlive => currentHealth > 0;
        public float SpeedModifierRemainingTime => Mathf.Max(0f, speedModifierDuration);

        public float EffectiveSpeedMultiplier
        {
            get => Mathf.Clamp(speedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);
            private set
            {
                speedMultiplier = Mathf.Clamp(value, MinSpeedMultiplier, MaxSpeedMultiplier);
                OnSpeedModifierChanged?.Invoke(EffectiveSpeedMultiplier);
            }
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            currentArmor = 0;
        }

        private void Update()
        {
            UpdateModifiers(Time.deltaTime);
        }

        public void Initialize(int health = 100, int armor = 50, int startGold = 0, int startDiamonds = 0, int startArmor = 0)
        {
            maxHealth = health;
            currentHealth = health;
            maxArmor = armor;
            currentArmor = Mathf.Clamp(startArmor, 0, maxArmor);
            gold = startGold;
            diamonds = startDiamonds;
            speedMultiplier = 1.0f;
            speedModifierDuration = 0f;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
            OnCurrencyChanged?.Invoke(gold, diamonds);
            OnSpeedModifierChanged?.Invoke(EffectiveSpeedMultiplier);
        }

        /// <summary>
        /// Absorbs damage through Armor first; remaining excess depletes Health.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive) return;

            if (currentArmor > 0)
            {
                if (currentArmor >= damage)
                {
                    currentArmor -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= currentArmor;
                    currentArmor = 0;
                }
                OnArmorChanged?.Invoke(currentArmor, maxArmor);
            }

            if (damage > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth - damage);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);

                if (currentHealth == 0)
                {
                    OnPlayerDied?.Invoke();
                }
            }
        }

        public void RestoreHealth(int amount)
        {
            if (amount <= 0 || !IsAlive) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void RestoreArmor(int amount)
        {
            if (amount <= 0) return;

            currentArmor = Mathf.Clamp(currentArmor + amount, 0, maxArmor);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        public void AddCurrency(int addGold, int addDiamonds)
        {
            gold = Mathf.Max(0, gold + addGold);
            diamonds = Mathf.Max(0, diamonds + addDiamonds);
            OnCurrencyChanged?.Invoke(gold, diamonds);
        }

        public void ApplySpeedModifier(float multiplier, float duration)
        {
            speedMultiplier = Mathf.Clamp(multiplier, MinSpeedMultiplier, MaxSpeedMultiplier);
            speedModifierDuration = Mathf.Max(speedModifierDuration, duration);
            OnSpeedModifierChanged?.Invoke(EffectiveSpeedMultiplier);
        }

        public void UpdateModifiers(float deltaTime)
        {
            if (speedModifierDuration > 0f)
            {
                speedModifierDuration -= deltaTime;
                if (speedModifierDuration <= 0f)
                {
                    speedModifierDuration = 0f;
                    speedMultiplier = 1.0f;
                    OnSpeedModifierChanged?.Invoke(1.0f);
                }
            }
        }
    }
}
