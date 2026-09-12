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

        private TargetController currentTarget;
        private bool hasDetonated = false;

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float TurnRate => turnRate;
        public float SplashRadius => splashRadius;
        public TargetController CurrentTarget => currentTarget;
        public bool HasDetonated => hasDetonated;

        public override void Initialize(Vector2 flightDirection, float flightSpeed)
        {
            base.Initialize(flightDirection, flightSpeed > 0f ? flightSpeed : initialSpeed);
            speed = flightSpeed > 0f ? flightSpeed : initialSpeed;
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
