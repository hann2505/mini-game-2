using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;
using KinematicsGame.UI;
using TMPro;

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
            TargetController.ClearEventSubscribers();

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

        [Test]
        public void TargetController_ApplyProfile_CalculatesEffectiveKinematicsWithoutCompounding()
        {
            targetController.BaseSpeed = 4f;
            targetController.WaveFrequency = 2f;
            targetController.WaveAmplitude = 1.5f;

            TargetProfile profile = new TargetProfile("Hummingbird", null, 1.5f, 2.5f, 0.8f, 30, 25f);
            targetController.ApplyProfile(profile);

            Assert.That(targetController.CurrentProfile, Is.EqualTo(profile));
            Assert.That(targetController.BaseSpeed, Is.EqualTo(4f));
            Assert.That(targetController.EffectiveSpeed, Is.EqualTo(6f).Within(0.001f));
            Assert.That(targetController.EffectiveWaveFrequency, Is.EqualTo(5f).Within(0.001f));
            Assert.That(targetController.EffectiveWaveAmplitude, Is.EqualTo(1.2f).Within(0.001f));

            // Re-apply same profile (simulate pool recycling): Base values MUST NOT compound
            targetController.ApplyProfile(profile);
            Assert.That(targetController.BaseSpeed, Is.EqualTo(4f));
            Assert.That(targetController.EffectiveSpeed, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void TargetController_OnTargetHit_FiresWithProfileAndImpactPosition()
        {
            TargetProfile profile = new TargetProfile("GoldenEagle", null, 2.2f, 0f, 0f, 100, 10f);
            targetController.ApplyProfile(profile);
            targetGo.transform.position = new Vector3(3f, 2f, 0f);

            bool eventFired = false;
            TargetController hitTarget = null;
            TargetProfile hitProfile = null;
            Vector3 hitPos = Vector3.zero;

            TargetController.OnTargetHit += (t, p, pos) =>
            {
                eventFired = true;
                hitTarget = t;
                hitProfile = p;
                hitPos = pos;
            };

            GameObject projGo = new GameObject("TestProjectile");
            disposables.Add(projGo);
            Collider2D projCol = projGo.AddComponent<CircleCollider2D>();
            projGo.AddComponent<Projectile>();

            targetController.OnTriggerEnter2D(projCol);

            Assert.That(eventFired, Is.True);
            Assert.That(hitTarget, Is.EqualTo(targetController));
            Assert.That(hitProfile, Is.EqualTo(profile));
            Assert.That(hitPos.x, Is.EqualTo(3f).Within(0.01f));
            Assert.That(hitPos.y, Is.EqualTo(2f).Within(0.01f));
        }

        [Test]
        public void TargetController_IsPooled_DeactivatesOnProjectileHit()
        {
            targetController.IsPooled = true;
            targetGo.transform.position = new Vector3(0f, 0f, 0f);

            GameObject projGo = new GameObject("TestProjectile");
            disposables.Add(projGo);
            Collider2D projCol = projGo.AddComponent<CircleCollider2D>();
            projGo.AddComponent<Projectile>();

            targetController.OnTriggerEnter2D(projCol);

            // In pooled mode, object must be deactivated (not respawned at opposite edge)
            Assert.That(targetGo.activeSelf, Is.False);
        }

        [Test]
        public void TargetController_IsPooled_DeactivatesOnBoundaryWrap()
        {
            targetController.IsPooled = true;
            float extentsX = targetController.SpriteRenderer.bounds.extents.x;
            float buffer = targetController.BoundaryBuffer;

            // Place past exit threshold
            targetGo.transform.position = new Vector3(viewportManager.MinX - extentsX - buffer - 1f, 0f, 0f);
            targetController.CheckBoundaryWrap();

            // In pooled mode, object must be deactivated (not respawned at opposite edge)
            Assert.That(targetGo.activeSelf, Is.False);
        }
    }

    [TestFixture]
    public class TargetSpawnerTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager viewportManager;
        private GameObject spawnerGo;
        private TargetSpawner spawner;
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

            spawnerGo = new GameObject("TestSpawner");
            disposables.Add(spawnerGo);
            spawner = spawnerGo.AddComponent<TargetSpawner>();
            spawner.AutoSpawn = false;
            spawner.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            TargetController.ClearEventSubscribers();

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
        public void TargetSpawner_InitializePool_PrewarmsExactly15InstancesWithIsPooled()
        {
            Assert.That(spawner.Pool, Is.Not.Null);
            Assert.That(spawner.Pool.Length, Is.EqualTo(15));
            Assert.That(spawner.ActiveTargetCount, Is.EqualTo(0));

            for (int i = 0; i < spawner.Pool.Length; i++)
            {
                TargetController target = spawner.Pool[i];
                Assert.That(target, Is.Not.Null);
                Assert.That(target.IsPooled, Is.True);
                Assert.That(target.gameObject.activeSelf, Is.False);
            }
        }

        [Test]
        public void TargetSpawner_UpdateDifficulty_ScalesQuotaAndSpawnInterval()
        {
            spawner.UpdateDifficulty(0);
            Assert.That(spawner.CurrentQuota, Is.EqualTo(2));
            Assert.That(spawner.CurrentSpawnInterval, Is.EqualTo(2.0f).Within(0.01f));

            spawner.UpdateDifficulty(500);
            Assert.That(spawner.CurrentQuota, Is.EqualTo(12));
            Assert.That(spawner.CurrentSpawnInterval, Is.EqualTo(1.325f).Within(0.01f));

            spawner.UpdateDifficulty(1500);
            Assert.That(spawner.CurrentQuota, Is.EqualTo(15));
            Assert.That(spawner.CurrentSpawnInterval, Is.EqualTo(0.65f).Within(0.01f));
        }

        [Test]
        public void TargetSpawner_SpawnTarget_AllocatesDistinctLanesWithoutOverlap()
        {
            TargetController t1 = spawner.SpawnTarget();
            TargetController t2 = spawner.SpawnTarget();

            Assert.That(t1, Is.Not.Null);
            Assert.That(t2, Is.Not.Null);
            Assert.That(t1, Is.Not.EqualTo(t2));

            Assert.That(t1.gameObject.activeSelf, Is.True);
            Assert.That(t2.gameObject.activeSelf, Is.True);
            Assert.That(spawner.ActiveTargetCount, Is.EqualTo(2));

            Assert.That(t1.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MaxX));
            Assert.That(t2.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MaxX));

            float yDiff = Mathf.Abs(t1.transform.position.y - t2.transform.position.y);
            Assert.That(yDiff, Is.GreaterThan(0.5f), "Successive spawns must allocate different lanes without clumping");
        }

        [Test]
        public void TargetSpawner_UpdateSpawner_RespectsActiveQuota()
        {
            spawner.AutoSpawn = true;
            spawner.UpdateDifficulty(0);

            for (int i = 0; i < 20; i++)
            {
                spawner.UpdateSpawner(0.5f);
            }

            Assert.That(spawner.ActiveTargetCount, Is.EqualTo(2));
        }

        [Test]
        public void TargetSpawner_SetOrientation_SwitchesAxisAndRealignsTargets()
        {
            TargetController t1 = spawner.SpawnTarget();
            Assert.That(t1.IsHorizontal, Is.True);
            Assert.That(t1.transform.rotation.eulerAngles.z, Is.EqualTo(0f).Within(0.1f));

            spawner.SetOrientation(GameOrientation.Vertical);

            Assert.That(spawner.CurrentOrientation, Is.EqualTo(GameOrientation.Vertical));
            Assert.That(t1.IsHorizontal, Is.False);
            Assert.That(t1.transform.rotation.eulerAngles.z, Is.EqualTo(90f).Within(0.1f));

            Assert.That(t1.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MinX - 0.01f));
            Assert.That(t1.transform.position.x, Is.LessThanOrEqualTo(viewportManager.MaxX + 0.01f));
            Assert.That(t1.transform.position.y, Is.GreaterThanOrEqualTo(viewportManager.MinY - 0.01f));
            Assert.That(t1.transform.position.y, Is.LessThanOrEqualTo(viewportManager.MaxY + 0.01f));
        }

        [Test]
        public void TargetSpawner_PoolReusesInactiveInstancesWithoutNewAllocations()
        {
            TargetController t1 = spawner.SpawnTarget();
            TargetController t2 = spawner.SpawnTarget();
            Assert.That(spawner.ActiveTargetCount, Is.EqualTo(2));

            t1.gameObject.SetActive(false);
            Assert.That(spawner.ActiveTargetCount, Is.EqualTo(1));

            TargetController reused = spawner.SpawnTarget();
            Assert.That(reused, Is.EqualTo(t1));
            Assert.That(reused.gameObject.activeSelf, Is.True);
            Assert.That(spawner.ActiveTargetCount, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class EndToEndScoringIntegrationTests
    {
        private GameObject rootGo;
        private ViewportManager viewportManager;
        private ScoreManager scoreManager;
        private TargetSpawner targetSpawner;
        private FloatingTextPool fctPool;
        private GameHUDController hudController;

        private GameObject scoreLabelGo, highScoreLabelGo, comboLabelGo;
        private TextMeshProUGUI scoreLabel, highScoreLabel, comboLabel;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(ScoreManager.HighScoreKey);
            PlayerPrefs.Save();

            ScoreManager.ClearEventSubscribers();
            TargetController.ClearEventSubscribers();

            rootGo = new GameObject("E2E_TestRoot");

            // 1. ViewportManager
            GameObject vmGo = new GameObject("ViewportManager");
            vmGo.transform.SetParent(rootGo.transform);
            viewportManager = vmGo.AddComponent<ViewportManager>();
            viewportManager.Initialize();

            // 2. ScoreManager
            GameObject smGo = new GameObject("ScoreManager");
            smGo.transform.SetParent(rootGo.transform);
            scoreManager = smGo.AddComponent<ScoreManager>();
            scoreManager.Initialize();

            // 3. FloatingTextPool
            GameObject fctGo = new GameObject("FloatingTextPool");
            fctGo.transform.SetParent(rootGo.transform);
            fctPool = fctGo.AddComponent<FloatingTextPool>();
            fctPool.Initialize();

            // 4. TargetSpawner
            GameObject spawnerGo = new GameObject("TargetSpawner");
            spawnerGo.transform.SetParent(rootGo.transform);
            targetSpawner = spawnerGo.AddComponent<TargetSpawner>();
            targetSpawner.Initialize();

            // 5. GameHUDController
            GameObject hudGo = new GameObject("GameHUD");
            hudGo.transform.SetParent(rootGo.transform);
            hudController = hudGo.AddComponent<GameHUDController>();

            scoreLabelGo = new GameObject("ScoreLabel");
            scoreLabelGo.transform.SetParent(hudGo.transform);
            scoreLabel = scoreLabelGo.AddComponent<TextMeshProUGUI>();

            highScoreLabelGo = new GameObject("HighScoreLabel");
            highScoreLabelGo.transform.SetParent(hudGo.transform);
            highScoreLabel = highScoreLabelGo.AddComponent<TextMeshProUGUI>();

            comboLabelGo = new GameObject("ComboLabel");
            comboLabelGo.transform.SetParent(hudGo.transform);
            comboLabel = comboLabelGo.AddComponent<TextMeshProUGUI>();

            hudController.SetLabels(scoreLabel, highScoreLabel, comboLabel);

            // Re-subscribe events for explicit EditMode deterministic binding
            scoreManager.SubscribeEvents();
            fctPool.SubscribeEvents();
            hudController.SubscribeEvents();
        }

        [TearDown]
        public void TearDown()
        {
            ScoreManager.ClearEventSubscribers();
            TargetController.ClearEventSubscribers();

            PlayerPrefs.DeleteKey(ScoreManager.HighScoreKey);
            PlayerPrefs.Save();

            if (rootGo != null)
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void EndToEnd_ProjectileHitPooledTarget_AdvancesScoreComboAndFCT()
        {
            Assert.That(scoreManager.CurrentScore, Is.EqualTo(0));
            Assert.That(scoreManager.ComboMultiplier, Is.EqualTo(1));
            Assert.That(fctPool.ActiveCount, Is.EqualTo(0));
            Assert.That(targetSpawner.ActiveTargetCount, Is.EqualTo(0));

            // Spawn first target from pool
            TargetController target1 = targetSpawner.SpawnTarget();
            Assert.That(target1, Is.Not.Null);
            Assert.That(target1.gameObject.activeSelf, Is.True);
            Assert.That(target1.IsPooled, Is.True);
            Assert.That(targetSpawner.ActiveTargetCount, Is.EqualTo(1));

            int basePoints1 = target1.CurrentProfile != null ? target1.CurrentProfile.PointValue : 10;

            // Simulate projectile collision
            GameObject bullet1 = new GameObject("Bullet1");
            bullet1.AddComponent<CircleCollider2D>();
            bullet1.AddComponent<Projectile>();

            target1.HandleHitByProjectile(bullet1);

            // Target should be deactivated back into pool
            Assert.That(target1.gameObject.activeSelf, Is.False);
            Assert.That(targetSpawner.ActiveTargetCount, Is.EqualTo(0));

            // Score and Combo should advance
            Assert.That(scoreManager.CurrentScore, Is.EqualTo(basePoints1));
            Assert.That(scoreManager.ComboMultiplier, Is.EqualTo(2));
            Assert.That(hudController.DisplayedScore, Is.EqualTo(basePoints1));

            // Floating combat text should be spawned
            Assert.That(fctPool.ActiveCount, Is.EqualTo(1));

            // Spawn second target and hit with 2x multiplier
            TargetController target2 = targetSpawner.SpawnTarget();
            Assert.That(target2, Is.Not.Null);
            int basePoints2 = target2.CurrentProfile != null ? target2.CurrentProfile.PointValue : 10;

            GameObject bullet2 = new GameObject("Bullet2");
            bullet2.AddComponent<CircleCollider2D>();
            bullet2.AddComponent<Projectile>();

            target2.HandleHitByProjectile(bullet2);

            // Cumulative score = basePoints1 + (basePoints2 * 2)
            int expectedTotal = basePoints1 + (basePoints2 * 2);
            Assert.That(scoreManager.CurrentScore, Is.EqualTo(expectedTotal));
            Assert.That(scoreManager.ComboMultiplier, Is.EqualTo(3));
            Assert.That(hudController.DisplayedScore, Is.EqualTo(expectedTotal));
            Assert.That(fctPool.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void EndToEnd_OrientationSwitchMidGame_RealignsAllActiveTargets()
        {
            targetSpawner.SetOrientation(GameOrientation.Horizontal);

            TargetController t0 = targetSpawner.SpawnTarget();
            TargetController t1 = targetSpawner.SpawnTarget();
            TargetController t2 = targetSpawner.SpawnTarget();

            Assert.That(t0, Is.Not.Null);
            Assert.That(t1, Is.Not.Null);
            Assert.That(t2, Is.Not.Null);
            Assert.That(t0.IsHorizontal, Is.True);
            Assert.That(t1.IsHorizontal, Is.True);
            Assert.That(t2.IsHorizontal, Is.True);

            // Switch to Vertical orientation mid-game
            targetSpawner.SetOrientation(GameOrientation.Vertical);

            Assert.That(t0.IsHorizontal, Is.False);
            Assert.That(t1.IsHorizontal, Is.False);
            Assert.That(t2.IsHorizontal, Is.False);

            Assert.That(t0.transform.eulerAngles.z, Is.EqualTo(90f).Within(0.5f));
            Assert.That(t1.transform.eulerAngles.z, Is.EqualTo(90f).Within(0.5f));
            Assert.That(t2.transform.eulerAngles.z, Is.EqualTo(90f).Within(0.5f));

            Assert.That(t0.transform.position.x, Is.GreaterThanOrEqualTo(viewportManager.MinX - 1f));
            Assert.That(t0.transform.position.x, Is.LessThanOrEqualTo(viewportManager.MaxX + 1f));
        }

        [Test]
        public void EndToEnd_ComboExpiration_DecaysHUDAndMultiplier()
        {
            TargetController target = targetSpawner.SpawnTarget();
            GameObject bullet = new GameObject("Bullet");
            bullet.AddComponent<CircleCollider2D>();
            bullet.AddComponent<Projectile>();

            target.HandleHitByProjectile(bullet);

            Assert.That(scoreManager.ComboMultiplier, Is.EqualTo(2));
            Assert.That(hudController.DisplayedCombo, Is.EqualTo(2));
            Assert.That(comboLabel.gameObject.activeSelf, Is.True);

            // Advance combo timer past 2.0s
            scoreManager.UpdateCombo(2.5f);

            Assert.That(scoreManager.ComboMultiplier, Is.EqualTo(1));
            Assert.That(hudController.DisplayedCombo, Is.EqualTo(1));
            Assert.That(comboLabel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void EndToEnd_DifficultyScaling_ExpandsActiveTargetQuotaAsScoreRises()
        {
            Assert.That(targetSpawner.CurrentQuota, Is.EqualTo(2));

            targetSpawner.UpdateDifficulty(150);
            Assert.That(targetSpawner.CurrentQuota, Is.EqualTo(4));

            targetSpawner.UpdateDifficulty(200);
            Assert.That(targetSpawner.CurrentQuota, Is.EqualTo(6));

            targetSpawner.UpdateDifficulty(1200);
            Assert.That(targetSpawner.CurrentQuota, Is.EqualTo(15));
        }
    }
}