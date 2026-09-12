using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;
using KinematicsGame.Audio;
using KinematicsGame.UI;
using KinematicsGame.Editor;

namespace KinematicsGame.Tests
{
    [TestFixture]
    public class FullSystemIntegrationTests
    {
        private List<Object> disposables;
        private Camera testCam;
        private ViewportManager viewportManager;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();

            GameObject camGo = new GameObject("TestMainCamera");
            disposables.Add(camGo);
            testCam = camGo.AddComponent<Camera>();
            testCam.orthographic = true;
            testCam.orthographicSize = 5f;
            camGo.tag = "MainCamera";

            viewportManager = camGo.AddComponent<ViewportManager>();
            viewportManager.TargetCamera = testCam;
            viewportManager.Initialize();

            CollisionEffectDispatcher.ResetTracking();
            PlayerPrefs.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            AudioManager.ResetInstanceForTesting();
            CollisionEffectDispatcher.ResetTracking();

            if (disposables != null)
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

            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void SceneSetupHelper_EnsurePrefabsExist_CreatesAllRequiredCombatPrefabs()
        {
            SceneSetupHelper.EnsurePrefabsExist();

            // Verify Blaster Projectile Prefab
            GameObject blasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            Assert.IsNotNull(blasterPrefab, "Projectile.prefab should exist.");
            Assert.IsNotNull(blasterPrefab.GetComponent<Projectile>(), "Projectile should have Projectile component.");

            // Verify Missile Prefab
            GameObject missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Missile.prefab");
            Assert.IsNotNull(missilePrefab, "Missile.prefab should exist.");
            Assert.IsNotNull(missilePrefab.GetComponent<HomingMissile>(), "Missile should have HomingMissile component.");

            // Verify Bomb Prefab
            GameObject bombPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bomb.prefab");
            Assert.IsNotNull(bombPrefab, "Bomb.prefab should exist.");
            Assert.IsNotNull(bombPrefab.GetComponent<ClusterBomb>(), "Bomb should have ClusterBomb component.");

            // Verify Hazard Mine Prefab (Object X)
            GameObject minePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hazard_Mine.prefab");
            Assert.IsNotNull(minePrefab, "Hazard_Mine.prefab should exist.");
            InteractiveEntity mineEntity = minePrefab.GetComponent<InteractiveEntity>();
            Assert.IsNotNull(mineEntity, "Hazard_Mine should have InteractiveEntity component.");
            Assert.AreEqual(EntityType.HazardMine, mineEntity.Type);

            // Verify Tech Supply Crate Prefab (Object Y)
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Supply_Crate.prefab");
            Assert.IsNotNull(cratePrefab, "Supply_Crate.prefab should exist.");
            InteractiveEntity crateEntity = cratePrefab.GetComponent<InteractiveEntity>();
            Assert.IsNotNull(crateEntity, "Supply_Crate should have InteractiveEntity component.");
            Assert.AreEqual(EntityType.SupplyCrate, crateEntity.Type);

            // Verify Gem Core Prefab (Object Z)
            GameObject gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gem_Core.prefab");
            Assert.IsNotNull(gemPrefab, "Gem_Core.prefab should exist.");
            InteractiveEntity gemEntity = gemPrefab.GetComponent<InteractiveEntity>();
            Assert.IsNotNull(gemEntity, "Gem_Core should have InteractiveEntity component.");
            Assert.AreEqual(EntityType.GemCore, gemEntity.Type);

            // Verify Shield Overlay Prefab
            GameObject shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShieldOverlay.prefab");
            Assert.IsNotNull(shieldPrefab, "ShieldOverlay.prefab should exist.");

            // Verify Player Prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            Assert.IsNotNull(playerPrefab, "Player.prefab should exist.");
            Assert.IsNotNull(playerPrefab.GetComponent<PlayerController>(), "Player should have PlayerController.");
            Assert.IsNotNull(playerPrefab.GetComponent<PlayerStats>(), "Player should have PlayerStats.");
            Assert.IsNotNull(playerPrefab.GetComponent<PlayerCombatSystem>(), "Player should have PlayerCombatSystem.");
            Assert.IsNotNull(playerPrefab.GetComponent<PlayerDefenseSystem>(), "Player should have PlayerDefenseSystem.");

            // Verify Target Prefab
            GameObject targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Target.prefab");
            Assert.IsNotNull(targetPrefab, "Target.prefab should exist.");
            Assert.IsNotNull(targetPrefab.GetComponent<TargetController>(), "Target should have TargetController.");
        }

