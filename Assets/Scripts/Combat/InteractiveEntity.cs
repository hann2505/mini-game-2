using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Player;

namespace KinematicsGame.Combat
{
    public enum EntityType
    {
        HazardMine = 0,   // Object X
        SupplyCrate = 1,  // Object Y
        GemCore = 2,      // Object Z
        MiniBonus = 3     // Subsidiary reward
    }

    /// <summary>
    /// Interactive world entity (Objects X, Y, Z) that drifts along the primary kinematic axis
    /// and triggers distinct combat/buff effects upon contact with Object A.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class InteractiveEntity : MonoBehaviour
    {
        [Header("Entity Type & Kinematics")]
        [SerializeField] private EntityType entityType = EntityType.HazardMine;
        [SerializeField] private float driftSpeed = 2.5f;
        [SerializeField] private bool isHorizontal = true;
        [SerializeField] private float exitBuffer = 1.0f;

        [Header("Audio & Pickups")]
        [SerializeField] private AudioClip interactionSfx;
        [SerializeField] private GameObject miniBonusPrefab;
        [SerializeField] private GameObject interactionEffectPrefab;
        [SerializeField] private bool splitIntoMiniBonuses = false;

        [Header("Components")]
        [SerializeField] private CircleCollider2D col;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public bool SplitIntoMiniBonuses
        {
            get => splitIntoMiniBonuses;
            set => splitIntoMiniBonuses = value;
        }

        public EntityType Type
        {
            get => entityType;
            set => entityType = value;
        }

        public float DriftSpeed
        {
            get => driftSpeed;
            set => driftSpeed = value;
        }

        public bool IsHorizontal
        {
            get => isHorizontal;
            set => isHorizontal = value;
        }

        public AudioClip InteractionSfx
        {
            get => interactionSfx;
            set => interactionSfx = value;
        }

        [Header("Visual Sizing")]
        [SerializeField] private float targetSize = 0f;

        public float TargetSize
        {
            get => targetSize;
            set
            {
                targetSize = value;
                ApplyTargetSize();
            }
        }

        public GameObject MiniBonusPrefab
        {
            get => miniBonusPrefab;
            set => miniBonusPrefab = value;
        }

        public GameObject InteractionEffectPrefab
        {
            get => interactionEffectPrefab;
            set => interactionEffectPrefab = value;
        }

        public bool IsConsumed { get; set; }

        [Header("Arming State (Hazard Mine)")]
        [SerializeField] private float armingTimer = 0f;

        public bool IsArmed => armingTimer <= 0f;
        public float ArmingTimer => armingTimer;

        public void SetArmingDelay(float delay)
        {
            armingTimer = Mathf.Max(0f, delay);
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                if (armingTimer > 0f)
                {
                    c.a = Mathf.PingPong((armingTimer * 8f) + 0.65f, 0.65f) + 0.35f;
                }
                else
                {
                    c.a = 1.0f;
                }
                spriteRenderer.color = c;
            }
        }

