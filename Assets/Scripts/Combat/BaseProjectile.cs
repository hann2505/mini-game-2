using UnityEngine;
using KinematicsGame.Core;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Abstract base class for all combat projectiles (Blaster, Missile, Bomb).
    /// Provides kinematic movement, orientation alignment, and viewport culling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public abstract class BaseProjectile : MonoBehaviour
    {
        [Header("Kinematics")]
        [SerializeField] protected Vector2 direction = Vector2.right;
        [SerializeField] protected float speed = 10f;
        [SerializeField] protected float exitBuffer = 0.5f;

        [Header("Combat")]
        [SerializeField] protected int damage = 25;

        [Header("Components")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected Collider2D col;

        public Vector2 Direction => direction;
        public float Speed => speed;
        public int Damage => damage;

        public float ExitBuffer
        {
            get => exitBuffer;
            set => exitBuffer = value;
        }

        public SpriteRenderer SpriteRenderer
        {
            get => spriteRenderer;
            set => spriteRenderer = value;
        }

        public virtual void EnsureComponents()
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
        }

        protected virtual void Reset()
        {
            EnsureComponents();
        }

        protected virtual void Awake()
        {
            EnsureComponents();
        }

        public virtual void Initialize(Vector2 flightDirection, float flightSpeed)
        {
            EnsureComponents();

            if (flightDirection.sqrMagnitude > 0.001f)
            {
                direction = flightDirection.normalized;
            }
            speed = flightSpeed;

            AlignRotationToDirection();
        }

        public virtual void AlignRotationToDirection()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected virtual void Update()
        {
            UpdateKinematics();
            CheckViewportBounds();
        }

        public virtual void UpdateKinematics(float deltaTime = -1f)
        {
            EnsureComponents();

            float dt = deltaTime >= 0f ? deltaTime : (Time.deltaTime > 0f ? Time.deltaTime : 0.0166667f);
            Vector3 movement = (Vector3)(direction * speed * dt);
            transform.position += movement;

            if (rb != null)
            {
                rb.position = (Vector2)transform.position;
            }
        }

        public virtual void CheckViewportBounds()
        {
            if (ViewportManager.Instance == null)
            {
                return;
            }

            EnsureComponents();

            Vector2 extents = Vector2.zero;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                extents = spriteRenderer.bounds.extents;
            }

            float minX = ViewportManager.Instance.MinX - extents.x - exitBuffer;
            float maxX = ViewportManager.Instance.MaxX + extents.x + exitBuffer;
            float minY = ViewportManager.Instance.MinY - extents.y - exitBuffer;
            float maxY = ViewportManager.Instance.MaxY + extents.y + exitBuffer;

            Vector3 pos = transform.position;
            if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
            {
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
}