        [Test]
        public void FullSystem_DualOrientation_AlignsRestrictedZoneAndSpawner()
        {
            GameObject rzGo = new GameObject("RestrictedZoneTest");
            disposables.Add(rzGo);
            RestrictedZoneTrigger zoneTrigger = rzGo.AddComponent<RestrictedZoneTrigger>();

            float totalWidth = viewportManager.Width;
            float totalHeight = viewportManager.Height;

            // Test Horizontal alignment (Left 20%)
            zoneTrigger.AlignToViewport(GameOrientation.Horizontal);
            BoxCollider2D col = rzGo.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(col);
            Assert.AreEqual(totalWidth * 0.2f, col.size.x, 0.05f);
            Assert.AreEqual(totalHeight, col.size.y, 0.05f);
            float expectedCenterX = viewportManager.MinX + (totalWidth * 0.2f * 0.5f);
            Assert.AreEqual(expectedCenterX, rzGo.transform.position.x, 0.05f);

            // Test Vertical alignment (Top 20%)
            zoneTrigger.AlignToViewport(GameOrientation.Vertical);
            Assert.AreEqual(totalWidth, col.size.x, 0.05f);
            Assert.AreEqual(totalHeight * 0.2f, col.size.y, 0.05f);
            float expectedCenterY = viewportManager.MaxY - (totalHeight * 0.2f * 0.5f);
            Assert.AreEqual(expectedCenterY, rzGo.transform.position.y, 0.05f);
        }

