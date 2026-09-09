using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Combat;
using KinematicsGame.Enemy;

namespace KinematicsGame.Tests
{
    [TestFixture]
    public class ScoreManagerTests
    {
        private GameObject scoreManagerGameObject;
        private ScoreManager scoreManager;
        private TargetProfile defaultProfile;
        private TargetProfile rareProfile;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(ScoreManager.HighScoreKey);
            PlayerPrefs.Save();

            ScoreManager.ClearEventSubscribers();
            TargetController.ClearEventSubscribers();

            scoreManagerGameObject = new GameObject("ScoreManager_Test");
            scoreManager = scoreManagerGameObject.AddComponent<ScoreManager>();
            scoreManager.Initialize();

            defaultProfile = new TargetProfile(
                "Common Pigeon",
                null,
                speedMult: 1.0f,
                freqMult: 1.0f,
                ampMult: 1.0f,
                points: 10,
                weight: 50f
            );

            rareProfile = new TargetProfile(
                "Rare Golden Eagle",
                null,
                speedMult: 2.2f,
                freqMult: 0.0f,
                ampMult: 0.0f,
                points: 100,
                weight: 10f
            );
        }

        [TearDown]
        public void TearDown()
        {
            ScoreManager.ClearEventSubscribers();
            TargetController.ClearEventSubscribers();

            PlayerPrefs.DeleteKey(ScoreManager.HighScoreKey);
            PlayerPrefs.Save();

            if (scoreManagerGameObject != null)
            {
                Object.DestroyImmediate(scoreManagerGameObject);
            }
        }

        [Test]
        public void Hit_IncrementsScore_ByPointValueMultipliedByCombo()
        {
            int receivedScore = 0;
            ScoreManager.OnScoreChanged += s => receivedScore = s;

            // First hit at 1x combo
            scoreManager.ProcessHit(defaultProfile, Vector3.zero, currentTime: 0f);

            Assert.AreEqual(10, scoreManager.CurrentScore);
            Assert.AreEqual(10, receivedScore);
            Assert.AreEqual(2, scoreManager.ComboMultiplier); // Incremented for next hit

            // Second hit at 2x combo
            scoreManager.ProcessHit(defaultProfile, Vector3.zero, currentTime: 0.5f);

            // 10 + (10 * 2) = 30
            Assert.AreEqual(30, scoreManager.CurrentScore);
            Assert.AreEqual(30, receivedScore);
            Assert.AreEqual(3, scoreManager.ComboMultiplier);
        }

        [Test]
        public void ConsecutiveHits_RampComboMultiplierSequentially()
        {
            List<int> comboHistory = new List<int>();
            ScoreManager.OnComboChanged += (multiplier, ratio) => comboHistory.Add(multiplier);

            // Hit 1: multiplier was 1, becomes 2
            scoreManager.ProcessHit(defaultProfile, Vector3.zero, 0.0f);
            Assert.AreEqual(2, scoreManager.ComboMultiplier);

            // Hit 2: multiplier was 2, becomes 3
            scoreManager.ProcessHit(defaultProfile, Vector3.zero, 0.4f);
            Assert.AreEqual(3, scoreManager.ComboMultiplier);

            // Hit 3: multiplier was 3, becomes 4
            scoreManager.ProcessHit(defaultProfile, Vector3.zero, 0.8f);
            Assert.AreEqual(4, scoreManager.ComboMultiplier);

            Assert.Contains(2, comboHistory);
            Assert.Contains(3, comboHistory);
            Assert.Contains(4, comboHistory);
        }

        [Test]
        public void ComboDecay_ResetsMultiplierToOne_WhenTimerExpires()
        {
            scoreManager.ProcessHit(defaultProfile, Vector3.zero, 0f);
            Assert.AreEqual(2, scoreManager.ComboMultiplier);
            Assert.AreEqual(scoreManager.ComboDuration, scoreManager.ComboTimer, 0.001f);

            // Advance time by 1.0s (partial decay, still active)
            scoreManager.UpdateCombo(1.0f);
            Assert.AreEqual(2, scoreManager.ComboMultiplier);
            Assert.AreEqual(1.0f, scoreManager.ComboTimer, 0.001f);

            // Advance time past 2.0s threshold
            bool resetFired = false;
            ScoreManager.OnComboChanged += (multiplier, ratio) =>
            {
                if (multiplier == 1 && ratio == 0f) resetFired = true;
            };

            scoreManager.UpdateCombo(1.1f);
            Assert.AreEqual(1, scoreManager.ComboMultiplier);
            Assert.AreEqual(0f, scoreManager.ComboTimer, 0.001f);
            Assert.IsTrue(resetFired, "OnComboChanged should fire reset with (1, 0f)");
        }

