using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using KinematicsGame.Combat;
using KinematicsGame.Player;
using KinematicsGame.UI;
using KinematicsGame.Core;

namespace KinematicsGame.Tests
{
    public class HazardAndHUDTests
    {
        private List<Object> disposables;
        private GameObject playerGo;
        private PlayerController player;
        private PlayerStats stats;
        private PlayerCombatSystem combat;
        private PlayerDefenseSystem defense;

        private GameObject hudGo;
        private GameHUDController hud;
        private TextMeshProUGUI hpLabel, armorLabel, shieldLabel, goldLabel, diamondLabel, weaponLabel, cooldownLabel;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();
            CollisionEffectDispatcher.ResetTracking();

            // Create Player with all subsystems
            playerGo = new GameObject("TestPlayerWithSystems");
            disposables.Add(playerGo);
            player = playerGo.AddComponent<PlayerController>();
            stats = playerGo.AddComponent<PlayerStats>();
            combat = playerGo.AddComponent<PlayerCombatSystem>();
            defense = playerGo.AddComponent<PlayerDefenseSystem>();

            player.Stats = stats;
            player.CombatSystem = combat;
            player.DefenseSystem = defense;
            stats.Initialize(100, 50, 0, 0);

            // Create HUD with labels
            hudGo = new GameObject("TestHUD");
            disposables.Add(hudGo);
            hud = hudGo.AddComponent<GameHUDController>();

            hpLabel = CreateTmpLabel("HpLabel");
            armorLabel = CreateTmpLabel("ArmorLabel");
            shieldLabel = CreateTmpLabel("ShieldLabel");
            goldLabel = CreateTmpLabel("GoldLabel");
            diamondLabel = CreateTmpLabel("DiamondLabel");
            weaponLabel = CreateTmpLabel("WeaponLabel");
            cooldownLabel = CreateTmpLabel("CooldownLabel");

            hud.SetStatsLabels(hpLabel, armorLabel, shieldLabel, goldLabel, diamondLabel, weaponLabel, null, cooldownLabel);
            hud.BindPlayer(player);
        }

        private TextMeshProUGUI CreateTmpLabel(string name)
        {
            GameObject go = new GameObject(name);
            disposables.Add(go);
            return go.AddComponent<TextMeshProUGUI>();
        }

