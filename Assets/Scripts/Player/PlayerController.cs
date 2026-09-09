using UnityEngine;
using UnityEngine.InputSystem;
using KinematicsGame.Core;
using KinematicsGame.Combat;

namespace KinematicsGame.Player
{
    /// <summary>
    /// Controls Object A (Player Spaceship):
    /// - 4-way responsive movement clamped strictly within camera viewport bounds.
    /// - Mouse click / touch tap projectile firing with configurable speed and cooldown.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Weapon Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float fireCooldown = 0.15f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 projectileDirection = Vector2.right;
        [SerializeField] private float projectileSpeed = 12f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float fireVolume = 0.25f;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerInput playerInput;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        public Vector2 ProjectileDirection
        {
            get => projectileDirection;
            set => projectileDirection = value.normalized;
        }

        public float ProjectileSpeed
        {
            get => projectileSpeed;
            set => projectileSpeed = value;
        }

        public GameObject ProjectilePrefab
        {
            get => projectilePrefab;
            set => projectilePrefab = value;
        }

        public float FireCooldown
        {
            get => fireCooldown;
            set => fireCooldown = value;
        }

        public Transform FirePoint
        {
            get => firePoint;
            set => firePoint = value;
        }

        public SpriteRenderer SpriteRenderer
        {
            get => spriteRenderer;
            set => spriteRenderer = value;
        }

        public PlayerInput PlayerInputComponent
        {
            get => playerInput;
            set => playerInput = value;
        }

        public AudioClip FireClip
        {
            get => fireClip;
            set => fireClip = value;
        }

        public AudioSource AudioSource
        {
            get => audioSource;
            set => audioSource = value;
        }

        public float FireVolume
        {
            get => fireVolume;
            set => fireVolume = Mathf.Clamp01(value);
        }

        public Vector2 MoveInput => moveInput;

        private Vector2 moveInput;
        private float lastFireTime = -100f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }
        }

        private void Update()
        {
            HandleDirectInputFallback();
            HandleMovement();
        }

        /// <summary>
        /// Translates the player by move input and clamps position strictly inside screen bounds.
        /// </summary>
        public void HandleMovement()
        {
            if (moveInput.sqrMagnitude > 0.001f)
            {
                Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f) * (moveSpeed * Time.deltaTime);
                transform.position += movement;
            }

            ClampToScreenBounds();
        }

        /// <summary>
        /// Restricts the player's position using sprite bounds extents so no part of the ship clips outside view.
        /// </summary>
        public void ClampToScreenBounds()
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            Vector2 extents = Vector2.zero;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                extents = spriteRenderer.bounds.extents;
            }

            float minX = ViewportManager.Instance.MinX + extents.x;
            float maxX = ViewportManager.Instance.MaxX - extents.x;
            float minY = ViewportManager.Instance.MinY + extents.y;
            float maxY = ViewportManager.Instance.MaxY - extents.y;

            // In cases where viewport is smaller than sprite, prevent inversions
            if (minX > maxX)
            {
                float midX = (minX + maxX) * 0.5f;
                minX = midX;
                maxX = midX;
            }

            if (minY > maxY)
            {
                float midY = (minY + maxY) * 0.5f;
                minY = midY;
                maxY = midY;
            }

            Vector3 pos = transform.position;
            float clampedX = Mathf.Clamp(pos.x, minX, maxX);
            float clampedY = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = new Vector3(clampedX, clampedY, pos.z);
        }

        /// <summary>
        /// Fallback direct input reading for editor testing and standalone input if PlayerInput component is absent.
        /// </summary>
        private void HandleDirectInputFallback()
        {
            // If PlayerInput is present and enabled, do not double-poll
            if (playerInput != null && playerInput.enabled)
            {
                return;
            }

            // Keyboard movement fallback
            if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

                moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);

                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    FireProjectile();
                }
            }

            // Mouse click fallback
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireProjectile();
            }

            // Touch tap fallback
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                FireProjectile();
            }
        }

        #region Input System Callbacks

        /// <summary>
        /// New Input System message callback for 'Move' action with diagonal clamping.
        /// </summary>
        public void OnMove(InputValue value)
        {
            moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
        }

        /// <summary>
        /// New Input System message callback for 'Attack' action (click/touch).
        /// </summary>
        public void OnAttack(InputValue value)
        {
            if (value.isPressed)
            {
                FireProjectile();
            }
        }

        #endregion

        /// <summary>
        /// Manually sets movement vector for testing or AI control.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Spawns Object C (Projectile) at fire point towards the target direction.
        /// </summary>
        public void FireProjectile()
        {
            if (Time.time - lastFireTime < fireCooldown)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                return;
            }

            lastFireTime = Time.time;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(projectileDirection, projectileSpeed);
            }

            PlayFireSound();
        }

        private void PlayFireSound()
        {
            if (audioSource != null && fireClip != null)
            {
                audioSource.PlayOneShot(fireClip, fireVolume);
            }
        }
    }
}
