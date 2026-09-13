using UnityEngine;
using KinematicsGame.Enemy;
using KinematicsGame.Audio;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Tactical deployable ordnance that drifts slowly forward, arms after 0.5s,
    /// and detonates upon enemy contact or when the 1.5s fuse timer expires, dealing 2.5m radial splash damage.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClusterBomb : BaseProjectile
    {
        [Header("Bomb Settings")]
        [SerializeField] private float armDelay = 0.5f;
        [SerializeField] private float fuseTime = 1.5f;
        [SerializeField] private float splashRadius = 2.5f;
        [SerializeField] private AudioClip explosionClip;

        private float timer = 0f;
        private bool hasDetonated = false;

        public float ArmDelay => armDelay;
        public float FuseTime => fuseTime;
        public float SplashRadius => splashRadius;
        public bool IsArmed => timer >= armDelay;
        public bool HasDetonated => hasDetonated;

        public override void Initialize(Vector2 flightDirection, float flightSpeed)
        {
            base.Initialize(flightDirection, flightSpeed > 0f ? flightSpeed : 1.0f);
            speed = flightSpeed > 0f ? flightSpeed : 1.0f;
            timer = 0f;
            hasDetonated = false;
        }

        public override void UpdateKinematics(float deltaTime = -1f)
        {
            if (hasDetonated) return;
            EnsureComponents();

            float dt = deltaTime >= 0f ? deltaTime : (Time.deltaTime > 0f ? Time.deltaTime : 0.0166667f);
            timer += dt;

            // Slow forward drift
            Vector3 movement = (Vector3)(direction * speed * dt);
            transform.position += movement;

            if (rb != null)
            {
                rb.position = (Vector2)transform.position;
            }

            if (timer >= fuseTime)
            {
                Detonate();
            }
            else if (IsArmed)
            {
                CheckTargetCollision();
            }
        }

        private void CheckTargetCollision()
        {
            if (hasDetonated || !IsArmed) return;

            float radius = col != null ? (col is CircleCollider2D cc ? cc.radius * transform.localScale.x : 0.5f) : 0.5f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.4f, radius));
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasDetonated || !IsArmed || other == null) return;

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

            // 2.5m radial splash damage
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
                    target.HandleHitByProjectile(null);
                }
            }

            if (AudioManager.Instance != null && explosionClip != null)
            {
                AudioManager.Instance.PlaySfx(explosionClip);
            }

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
