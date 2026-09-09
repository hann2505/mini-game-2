using System;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Combat;

namespace KinematicsGame.Enemy
{
    /// <summary>
    /// Controls Object B (Target Enemy):
    /// - Autonomous 4-way kinematics (primary drift + sinusoidal perpendicular oscillation).
    /// - Archetype configuration via TargetProfile with effective speed/wave multipliers.
    /// - Flyweight pooled lifecycle (IsPooled) or standalone edge-wrapping mode.
    /// - Collision detection with Projectiles, triggering hit event and recycling/respawn.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public class TargetController : MonoBehaviour
    {
        [Header("Kinematics")]
        [SerializeField] private bool isHorizontal = true;
        [SerializeField] private float baseSpeed = 3f;
        [SerializeField] private float waveFrequency = 2f;
        [SerializeField] private float waveAmplitude = 1.5f;
        [SerializeField] private float boundaryBuffer = 0.5f;

        [Header("Archetype & Pooling")]
        [SerializeField] private TargetProfile currentProfile;
        [SerializeField] private bool isPooled = false;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioSource audioSource;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;

        public static event Action<TargetController, TargetProfile, Vector3> OnTargetHit;

        public static void ClearEventSubscribers()
        {
            OnTargetHit = null;
        }

        public bool IsHorizontal
        {
            get => isHorizontal;
            set
            {
                if (isHorizontal != value)
                {
                    isHorizontal = value;
                    anchorPerpendicular = isHorizontal ? transform.position.y : transform.position.x;
                }
            }
        }

        public float BaseSpeed
        {
            get => baseSpeed;
            set => baseSpeed = value;
        }

        public float WaveFrequency
        {
            get => waveFrequency;
            set => waveFrequency = value;
        }

        public float WaveAmplitude
        {
            get => waveAmplitude;
            set => waveAmplitude = value;
        }

        public float BoundaryBuffer
        {
            get => boundaryBuffer;
            set => boundaryBuffer = value;
        }

        public TargetProfile CurrentProfile
        {
            get => currentProfile;
            set => currentProfile = value;
        }

        public bool IsPooled
        {
            get => isPooled;
            set => isPooled = value;
        }

        public float EffectiveSpeed => baseSpeed * (currentProfile != null ? currentProfile.SpeedMultiplier : 1f);
        public float EffectiveWaveFrequency => waveFrequency * (currentProfile != null ? currentProfile.WaveFrequencyMultiplier : 1f);
        public float EffectiveWaveAmplitude => waveAmplitude * (currentProfile != null ? currentProfile.WaveAmplitudeMultiplier : 1f);

        public AudioClip HitClip
        {
            get => hitClip;
            set => hitClip = value;
        }

        public AudioSource AudioSource
        {
            get => audioSource;
            set => audioSource = value;
        }

        public SpriteRenderer SpriteRenderer
        {
            get => spriteRenderer;
            set => spriteRenderer = value;
        }

        public float AnchorPerpendicular
        {
            get => anchorPerpendicular;
            set => anchorPerpendicular = value;
        }

        private float anchorPerpendicular;
        private float elapsedTime;

        public void EnsureComponents()
        {
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
                rb.useFullKinematicContacts = false;
            }

            if (col == null)
            {
                col = GetComponent<Collider2D>();
                if (col == null)
                {
                    col = gameObject.AddComponent<CircleCollider2D>();
                }
            }

            if (col != null)
            {
                col.isTrigger = true;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Reset()
        {
            EnsureComponents();
        }

        private void Awake()
        {
            EnsureComponents();
            anchorPerpendicular = isHorizontal ? transform.position.y : transform.position.x;
        }

        /// <summary>
        /// Applies an archetype profile, updating sprite and resetting phase timing.
        /// </summary>
        public void ApplyProfile(TargetProfile profile)
        {
            EnsureComponents();
            currentProfile = profile;

            if (profile != null && profile.Sprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = profile.Sprite;
                if (ViewportManager.Instance != null)
                {
                    ViewportManager.Instance.MatchObjectUniformSize(spriteRenderer, 1.5f);
                }
            }

            elapsedTime = 0f;
        }

        /// <summary>
        /// Configures target kinematic parameters (legacy signature).
        /// </summary>
        public void Initialize(bool horizontal, float speed, float freq, float amp)
        {
            EnsureComponents();
            isHorizontal = horizontal;
            baseSpeed = speed;
            waveFrequency = freq;
            waveAmplitude = amp;
            anchorPerpendicular = isHorizontal ? transform.position.y : transform.position.x;
            elapsedTime = 0f;
        }

        /// <summary>
        /// Configures target kinematic parameters and archetype profile.
        /// </summary>
        public void Initialize(TargetProfile profile, bool horizontal, float speed = 4f, float freq = 2f, float amp = 1f)
        {
            Initialize(horizontal, speed, freq, amp);
            ApplyProfile(profile);
        }

        private void Update()
        {
            UpdateKinematics();
            CheckBoundaryWrap();
        }

        /// <summary>
        /// Calculates autonomous 4-way movement: primary linear drift and perpendicular sinusoidal wave.
        /// Supports optional explicit delta time for deterministic testing.
        /// </summary>
        public void UpdateKinematics(float deltaTime = -1f)
        {
            EnsureComponents();

            float dt = deltaTime >= 0f ? deltaTime : (Time.deltaTime > 0f ? Time.deltaTime : 0.0166667f);
            elapsedTime += dt;

            // Calculate anchored wave oscillation using effective multipliers
            float waveOffset = Mathf.Sin(elapsedTime * EffectiveWaveFrequency) * EffectiveWaveAmplitude;

            Vector2 extents = spriteRenderer != null && spriteRenderer.sprite != null
                ? (Vector2)spriteRenderer.bounds.extents
                : Vector2.zero;

            Vector3 pos = transform.position;

            if (isHorizontal)
            {
                // Primary drift along X axis (moving Left towards Object A origin)
                pos.x -= EffectiveSpeed * dt;

                // Perpendicular oscillation along Y axis
                float targetY = anchorPerpendicular + waveOffset;
                if (ViewportManager.Instance != null)
                {
                    float minY = ViewportManager.Instance.MinY + extents.y;
                    float maxY = ViewportManager.Instance.MaxY - extents.y;
                    if (minY > maxY)
                    {
                        float midY = (minY + maxY) * 0.5f;
                        minY = midY;
                        maxY = midY;
                    }
                    targetY = Mathf.Clamp(targetY, minY, maxY);
                }
                pos.y = targetY;
            }
            else
            {
                // Primary drift along Y axis (moving Up towards Object A origin at Mid-Top)
                pos.y += EffectiveSpeed * dt;

                // Perpendicular oscillation along X axis
                float targetX = anchorPerpendicular + waveOffset;
                if (ViewportManager.Instance != null)
                {
                    float minX = ViewportManager.Instance.MinX + extents.x;
                    float maxX = ViewportManager.Instance.MaxX - extents.x;
                    if (minX > maxX)
                    {
                        float midX = (minX + maxX) * 0.5f;
                        minX = midX;
                        maxX = midX;
                    }
                    targetX = Mathf.Clamp(targetX, minX, maxX);
                }
                pos.x = targetX;
            }

            transform.position = pos;
            if (rb != null)
            {
                rb.position = new Vector2(pos.x, pos.y);
            }
        }

        /// <summary>
        /// Checks if Object B completely crosses the origin screen boundary.
        /// If pooled, deactivates to return control to TargetSpawner; otherwise respawns at opposite edge.
        /// </summary>
        public void CheckBoundaryWrap()
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            EnsureComponents();

            Vector2 extents = spriteRenderer != null && spriteRenderer.sprite != null
                ? (Vector2)spriteRenderer.bounds.extents
                : Vector2.zero;

            Vector3 pos = transform.position;

            if (isHorizontal)
            {
                float exitThreshold = ViewportManager.Instance.MinX - extents.x - boundaryBuffer;
                if (pos.x < exitThreshold)
                {
                    if (isPooled)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        RespawnAtOppositeEdge();
                    }
                }
            }
            else
            {
                // In vertical mode, exits past top boundary where Object A originated
                float exitThreshold = ViewportManager.Instance.MaxY + extents.y + boundaryBuffer;
                if (pos.y > exitThreshold)
                {
                    if (isPooled)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        RespawnAtOppositeEdge();
                    }
                }
            }
        }

        /// <summary>
        /// Teleports Object B to the opposite screen boundary with a randomized perpendicular position.
        /// Prevents teleport collider sweep false triggers.
        /// </summary>
        public void RespawnAtOppositeEdge()
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            EnsureComponents();

            Vector2 extents = spriteRenderer != null && spriteRenderer.sprite != null
                ? (Vector2)spriteRenderer.bounds.extents
                : Vector2.zero;

            Vector3 newPos = transform.position;

            if (isHorizontal)
            {
                // Teleport to Right edge outside visible view
                newPos.x = ViewportManager.Instance.MaxX + extents.x + boundaryBuffer;

                // Randomize Y position within visible camera viewport
                float minY = ViewportManager.Instance.MinY + extents.y;
                float maxY = ViewportManager.Instance.MaxY - extents.y;
                newPos.y = minY < maxY ? UnityEngine.Random.Range(minY, maxY) : (minY + maxY) * 0.5f;

                anchorPerpendicular = newPos.y;
            }
            else
            {
                // Teleport to Bottom edge outside visible view
                newPos.y = ViewportManager.Instance.MinY - extents.y - boundaryBuffer;

                // Randomize X position within visible camera viewport
                float minX = ViewportManager.Instance.MinX + extents.x;
                float maxX = ViewportManager.Instance.MaxX - extents.x;
                newPos.x = minX < maxX ? UnityEngine.Random.Range(minX, maxX) : (minX + maxX) * 0.5f;

                anchorPerpendicular = newPos.x;
            }

            // Reset wave phase so oscillation smoothly anchors to new coordinate without popping
            elapsedTime = 0f;

            TeleportTo(newPos, resetAnchor: false);
        }

        /// <summary>
        /// Snaps position cleanly without physics collider sweep triggers across the viewport.
        /// </summary>
        public void TeleportTo(Vector3 newPosition, bool resetAnchor = true)
        {
            EnsureComponents();

            if (col != null)
            {
                col.enabled = false;
            }

            transform.position = newPosition;

            if (rb != null)
            {
                rb.position = new Vector2(newPosition.x, newPosition.y);
            }

            if (resetAnchor)
            {
                anchorPerpendicular = isHorizontal ? newPosition.y : newPosition.x;
            }

            if (col != null)
            {
                col.enabled = true;
            }
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            // Type-safe lookup independent of external TagManager settings
            if (other.TryGetComponent<Projectile>(out _) || other.gameObject.name.Contains("Projectile"))
            {
                HandleHitByProjectile(other.gameObject);
            }
        }

        /// <summary>
        /// Handles collision with Object C: dispatches OnTargetHit, cleans up bullet,
        /// and either deactivates (if pooled) or plays local sound and teleports (if standalone).
        /// </summary>
        public void HandleHitByProjectile(GameObject projectileObj)
        {
            Vector3 impactPos = transform.position;

            if (projectileObj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(projectileObj);
                }
                else
                {
                    Destroy(projectileObj);
                }
#else
                Destroy(projectileObj);
#endif
            }

            OnTargetHit?.Invoke(this, currentProfile, impactPos);

            if (isPooled)
            {
                gameObject.SetActive(false);
            }
            else
            {
                PlayExplosionSound();
                RespawnAtOppositeEdge();
            }
        }

        private void PlayExplosionSound()
        {
            if (audioSource != null && hitClip != null)
            {
                audioSource.volume = 0.2f;
                audioSource.PlayOneShot(hitClip);
            }
        }
    }
}