        [Test]
        public void FullSystem_CombatLoop_FiresAllWeaponsAndActivatesDefenses()
        {
            // Set up projectile prefabs
            GameObject blasterPrefab = new GameObject("BlasterPrefab");
            disposables.Add(blasterPrefab);
            blasterPrefab.AddComponent<CircleCollider2D>();
            blasterPrefab.AddComponent<Projectile>();
            blasterPrefab.SetActive(false);

            GameObject missilePrefab = new GameObject("MissilePrefab");
            disposables.Add(missilePrefab);
            missilePrefab.AddComponent<CircleCollider2D>();
            missilePrefab.AddComponent<HomingMissile>();
            missilePrefab.SetActive(false);

            GameObject bombPrefab = new GameObject("BombPrefab");
            disposables.Add(bombPrefab);
            bombPrefab.AddComponent<CircleCollider2D>();
            bombPrefab.AddComponent<ClusterBomb>();
            bombPrefab.SetActive(false);

            // Set up Player
            GameObject playerGo = new GameObject("CombatPlayer");
            disposables.Add(playerGo);
            PlayerController pc = playerGo.AddComponent<PlayerController>();
            PlayerStats ps = playerGo.AddComponent<PlayerStats>();
            PlayerCombatSystem pcs = playerGo.AddComponent<PlayerCombatSystem>();
            PlayerDefenseSystem pds = playerGo.AddComponent<PlayerDefenseSystem>();

            pcs.BlasterPrefab = blasterPrefab;
            pcs.MissilePrefab = missilePrefab;
            pcs.BombPrefab = bombPrefab;
            pcs.EnableSimulatedTime(0f);

            pc.Stats = ps;
            pc.CombatSystem = pcs;
            pc.DefenseSystem = pds;

            // 1. Fire Blaster
            pcs.SelectWeapon(WeaponType.Blaster);
            GameObject firedProjObj = pcs.FireCurrent(Vector2.right);
            Assert.IsNotNull(firedProjObj, "Blaster should fire successfully.");
            disposables.Add(firedProjObj);

            // 2. Fire Missile
            pcs.AdvanceSimulatedTime(2.0f);
            pcs.SelectWeapon(WeaponType.Missile);
            GameObject firedMissileObj = pcs.FireCurrent(Vector2.right);
            Assert.IsNotNull(firedMissileObj, "Missile should fire successfully.");
            disposables.Add(firedMissileObj);

            // 3. Fire Bomb
            pcs.AdvanceSimulatedTime(3.0f);
            pcs.SelectWeapon(WeaponType.Bomb);
            GameObject firedBombObj = pcs.FireCurrent(Vector2.right);
            Assert.IsNotNull(firedBombObj, "Bomb should deploy successfully.");
            disposables.Add(firedBombObj);

            // 4. Activate Energy Shield
            Assert.IsFalse(pds.IsShieldActive);
            bool shieldActivated = pds.ActivateShield(3, 8.0f);
            Assert.IsTrue(shieldActivated, "Shield should activate.");
            Assert.IsTrue(pds.IsShieldActive);
            Assert.AreEqual(3, pds.RemainingShieldHits);

            // Shield absorbs damage without affecting stats
            bool absorbed = pds.TryAbsorbDamage();
            Assert.IsTrue(absorbed);
            Assert.AreEqual(2, pds.RemainingShieldHits);
            Assert.AreEqual(100, ps.CurrentHealth);
            Assert.AreEqual(50, ps.CurrentArmor);

            // 5. Activate EMP Stun Wave on nearby enemy
            GameObject enemyGo = new GameObject("EnemyTarget");
            disposables.Add(enemyGo);
            enemyGo.transform.position = playerGo.transform.position + new Vector3(2f, 0f, 0f);
            TargetController tc = enemyGo.AddComponent<TargetController>();
            Assert.IsFalse(tc.IsStunned);

            bool empActivated = pds.TriggerEmpStun();
            Assert.IsTrue(empActivated, "EMP should activate.");
            Assert.IsTrue(tc.IsStunned, "Nearby enemy should be stunned by EMP.");
        }

        [Test]
        public void FullSystem_InteractiveHazards_TriggersAllNineCollisionEffects()
        {
            // Set up player
            GameObject playerGo = new GameObject("HazardTestPlayer");
            disposables.Add(playerGo);
            PlayerController pc = playerGo.AddComponent<PlayerController>();
            PlayerStats ps = playerGo.AddComponent<PlayerStats>();
            PlayerCombatSystem pcs = playerGo.AddComponent<PlayerCombatSystem>();
            PlayerDefenseSystem pds = playerGo.AddComponent<PlayerDefenseSystem>();

            pc.Stats = ps;
            pc.CombatSystem = pcs;
            pc.DefenseSystem = pds;

            // ── Collision with Object X (Hazard Mine) ───────────────────────
            GameObject mineGo = new GameObject("Mine");
            InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
            mine.Type = EntityType.HazardMine;

            CollisionEffectDispatcher.ResolveCollision(mine, pc);

            // Effect 1 & 2: Despawn triggered & recorded
            Assert.IsTrue(mine == null || mine.IsConsumed);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(1), "Effect 1 should trigger.");
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(2), "Effect 2 should trigger.");

