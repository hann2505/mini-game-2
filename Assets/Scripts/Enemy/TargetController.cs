using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Combat;

namespace KinematicsGame.Enemy
{
    /// <summary>
    /// Controls Object B (Target Enemy):
    /// - Autonomous 4-way kinematics (primary drift + sinusoidal perpendicular oscillation).
    /// - Seamless edge wrapping when crossing the boundary where Object A originated.
    /// - Collision detection with Projectiles, triggering explosion SFX and respawn.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class TargetController : MonoBehaviour
    {
        [Header("Kinematics")]
        [SerializeField] private bool isHorizontal = true;
        [SerializeField] private float baseSpeed = 3f;
        [SerializeField] private float waveFrequency = 2f;
        [SerializeField] private float waveAmplitude = 1.5f;
        [SerializeField] private float boundaryBuffer = 0.5f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioSource audioSource;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;

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

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = false;
            }

            if (col == null)
            {
                col = GetComponent<Collider2D>();
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

            // Anchor initial perpendicular coordinate
            anchorPerpendicular = isHorizontal ? transform.position.y : transform.position.x;
        }

        /// <summary>
        /// Configures target kinematic parameters.
        /// </summary>
        public void Initialize(bool horizontal, float speed, float freq, float amp)
        {
            isHorizontal = horizontal;
            baseSpeed = speed;
            waveFrequency = freq;
            waveAmplitude = amp;
            anchorPerpendicular = isHorizontal ? transform.position.y : transform.position.x;
            elapsedTime = 0f;
        }

        private void Update()
        {
            UpdateKinematics();
            CheckBoundaryWrap();
        }

        /// <summary>
        /// Calculates autonomous 4-way movement: primary linear drift and perpendicular sinusoidal wave.
        /// </summary>
        public void UpdateKinematics()
        {
            elapsedTime += Time.deltaTime;

            // Calculate anchored wave oscillation
            float waveOffset = Mathf.Sin(elapsedTime * waveFrequency) * waveAmplitude;

            Vector2 extents = spriteRenderer != null && spriteRenderer.sprite != null
                ? (Vector2)spriteRenderer.bounds.extents
                : Vector2.zero;

            Vector3 pos = transform.position;

            if (isHorizontal)
            {
                // Primary drift along X axis (moving Left towards Object A origin)
                pos.x -= baseSpeed * Time.deltaTime;

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
                pos.y += baseSpeed * Time.deltaTime;

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
        /// Checks if Object B completely crosses the origin screen boundary and wraps to the opposite edge.
        /// </summary>
        public void CheckBoundaryWrap()
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            Vector2 extents = spriteRenderer != null && spriteRenderer.sprite != null
                ? (Vector2)spriteRenderer.bounds.extents
                : Vector2.zero;

            Vector3 pos = transform.position;

            if (isHorizontal)
            {
                float exitThreshold = ViewportManager.Instance.MinX - extents.x - boundaryBuffer;
                if (pos.x < exitThreshold)
                {
                    RespawnAtOppositeEdge();
                }
            }
            else
            {
                // In vertical mode, exits past top boundary where Object A originated
                float exitThreshold = ViewportManager.Instance.MaxY + extents.y + boundaryBuffer;
                if (pos.y > exitThreshold)
                {
                    RespawnAtOppositeEdge();
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
                newPos.y = minY < maxY ? Random.Range(minY, maxY) : (minY + maxY) * 0.5f;

                anchorPerpendicular = newPos.y;
            }
            else
            {
                // Teleport to Bottom edge outside visible view
                newPos.y = ViewportManager.Instance.MinY - extents.y - boundaryBuffer;

                // Randomize X position within visible camera viewport
                float minX = ViewportManager.Instance.MinX + extents.x;
                float maxX = ViewportManager.Instance.MaxX - extents.x;
                newPos.x = minX < maxX ? Random.Range(minX, maxX) : (minX + maxX) * 0.5f;

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
        /// Handles collision with Object C: plays sound, cleans up bullet, and teleports to opposite edge.
        /// </summary>
        public void HandleHitByProjectile(GameObject projectileObj)
        {
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

            PlayExplosionSound();
            RespawnAtOppositeEdge();
        }

        private void PlayExplosionSound()
        {
            if (audioSource != null && hitClip != null)
            {
                audioSource.PlayOneShot(hitClip);
            }
        }
    }
}
