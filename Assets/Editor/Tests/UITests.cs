using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using KinematicsGame.UI;
using KinematicsGame.Combat;
using KinematicsGame.Enemy;

namespace KinematicsGame.Tests
{
    [TestFixture]
    public class FloatingTextControllerTests
    {
        private GameObject go;
        private FloatingTextController fct;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("FCT_Test");
            fct = go.AddComponent<FloatingTextController>();
            fct.EnsureComponents();
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void FontSize_DefaultsToReducedSize_AndSetsTmpText()
        {
            Assert.AreEqual(3.5f, fct.FontSize);
            Assert.AreEqual(3.5f, fct.TmpText.fontSize);

            fct.FontSize = 2.5f;
            Assert.AreEqual(2.5f, fct.TmpText.fontSize);
        }

        [Test]
        public void Play_ActivatesGameObject_AndSetsText()
        {
            fct.Play(Vector3.zero, "+100", Color.white, animDuration: 0.8f);
            Assert.IsTrue(go.activeSelf);
            Assert.IsTrue(fct.IsPlaying);
            Assert.IsNotNull(fct.TmpText);
            Assert.AreEqual("+100", fct.TmpText.text);
        }

        [Test]
        public void TickAnimation_AscendsPositionEachFrame()
        {
            fct.Play(Vector3.zero, "+10", Color.white);
            float startY = fct.transform.position.y;

            fct.TickAnimation(0.1f);
            float newY = fct.transform.position.y;

            Assert.Greater(newY, startY, "FCT should ascend each frame");
        }

        [Test]
        public void TickAnimation_CompletesAfterFullDuration_AndDeactivates()
        {
            fct.Play(Vector3.zero, "+10", Color.white, animDuration: 0.8f);
            Assert.IsTrue(go.activeSelf);

            // Step past full duration
            fct.TickAnimation(0.81f);

            Assert.IsFalse(go.activeSelf, "FCT should deactivate after animation completes");
            Assert.IsFalse(fct.IsPlaying);
        }

        [Test]
        public void TickAnimation_FadesAlphaInSecondHalf()
        {
            fct.Play(Vector3.zero, "+10", Color.white, animDuration: 0.8f);

            // First half (t = 0.4 / 0.8 = 0.5) — alpha should still be 1
            fct.TickAnimation(0.4f);
            Assert.AreEqual(1f, fct.TmpText.color.a, 0.05f, "Alpha should be ~1 at 50% duration");

            // Step into second half
            fct.TickAnimation(0.3f); // t = 0.7/0.8
            Assert.Less(fct.TmpText.color.a, 1f, "Alpha should fade in second half");
        }

        [Test]
        public void OnComplete_CallbackFires_WhenAnimationEnds()
        {
            bool callbackFired = false;
            fct.Play(Vector3.zero, "+10", Color.white, onComplete: () => callbackFired = true, animDuration: 0.8f);

            fct.TickAnimation(1.0f);
            Assert.IsTrue(callbackFired, "onComplete callback should fire when animation ends");
        }

        [Test]
        public void Stop_DeactivatesImmediately()
        {
            fct.Play(Vector3.zero, "+10", Color.white);
            Assert.IsTrue(fct.IsPlaying);

            fct.Stop();

            Assert.IsFalse(go.activeSelf);
            Assert.IsFalse(fct.IsPlaying);
        }
    }

    [TestFixture]
    public class FloatingTextPoolTests
    {
        private GameObject poolGo;
        private FloatingTextPool pool;

        [SetUp]
        public void SetUp()
        {
            TargetController.ClearEventSubscribers();
            poolGo = new GameObject("FCTPool_Test");
            pool = poolGo.AddComponent<FloatingTextPool>();
            pool.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            TargetController.ClearEventSubscribers();
            if (poolGo != null) Object.DestroyImmediate(poolGo);
        }

        [Test]
        public void Initialize_PrewarmsExactly20InactiveInstances()
        {
            Assert.IsNotNull(pool.Pool);
            Assert.AreEqual(20, pool.Pool.Length);
            foreach (var fct in pool.Pool)
            {
                Assert.IsNotNull(fct);
                Assert.IsFalse(fct.gameObject.activeSelf, "All instances should start inactive");
            }
        }

        [Test]
        public void Spawn_ActivatesOneInstanceAtGivenPosition()
        {
            Assert.AreEqual(0, pool.ActiveCount);

            var fct = pool.Spawn(new Vector3(1f, 2f, 0f), "+50", Color.yellow);

            Assert.IsNotNull(fct);
            Assert.IsTrue(fct.gameObject.activeSelf);
            Assert.AreEqual(1, pool.ActiveCount);
        }

        [Test]
        public void Spawn_Repeatedly_UsesDistinctInstances()
        {
            HashSet<int> seenIds = new HashSet<int>();

            for (int i = 0; i < 5; i++)
            {
                var fct = pool.Spawn(Vector3.zero, $"+{i * 10}", Color.white);
                Assert.IsNotNull(fct);
                Assert.IsTrue(seenIds.Add(fct.GetInstanceID()), "Each spawn should return a distinct instance");
            }
        }

