using UnityEngine;
using KinematicsGame.Enemy;
using KinematicsGame.Audio;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Tactical homing missile that accelerates from initialSpeed (6) to maxSpeed (14),
    /// steers towards the nearest active enemy within a forward cone, and deals radial splash damage.
    /// </summary>
    [DisallowMultipleComponent]
    public class HomingMissile : BaseProjectile
    {
        [Header("Homing Settings")]
        [SerializeField] private float initialSpeed = 6f;
        [SerializeField] private float maxSpeed = 14f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float turnRate = 240f;
        [SerializeField] private float detectionRadius = 15f;
        [SerializeField] private float maxDetectionAngle = 90f;

        [Header("Explosion Settings")]
        [SerializeField] private float splashRadius = 1.5f;
        [SerializeField] private AudioClip explosionClip;

        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private Sprite[] flyingFrames;
        [SerializeField] private Sprite[] explosionFrames;
        [SerializeField] private float animationFps = 15f;

        [Header("Sizing")]
        [SerializeField] private float targetUniformSize = 1.1f;

        private TargetController currentTarget;
        private bool hasDetonated = false;
        private float animTimer = 0f;
        private int currentFlyingFrame = 0;

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float TurnRate => turnRate;
        public float SplashRadius => splashRadius;
        public TargetController CurrentTarget => currentTarget;
        public bool HasDetonated => hasDetonated;

        public Animator AnimatorComponent { get => animator; set => animator = value; }
        public GameObject ExplosionPrefab { get => explosionPrefab; set => explosionPrefab = value; }
        public Sprite[] FlyingFrames { get => flyingFrames; set => flyingFrames = value; }
        public Sprite[] ExplosionFrames { get => explosionFrames; set => explosionFrames = value; }
        public float AnimationFps { get => animationFps; set => animationFps = Mathf.Max(1f, value); }
        public float TargetUniformSize
        {
            get => targetUniformSize;
            set
            {
                targetUniformSize = value;
                ApplyTargetSize();
            }
        }

        public void ApplyTargetSize(float customSize = -1f)
        {
            float targetSize = customSize > 0f ? customSize : targetUniformSize;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Vector2 unscaledSize = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
                float maxDim = Mathf.Max(unscaledSize.x, unscaledSize.y);
                if (maxDim > 0.0001f)
                {
                    float uniformScale = targetSize / maxDim;
                    transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ApplyTargetSize();
#if UNITY_EDITOR
            if (explosionPrefab == null)
            {
                explosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Missile_Explosion.prefab");
            }
#endif
        }

        public override void AlignRotationToDirection()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public override void Initialize(Vector2 flightDirection, float flightSpeed)
        {
            base.Initialize(flightDirection, flightSpeed > 0f ? flightSpeed : initialSpeed);
            speed = flightSpeed > 0f ? flightSpeed : initialSpeed;
            AlignRotationToDirection();
            ApplyTargetSize();
        }

        public override void UpdateKinematics(float deltaTime = -1f)
        {
            if (hasDetonated) return;
            EnsureComponents();

            float dt = deltaTime >= 0f ? deltaTime : (Time.deltaTime > 0f ? Time.deltaTime : 0.0166667f);

            // Accelerate towards maximum speed
            speed = Mathf.Min(maxSpeed, speed + (acceleration * dt));

            // Target tracking and steering
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = AcquireNearestTarget();
            }

            if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
            {
                Vector2 toTarget = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
                float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;

                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnRate * dt);
                direction = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad)).normalized;
                AlignRotationToDirection();
            }

            Vector3 movement = (Vector3)(direction * speed * dt);
            transform.position += movement;

            if (rb != null)
            {
                rb.position = (Vector2)transform.position;
            }

            UpdateFlyingAnimation(dt);
            CheckTargetCollision();
        }

        public override void EnsureComponents()
        {
            base.EnsureComponents();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void CheckTargetCollision()
        {
            if (hasDetonated) return;

            float radius = col != null ? (col is CircleCollider2D cc ? cc.radius * transform.localScale.x : 0.6f) : 0.6f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.5f, radius));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.gameObject == gameObject) continue;

                TargetController target = hit.GetComponent<TargetController>() ?? hit.GetComponentInParent<TargetController>();
                if (target != null && target.gameObject.activeInHierarchy)
                {
                    Detonate();
                    return;
                }
            }
        }

        public void UpdateFlyingAnimation(float dt)
        {
            if (flyingFrames == null || flyingFrames.Length == 0) return;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            animTimer += dt;
            float interval = 1f / (animationFps > 0f ? animationFps : 15f);
            if (animTimer >= interval)
            {
                animTimer -= interval;
                currentFlyingFrame = (currentFlyingFrame + 1) % flyingFrames.Length;
                spriteRenderer.sprite = flyingFrames[currentFlyingFrame];
            }
        }

        public TargetController AcquireNearestTarget()
        {
            TargetController[] targets = FindObjectsByType<TargetController>(FindObjectsSortMode.None);
            TargetController bestTarget = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < targets.Length; i++)
            {
                TargetController t = targets[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;

                Vector2 toTarget = (Vector2)t.transform.position - (Vector2)transform.position;
                float dist = toTarget.magnitude;

                if (dist > detectionRadius) continue;

                float angle = Vector2.Angle(direction, toTarget);
                if (angle <= maxDetectionAngle * 0.5f && dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = t;
                }
            }

            return bestTarget;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasDetonated || other == null) return;

            TargetController target = other.GetComponent<TargetController>();
            if (target == null)
            {
                target = other.GetComponentInParent<TargetController>();
            }

            if (target != null)
            {
                Detonate();
            }
        }

        public void Detonate()
        {
            if (hasDetonated) return;
            hasDetonated = true;

            // Radial splash damage
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, splashRadius);
            for (int i = 0; i < hitColliders.Length; i++)
            {
                TargetController target = hitColliders[i].GetComponent<TargetController>();
                if (target == null)
                {
                    target = hitColliders[i].GetComponentInParent<TargetController>();
                }

                if (target != null)
                {
                    // Simulates hit on target
                    target.HandleHitByProjectile(null);
                }
            }

            if (AudioManager.Instance != null && explosionClip != null)
            {
                AudioManager.Instance.PlaySfx(explosionClip);
            }

            SpawnExplosionEffect();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