        public void TickArming(float deltaTime)
        {
            if (armingTimer > 0f)
            {
                armingTimer -= deltaTime;
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponent<SpriteRenderer>();
                }

                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    if (armingTimer > 0f)
                    {
                        c.a = Mathf.PingPong((armingTimer * 8f) + 0.65f, 0.65f) + 0.35f;
                    }
                    else
                    {
                        c.a = 1.0f;
                    }
                    spriteRenderer.color = c;
                }
            }
        }

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnEnable()
        {
            IsConsumed = false;
            GameController.OnOrientationChanged += HandleOrientationChanged;
            if (GameController.Instance != null)
            {
                isHorizontal = GameController.Instance.Orientation == GameOrientation.Horizontal;
            }
        }

        private void OnDisable()
        {
            GameController.OnOrientationChanged -= HandleOrientationChanged;
        }

        private void HandleOrientationChanged(GameOrientation orientation)
        {
            isHorizontal = orientation == GameOrientation.Horizontal;
        }

        [SerializeField] private Rigidbody2D rb;

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
                rb.useFullKinematicContacts = true;
            }

            if (col == null)
            {
                col = GetComponent<CircleCollider2D>();
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

#if UNITY_EDITOR
            if (entityType == EntityType.HazardMine)
            {
                if (interactionEffectPrefab == null)
                {
                    interactionEffectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bomb_3_Explosion.prefab");
                }
                if (interactionSfx == null)
                {
                    interactionSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/explosion.wav");
                }
                if (spriteRenderer != null && spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/Bombs/Bomb_3/Idle/Bomb_3_Idle_000.png");
                }
            }
            else if (entityType == EntityType.GemCore)
            {
                if (spriteRenderer != null && (spriteRenderer.sprite == null || spriteRenderer.sprite.name.Contains("diamond")))
                {
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/Collectibles/star3.png");
                    if (s == null)
                    {
                        Object[] all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Items/Collectibles/star3.png");
                        if (all != null)
                        {
                            foreach (Object obj in all)
                            {
                                if (obj is Sprite sp)
                                {
                                    s = sp;
                                    break;
                                }
                            }
                        }
                    }
                    spriteRenderer.sprite = s;
                }
            }
#endif

            ApplyTargetSize();
        }

        public void ApplyTargetSize()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (targetSize <= 0f)
            {
                switch (entityType)
                {
                    case EntityType.HazardMine:
                        targetSize = 1.0f;
                        break;
                    case EntityType.SupplyCrate:
                        targetSize = 1.0f;
                        break;
                    case EntityType.GemCore:
                        targetSize = 1.5f; // Matches Character A uniform size (1.5)
                        break;
                    case EntityType.MiniBonus:
                        targetSize = 0.5f;
                        break;
                }
            }

            if (spriteRenderer != null && spriteRenderer.sprite != null && targetSize > 0f)
            {
                Vector2 unscaledSize = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
                float maxDim = Mathf.Max(unscaledSize.x, unscaledSize.y);
                if (maxDim > 0.0001f)
                {
                    float s = targetSize / maxDim;
                    transform.localScale = new Vector3(s, s, 1f);
                }
            }

            EnsureCollider();
        }

        /// <summary>
        /// Returns the exact snug world collision radius for each object type
        /// so that physical contact is strictly required (no ghost hitboxes).
        /// </summary>
        public float GetSnugWorldRadius()
        {
            switch (entityType)
            {
                case EntityType.HazardMine:
                    return 0.30f; // Snug inside Bomb 3 visible body (visible radius 0.33)
                case EntityType.SupplyCrate:
                    return 0.40f; // Snug inside crate (visible radius 0.50)
                case EntityType.GemCore:
                    return 0.35f; // Snug inside star3.png body (visible radius ~0.50)
                default:
                    return targetSize > 0f ? targetSize * 0.35f : 0.35f;
            }
        }

        public void EnsureCollider()
        {
            if (col == null)
            {
                col = GetComponent<CircleCollider2D>();
                if (col == null)
                {
                    col = gameObject.AddComponent<CircleCollider2D>();
                }
            }
            if (col != null)
            {
                col.isTrigger = true;
                col.offset = Vector2.zero;
                float currentScale = Mathf.Max(0.0001f, transform.localScale.x);
                float desiredWorldRadius = GetSnugWorldRadius();
                col.radius = desiredWorldRadius / currentScale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsConsumed || !IsArmed || other == null) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                IsConsumed = true;
                CollisionEffectDispatcher.ResolveCollision(this, player);
            }
        }

        private void Update()
        {
            TickArming(Time.deltaTime);
            UpdateKinematics();
            CheckPlayerCollision();
            CheckViewportBounds();
        }

        /// <summary>
        /// Proximity check ensuring immediate collision resolution when player touches the entity,
        /// independent of physics engine trigger dispatch timing between kinematic bodies.
        /// </summary>
        public void CheckPlayerCollision()
        {
            if (IsConsumed || !IsArmed) return;

            PlayerController player = GameController.Instance != null ? GameController.Instance.PlayerInstance : null;
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            CircleCollider2D eCol = col != null ? col : GetComponent<CircleCollider2D>();

            if (player != null && player.gameObject.activeInHierarchy)
            {
                Collider2D pCol = player.GetComponent<Collider2D>();
                if (eCol != null && pCol != null)
                {
                    ColliderDistance2D dist = eCol.Distance(pCol);
                    if (dist.isOverlapped)
                    {
                        IsConsumed = true;
                        CollisionEffectDispatcher.ResolveCollision(this, player);
                        return;
                    }
                }
                else
                {
                    float eRadius = eCol != null
                        ? eCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y)
                        : GetSnugWorldRadius();
                    float pRadius = player.HitboxRadius > 0f ? player.HitboxRadius : 0.38f;
                    if (Vector2.Distance(transform.position, player.transform.position) <= (eRadius + pRadius))
                    {
                        IsConsumed = true;
                        CollisionEffectDispatcher.ResolveCollision(this, player);
                        return;
                    }
                }
            }

            float checkRadius = eCol != null
                ? eCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y)
                : GetSnugWorldRadius();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsConsumed) break;
                Collider2D hit = hits[i];
                if (hit == null || hit.gameObject == gameObject) continue;

                PlayerController hitPlayer = hit.GetComponent<PlayerController>() ?? hit.GetComponentInParent<PlayerController>();
                if (hitPlayer != null)
                {
                    Collider2D otherCol = hitPlayer.GetComponent<Collider2D>();
                    if (eCol != null && otherCol != null)
                    {
                        ColliderDistance2D dist = eCol.Distance(otherCol);
                        if (!dist.isOverlapped) continue;
                    }

                    IsConsumed = true;
                    CollisionEffectDispatcher.ResolveCollision(this, hitPlayer);
                    break;
                }
            }
        }

        public void UpdateKinematics(float deltaTime = -1f)
        {
            float dt = deltaTime >= 0f ? deltaTime : (Time.deltaTime > 0f ? Time.deltaTime : 0.0166667f);

            Vector3 movement = Vector3.zero;
            if (isHorizontal)
            {
                // Drift Left towards player
                movement.x = -driftSpeed * dt;
            }
            else
            {
                // Drift Up towards player
                movement.y = driftSpeed * dt;
            }

            transform.position += movement;
        }

        public void CheckViewportBounds()
        {
            if (ViewportManager.Instance == null) return;

            Vector3 pos = transform.position;
            if (pos.x < ViewportManager.Instance.MinX - exitBuffer ||
                pos.x > ViewportManager.Instance.MaxX + exitBuffer ||
                pos.y < ViewportManager.Instance.MinY - exitBuffer ||
                pos.y > ViewportManager.Instance.MaxY + exitBuffer)
            {
                Despawn();
            }
        }

        public GameObject SpawnInteractionEffect()
        {
            GameObject prefabToSpawn = interactionEffectPrefab;

#if UNITY_EDITOR
            if (prefabToSpawn == null && entityType == EntityType.HazardMine)
            {
                prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bomb_3_Explosion.prefab");
            }
#endif

            if (prefabToSpawn == null)
            {
                if (entityType == EntityType.HazardMine)
                {
                    return CreateProceduralBomb3Explosion();
                }
                return null;
            }

            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);
            GameObject effect = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            effect.name = prefabToSpawn.name;
            if (effect.transform.localScale.x < 0.35f)
            {
                effect.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            }
            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 15;
            }
            effect.SetActive(true);
            return effect;
        }

        private GameObject CreateProceduralBomb3Explosion()
        {
            GameObject effect = new GameObject("Bomb_3_Explosion");
            effect.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
            effect.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;

            AnimatedSpriteEffect anim = effect.AddComponent<AnimatedSpriteEffect>();
            anim.FramesPerSecond = 15f;
            anim.Loop = false;
            anim.AutoDestroy = true;

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites/Weapons/Bombs/Bomb_3/Explosion" });
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null)
                {
                    foreach (var obj in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (obj is Sprite sp) { s = sp; break; }
                    }
                }
                if (s != null && !sprites.Contains(s)) sprites.Add(s);
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            anim.Frames = sprites.ToArray();
            if (sprites.Count > 0) sr.sprite = sprites[0];
#endif

            return effect;
        }

        public void Despawn()
        {
            IsConsumed = true;
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
    }
}