        [TearDown]
        public void TearDown()
        {
            CollisionEffectDispatcher.ResetTracking();
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
        public void HazardMine_Collision_TriggersExplosionDespawnDamageAndSlow()
        {
            GameObject mineGo = new GameObject("HazardMine");
            disposables.Add(mineGo);
            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;

            CollisionEffectDispatcher.ResolveCollision(mine, player);

            // Effect 3: Deducts 25 from Armor (50 -> 25)
            Assert.AreEqual(25, stats.CurrentArmor);
            Assert.AreEqual(100, stats.CurrentHealth);

            // Effect 4: Applies -40% speed debuff (0.6x)
            Assert.AreEqual(0.6f, stats.EffectiveSpeedMultiplier, 0.001f);

            // Effect 1 & 2: Tracked in dispatcher
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(1));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(2));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(3));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(4));
        }

        [Test]
        public void HazardMine_ShieldActive_AbsorbsDamagePreservingHpAndArmor()
        {
            defense.ActivateShield(3, 8.0f);
            Assert.IsTrue(defense.IsShieldActive);

            GameObject mineGo = new GameObject("HazardMineWithShield");
            disposables.Add(mineGo);
            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;

            CollisionEffectDispatcher.ResolveCollision(mine, player);

            // Shield absorbed hit: 3 -> 2
            Assert.AreEqual(2, defense.RemainingShieldHits);
            Assert.AreEqual(50, stats.CurrentArmor); // Armor intact
            Assert.AreEqual(100, stats.CurrentHealth); // Health intact
        }

        [Test]
        public void SupplyCrate_Collision_RestoresArmorActivatesShieldBuffsSpeedAndUpgradesWeapon()
        {
            stats.TakeDamage(30); // 20 armor, 100 HP
            Assert.AreEqual(20, stats.CurrentArmor);
            Assert.IsFalse(defense.IsShieldActive);

            GameObject crateGo = new GameObject("SupplyCrate");
            disposables.Add(crateGo);
            InteractiveEntity crate = crateGo.AddComponent<InteractiveEntity>();
            crate.Type = EntityType.SupplyCrate;

            CollisionEffectDispatcher.ResolveCollision(crate, player);

            // Effect 5: Restored Armor to max (50) & activated shield
            Assert.AreEqual(50, stats.CurrentArmor);
            Assert.IsTrue(defense.IsShieldActive);

            // Effect 6: Speed buff 1.5x
            Assert.AreEqual(1.5f, stats.EffectiveSpeedMultiplier, 0.001f);

            // Effect 7: Weapon switched to Missile
            Assert.AreEqual(WeaponType.Missile, combat.CurrentWeapon);

            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(5));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(6));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(7));
        }

        [Test]
        public void GemCore_Collision_AwardsCurrenciesAndDropsMiniBonuses()
        {
            GameObject gemGo = new GameObject("GemCore");
            disposables.Add(gemGo);
            InteractiveEntity gem = gemGo.AddComponent<InteractiveEntity>();
            gem.Type = EntityType.GemCore;

            CollisionEffectDispatcher.ResolveCollision(gem, player);

            // Effect 8: +50 Gold, +5 Diamonds
            Assert.AreEqual(50, stats.Gold);
            Assert.AreEqual(5, stats.Diamonds);

            // Effect 9: Spawned mini bonuses
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(8));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(9));
        }

        [Test]
        public void CollisionEffectDispatcher_VerifiesAll9DistinctEffects()
        {
            GameObject xGo = new GameObject("X");
            disposables.Add(xGo);
            InteractiveEntity x = xGo.AddComponent<InteractiveEntity>();
            x.Type = EntityType.HazardMine;

            GameObject yGo = new GameObject("Y");
            disposables.Add(yGo);
            InteractiveEntity y = yGo.AddComponent<InteractiveEntity>();
            y.Type = EntityType.SupplyCrate;

            GameObject zGo = new GameObject("Z");
            disposables.Add(zGo);
            InteractiveEntity z = zGo.AddComponent<InteractiveEntity>();
            z.Type = EntityType.GemCore;

            CollisionEffectDispatcher.ResolveCollision(x, player);
            CollisionEffectDispatcher.ResolveCollision(y, player);
            CollisionEffectDispatcher.ResolveCollision(z, player);

            for (int id = 1; id <= 9; id++)
            {
                Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(id), $"Missing effect {id}");
            }
            Assert.AreEqual(9, CollisionEffectDispatcher.TriggeredEffectIds.Count);
        }

        [Test]
        public void HazardSpawner_SpawnsAtPerimeter_OrientationAware()
        {
            GameObject camGo = new GameObject("Cam");
            disposables.Add(camGo);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.aspect = 16f / 9f;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            GameObject vpGo = new GameObject("VP");
            disposables.Add(vpGo);
            ViewportManager vp = vpGo.AddComponent<ViewportManager>();
            vp.TargetCamera = cam;
            vp.Initialize();

            GameObject spawnerGo = new GameObject("Spawner");
            disposables.Add(spawnerGo);
            HazardSpawner spawner = spawnerGo.AddComponent<HazardSpawner>();

            // Horizontal test: enters right edge
            spawner.IsHorizontal = true;
            GameObject hHazard = spawner.SpawnHazard(EntityType.HazardMine);
            disposables.Add(hHazard);
            Assert.GreaterOrEqual(hHazard.transform.position.x, vp.MaxX);

            // Vertical test: enters bottom edge
            spawner.IsHorizontal = false;
            GameObject vHazard = spawner.SpawnHazard(EntityType.SupplyCrate);
            disposables.Add(vHazard);
            Assert.LessOrEqual(vHazard.transform.position.y, vp.MinY);
        }

        [Test]
        public void GameHUDController_PlayerVitalsBinding_UpdatesHpAndArmorLabels()
        {
            Assert.AreEqual("HP: 100/100", hpLabel.text);
            Assert.AreEqual("ARMOR: 50/50", armorLabel.text);

            stats.TakeDamage(30);

            Assert.AreEqual("HP: 100/100", hpLabel.text);
            Assert.AreEqual("ARMOR: 20/50", armorLabel.text);

            stats.TakeDamage(40); // 0 armor, 80 HP
            Assert.AreEqual("HP: 80/100", hpLabel.text);
            Assert.AreEqual("ARMOR: 0/50", armorLabel.text);
        }

        [Test]
        public void GameHUDController_ShieldStateBinding_DisplaysActiveHitsOrReady()
        {
            Assert.AreEqual("SHIELD: READY", shieldLabel.text);

            defense.ActivateShield(3, 8.0f);
            Assert.AreEqual("SHIELD: ACTIVE (3)", shieldLabel.text);

            defense.TryAbsorbDamage();
            Assert.AreEqual("SHIELD: ACTIVE (2)", shieldLabel.text);

            defense.DeactivateShield();
            Assert.AreEqual("SHIELD: READY", shieldLabel.text);
        }

        [Test]
        public void GameHUDController_CurrencyBinding_UpdatesGoldAndDiamondCounters()
        {
            stats.AddCurrency(50, 5);

            Assert.AreEqual("🪙 50", goldLabel.text);
            Assert.AreEqual("💎 5", diamondLabel.text);
        }

        [Test]
        public void GameHUDController_WeaponAndCooldownBinding_DisplaysCurrentModeAndTimers()
        {
            Assert.AreEqual("WEAPON: Blaster", weaponLabel.text);

            combat.SelectWeapon(WeaponType.Missile);
            Assert.AreEqual("WEAPON: Missile", weaponLabel.text);

            defense.ActivateShield(3, 8.0f);
            defense.TriggerEmpStun();
            defense.UpdateDefense(1.0f);

            Assert.That(cooldownLabel.text, Does.Contain("S: 11.0s"));
            Assert.That(cooldownLabel.text, Does.Contain("EMP: 14.0s"));
        }
    }
}
