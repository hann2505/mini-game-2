using System;
using UnityEngine;

namespace KinematicsGame.Core
{
    /// <summary>
    /// Manages continuous, seamless background scrolling in 2D using a dual-sprite leapfrog translation approach.
    /// Provides deterministic Tick(float deltaTime) for NUnit tests, dynamic orientation switching (Horizontal / Vertical),
    /// and resize continuity without position resets.
    /// </summary>
    [DisallowMultipleComponent]
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("Child Segment References")]
        [SerializeField] private SpriteRenderer segmentA;
        [SerializeField] private SpriteRenderer segmentB;

        [Header("Kinematics Parameters")]
        [SerializeField] private float scrollSpeed = 3f;
        [SerializeField] private Vector2 scrollDirection = Vector2.left;
        [SerializeField] private bool isScrolling = true;
        [SerializeField] private GameOrientation currentOrientation = GameOrientation.Horizontal;

        [Header("Runtime Dimension Cache")]
        [SerializeField] private float segmentWidth;
        [SerializeField] private float segmentHeight;

        private ViewportManager viewportManager;
        private bool isInitialized;

        public float ScrollSpeed
        {
            get => scrollSpeed;
            set => scrollSpeed = value;
        }

        public Vector2 ScrollDirection
        {
            get => scrollDirection;
            set => scrollDirection = value.sqrMagnitude > 0.0001f 
                ? value.normalized 
                : (currentOrientation == GameOrientation.Horizontal ? Vector2.left : Vector2.up);
        }

        public bool IsScrolling
        {
            get => isScrolling;
            set => isScrolling = value;
        }

        public GameOrientation CurrentOrientation => currentOrientation;
        public float SegmentWidth => segmentWidth;
        public float SegmentHeight => segmentHeight;
        public bool IsInitialized => isInitialized;

        public SpriteRenderer SegmentA
        {
            get => segmentA;
            set => segmentA = value;
        }

        public SpriteRenderer SegmentB
        {
            get => segmentB;
            set => segmentB = value;
        }

        public ViewportManager ViewportManagerRef
        {
            get => viewportManager;
            set => viewportManager = value;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Bootstraps or resets the dual-segment background system.
        /// Scales child segments directly while leaving the parent transform at Vector3.one to avoid double-scaling.
        /// </summary>
        public void Initialize(Sprite sprite, float scaleFactor, ViewportManager vm, GameOrientation orientation)
        {
            if (sprite == null || scaleFactor <= 0.0001f)
            {
                return;
            }

            viewportManager = vm != null ? vm : ViewportManager.Instance;
            currentOrientation = orientation;

            EnsureSegmentsExist();

            segmentA.sprite = sprite;
            segmentB.sprite = sprite;
            segmentA.sortingOrder = -100;
            segmentB.sortingOrder = -100;

            // Apply scale exclusively to child segments
            segmentA.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
            segmentB.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

            // Compute scaled world dimensions
            Vector2 rawSize = sprite.rect.size / sprite.pixelsPerUnit;
            segmentWidth = rawSize.x * scaleFactor;
            segmentHeight = rawSize.y * scaleFactor;

            Vector3 center = viewportManager != null ? viewportManager.Center : transform.position;
            center.z = 5f;

            if (currentOrientation == GameOrientation.Horizontal)
            {
                scrollDirection = Vector2.left;
                segmentA.transform.position = center;
                segmentB.transform.position = new Vector3(center.x + segmentWidth, center.y, center.z);
            }
            else
            {
                scrollDirection = Vector2.up;
                segmentA.transform.position = center;
                segmentB.transform.position = new Vector3(center.x, center.y - segmentHeight, center.z);
            }

            isInitialized = true;
        }

        /// <summary>
        /// Recalculates segment dimensions and scales on screen/window resize without resetting active scroll phase
        /// and without opening gaps or causing overlaps between adjacent segments.
        /// </summary>
        public void RefreshScale(float newScaleFactor)
        {
            if (!isInitialized || segmentA == null || segmentA.sprite == null || segmentB == null || newScaleFactor <= 0.0001f)
            {
                return;
            }

            segmentA.transform.localScale = new Vector3(newScaleFactor, newScaleFactor, 1f);
            segmentB.transform.localScale = new Vector3(newScaleFactor, newScaleFactor, 1f);

            Vector2 rawSize = segmentA.sprite.rect.size / segmentA.sprite.pixelsPerUnit;
            segmentWidth = rawSize.x * newScaleFactor;
            segmentHeight = rawSize.y * newScaleFactor;

            // Re-align adjacent segment to eliminate gaps/overlap while preserving current scroll phase
            if (currentOrientation == GameOrientation.Horizontal)
            {
                if (segmentB.transform.position.x >= segmentA.transform.position.x)
                {
                    segmentB.transform.position = new Vector3(segmentA.transform.position.x + segmentWidth, segmentA.transform.position.y, segmentA.transform.position.z);
                }
                else
                {
                    segmentA.transform.position = new Vector3(segmentB.transform.position.x + segmentWidth, segmentB.transform.position.y, segmentB.transform.position.z);
                }
            }
            else
            {
                if (segmentB.transform.position.y <= segmentA.transform.position.y)
                {
                    segmentB.transform.position = new Vector3(segmentA.transform.position.x, segmentA.transform.position.y - segmentHeight, segmentA.transform.position.z);
                }
                else
                {
                    segmentA.transform.position = new Vector3(segmentB.transform.position.x, segmentB.transform.position.y - segmentHeight, segmentB.transform.position.z);
                }
            }
        }

        /// <summary>
        /// Dynamically alters orientation and aligns segments along the active axis.
        /// </summary>
        public void SetOrientation(GameOrientation orientation)
        {
            currentOrientation = orientation;

            Vector3 center = viewportManager != null ? viewportManager.Center : transform.position;
            center.z = 5f;

            if (currentOrientation == GameOrientation.Horizontal)
            {
                scrollDirection = Vector2.left;
                if (segmentA != null && segmentB != null)
                {
                    segmentA.transform.position = center;
                    segmentB.transform.position = new Vector3(center.x + segmentWidth, center.y, center.z);
                }
            }
            else
            {
                scrollDirection = Vector2.up;
                if (segmentA != null && segmentB != null)
                {
                    segmentA.transform.position = center;
                    segmentB.transform.position = new Vector3(center.x, center.y - segmentHeight, center.z);
                }
            }
        }

        /// <summary>
        /// Deterministic update step. Advances segments by velocity * deltaTime and leapfrogs segments exiting the viewport.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isScrolling || deltaTime <= 0f || !isInitialized || segmentA == null || segmentB == null)
            {
                return;
            }

            Vector3 translation = (Vector3)(scrollDirection.normalized * (scrollSpeed * deltaTime));
            segmentA.transform.position += translation;
            segmentB.transform.position += translation;

            if (viewportManager == null)
            {
                viewportManager = ViewportManager.Instance;
            }

            float minX = viewportManager != null ? viewportManager.MinX : -10f;
            float maxX = viewportManager != null ? viewportManager.MaxX : 10f;
            float minY = viewportManager != null ? viewportManager.MinY : -5f;
            float maxY = viewportManager != null ? viewportManager.MaxY : 5f;

            if (currentOrientation == GameOrientation.Horizontal)
            {
                if (scrollDirection.x < 0f)
                {
                    // Moving Left (-X)
                    if (segmentA.transform.position.x + (segmentWidth * 0.5f) <= minX)
                    {
                        segmentA.transform.position = new Vector3(segmentB.transform.position.x + segmentWidth, segmentB.transform.position.y, segmentB.transform.position.z);
                    }
                    if (segmentB.transform.position.x + (segmentWidth * 0.5f) <= minX)
                    {
                        segmentB.transform.position = new Vector3(segmentA.transform.position.x + segmentWidth, segmentA.transform.position.y, segmentA.transform.position.z);
                    }
                }
                else if (scrollDirection.x > 0f)
                {
                    // Moving Right (+X)
                    if (segmentA.transform.position.x - (segmentWidth * 0.5f) >= maxX)
                    {
                        segmentA.transform.position = new Vector3(segmentB.transform.position.x - segmentWidth, segmentB.transform.position.y, segmentB.transform.position.z);
                    }
                    if (segmentB.transform.position.x - (segmentWidth * 0.5f) >= maxX)
                    {
                        segmentB.transform.position = new Vector3(segmentA.transform.position.x - segmentWidth, segmentA.transform.position.y, segmentA.transform.position.z);
                    }
                }
            }
            else
            {
                if (scrollDirection.y > 0f)
                {
                    // Moving Up (+Y)
                    if (segmentA.transform.position.y - (segmentHeight * 0.5f) >= maxY)
                    {
                        segmentA.transform.position = new Vector3(segmentB.transform.position.x, segmentB.transform.position.y - segmentHeight, segmentB.transform.position.z);
                    }
                    if (segmentB.transform.position.y - (segmentHeight * 0.5f) >= maxY)
                    {
                        segmentB.transform.position = new Vector3(segmentA.transform.position.x, segmentA.transform.position.y - segmentHeight, segmentA.transform.position.z);
                    }
                }
                else if (scrollDirection.y < 0f)
                {
                    // Moving Down (-Y)
                    if (segmentA.transform.position.y + (segmentHeight * 0.5f) <= minY)
                    {
                        segmentA.transform.position = new Vector3(segmentB.transform.position.x, segmentB.transform.position.y + segmentHeight, segmentB.transform.position.z);
                    }
                    if (segmentB.transform.position.y + (segmentHeight * 0.5f) <= minY)
                    {
                        segmentB.transform.position = new Vector3(segmentA.transform.position.x, segmentA.transform.position.y + segmentHeight, segmentA.transform.position.z);
                    }
                }
            }
        }

        private void EnsureSegmentsExist()
        {
            if (segmentA == null)
            {
                Transform tA = transform.Find("Segment_A");
                if (tA != null)
                {
                    segmentA = tA.GetComponent<SpriteRenderer>();
                }

                if (segmentA == null)
                {
                    GameObject goA = new GameObject("Segment_A");
                    goA.transform.SetParent(transform, false);
                    segmentA = goA.AddComponent<SpriteRenderer>();
                }
            }

            if (segmentB == null)
            {
                Transform tB = transform.Find("Segment_B");
                if (tB != null)
                {
                    segmentB = tB.GetComponent<SpriteRenderer>();
                }

                if (segmentB == null)
                {
                    GameObject goB = new GameObject("Segment_B");
                    goB.transform.SetParent(transform, false);
                    segmentB = goB.AddComponent<SpriteRenderer>();
                }
            }
        }
    }
}
