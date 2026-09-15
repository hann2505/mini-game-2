using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Player;
using KinematicsGame.Core;

namespace KinematicsGame.Tests
{
    public class PlayerStatsTests
    {
        private List<Object> disposables;
        private GameObject playerGo;
        private PlayerStats playerStats;
        private PlayerController playerController;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();

            playerGo = new GameObject("TestPlayer");
            disposables.Add(playerGo);
            playerStats = playerGo.AddComponent<PlayerStats>();
            playerController = playerGo.AddComponent<PlayerController>();
            playerController.Stats = playerStats;
            playerController.MoveSpeed = 10f;
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
        public void PlayerStats_InitializesWithDefaultValues()
        {
            Assert.AreEqual(100, playerStats.MaxHealth);
            Assert.AreEqual(100, playerStats.CurrentHealth);
            Assert.AreEqual(50, playerStats.MaxArmor);
            Assert.AreEqual(0, playerStats.CurrentArmor);
            Assert.AreEqual(0, playerStats.Gold);
            Assert.AreEqual(0, playerStats.Diamonds);
            Assert.AreEqual(1.0f, playerStats.EffectiveSpeedMultiplier);
            Assert.IsTrue(playerStats.IsAlive);
        }

        [Test]
        public void PlayerStats_TakeDamage_AbsorbsWithArmorFirst()
        {
            bool armorEventFired = false;
            int recordedArmor = -1;
            playerStats.OnArmorChanged += (cur, max) =>
            {
                armorEventFired = true;
                recordedArmor = cur;
            };

            playerStats.RestoreArmor(50);
            playerStats.TakeDamage(30);

            Assert.AreEqual(20, playerStats.CurrentArmor);
            Assert.AreEqual(100, playerStats.CurrentHealth);
            Assert.IsTrue(armorEventFired);
            Assert.AreEqual(20, recordedArmor);
        }

        [Test]
        public void PlayerStats_TakeDamage_DepletesArmorAndExcessDamageReducesHealth()
        {
            bool hpEventFired = false;
            int recordedHp = -1;
            playerStats.OnHealthChanged += (cur, max) =>
            {
                hpEventFired = true;
                recordedHp = cur;
            };

            playerStats.RestoreArmor(50);
            playerStats.TakeDamage(70);

            Assert.AreEqual(0, playerStats.CurrentArmor);
            Assert.AreEqual(80, playerStats.CurrentHealth);
            Assert.IsTrue(hpEventFired);
            Assert.AreEqual(80, recordedHp);
        }

        [Test]
        public void PlayerStats_TakeDamage_FiresDeathEventWhenHpZero()
        {
            bool died = false;
            playerStats.OnPlayerDied += () => died = true;

            playerStats.TakeDamage(200);

            Assert.AreEqual(0, playerStats.CurrentHealth);
            Assert.IsFalse(playerStats.IsAlive);
            Assert.IsTrue(died);
        }

        [Test]
        public void PlayerStats_RestoreHealthAndArmor_ClampsToMax()
        {
            playerStats.RestoreArmor(50);
            playerStats.TakeDamage(80); // 0 armor, 70 HP
            Assert.AreEqual(0, playerStats.CurrentArmor);
            Assert.AreEqual(70, playerStats.CurrentHealth);

            playerStats.RestoreHealth(50);
            Assert.AreEqual(100, playerStats.CurrentHealth); // Clamped to 100

            playerStats.RestoreArmor(100);
            Assert.AreEqual(50, playerStats.CurrentArmor); // Clamped to 50
        }

        [Test]
        public void PlayerStats_AddCurrency_AccumulatesAndFiresEvent()
        {
            bool eventFired = false;
            int recordedGold = 0;
            int recordedDiamonds = 0;

            playerStats.OnCurrencyChanged += (g, d) =>
            {
                eventFired = true;
                recordedGold = g;
                recordedDiamonds = d;
            };

            playerStats.AddCurrency(50, 5);

            Assert.AreEqual(50, playerStats.Gold);
            Assert.AreEqual(5, playerStats.Diamonds);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(50, recordedGold);
            Assert.AreEqual(5, recordedDiamonds);
        }

        [Test]
        public void PlayerStats_ApplySpeedModifier_ScalesMultiplierAndClamps()
        {
            // Speed debuff (0.6x)
            playerStats.ApplySpeedModifier(0.6f, 3.0f);
            Assert.AreEqual(0.6f, playerStats.EffectiveSpeedMultiplier, 0.001f);

            // Speed buff (1.5x)
            playerStats.ApplySpeedModifier(1.5f, 2.0f);
            Assert.AreEqual(1.5f, playerStats.EffectiveSpeedMultiplier, 0.001f);

            // Extreme low clamp (floor 0.2f)
            playerStats.ApplySpeedModifier(0.05f, 1.0f);
            Assert.AreEqual(0.2f, playerStats.EffectiveSpeedMultiplier, 0.001f);

            // Extreme high clamp (ceiling 3.0f)
            playerStats.ApplySpeedModifier(10.0f, 1.0f);
            Assert.AreEqual(3.0f, playerStats.EffectiveSpeedMultiplier, 0.001f);
        }

        [Test]
        public void PlayerStats_UpdateModifiers_DecaysDurationAndRestoresBaseline()
        {
            playerStats.ApplySpeedModifier(1.5f, 2.0f);
            Assert.AreEqual(1.5f, playerStats.EffectiveSpeedMultiplier, 0.001f);

            // Advance 1 second
            playerStats.UpdateModifiers(1.0f);
            Assert.AreEqual(1.5f, playerStats.EffectiveSpeedMultiplier, 0.001f);
            Assert.AreEqual(1.0f, playerStats.SpeedModifierRemainingTime, 0.001f);

            // Advance 1.5 seconds (past total 2.0s duration)
            playerStats.UpdateModifiers(1.5f);
            Assert.AreEqual(1.0f, playerStats.EffectiveSpeedMultiplier, 0.001f);
            Assert.AreEqual(0f, playerStats.SpeedModifierRemainingTime, 0.001f);
        }

        [Test]
        public void PlayerController_SimulateMovement_ScalesWithEffectiveSpeedMultiplier()
        {
            Vector3 startPos = Vector3.zero;
            playerGo.transform.position = startPos;

            // Baseline movement (1.0x speed, 10 units/sec for 1 sec = 10 units)
            playerController.SimulateMovement(Vector2.right, 1.0f);
            Assert.AreEqual(10f, playerGo.transform.position.x, 0.05f);

            // Reset position
            playerGo.transform.position = startPos;

            // Apply 0.6x slow (10 * 0.6 * 1 = 6 units)
            playerStats.ApplySpeedModifier(0.6f, 5.0f);
            playerController.SimulateMovement(Vector2.right, 1.0f);
            Assert.AreEqual(6f, playerGo.transform.position.x, 0.05f);

            // Reset position
            playerGo.transform.position = startPos;

            // Apply 1.5x haste (10 * 1.5 * 1 = 15 units)
            playerStats.ApplySpeedModifier(1.5f, 5.0f);
            playerController.SimulateMovement(Vector2.right, 1.0f);
            Assert.AreEqual(15f, playerGo.transform.position.x, 0.05f);
        }

        [Test]
        public void PlayerController_NullStats_DefaultsToBaselineMultiplier()
        {
            playerController.Stats = null;
            playerGo.transform.position = Vector3.zero;

            Assert.DoesNotThrow(() => playerController.SimulateMovement(Vector2.up, 1.0f));
            Assert.AreEqual(10f, playerGo.transform.position.y, 0.05f);
        }
    }
}
