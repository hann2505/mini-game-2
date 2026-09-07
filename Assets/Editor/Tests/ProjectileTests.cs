using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Combat;

namespace KinematicsGame.Tests
{
    public class ProjectileTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager viewportManager;
        private GameObject projectileGo;
        private Projectile projectile;
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

            projectileGo = new GameObject("TestProjectile");
            disposables.Add(projectileGo);
            projectileGo.AddComponent<CircleCollider2D>();
            projectile = projectileGo.AddComponent<Projectile>();
            SpriteRenderer sr = projectileGo.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(32, 32);
            disposables.Add(tex);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
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
        public void Projectile_InitializesDirectionAndSpeed()
        {
            projectile.Initialize(new Vector2(1f, 1f), 20f);

            Vector2 expectedDir = new Vector2(1f, 1f).normalized;
            Assert.That(projectile.Direction.x, Is.EqualTo(expectedDir.x).Within(0.001f));
            Assert.That(projectile.Direction.y, Is.EqualTo(expectedDir.y).Within(0.001f));
            Assert.That(projectile.Speed, Is.EqualTo(20f));
        }

        [Test]
        public void Projectile_HasKinematicRigidbodyAndTriggerCollider()
        {
            Rigidbody2D rb = projectileGo.GetComponent<Rigidbody2D>();
            Assert.That(rb, Is.Not.Null);
            Assert.That(rb.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));

            Collider2D col = projectileGo.GetComponent<Collider2D>();
            Assert.That(col, Is.Not.Null);
            Assert.That(col.isTrigger, Is.True);
        }

        [Test]
        public void Projectile_CullsWhenExceedingViewportWithExitBuffer()
        {
            // Inside bounds: should remain active
            projectileGo.transform.position = Vector3.zero;
            projectile.CheckViewportBounds();
            Assert.That(projectileGo != null && projectileGo, Is.True);

            // Move outside viewport + buffer: must be destroyed
            projectileGo.transform.position = new Vector3(viewportManager.MaxX + 5f, 0f, 0f);
            projectile.CheckViewportBounds();
            Assert.That(projectileGo == null || !projectileGo, Is.True, "Projectile must be destroyed when outside viewport + exitBuffer");
        }
    }
}