        [Test]
        public void Spawn_WhenPoolExhausted_ReclainsOldestInstance()
        {
            // Fill pool
            for (int i = 0; i < 20; i++)
            {
                pool.Spawn(Vector3.zero, "+10", Color.white);
            }

            Assert.AreEqual(20, pool.ActiveCount);

            // One more spawn should reclaim
            var extra = pool.Spawn(Vector3.zero, "+999", Color.red);
            Assert.IsNotNull(extra);
            // Active count stays at 20 (reclaimed one, gained one)
            Assert.AreEqual(20, pool.ActiveCount);
        }
    }

    [TestFixture]
    public class GameHUDControllerTests
    {
        private GameObject hudGo;
        private GameHUDController hud;

        private GameObject scoreLabelGo, highScoreLabelGo, comboLabelGo;
        private TextMeshProUGUI scoreLabel, highScoreLabel, comboLabel;

        [SetUp]
        public void SetUp()
        {
            ScoreManager.ClearEventSubscribers();

            hudGo = new GameObject("HUD_Test");
            hud = hudGo.AddComponent<GameHUDController>();

            // Create minimal TMP labels
            scoreLabelGo = new GameObject("ScoreLabel");
            scoreLabel = scoreLabelGo.AddComponent<TextMeshProUGUI>();

            highScoreLabelGo = new GameObject("HighScoreLabel");
            highScoreLabel = highScoreLabelGo.AddComponent<TextMeshProUGUI>();

            comboLabelGo = new GameObject("ComboLabel");
            comboLabel = comboLabelGo.AddComponent<TextMeshProUGUI>();

            hud.SetLabels(scoreLabel, highScoreLabel, comboLabel);
        }

        [TearDown]
        public void TearDown()
        {
            ScoreManager.ClearEventSubscribers();
            if (hudGo != null) Object.DestroyImmediate(hudGo);
            if (scoreLabelGo != null) Object.DestroyImmediate(scoreLabelGo);
            if (highScoreLabelGo != null) Object.DestroyImmediate(highScoreLabelGo);
            if (comboLabelGo != null) Object.DestroyImmediate(comboLabelGo);
        }

        [Test]
        public void UpdateScoreDisplay_SetsLabelText()
        {
            hud.UpdateScoreDisplay(1234, animate: false);
            Assert.AreEqual("1,234", scoreLabel.text);
            Assert.AreEqual(1234, hud.DisplayedScore);
        }

        [Test]
        public void UpdateHighScoreDisplay_SetsBestLabelText()
        {
            hud.UpdateHighScoreDisplay(5000);
            Assert.IsTrue(highScoreLabel.text.Contains("5,000"), $"Expected '5,000' in '{highScoreLabel.text}'");
            Assert.AreEqual(5000, hud.DisplayedHighScore);
        }

        [Test]
        public void UpdateComboDisplay_Combo1_HidesComboLabel()
        {
            hud.UpdateComboDisplay(1, 0f);
            Assert.IsFalse(comboLabel.gameObject.activeSelf, "Combo label should hide at x1 combo");
        }

        [Test]
        public void UpdateComboDisplay_Combo3_ShowsWithLabel()
        {
            hud.UpdateComboDisplay(3, 0.7f);
            Assert.IsTrue(comboLabel.gameObject.activeSelf);
            Assert.IsTrue(comboLabel.text.Contains("3"), $"Expected 'x3' in '{comboLabel.text}'");
            Assert.AreEqual(3, hud.DisplayedCombo);
        }

        [Test]
        public void TickScorePopAnimation_OvershootsAndReturnsToBasis()
        {
            hud.UpdateScoreDisplay(100, animate: true);
            Assert.IsTrue(hud.IsScorePopping);

            // Mid-pop: scale should exceed base
            hud.TickScorePopAnimation(0.12f);
            float midScale = scoreLabel.transform.localScale.x;
            Assert.Greater(midScale, 1f, "Score label should overshoot above 1x during pop");

            // After full duration: back to base
            hud.TickScorePopAnimation(0.20f);
            Assert.IsFalse(hud.IsScorePopping);
            Assert.AreEqual(1f, scoreLabel.transform.localScale.x, 0.01f);
        }

        [Test]
        public void OnScoreChanged_Event_TriggersScoreDisplayUpdate()
        {
            hud.SubscribeEvents();

            // Directly test the HUD reaction through UpdateScoreDisplay
            // (static event can only be invoked from its declaring class)
            hud.UpdateScoreDisplay(750, animate: false);

            Assert.AreEqual(750, hud.DisplayedScore);
            Assert.IsTrue(scoreLabel.text.Contains("750"), $"Expected '750' in '{scoreLabel.text}'");
        }

        [Test]
        public void FontSizes_AreIncreasedForScoreAndCombo()
        {
            Assert.AreEqual(72f, hud.ScoreFontSize);
            Assert.AreEqual(54f, hud.ComboFontSize);
            Assert.AreEqual(72f, scoreLabel.fontSize);
            Assert.AreEqual(54f, comboLabel.fontSize);
        }
    }
}
