using System;
using UnityEngine;
using KinematicsGame.Combat;
using KinematicsGame.Audio;

namespace KinematicsGame.Player
{
    public enum WeaponType
    {
        Blaster = 0,
        Missile = 1,
        Bomb = 2
    }

    /// <summary>
    /// Manages player offensive loadout: Plasma Blaster, Homing Missile, and Cluster Bomb.
    /// Handles weapon switching, individual firing cooldowns, and projectile dispatch.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCombatSystem : MonoBehaviour
    {
        [Header("Active Weapon")]
        [SerializeField] private WeaponType currentWeapon = WeaponType.Blaster;

        [Header("Weapon Prefabs")]
        [SerializeField] private GameObject blasterPrefab;
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private GameObject bombPrefab;

        [Header("Cooldowns")]
        [SerializeField] private float blasterCooldown = 0.12f;
        [SerializeField] private float missileCooldown = 1.0f;
        [SerializeField] private float bombCooldown = 2.5f;

        [Header("Kinematic Speeds")]
        [SerializeField] private float blasterSpeed = 14f;
        [SerializeField] private float missileSpeed = 6f;
        [SerializeField] private float bombSpeed = 1f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip blasterFireClip;
        [SerializeField] private AudioClip missileFireClip;
        [SerializeField] private AudioClip bombDeployClip;

        private float lastBlasterTime = -100f;
        private float lastMissileTime = -100f;
        private float lastBombTime = -100f;

        private bool useSimulatedTime = false;
        private float simulatedTime = 0f;

        public event Action<WeaponType> OnWeaponChanged;
        public event Action<WeaponType, GameObject> OnWeaponFired;

        public WeaponType CurrentWeapon => currentWeapon;
        public float BlasterCooldown => blasterCooldown;
        public float MissileCooldown => missileCooldown;
        public float BombCooldown => bombCooldown;

        public GameObject BlasterPrefab { get => blasterPrefab; set => blasterPrefab = value; }
        public GameObject MissilePrefab { get => missilePrefab; set => missilePrefab = value; }
        public GameObject BombPrefab { get => bombPrefab; set => bombPrefab = value; }

        public AudioClip BlasterFireClip { get => blasterFireClip; set => blasterFireClip = value; }
        public AudioClip MissileFireClip { get => missileFireClip; set => missileFireClip = value; }
        public AudioClip BombDeployClip { get => bombDeployClip; set => bombDeployClip = value; }

        public void SelectWeapon(WeaponType weapon)
        {
            if (currentWeapon != weapon)
            {
                currentWeapon = weapon;
                OnWeaponChanged?.Invoke(currentWeapon);
            }
        }

        public void SelectNextWeapon()
        {
            int next = ((int)currentWeapon + 1) % 3;
            SelectWeapon((WeaponType)next);
        }

        public void SelectPreviousWeapon()
        {
            int prev = ((int)currentWeapon + 2) % 3;
            SelectWeapon((WeaponType)prev);
        }

        private float GetCurrentTime()
        {
            return useSimulatedTime ? simulatedTime : Time.time;
        }

        public bool CanFireCurrent()
        {
            float now = GetCurrentTime();
            switch (currentWeapon)
            {
                case WeaponType.Blaster:
                    return now - lastBlasterTime >= blasterCooldown;
                case WeaponType.Missile:
                    return now - lastMissileTime >= missileCooldown;
                case WeaponType.Bomb:
                    return now - lastBombTime >= bombCooldown;
                default:
                    return false;
            }
        }

        public GameObject FireCurrent(Vector2 direction, Transform firePoint = null, Vector3 fallbackPos = default)
        {
            if (!CanFireCurrent()) return null;

            GameObject prefab = null;
            float speed = 10f;
            AudioClip sfx = null;
            float now = GetCurrentTime();

            switch (currentWeapon)
            {
                case WeaponType.Blaster:
                    prefab = blasterPrefab;
                    speed = blasterSpeed;
                    sfx = blasterFireClip;
                    lastBlasterTime = now;
                    break;
                case WeaponType.Missile:
                    prefab = missilePrefab;
                    speed = missileSpeed;
                    sfx = missileFireClip;
                    lastMissileTime = now;
                    break;
                case WeaponType.Bomb:
                    prefab = bombPrefab;
                    speed = bombSpeed;
                    sfx = bombDeployClip;
                    lastBombTime = now;
                    break;
            }

            if (prefab == null) return null;

            Vector3 spawnPos = firePoint != null ? firePoint.position : (fallbackPos != default ? fallbackPos : transform.position);
            GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

            BaseProjectile proj = spawned.GetComponent<BaseProjectile>();
            if (proj != null)
            {
                proj.Initialize(direction, speed);
            }

            if (AudioManager.Instance != null && sfx != null)
            {
                AudioManager.Instance.PlaySfx(sfx);
            }

            OnWeaponFired?.Invoke(currentWeapon, spawned);
            return spawned;
        }

        #region EditMode Test Helpers

        public void EnableSimulatedTime(float initialTime = 0f)
        {
            useSimulatedTime = true;
            simulatedTime = initialTime;
            lastBlasterTime = -100f;
            lastMissileTime = -100f;
            lastBombTime = -100f;
        }

        public void AdvanceSimulatedTime(float deltaTime)
        {
            simulatedTime += deltaTime;
        }

        #endregion
    }
}
