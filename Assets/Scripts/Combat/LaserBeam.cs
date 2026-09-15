using System;
using System.Collections.Generic;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Enemy;
using KinematicsGame.Audio;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Renders a continuous piercing visual beam using LineRenderer and applies tick-based damage
    /// to targets along its trajectory using Physics2D.CircleCastNonAlloc without heap allocations.
    /// Visuals are configured with bright/intense colors and glow for high visibility.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [DisallowMultipleComponent]
    public class LaserBeam : MonoBehaviour
    {
        [Header("Beam Dimensions & Visuals")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float beamRadius = 0.2f;
        [SerializeField] private float startWidth = 0.28f;
        [SerializeField] private float endWidth = 0.22f;
        [SerializeField] private Color startColor = new Color(0.4f, 1.0f, 1.0f, 1f); // Bright cyan
        [SerializeField] private Color endColor = new Color(0.8f, 0.4f, 1.0f, 0.95f); // Bright magenta/neon
        [SerializeField] private float maxBeamDistance = 30f;

        [Header("Tick Damage")]
        [SerializeField] private float tickInterval = 0.1f;
        [SerializeField] private int damagePerTick = 10;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip laserLoopClip;
        [SerializeField] private float loopVolume = 0.3f;

        private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
        private readonly Dictionary<Collider2D, float> lastHitTimes = new Dictionary<Collider2D, float>();
        private readonly List<Collider2D> staleColliders = new List<Collider2D>();

        private bool isBeamActive = false;
        private bool useSimulatedTime = false;
        private float simulatedTime = 0f;

        public bool IsBeamActive => isBeamActive;
        public float BeamRadius { get => beamRadius; set => beamRadius = value; }
        public float TickInterval { get => tickInterval; set => tickInterval = value; }
        public int DamagePerTick { get => damagePerTick; set => damagePerTick = value; }
        public float StartWidth { get => startWidth; set => startWidth = value; }
        public float EndWidth { get => endWidth; set => endWidth = value; }
        public Color StartColor { get => startColor; set => startColor = value; }
        public Color EndColor { get => endColor; set => endColor = value; }
        public LineRenderer LineRenderer => lineRenderer;

        private void Awake()
        {
            EnsureComponents();
            SetBeamActive(false);
        }

        public void EnsureComponents()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.startWidth = startWidth;
                lineRenderer.endWidth = endWidth;

                // Configure bright neon gradient
                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;

                if (lineRenderer.sharedMaterial == null)
                {
                    Shader defaultShader = Shader.Find("Sprites/Default");
                    if (defaultShader != null)
                    {
                        lineRenderer.material = new Material(defaultShader);
                    }
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        public void SetBeamActive(bool active)
        {
            EnsureComponents();
            isBeamActive = active;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = active;
            }

            if (active)
            {
                if (audioSource != null && laserLoopClip != null && !audioSource.isPlaying)
                {
                    audioSource.clip = laserLoopClip;
                    audioSource.loop = true;
                    audioSource.volume = loopVolume;
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                lastHitTimes.Clear();
            }
        }

        public void UpdateBeam(Vector2 origin, Vector2 direction)
        {
            if (!isBeamActive) return;

            EnsureComponents();

            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            // Compute max endpoint reaching beyond camera viewport or maxBeamDistance
            float targetX = (ViewportManager.Instance != null)
                ? ViewportManager.Instance.MaxX + 2f
                : origin.x + maxBeamDistance;

            float distance = maxBeamDistance;
            if (Mathf.Abs(dir.x) > 0.001f)
            {
                float d = (targetX - origin.x) / dir.x;
                if (d > 0f)
                {
                    distance = Mathf.Min(distance, d);
                }
            }

            Vector2 endpoint = origin + dir * distance;

            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
                lineRenderer.SetPosition(1, new Vector3(endpoint.x, endpoint.y, 0f));
            }

            // Sweep and detect targets along the beam without GC allocation
            int hitCount = Physics2D.CircleCast(origin, beamRadius, dir, default(ContactFilter2D).NoFilter(), hitBuffer, distance);
            float now = GetCurrentTime();

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hitBuffer[i];
                Collider2D col = hit.collider;
                if (col == null) continue;

                // Check if target cooldown has elapsed
                if (lastHitTimes.TryGetValue(col, out float lastTime))
                {
                    if (now - lastTime < tickInterval)
                    {
                        continue;
                    }
                }

                // Apply damage to TargetController
                TargetController target = col.GetComponent<TargetController>() ?? col.GetComponentInParent<TargetController>();
                if (target != null)
                {
                    lastHitTimes[col] = now;
                    // Trigger hit on target
                    target.HandleHitByProjectile(null);
                    continue;
                }

                // Apply damage/interaction to InteractiveEntity if applicable
                InteractiveEntity entity = col.GetComponent<InteractiveEntity>() ?? col.GetComponentInParent<InteractiveEntity>();
                if (entity != null)
                {
                    lastHitTimes[col] = now;
                }
            }

            PruneStaleHits(now);
        }

        private void PruneStaleHits(float now)
        {
            if (lastHitTimes.Count > 32)
            {
                staleColliders.Clear();
                foreach (var kvp in lastHitTimes)
                {
                    if (kvp.Key == null || now - kvp.Value > 2.0f)
                    {
                        staleColliders.Add(kvp.Key);
                    }
                }
                for (int i = 0; i < staleColliders.Count; i++)
                {
                    lastHitTimes.Remove(staleColliders[i]);
                }
            }
        }

        private float GetCurrentTime()
        {
            return useSimulatedTime ? simulatedTime : Time.time;
        }

        public void EnableSimulatedTime(float initialTime = 0f)
        {
            useSimulatedTime = true;
            simulatedTime = initialTime;
            lastHitTimes.Clear();
        }

        public void AdvanceSimulatedTime(float deltaTime)
        {
            simulatedTime += deltaTime;
        }
    }
}
