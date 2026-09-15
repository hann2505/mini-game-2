using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using KinematicsGame.Combat;
using KinematicsGame.Player;
using KinematicsGame.UI;
using KinematicsGame.Core;
using KinematicsGame.Enemy;

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
            stats.Initialize(100, 50, 0, 0, startArmor: 50);

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
            foreach (var entity in Object.FindObjectsByType<InteractiveEntity>(FindObjectsSortMode.None))
            {
                if (entity.Type == EntityType.MiniBonus) Object.DestroyImmediate(entity.gameObject);
            }
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

            // X removes five armor before health.
            Assert.AreEqual(45, stats.CurrentArmor);
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
        public void HazardMine_Collision_SpawnsConfiguredExplosionAnimation()
        {
            GameObject effectTemplate = new GameObject("Bomb_3_Explosion_Test");
            disposables.Add(effectTemplate);
            effectTemplate.AddComponent<SpriteRenderer>();
            AnimatedSpriteEffect templateAnimation = effectTemplate.AddComponent<AnimatedSpriteEffect>();

            Texture2D texture = new Texture2D(16, 16);
            disposables.Add(texture);
            Sprite frame = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f);
            disposables.Add(frame);
            templateAnimation.Frames = new[] { frame };
            templateAnimation.Loop = false;
            templateAnimation.AutoDestroy = true;

            GameObject mineGo = new GameObject("AnimatedHazardMine");
            disposables.Add(mineGo);
            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;
            mine.InteractionEffectPrefab = effectTemplate;

            CollisionEffectDispatcher.ResolveCollision(mine, player);

            AnimatedSpriteEffect spawnedAnimation = null;
            foreach (AnimatedSpriteEffect animation in Object.FindObjectsByType<AnimatedSpriteEffect>(FindObjectsSortMode.None))
            {
                if (animation != templateAnimation) spawnedAnimation = animation;
            }
            Assert.IsNotNull(spawnedAnimation);
            Assert.AreEqual(frame, spawnedAnimation.Frames[0]);
            Assert.IsTrue(spawnedAnimation.AutoDestroy);
            disposables.Add(spawnedAnimation.gameObject);
        }

        [TestCase(0, 95)]
        [TestCase(3, 98)]
        [TestCase(5, 100)]
        public void HazardMine_UsesAvailableArmorThenHealth(int armor, int expectedHealth)
        {
            stats.Initialize();
            stats.RestoreArmor(armor);
            var mineGo = new GameObject("Mine");
            disposables.Add(mineGo);
            var mine = mineGo.AddComponent<InteractiveEntity>();
            CollisionEffectDispatcher.ResolveCollision(mine, player);
            Assert.AreEqual(0, stats.CurrentArmor);
            Assert.AreEqual(expectedHealth, stats.CurrentHealth);
            Assert.AreEqual(0.6f, stats.EffectiveSpeedMultiplier, 0.001f);
            stats.UpdateModifiers(3f);
            Assert.AreEqual(1f, stats.EffectiveSpeedMultiplier);
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

            Assert.AreEqual(1f, stats.EffectiveSpeedMultiplier);

            // Shield absorbed hit: 3 -> 2
            Assert.AreEqual(2, defense.RemainingShieldHits);
            Assert.AreEqual(50, stats.CurrentArmor); // Armor intact
            Assert.AreEqual(100, stats.CurrentHealth); // Health intact
        }

        [Test]
        public void SupplyCrate_Collision_AddsFiveArmorActivatesShieldAndBuffsSpeed()
        {
            stats.TakeDamage(30); // 20 armor, 100 HP
            Assert.AreEqual(20, stats.CurrentArmor);
            Assert.IsFalse(defense.IsShieldActive);

            GameObject crateGo = new GameObject("SupplyCrate");
            disposables.Add(crateGo);
            InteractiveEntity crate = crateGo.AddComponent<InteractiveEntity>();
            crate.Type = EntityType.SupplyCrate;

            CollisionEffectDispatcher.ResolveCollision(crate, player);

            // Y adds five armor and activates immunity.
            Assert.AreEqual(25, stats.CurrentArmor);
            Assert.IsTrue(defense.IsShieldActive);

            // Effect 6: Speed buff 1.5x
            Assert.AreEqual(1.5f, stats.EffectiveSpeedMultiplier, 0.001f);

            // Rockets are awarded by Z.
            Assert.AreEqual(WeaponType.Blaster, combat.CurrentWeapon);

            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(5));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(6));
            Assert.IsFalse(CollisionEffectDispatcher.TriggeredEffectIds.Contains(7));
        }

        [Test]
        public void GemCore_Collision_AwardsCurrenciesAndDoesNotSplitByDefault()
        {
            CollisionEffectDispatcher.GemWeaponSelector = () => WeaponType.Missile;

            GameObject gemGo = new GameObject("GemCore");
            disposables.Add(gemGo);
            InteractiveEntity gem = gemGo.AddComponent<InteractiveEntity>();
            gem.Type = EntityType.GemCore;

            CollisionEffectDispatcher.ResolveCollision(gem, player);

            // Effect 8: +50 Gold, +5 Diamonds
            Assert.AreEqual(50, stats.Gold);
            Assert.AreEqual(5, stats.Diamonds);

            Assert.AreEqual(WeaponType.Missile, combat.CurrentWeapon);
            Assert.AreEqual(10f, combat.TemporaryWeaponTimeRemaining);
            combat.UpdateCombat(10f);
            Assert.AreEqual(WeaponType.Blaster, combat.CurrentWeapon);

            // Effect 8: Currencies awarded; Effect 9: Should NOT trigger because split is disabled by default
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(8));
            Assert.IsFalse(CollisionEffectDispatcher.TriggeredEffectIds.Contains(9), "Gem Core must not split into mini bonuses by default.");
        }

        [Test]
        public void GemCore_Collision_WithLaserSelector_UpdatesHUDWeaponLabel()
        {
            CollisionEffectDispatcher.GemWeaponSelector = () => WeaponType.Laser;

            GameObject gemGo = new GameObject("GemCoreLaserHUD");
            disposables.Add(gemGo);
            InteractiveEntity gem = gemGo.AddComponent<InteractiveEntity>();
            gem.Type = EntityType.GemCore;

            CollisionEffectDispatcher.ResolveCollision(gem, player);

            Assert.AreEqual(WeaponType.Laser, combat.CurrentWeapon);
            Assert.AreEqual(10f, combat.TemporaryWeaponTimeRemaining);

            hud.UpdateHUD();
            StringAssert.Contains("WEAPON: Laser", weaponLabel.text);
            StringAssert.Contains("10.0s", weaponLabel.text);
        }

        [Test]
        public void GemCore_Collision_WithSplitEnabled_SpawnsMiniBonuses()
        {
            GameObject gemGo = new GameObject("GemCoreSplit");
            disposables.Add(gemGo);
            InteractiveEntity gem = gemGo.AddComponent<InteractiveEntity>();
            gem.Type = EntityType.GemCore;
            gem.SplitIntoMiniBonuses = true;

            CollisionEffectDispatcher.ResolveCollision(gem, player);

            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(8));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(9), "Effect 9 should trigger when split is enabled.");
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
            z.SplitIntoMiniBonuses = true;

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
        public void GameHUDController_VitalBars_ShowLiveValuesWithoutAcceptingInput()
        {
            hudGo.AddComponent<Canvas>();
            hpLabel.transform.SetParent(hudGo.transform, false);
            armorLabel.transform.SetParent(hudGo.transform, false);
            stats.Initialize();
            hud.EnsureVitalBars();
            Assert.AreEqual(100, hud.HpSlider.value);
            Assert.AreEqual(0, hud.ArmorSlider.value);
            Assert.IsFalse(hud.HpSlider.interactable);
            Assert.IsFalse(hud.ArmorSlider.interactable);
            Assert.AreEqual(UnityEngine.UI.Navigation.Mode.None, hud.HpSlider.navigation.mode);
            Assert.AreEqual(GameHUDController.VitalBarWidth, hud.HpSlider.GetComponent<RectTransform>().rect.width, 0.01f);
            Assert.AreEqual(GameHUDController.VitalBarHeight, hud.HpSlider.GetComponent<RectTransform>().rect.height, 0.01f);
            Assert.AreEqual(GameHUDController.VitalFontSize, hpLabel.fontSize);
            Assert.AreEqual(GameHUDController.VitalFontSize, armorLabel.fontSize);
            Assert.Greater(shieldLabel.fontSize, 16f);
            Assert.Greater(goldLabel.fontSize, 22f);
            Assert.Greater(weaponLabel.fontSize, 20f);

            stats.RestoreArmor(5);
            Assert.AreEqual(5, hud.ArmorSlider.value);
            stats.TakeDamage(10);
            Assert.AreEqual(0, hud.ArmorSlider.value);
            Assert.AreEqual(95, hud.HpSlider.value);
            Assert.AreEqual("HP: 95/100", hpLabel.text);
            stats.TakeDamage(75);
            Assert.AreEqual(20, hud.HpSlider.value);
            var fill = hud.HpSlider.fillRect.GetComponent<UnityEngine.UI.Image>();
            Assert.Greater(fill.color.r, fill.color.g);
            stats.RestoreHealth(80);
            Assert.AreEqual(100, hud.HpSlider.value);
            Assert.Greater(fill.color.g, fill.color.r);

            hud.EnsureVitalBars();
            Assert.AreEqual(2, hudGo.GetComponentsInChildren<UnityEngine.UI.Slider>().Length);
            foreach (var graphic in hudGo.GetComponentsInChildren<UnityEngine.UI.Image>())
                Assert.IsFalse(graphic.raycastTarget);
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

            Assert.AreEqual("GOLD: 50", goldLabel.text);
            Assert.AreEqual("GEMS: 5", diamondLabel.text);
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

        [Test]
        public void InteractiveEntity_PlayerOnTriggerEnter2D_DispatchesEffectsAndDespawns()
        {
            GameObject mineGo = new GameObject("TestMine");
            disposables.Add(mineGo);
            var mineCol = mineGo.AddComponent<CircleCollider2D>();
            var ie = mineGo.AddComponent<InteractiveEntity>();
            ie.Type = EntityType.HazardMine;
            ie.EnsureComponents();

            var method = typeof(PlayerController).GetMethod("OnTriggerEnter2D",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(player, new object[] { mineCol });

            Assert.IsTrue(ie == null || ie.IsConsumed);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(1));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(2));
        }

        [Test]
        public void InteractiveEntity_EntityOnTriggerEnter2D_DispatchesEffectsAndDespawns()
        {
            GameObject crateGo = new GameObject("TestCrate");
            disposables.Add(crateGo);
            var crateCol = crateGo.AddComponent<CircleCollider2D>();
            var ie = crateGo.AddComponent<InteractiveEntity>();
            ie.Type = EntityType.SupplyCrate;
            ie.EnsureComponents();

            Collider2D playerCol = playerGo.GetComponent<Collider2D>();
            Assert.IsNotNull(playerCol, "Player must have Collider2D.");

            var method = typeof(InteractiveEntity).GetMethod("OnTriggerEnter2D",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(ie, new object[] { playerCol });

            Assert.IsTrue(ie == null || ie.IsConsumed);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(5));
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(6));
        }

        [Test]
        public void InteractiveEntity_TargetSizes_MatchDesignSpecifications()
        {
            GameObject mineGo = new GameObject("Mine");
            disposables.Add(mineGo);
            var ieMine = mineGo.AddComponent<InteractiveEntity>();
            ieMine.Type = EntityType.HazardMine;
            ieMine.EnsureComponents();
            Assert.AreEqual(1.0f, ieMine.TargetSize, "Object X (Hazard Mine) should have reduced target size 1.0.");

            GameObject crateGo = new GameObject("Crate");
            disposables.Add(crateGo);
            var ieCrate = crateGo.AddComponent<InteractiveEntity>();
            ieCrate.Type = EntityType.SupplyCrate;
            ieCrate.EnsureComponents();
            Assert.AreEqual(1.0f, ieCrate.TargetSize, "Object Y (Supply Crate) should have reduced target size 1.0.");

            GameObject gemGo = new GameObject("Gem");
            disposables.Add(gemGo);
            var ieGem = gemGo.AddComponent<InteractiveEntity>();
            ieGem.Type = EntityType.GemCore;
            ieGem.EnsureComponents();
            Assert.AreEqual(1.5f, ieGem.TargetSize, "Object Z (Gem Core) should match character A size (1.5).");
        }

        [Test]
        public void InteractiveEntity_Hitboxes_SnugFitWithoutBloat()
        {
            // Test HazardMine (Object X) hitbox
            GameObject mineGo = new GameObject("TestHitboxMine");
            disposables.Add(mineGo);
            var ieMine = mineGo.AddComponent<InteractiveEntity>();
            ieMine.Type = EntityType.HazardMine;
            ieMine.EnsureComponents();
            var mineCol = mineGo.GetComponent<CircleCollider2D>();
            float mineWorldRadius = mineCol.radius * mineGo.transform.localScale.x;
            Assert.AreEqual(0.30f, mineWorldRadius, 0.05f, "HazardMine world collider radius should be snug (~0.30).");

            // Test SupplyCrate (Object Y) hitbox
            GameObject crateGo = new GameObject("TestHitboxCrate");
            disposables.Add(crateGo);
            var ieCrate = crateGo.AddComponent<InteractiveEntity>();
            ieCrate.Type = EntityType.SupplyCrate;
            ieCrate.EnsureComponents();
            var crateCol = crateGo.GetComponent<CircleCollider2D>();
            float crateWorldRadius = crateCol.radius * crateGo.transform.localScale.x;
            Assert.AreEqual(0.40f, crateWorldRadius, 0.05f, "SupplyCrate world collider radius should be snug (~0.40).");

            // Test GemCore (Object Z) hitbox - ensure scale 6.25 does NOT cause 3.125 world radius!
            GameObject gemGo = new GameObject("TestHitboxGem");
            disposables.Add(gemGo);
            gemGo.transform.localScale = new Vector3(6.25f, 6.25f, 1f);
            var ieGem = gemGo.AddComponent<InteractiveEntity>();
            ieGem.Type = EntityType.GemCore;
            ieGem.EnsureComponents();
            var gemCol = gemGo.GetComponent<CircleCollider2D>();
            float gemWorldRadius = gemCol.radius * gemGo.transform.localScale.x;
            Assert.Less(gemWorldRadius, 0.5f, "GemCore world radius must be snug (< 0.5), not bloated.");
            Assert.AreEqual(0.35f, gemWorldRadius, 0.05f, "GemCore world collider radius should be ~0.35.");

            // Verify Player hitbox is snug (~0.38 world units) and NOT 7.68 bloated!
            player.EnsureCollider();
            float playerWorldRadius = player.PlayerCollider.radius * playerGo.transform.localScale.x;
            Assert.AreEqual(0.38f, playerWorldRadius, 0.05f, "Player world collider radius should be snug (~0.38).");

            // Verify that a distant player at 2.5 units does NOT trigger GemCore collision
            gemGo.transform.position = new Vector3(2.5f, 0f, 0f);
            playerGo.transform.position = Vector3.zero;
            ieGem.CheckPlayerCollision();
            Assert.IsFalse(ieGem.IsConsumed, "Distant player (2.5 units) must NOT trigger GemCore collision.");

            // Verify that a player separated by distance 0.85 units from HazardMine (0.38 + 0.30 = 0.68) does NOT collide
            mineGo.transform.position = new Vector3(0.85f, 0f, 0f);
            ieMine.CheckPlayerCollision();
            Assert.IsFalse(ieMine.IsConsumed, "Player 0.85 units away (visual gap) must NOT trigger collision with Hazard Mine.");

            // Verify that a player actually touching HazardMine at distance 0.50 units DOES collide
            mineGo.transform.position = new Vector3(0.50f, 0f, 0f);
            ieMine.CheckPlayerCollision();
            Assert.IsTrue(ieMine.IsConsumed, "Player actually touching Hazard Mine (0.50 units) must trigger collision.");
        }

        [Test]
        public void HazardSpawner_BalancedRate_DoesNotSpawnAsManyAsTargetB()
        {
            GameObject spawnerGo = new GameObject("BalancedHazardSpawner");
            disposables.Add(spawnerGo);
            HazardSpawner spawner = spawnerGo.AddComponent<HazardSpawner>();

            // Verify balanced spawn rate: spawnInterval >= 3.0f (3.5f default, slower than Target B's 1.0s-2.5s)
            Assert.GreaterOrEqual(spawner.SpawnInterval, 3.0f, "Spawn interval should be >= 3.0f so it does NOT spawn as many as Target B.");
            Assert.GreaterOrEqual(spawner.MinSpawnInterval, 1.5f, "Min spawn interval should be >= 1.5f.");
            Assert.AreEqual(1, spawner.MaxSpawnPerCycle, "MaxSpawnPerCycle should be 1 to prevent multi-object spam.");
            Assert.AreEqual(0.0f, spawner.MultiSpawnChance, "MultiSpawnChance should be 0 to avoid flooding the screen.");
            Assert.IsFalse(spawner.PrewarmViewport, "PrewarmViewport should be disabled so the screen is not crowded at start.");
            Assert.AreEqual(0, spawner.InitialPrewarmCount, "InitialPrewarmCount should be 0.");
            Assert.IsFalse(spawner.AutoSpawn, "AutoSpawn must default to false so hazards do not spawn passively on map.");
            Assert.AreEqual(1.0f, spawner.EnemyDropChance, 0.001f, "EnemyDropChance must default to 1.0 (100%) so every defeated enemy drops an item.");

            // Verify weights for objects X, Y, Z
            Assert.AreEqual(0.25f, spawner.HazardMineWeight, 0.001f, "Hazard Mine (X) weight must be 0.25.");
            Assert.AreEqual(0.25f, spawner.SupplyCrateWeight, 0.001f, "Supply Crate (Y) weight must be 0.25.");
            Assert.AreEqual(0.50f, spawner.GemCoreWeight, 0.001f, "Gem Core (Z) weight must be 0.50.");

            // Verify timer triggers spawn when AutoSpawn is enabled
            spawner.AutoSpawn = true;
            GameObject spawned = null;
            spawner.SpawnInterval = 0.3f;
            spawner.UpdateSpawner(0.4f);

            foreach (var entity in Object.FindObjectsByType<InteractiveEntity>(FindObjectsSortMode.None))
            {
                if (entity.gameObject != spawnerGo)
                {
                    spawned = entity.gameObject;
                    disposables.Add(spawned);
                    break;
                }
            }
            Assert.IsNotNull(spawned, "HazardSpawner should spawn an object when interval elapses.");
        }

        [Test]
        public void HazardSpawner_EnemyDefeatDropChance_SpawnsHazardOnTargetHit()
        {
            GameObject spawnerGo = new GameObject("DropSpawner");
            disposables.Add(spawnerGo);
            HazardSpawner spawner = spawnerGo.AddComponent<HazardSpawner>();
            spawner.EnemyDropChance = 1.0f; // 100% chance for test determinism
            spawner.AutoSpawn = false;

            GameObject targetGo = new GameObject("TestEnemyTarget");
            disposables.Add(targetGo);
            TargetController target = targetGo.AddComponent<TargetController>();

            Vector3 deathPos = new Vector3(2.0f, 1.0f, 0f);
            target.transform.position = deathPos;

            // Simulate enemy defeat projectile hit
            GameObject bullet = new GameObject("Bullet");
            bullet.AddComponent<CircleCollider2D>();
            bullet.AddComponent<Projectile>();
            target.HandleHitByProjectile(bullet);

            // Verify an interactive entity (X, Y, or Z) spawned at the death position
            InteractiveEntity droppedEntity = null;
            foreach (var entity in Object.FindObjectsByType<InteractiveEntity>(FindObjectsSortMode.None))
            {
                if (entity.gameObject != spawnerGo && entity.gameObject != targetGo)
                {
                    droppedEntity = entity;
                    disposables.Add(droppedEntity.gameObject);
                    break;
                }
            }

            Assert.IsNotNull(droppedEntity, "Defeated enemy should drop an Object X, Y, or Z.");
            Assert.AreEqual(deathPos.x, droppedEntity.transform.position.x, 0.1f);
            Assert.AreEqual(deathPos.y, droppedEntity.transform.position.y, 0.1f);
        }

        [Test]
        public void HazardMine_IsBomb3_HasIdleAndExplosionAnimations()
        {
            GameObject minePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hazard_Mine.prefab");
            Assert.IsNotNull(minePrefab, "Hazard_Mine.prefab must exist.");

            InteractiveEntity entity = minePrefab.GetComponent<InteractiveEntity>();
            Assert.IsNotNull(entity, "Hazard_Mine must have InteractiveEntity.");
            Assert.AreEqual(EntityType.HazardMine, entity.Type, "Hazard_Mine must be EntityType.HazardMine (Object X).");

            AnimatedSpriteEffect idleAnim = minePrefab.GetComponent<AnimatedSpriteEffect>();
            Assert.IsNotNull(idleAnim, "Hazard_Mine must have AnimatedSpriteEffect for Bomb 3 idle.");
            Assert.AreEqual(10, idleAnim.Frames.Length, "Bomb 3 idle animation must have 10 frames.");
            Assert.IsTrue(idleAnim.Loop, "Bomb 3 idle animation must loop.");
            StringAssert.StartsWith("Bomb_3_Idle_", idleAnim.Frames[0].name);

            Assert.IsNotNull(entity.InteractionEffectPrefab, "Hazard_Mine must have InteractionEffectPrefab assigned.");
            AnimatedSpriteEffect expAnim = entity.InteractionEffectPrefab.GetComponent<AnimatedSpriteEffect>();
            Assert.IsNotNull(expAnim, "Explosion prefab must have AnimatedSpriteEffect.");
            Assert.AreEqual(9, expAnim.Frames.Length, "Bomb 3 explosion must have 9 frames.");
            Assert.IsFalse(expAnim.Loop, "Bomb 3 explosion must not loop.");
            Assert.IsTrue(expAnim.AutoDestroy, "Bomb 3 explosion must auto-destroy.");
            StringAssert.StartsWith("Bomb_3_Explosion_", expAnim.Frames[0].name);
        }

        [Test]
        public void HazardMine_PlayerTouch_TriggersExplosionAnimationAndEffects()
        {
            GameObject explosionTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bomb_3_Explosion.prefab");
            Assert.IsNotNull(explosionTemplate, "Bomb_3_Explosion.prefab must exist.");

            GameObject mineGo = new GameObject("TouchMine");
            disposables.Add(mineGo);
            mineGo.transform.position = new Vector3(0.2f, 0f, 0f);
            playerGo.transform.position = Vector3.zero;

            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;
            mine.InteractionEffectPrefab = explosionTemplate;
            mine.EnsureComponents();

            // Simulate player touching Object X via proximity check
            mine.CheckPlayerCollision();

            Assert.IsTrue(mine.IsConsumed, "Mine should be marked as consumed upon player touch.");
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(1), "Explosion SFX effect triggered.");
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(2), "Despawn effect triggered.");
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(3), "Damage effect triggered.");
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(4), "Speed debuff effect triggered.");

            // Verify explosion animation was spawned
            GameObject spawnedExp = GameObject.Find("Bomb_3_Explosion");
            Assert.IsNotNull(spawnedExp, "Bomb_3_Explosion should be spawned when player touches Object X.");
            disposables.Add(spawnedExp);

            AnimatedSpriteEffect spawnedAnim = spawnedExp.GetComponent<AnimatedSpriteEffect>();
            Assert.IsNotNull(spawnedAnim, "Spawned explosion must have AnimatedSpriteEffect.");
            Assert.AreEqual(9, spawnedAnim.Frames.Length);
            Assert.IsTrue(spawnedAnim.AutoDestroy);
            Assert.AreEqual(0.5f, spawnedExp.transform.localScale.x, 0.01f);
        }

        [Test]
        public void HazardSpawner_ZeroPassiveSpawns_WhenAutoSpawnDisabled()
        {
            GameObject spawnerGo = new GameObject("PassiveTestSpawner");
            disposables.Add(spawnerGo);
            HazardSpawner spawner = spawnerGo.AddComponent<HazardSpawner>();
            spawner.EnsurePrefabsLoaded();

            // Assert AutoSpawn is false by default
            Assert.IsFalse(spawner.AutoSpawn);

            // Simulate multiple update frames
            for (int i = 0; i < 20; i++)
            {
                spawner.UpdateSpawner(1.0f);
            }

            // Verify zero spawned entities in the scene
            int entityCount = 0;
            foreach (var entity in Object.FindObjectsByType<InteractiveEntity>(FindObjectsSortMode.None))
            {
                if (entity.gameObject != spawnerGo)
                    entityCount++;
            }
            Assert.AreEqual(0, entityCount, "No entities should spawn when AutoSpawn is disabled.");
        }

        [Test]
        public void HazardSpawner_100PercentDropRate_SpawnsOnTargetHit()
        {
            GameObject spawnerGo = new GameObject("DropSpawner");
            disposables.Add(spawnerGo);
            HazardSpawner spawner = spawnerGo.AddComponent<HazardSpawner>();
            spawner.EnsurePrefabsLoaded();
            Assert.AreEqual(1.0f, spawner.EnemyDropChance);

            GameObject targetGo = new GameObject("TestEnemyTarget");
            disposables.Add(targetGo);
            TargetController target = targetGo.AddComponent<TargetController>();

            Vector3 deathPos = new Vector3(5.0f, -2.0f, 0f);
            target.transform.position = deathPos;

            GameObject bullet = new GameObject("Bullet");
            disposables.Add(bullet);
            bullet.AddComponent<CircleCollider2D>();
            bullet.AddComponent<Projectile>();
            target.HandleHitByProjectile(bullet);

            InteractiveEntity dropped = null;
            foreach (var entity in Object.FindObjectsByType<InteractiveEntity>(FindObjectsSortMode.None))
            {
                if (entity.gameObject != spawnerGo && entity.gameObject != targetGo)
                {
                    dropped = entity;
                    disposables.Add(dropped.gameObject);
                    break;
                }
            }

            Assert.IsNotNull(dropped, "An entity must drop on 100% of enemy kills.");
            Assert.AreEqual(deathPos.x, dropped.transform.position.x, 0.1f);
            Assert.AreEqual(deathPos.y, dropped.transform.position.y, 0.1f);
        }

        [Test]
        public void HazardMine_GracePeriod_ImmuneToPlayerContactUntilArmed()
        {
            GameObject pGo = new GameObject("Player");
            disposables.Add(pGo);
            PlayerController p = pGo.AddComponent<PlayerController>();
            PlayerStats pStats = pGo.AddComponent<PlayerStats>();
            pStats.Initialize(100, 50, 0, 0, startArmor: 0);
            pGo.AddComponent<CircleCollider2D>();
            int startingHp = pStats.CurrentHealth;

            GameObject mineGo = new GameObject("Mine");
            disposables.Add(mineGo);
            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;
            mine.EnsureComponents();
            mine.SetArmingDelay(0.75f);

            Assert.IsFalse(mine.IsArmed);

            // Place mine at player position during arming period
            mineGo.transform.position = pGo.transform.position;
            mine.TickArming(0.3f);
            mine.CheckPlayerCollision();

            Assert.IsFalse(mine.IsConsumed, "Mine must not detonate while arming.");
            Assert.AreEqual(startingHp, pStats.CurrentHealth, "Player must not take damage while mine is arming.");

            // Advance time past remaining arming window
            mine.TickArming(0.5f);
            Assert.IsTrue(mine.IsArmed, "Mine must be armed after 0.8s total.");

            mine.CheckPlayerCollision();
            Assert.IsTrue(mine.IsConsumed, "Mine must detonate once fully armed.");
            Assert.Less(pStats.CurrentHealth, startingHp, "Player must take damage once mine is armed.");

            GameObject exp = GameObject.Find("Bomb_3_Explosion");
            if (exp != null) disposables.Add(exp);
        }

        [Test]
        public void HazardMine_ArmingExpiry_RestoresFullAlpha()
        {
            GameObject mineGo = new GameObject("MineWithSprite");
            disposables.Add(mineGo);
            SpriteRenderer sr = mineGo.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;
            mine.SetArmingDelay(0.5f);

            mine.TickArming(0.2f);
            // During arming, alpha should modulate below 1.0f
            Assert.LessOrEqual(sr.color.a, 1.0f);

            mine.TickArming(0.35f);
            Assert.IsTrue(mine.IsArmed);
            Assert.AreEqual(1.0f, sr.color.a, 0.001f, "Alpha must be exactly 1.0f when arming is complete.");
        }
    }
}
