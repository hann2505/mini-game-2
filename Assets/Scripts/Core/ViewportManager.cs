using UnityEngine;

namespace KinematicsGame.Core
{
    /// <summary>
    /// Computes and caches dynamic screen/viewport camera world bounds and handles sprite dimension normalization.
    /// Works across arbitrary screen aspect ratios and resolutions with zero per-frame allocation.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ViewportManager : MonoBehaviour
    {
        public static ViewportManager Instance { get; private set; }

        [Header("Camera Configuration")]
        [SerializeField] private Camera targetCamera;

        public Camera TargetCamera
        {
            get => targetCamera;
            set
            {
                targetCamera = value;
                UpdateBounds();
            }
        }

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.cyan;

        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        public Vector3 Center => new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, 0f);

        private int lastScreenWidth;
        private int lastScreenHeight;
        private float lastOrthoSize;
        private Vector3 lastCameraPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Explicitly initialize or refresh camera reference and world bounds.
        /// </summary>
        public void Initialize()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindFirstObjectByType<Camera>();
                }
            }

            UpdateBounds();
        }

        private void Update()
        {
            // Detect resolution, camera orthographic size, or camera position changes at runtime without allocations
            if (targetCamera != null)
            {
                if (Screen.width != lastScreenWidth ||
                    Screen.height != lastScreenHeight ||
                    !Mathf.Approximately(targetCamera.orthographicSize, lastOrthoSize) ||
                    targetCamera.transform.position != lastCameraPos)
                {
                    UpdateBounds();
                }
            }
        }

        /// <summary>
        /// Recalculates screen world boundary extents.
        /// </summary>
        public void UpdateBounds()
        {
            if (targetCamera == null)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastOrthoSize = targetCamera.orthographicSize;
            lastCameraPos = targetCamera.transform.position;

            float distanceToPlane = Mathf.Abs(targetCamera.transform.position.z);
            Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceToPlane));
            Vector3 topRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceToPlane));

            MinX = Mathf.Min(bottomLeft.x, topRight.x);
            MaxX = Mathf.Max(bottomLeft.x, topRight.x);
            MinY = Mathf.Min(bottomLeft.y, topRight.y);
            MaxY = Mathf.Max(bottomLeft.y, topRight.y);
        }

        /// <summary>
        /// Converts normalized viewport coordinates (0 to 1) to world position at specified Z depth.
        /// </summary>
        public Vector3 GetViewportWorldPosition(float viewportX, float viewportY, float z = 0f)
        {
            if (targetCamera == null)
            {
                Initialize();
                if (targetCamera == null)
                {
                    return Vector3.zero;
                }
            }

            float distanceToPlane = Mathf.Abs(targetCamera.transform.position.z - z);
            Vector3 worldPos = targetCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, distanceToPlane));
            worldPos.z = z;
            return worldPos;
        }

        /// <summary>
        /// Scales target renderer's transform so that its world bounding box matches the desired world dimensions.
        /// </summary>
        public void MatchObjectSize(SpriteRenderer targetRenderer, Vector2 desiredWorldSize, bool preserveAspect = true)
        {
            if (targetRenderer == null || targetRenderer.sprite == null)
            {
                return;
            }

            Vector2 unscaledSize = targetRenderer.sprite.rect.size / targetRenderer.sprite.pixelsPerUnit;
            if (unscaledSize.x <= 0.0001f || unscaledSize.y <= 0.0001f)
            {
                return;
            }

            if (preserveAspect)
            {
                float scale = Mathf.Min(desiredWorldSize.x / unscaledSize.x, desiredWorldSize.y / unscaledSize.y);
                targetRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                float scaleX = desiredWorldSize.x / unscaledSize.x;
                float scaleY = desiredWorldSize.y / unscaledSize.y;
                targetRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        /// <summary>
        /// Scales target renderer uniformly so its dominant dimension matches targetMaxDimension.
        /// </summary>
        public void MatchObjectUniformSize(SpriteRenderer targetRenderer, float targetMaxDimension)
        {
            if (targetRenderer == null || targetRenderer.sprite == null)
            {
                return;
            }

            Vector2 unscaledSize = targetRenderer.sprite.rect.size / targetRenderer.sprite.pixelsPerUnit;
            float maxDim = Mathf.Max(unscaledSize.x, unscaledSize.y);
            if (maxDim <= 0.0001f)
            {
                return;
            }

            float uniformScale = targetMaxDimension / maxDim;
            targetRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        /// <summary>
        /// Adjusts target renderer's transform so its bounding dimensions match reference renderer's bounds exactly,
        /// immune to reference rotation artifacts.
        /// </summary>
        public void MatchObjectBounds(SpriteRenderer targetRenderer, SpriteRenderer referenceRenderer, bool uniform = true)
        {
            if (targetRenderer == null || referenceRenderer == null)
            {
                return;
            }

            if (targetRenderer.sprite == null || referenceRenderer.sprite == null)
            {
                return;
            }

            Vector2 refUnscaledSize = referenceRenderer.sprite.rect.size / referenceRenderer.sprite.pixelsPerUnit;
            Vector3 refLossy = referenceRenderer.transform.lossyScale;
            Vector2 refWorldSize = new Vector2(refUnscaledSize.x * Mathf.Abs(refLossy.x), refUnscaledSize.y * Mathf.Abs(refLossy.y));

            Vector2 targetUnscaledSize = targetRenderer.sprite.rect.size / targetRenderer.sprite.pixelsPerUnit;
            if (targetUnscaledSize.x <= 0.0001f || targetUnscaledSize.y <= 0.0001f)
            {
                return;
            }

            if (uniform)
            {
                float refMaxDim = Mathf.Max(refWorldSize.x, refWorldSize.y);
                float targetMaxDim = Mathf.Max(targetUnscaledSize.x, targetUnscaledSize.y);
                float uniformScale = refMaxDim / targetMaxDim;
                targetRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
            }
            else
            {
                float scaleX = refWorldSize.x / targetUnscaledSize.x;
                float scaleY = refWorldSize.y / targetUnscaledSize.y;
                targetRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            float distanceToPlane = Mathf.Abs(cam.transform.position.z);
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distanceToPlane));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceToPlane));

            Vector3 center = (bl + tr) * 0.5f;
            Vector3 size = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 0.1f);

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
