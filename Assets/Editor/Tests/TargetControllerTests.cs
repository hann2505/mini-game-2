using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;

namespace KinematicsGame.Tests
{
    public class TargetControllerTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager viewportManager;
        private GameObject targetGo;
        private TargetController targetController;
        private List<Object> disposables;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();

            cameraGo = new GameObject("TestCamera");
            disposables.Add(cameraGo);
            testCamera = cameraGo.AddComponent<Camera>();
            testCamera.orthographic = true;
            testCamera.orthographicSize = 5f;
            testCamera.aspect = 16f / 9f;
            testCamera.transform.position = new Vector3(0f, 0f, -10f);

            managerGo = new GameObject("TestViewportManager");
            disposables.Add(managerGo);
            viewportManager = managerGo.AddComponent<ViewportManager>();
            viewportManager.TargetCamera = testCamera;
            viewportManager.Initialize();

            targetGo = new GameObject("Target");
            disposables.Add(targetGo);
            targetGo.AddComponent<CircleCollider2D>();
            targetController = targetGo.AddComponent<TargetController>();

            SpriteRenderer sr = targetGo.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(64, 64);
            disposables.Add(tex);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
            targetController.SpriteRenderer = sr;
            targetController.Initialize(horizontal: true, speed: 4f, freq: 2f, amp: 1f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                if (disposables[i] != null)
                {
                    Object.DestroyImmediate(disposables[i]);
                }
            }
            disposables.Clear();
        }

        [Test]
        public void TargetController_HasKinematicRigidbodyAndTriggerCollider()
        {
            Rigidbody2D rb = targetGo.GetComponent<Rigidbody2D>();
            Assert.That(rb, Is.Not.Null);
            Assert.That(rb.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));

            Collider2D col = targetGo.GetComponent<Collider2D>();
            Assert.That(col, Is.Not.Null);
            Assert.That(col.isTrigger, Is.True);
        }

        [Test]
        public void TargetController_UpdateKinematics_DriftsAndOscillates()
        {
            Vector3 startPos = new Vector3(5f, 0f, 0f);
            targetGo.transform.position = startPos;
            targetController.Initialize(horizontal: true, speed: 4f, freq: 2f, amp: 1.5f);

            targetController.UpdateKinematics();

            // X must drift left
            Assert.That(targetGo.transform.position.x, Is.LessThan(startPos.x));
            // Y must stay within viewport limits
            Assert.That(targetGo.transform.position.y, Is.GreaterThanOrEqualTo(viewportManager.MinY));
            Assert.That(targetGo.transform.position.y, Is.LessThanOrEqualTo(viewportManager.MaxY));
        }

        [Test]
        public void TargetController_CheckBoundaryWrap_TeleportsToOppositeEdgeInHorizontalMode()
        {
            float extentsX = targetController.SpriteRenderer.bounds.extents.x;
            float extentsY = targetController.SpriteRenderer.bounds.extents.y;
            float buffer = targetController.BoundaryBuffer;

            // Place target completely past left origin boundary (behind Object A)
            targetGo.transform.position = new Vector3(viewportManager.MinX - extentsX - buffer - 1f, 0f, 0f);
            targetController.CheckBoundaryWrap();

            // Must now be placed at the right boundary
            Assert.That(targetGo.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MaxX + extentsX + buffer - 0.01f));

            // Perpendicular Y coordinate must be randomized within visible bounds
            Assert.That(targetGo.transform.position.y, Is.GreaterThanOrEqualTo(viewportManager.MinY + extentsY - 0.01f));
            Assert.That(targetGo.transform.position.y, Is.LessThanOrEqualTo(viewportManager.MaxY - extentsY + 0.01f));
        }

        [Test]
        public void TargetController_CheckBoundaryWrap_TeleportsToOppositeEdgeInVerticalMode()
        {
            targetController.IsHorizontal = false;
            float extentsY = targetController.SpriteRenderer.bounds.extents.y;
            float extentsX = targetController.SpriteRenderer.bounds.extents.x;
            float buffer = targetController.BoundaryBuffer;

            // In vertical mode, Object B drifts UP towards Mid-Top origin where Object A starts.
            // When exiting past the top boundary:
            targetGo.transform.position = new Vector3(0f, viewportManager.MaxY + extentsY + buffer + 1f, 0f);
            targetController.CheckBoundaryWrap();

            // Must wrap to the bottom boundary
            Assert.That(targetGo.transform.position.y, Is.LessThanOrEqualTo(viewportManager.MinY - extentsY - buffer + 0.01f));

            // Perpendicular X coordinate must be randomized within visible bounds
            Assert.That(targetGo.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MinX + extentsX - 0.01f));
            Assert.That(targetGo.transform.position.x, Is.LessThanOrEqualTo(viewportManager.MaxX - extentsX + 0.01f));
        }

        [Test]
        public void TargetController_OnTriggerEnter2D_HandlesProjectileHitAndRespawns()
        {
            GameObject projGo = new GameObject("ProjectileTestObject");
            disposables.Add(projGo);
            Collider2D projCol = projGo.AddComponent<CircleCollider2D>();
            projGo.AddComponent<Projectile>();

            targetGo.transform.position = new Vector3(0f, 0f, 0f);
            
            // Pass collider directly to OnTriggerEnter2D
            targetController.OnTriggerEnter2D(projCol);

            // Projectile must be destroyed immediately in EditMode
            Assert.That(projGo == null || !projGo, Is.True);

            // Target must have respawned at opposite right edge
            Assert.That(targetGo.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MaxX));
        }

        [Test]
        public void TargetController_AnchorPerpendicular_SynchronizesOnModeAndTeleport()
        {
            targetController.TeleportTo(new Vector3(2f, 3f, 0f), resetAnchor: true);
            Assert.That(targetController.AnchorPerpendicular, Is.EqualTo(3f).Within(0.001f));

            // Switch to vertical: anchor should switch to X coordinate
            targetController.IsHorizontal = false;
            Assert.That(targetController.AnchorPerpendicular, Is.EqualTo(2f).Within(0.001f));
        }
    }
}