        [Test]
        public void HighScore_UpdatesAndPersistsViaPlayerPrefs()
        {
            int receivedHighScore = 0;
            ScoreManager.OnHighScoreChanged += hs => receivedHighScore = hs;

            scoreManager.ProcessHit(rareProfile, Vector3.zero, 0f); // 100 points
            Assert.AreEqual(100, scoreManager.HighScore);
            Assert.AreEqual(100, receivedHighScore);
            Assert.AreEqual(100, PlayerPrefs.GetInt(ScoreManager.HighScoreKey, 0));

            // Another instance should load persisted high score
            GameObject anotherGo = new GameObject("ScoreManager_Second");
            ScoreManager anotherScoreMgr = anotherGo.AddComponent<ScoreManager>();
            anotherScoreMgr.Initialize();

            Assert.AreEqual(100, anotherScoreMgr.HighScore);
            Object.DestroyImmediate(anotherGo);
        }

        [Test]
        public void AudioThrottlingGate_SuppressesSoundsWithin50msWindow()
        {
            // First explosion at t=0.000s -> Allowed
            bool canPlay1 = scoreManager.CanPlayExplosion(0.000f);
            Assert.IsTrue(canPlay1);
            Assert.AreEqual(1, scoreManager.ExplosionSoundPlayCount);

            // Rapid second hit at t=0.030s (< 50ms) -> Throttled
            bool canPlay2 = scoreManager.CanPlayExplosion(0.030f);
            Assert.IsFalse(canPlay2);
            Assert.AreEqual(1, scoreManager.ExplosionSoundPlayCount);

            // Another rapid hit at t=0.049s (< 50ms) -> Throttled
            bool canPlay3 = scoreManager.CanPlayExplosion(0.049f);
            Assert.IsFalse(canPlay3);
            Assert.AreEqual(1, scoreManager.ExplosionSoundPlayCount);

            // Hit at t=0.051s (>= 50ms) -> Allowed
            bool canPlay4 = scoreManager.CanPlayExplosion(0.051f);
            Assert.IsTrue(canPlay4);
            Assert.AreEqual(2, scoreManager.ExplosionSoundPlayCount);
        }

        [Test]
        public void TargetController_OnTargetHit_AutomaticallyNotifiesScoreManager()
        {
            // In EditMode, Awake/OnEnable ordering may differ from PlayMode,
            // so explicitly re-subscribe to guarantee the event link is live.
            scoreManager.SubscribeEvents();

            GameObject targetGo = new GameObject("TestTarget");
            targetGo.AddComponent<SpriteRenderer>();
            targetGo.AddComponent<CircleCollider2D>();
            TargetController target = targetGo.AddComponent<TargetController>();
            target.Initialize(rareProfile, horizontal: true, 3f, 2f, 1.5f);

            // projGo is destroyed inside HandleHitByProjectile (DestroyImmediate in EditMode)
            GameObject projGo = new GameObject("TestProjectile");
            projGo.AddComponent<CircleCollider2D>();
            projGo.AddComponent<Projectile>();

            Assert.AreEqual(0, scoreManager.CurrentScore);

            // Trigger hit through TargetController – fires OnTargetHit → ScoreManager.HandleTargetHit
            target.HandleHitByProjectile(projGo);

            Assert.AreEqual(100, scoreManager.CurrentScore);
            Assert.AreEqual(2, scoreManager.ComboMultiplier);

            Object.DestroyImmediate(targetGo);
            // projGo was already destroyed by HandleHitByProjectile in EditMode
        }

        [Test]
        public void ResetScore_ClearsCurrentScoreAndCombo_RetainingHighScore()
        {
            scoreManager.ProcessHit(rareProfile, Vector3.zero, 0f);
            Assert.AreEqual(100, scoreManager.CurrentScore);
            Assert.AreEqual(100, scoreManager.HighScore);

            scoreManager.ResetScore();

            Assert.AreEqual(0, scoreManager.CurrentScore);
            Assert.AreEqual(1, scoreManager.ComboMultiplier);
            Assert.AreEqual(100, scoreManager.HighScore);
        }

        [Test]
        public void ExplosionVolume_DefaultsToReducedLevel_AndClampsWithinZeroToOne()
        {
            Assert.AreEqual(0.25f, scoreManager.ExplosionVolume, 0.01f);

            scoreManager.ExplosionVolume = 0.5f;
            Assert.AreEqual(0.5f, scoreManager.ExplosionVolume, 0.01f);

            scoreManager.ExplosionVolume = 1.5f;
            Assert.AreEqual(1.0f, scoreManager.ExplosionVolume, 0.01f);

            scoreManager.ExplosionVolume = -0.5f;
            Assert.AreEqual(0.0f, scoreManager.ExplosionVolume, 0.01f);
        }

        [Test]
        public void SfxVolume_DefaultsToReducedLevel_AndClampsWithinZeroToOne()
        {
            Assert.AreEqual(0.25f, scoreManager.SfxVolume, 0.01f);

            scoreManager.SfxVolume = 0.5f;
            Assert.AreEqual(0.5f, scoreManager.SfxVolume, 0.01f);

            scoreManager.SfxVolume = 1.5f;
            Assert.AreEqual(1.0f, scoreManager.SfxVolume, 0.01f);

            scoreManager.SfxVolume = -0.5f;
            Assert.AreEqual(0.0f, scoreManager.SfxVolume, 0.01f);
        }
    }
}
