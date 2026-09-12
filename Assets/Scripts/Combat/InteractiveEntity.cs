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

        [Header("Components")]
        [SerializeField] private CircleCollider2D col;
        [SerializeField] private SpriteRenderer spriteRenderer;

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

        public GameObject MiniBonusPrefab
        {
            get => miniBonusPrefab;
            set => miniBonusPrefab = value;
        }

        public bool IsConsumed { get; set; }

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
                if (col.radius < 0.5f)
                {
                    col.radius = 0.5f;
                }
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsConsumed || other == null) return;

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
            UpdateKinematics();
            CheckViewportBounds();
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
