using UnityEngine;
using KinematicsGame.Core;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Represents Object C (Projectile bullet) fired by Player.
    /// Moves with customizable speed/direction and self-destructs upon exiting the screen viewport.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour
    {
        [Header("Kinematics")]
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float exitBuffer = 0.5f;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;

        public Vector2 Direction => direction;
        public float Speed => speed;
        public float ExitBuffer
        {
            get => exitBuffer;
            set => exitBuffer = value;
        }

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

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Ensure trigger collider exists
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// Configures the flight vector and speed of this projectile.
        /// </summary>
        public void Initialize(Vector2 flightDirection, float flightSpeed)
        {
            if (flightDirection.sqrMagnitude > 0.001f)
            {
                direction = flightDirection.normalized;
            }
            speed = flightSpeed;

            // Rotate projectile to face flight direction if 2D sprite orientation matches
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private void Update()
        {
            // Frame-rate independent translation
            Vector3 delta = (Vector3)(direction.normalized * (speed * Time.deltaTime));
            transform.position += delta;

            CheckViewportBounds();
        }

        /// <summary>
        /// Cleans up projectile when it completely exits the visible camera viewport with padding.
        /// Safe in both PlayMode and EditMode.
        /// </summary>
        public void CheckViewportBounds()
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
