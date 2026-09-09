using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Combat;

namespace KinematicsGame.Tests
{
    public class PlayerControllerTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager viewportManager;
        private GameObject playerGo;
        private PlayerController playerController;
        private GameObject projectilePrefab;
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

            // Create projectile prefab dummy
            projectilePrefab = new GameObject("ProjectilePrefab");
            disposables.Add(projectilePrefab);
            projectilePrefab.AddComponent<Rigidbody2D>();
            projectilePrefab.AddComponent<CircleCollider2D>();
            projectilePrefab.AddComponent<Projectile>();

            // Create Player
            playerGo = new GameObject("Player");
            disposables.Add(playerGo);
            playerController = playerGo.AddComponent<PlayerController>();
            SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(100, 100);
            disposables.Add(tex);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100f);
            playerController.SpriteRenderer = sr;
            playerController.ProjectilePrefab = projectilePrefab;
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
        public void PlayerController_ClampsPosition_StrictlyInsideScreenBounds()
        {
            float extentsX = playerController.SpriteRenderer.bounds.extents.x; // 0.5f
            float extentsY = playerController.SpriteRenderer.bounds.extents.y; // 0.5f

            // Move way past right boundary
            playerGo.transform.position = new Vector3(100f, 0f, 0f);
            playerController.ClampToScreenBounds();
            Assert.That(playerGo.transform.position.x, Is.EqualTo(viewportManager.MaxX - extentsX).Within(0.01f));

            // Move way past left boundary
            playerGo.transform.position = new Vector3(-100f, 0f, 0f);
            playerController.ClampToScreenBounds();
            Assert.That(playerGo.transform.position.x, Is.EqualTo(viewportManager.MinX + extentsX).Within(0.01f));

            // Move way past top boundary
            playerGo.transform.position = new Vector3(0f, 100f, 0f);
            playerController.ClampToScreenBounds();
            Assert.That(playerGo.transform.position.y, Is.EqualTo(viewportManager.MaxY - extentsY).Within(0.01f));

            // Move way past bottom boundary
            playerGo.transform.position = new Vector3(0f, -100f, 0f);
            playerController.ClampToScreenBounds();
            Assert.That(playerGo.transform.position.y, Is.EqualTo(viewportManager.MinY + extentsY).Within(0.01f));
        }

        [Test]
        public void PlayerController_FireProjectile_InstantiatesWithConfiguredKinematics()
        {
            playerGo.transform.position = new Vector3(2f, 1f, 0f);
            playerController.ProjectileDirection = Vector2.right;
            playerController.ProjectileSpeed = 15f;

            playerController.FireProjectile();

            Projectile[] spawned = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            Assert.That(spawned.Length, Is.GreaterThan(0));

            // Clean up spawned projectiles in test
            foreach (var p in spawned)
            {
                if (p.gameObject != projectilePrefab)
                {
                    Assert.That(p.Direction, Is.EqualTo(Vector2.right));
                    Assert.That(p.Speed, Is.EqualTo(15f));
                    Object.DestroyImmediate(p.gameObject);
                }
            }
        }

        [Test]
        public void PlayerController_FireCooldown_PreventsSpam()
        {
            playerController.FireProjectile();
            int countAfterFirst = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;

            // Immediate second call should be blocked by cooldown
            playerController.FireProjectile();
            int countAfterSecond = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;

            Assert.That(countAfterSecond, Is.EqualTo(countAfterFirst));

            // Clean up
            foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if (p.gameObject != projectilePrefab)
                {
                    Object.DestroyImmediate(p.gameObject);
                }
            }
        }

        [Test]
        public void PlayerController_DiagonalMovement_ClampsMagnitude()
        {
            playerController.SetMoveInput(new Vector2(1f, 1f));
            Assert.That(playerController.MoveInput.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PlayerController_NullPrefab_DoesNotThrow()
        {
            playerController.ProjectilePrefab = null;
            Assert.DoesNotThrow(() => playerController.FireProjectile());
        }

        [Test]
        public void PlayerController_FireVolume_DefaultsToReducedLevel_AndClampsWithinZeroToOne()
        {
            Assert.That(playerController.FireVolume, Is.EqualTo(0.25f).Within(0.01f));

            playerController.FireVolume = 0.5f;
            Assert.That(playerController.FireVolume, Is.EqualTo(0.5f).Within(0.01f));

            playerController.FireVolume = 1.5f;
            Assert.That(playerController.FireVolume, Is.EqualTo(1.0f).Within(0.01f));

            playerController.FireVolume = -0.5f;
            Assert.That(playerController.FireVolume, Is.EqualTo(0.0f).Within(0.01f));
        }
    }
}