            // Effect 3: Damage dealt (25 absorbed by Armor: 50 -> 25, HP 100)
            Assert.AreEqual(100, ps.CurrentHealth);
            Assert.AreEqual(25, ps.CurrentArmor);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(3), "Effect 3 should trigger.");

            // Effect 4: Speed debuff applied
            Assert.AreEqual(0.6f, ps.EffectiveSpeedMultiplier, 0.01f);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(4), "Effect 4 should trigger.");

            // ── Collision with Object Y (Tech Supply Crate) ─────────────────
            GameObject crateGo = new GameObject("Crate");
            InteractiveEntity crate = crateGo.AddComponent<InteractiveEntity>();
            crate.Type = EntityType.SupplyCrate;

            CollisionEffectDispatcher.ResolveCollision(crate, pc);

            // Effect 5: Restore Armor (+50 -> 75 capped at MaxArmor 50) & deploy Energy Shield
            Assert.AreEqual(50, ps.CurrentArmor);
            Assert.IsTrue(pds.IsShieldActive);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(5), "Effect 5 should trigger.");

            // Effect 6: Haste applied (+50% speed: 1.5x)
            Assert.AreEqual(1.5f, ps.EffectiveSpeedMultiplier, 0.01f);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(6), "Effect 6 should trigger.");

            // Effect 7: Switch weapon to Missile
            Assert.AreEqual(WeaponType.Missile, pcs.CurrentWeapon);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(7), "Effect 7 should trigger.");

            // ── Collision with Object Z (Gem Core) ──────────────────────────
            GameObject gemGo = new GameObject("Gem");
            InteractiveEntity gem = gemGo.AddComponent<InteractiveEntity>();
            gem.Type = EntityType.GemCore;

            CollisionEffectDispatcher.ResolveCollision(gem, pc);

            // Effect 8: Currencies awarded (+50 Gold, +5 Diamonds)
            Assert.AreEqual(50, ps.Gold);
            Assert.AreEqual(5, ps.Diamonds);
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(8), "Effect 8 should trigger.");

            // Effect 9: 3 mini-bonus pickups spawned
            Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(9), "Effect 9 should trigger.");
            InteractiveEntity[] bonuses = Object.FindObjectsByType<InteractiveEntity>(FindObjectsSortMode.None);
            int miniBonusCount = 0;
            foreach (InteractiveEntity entity in bonuses)
            {
                if (entity.Type == EntityType.MiniBonus)
                {
                    miniBonusCount++;
                    disposables.Add(entity.gameObject);
                }
            }
            Assert.AreEqual(3, miniBonusCount, "Exactly 3 MiniBonus pickups should spawn from Gem Core.");

            // Assert all 9 distinct effects were executed
            for (int i = 1; i <= 9; i++)
            {
                Assert.IsTrue(CollisionEffectDispatcher.TriggeredEffectIds.Contains(i), $"Effect {i} must be triggered.");
            }
        }

        [Test]
        public void FullSystem_HUDController_ReflectsVitalsAndCooldownsInRealTime()
        {
            GameObject canvasGo = new GameObject("HUDCanvas");
            disposables.Add(canvasGo);
            GameHUDController hud = canvasGo.AddComponent<GameHUDController>();

            TextMeshProUGUI CreateChildLabel(string name)
            {
                GameObject lblGo = new GameObject(name);
                lblGo.transform.SetParent(canvasGo.transform, false);
                return lblGo.AddComponent<TextMeshProUGUI>();
            }

            TextMeshProUGUI hpLbl = CreateChildLabel("HpLabel");
            TextMeshProUGUI armorLbl = CreateChildLabel("ArmorLabel");
            TextMeshProUGUI shieldLbl = CreateChildLabel("ShieldLabel");
            TextMeshProUGUI goldLbl = CreateChildLabel("GoldLabel");
            TextMeshProUGUI diamondLbl = CreateChildLabel("DiamondLabel");
            TextMeshProUGUI weaponLbl = CreateChildLabel("WeaponLabel");
            TextMeshProUGUI cdLbl = CreateChildLabel("CooldownLabel");

            hud.SetStatsLabels(hpLbl, armorLbl, shieldLbl, goldLbl, diamondLbl, weaponLbl, null, cdLbl);

            GameObject playerGo = new GameObject("HUDPlayer");
            disposables.Add(playerGo);
            PlayerStats ps = playerGo.AddComponent<PlayerStats>();
            PlayerCombatSystem pcs = playerGo.AddComponent<PlayerCombatSystem>();
            PlayerDefenseSystem pds = playerGo.AddComponent<PlayerDefenseSystem>();
            hud.BindStats(ps, pcs, pds);

            // Verify initial HUD text
            Assert.IsTrue(hpLbl.text.Contains("100/100"));
            Assert.IsTrue(armorLbl.text.Contains("50/50"));
            Assert.IsTrue(shieldLbl.text.Contains("SHIELD: READY"));
            Assert.IsTrue(goldLbl.text.Contains("0"));
            Assert.IsTrue(diamondLbl.text.Contains("0"));
            Assert.IsTrue(weaponLbl.text.Contains("Blaster"));
            Assert.IsTrue(cdLbl.text.Contains("S: READY"));

            // Simulate damage, currencies, and weapon switch
            ps.TakeDamage(25); // Armor absorbs 25 -> 25/50
            ps.AddCurrency(100, 10);
            pcs.SelectWeapon(WeaponType.Bomb);

            Assert.IsTrue(armorLbl.text.Contains("25/50"));
            Assert.IsTrue(goldLbl.text.Contains("100"));
            Assert.IsTrue(diamondLbl.text.Contains("10"));
            Assert.IsTrue(weaponLbl.text.Contains("Bomb"));
        }

        [Test]
        public void FullSystem_AudioToggles_PreserveExactDimensionsAndPlayerPrefs()
        {
            GameObject amGo = new GameObject("AudioManager");
            disposables.Add(amGo);
            AudioManager am = amGo.AddComponent<AudioManager>();
            am.InitializeChannels();
            AudioManager.ResetInstanceForTesting(am);

            GameObject btnGo = new GameObject("SoundToggle");
            disposables.Add(btnGo);
            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64f, 64f);
            Image img = btnGo.AddComponent<Image>();
            Button btn = btnGo.AddComponent<Button>();

            Sprite offSprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            Sprite onSprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            disposables.Add(offSprite);
            disposables.Add(onSprite);

            AudioToggleButton toggle = btnGo.AddComponent<AudioToggleButton>();
            toggle.ToggleType = AudioToggleType.Sound;
            toggle.ActiveSprite = offSprite;
            toggle.InactiveSprite = onSprite;
            toggle.TargetImage = img;
            toggle.ButtonComponent = btn;
            toggle.FixedDimensions = new Vector2(64f, 64f);
            toggle.UpdateVisuals();

            // Dimensions should be 64x64
            Assert.AreEqual(64f, rt.sizeDelta.x);
            Assert.AreEqual(64f, rt.sizeDelta.y);
            Assert.AreEqual(offSprite, img.sprite);

            // Toggle sound mute
            toggle.TriggerClick();
            toggle.UpdateVisuals();
            Assert.IsTrue(am.IsSfxMuted);
            Assert.AreEqual(1, PlayerPrefs.GetInt(AudioManager.PrefKeySfxMuted, 0));
            Assert.AreEqual(onSprite, img.sprite);

            // Dimensions preserved exactly
            Assert.AreEqual(64f, rt.sizeDelta.x);
            Assert.AreEqual(64f, rt.sizeDelta.y);
        }

        [Test]
        public void SampleScene_HierarchyIntegrity_ContainsAllPhase5Components()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            try
            {
                Assert.IsNotNull(Object.FindFirstObjectByType<AudioManager>(),
                    "AudioManager must exist in SampleScene.");
                Assert.IsNotNull(Object.FindFirstObjectByType<HazardSpawner>(),
                    "HazardSpawner must exist in SampleScene.");
                Assert.IsNotNull(Object.FindFirstObjectByType<RestrictedZoneTrigger>(),
                    "RestrictedZoneTrigger must exist in SampleScene.");
                Assert.IsNotNull(Object.FindFirstObjectByType<GameHUDController>(),
                    "GameHUDController must exist in SampleScene.");

                var hud = Object.FindFirstObjectByType<GameHUDController>();
                Assert.IsNotNull(hud.HpLabel,     "HpLabel must be wired in SampleScene.");
                Assert.IsNotNull(hud.GoldLabel,   "GoldLabel must be wired in SampleScene.");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