#else
            Destroy(gameObject);
#endif
        }

        private void SpawnExplosionEffect()
        {
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);

#if UNITY_EDITOR
            if (explosionPrefab == null)
            {
                explosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Missile_Explosion.prefab");
            }
#endif
            Quaternion explosionRotation = transform.rotation;
            if (direction.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                explosionRotation = Quaternion.Euler(0f, 0f, angle);
            }

            GameObject expGo = null;
            if (explosionPrefab != null)
            {
                expGo = Instantiate(explosionPrefab, spawnPos, explosionRotation);
                expGo.transform.rotation = explosionRotation;
                expGo.transform.localScale = Vector3.one * 0.5f;
            }
            else if (explosionFrames != null && explosionFrames.Length > 0)
            {
                expGo = new GameObject("Missile_Explosion");
                expGo.transform.position = spawnPos;
                expGo.transform.rotation = explosionRotation;
                expGo.transform.localScale = Vector3.one * 0.5f;
                SpriteRenderer expSr = expGo.AddComponent<SpriteRenderer>();
                expSr.sortingOrder = 15;
                AnimatedSpriteEffect effect = expGo.AddComponent<AnimatedSpriteEffect>();
                effect.Frames = explosionFrames;
                effect.FramesPerSecond = animationFps > 0f ? animationFps : 15f;
                effect.Loop = false;
                effect.AutoDestroy = true;
            }

            if (expGo != null)
            {
                expGo.transform.position = spawnPos;
                expGo.transform.rotation = explosionRotation;
                SpriteRenderer expSr = expGo.GetComponent<SpriteRenderer>();
                if (expSr != null)
                {
                    expSr.sortingOrder = 15;
                }
                expGo.SetActive(true);
            }
        }
    }
}
