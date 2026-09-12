using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Combat;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Core;

namespace KinematicsGame.Tests
{
    public class CombatAndDefenseTests
    {
        private List<Object> disposables;
        private GameObject playerGo;
        private PlayerCombatSystem combatSystem;
        private PlayerDefenseSystem defenseSystem;

        private GameObject blasterPrefab;
        private GameObject missilePrefab;
        private GameObject bombPrefab;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();

            // Create projectile prefabs
            blasterPrefab = new GameObject("BlasterPrefab");
            disposables.Add(blasterPrefab);
            blasterPrefab.AddComponent<CircleCollider2D>();
            blasterPrefab.AddComponent<Projectile>();
            blasterPrefab.SetActive(false);

            missilePrefab = new GameObject("MissilePrefab");
            disposables.Add(missilePrefab);
            missilePrefab.AddComponent<CircleCollider2D>();
            missilePrefab.AddComponent<HomingMissile>();
            missilePrefab.SetActive(false);

            bombPrefab = new GameObject("BombPrefab");
            disposables.Add(bombPrefab);
            bombPrefab.AddComponent<CircleCollider2D>();
            bombPrefab.AddComponent<ClusterBomb>();
            bombPrefab.SetActive(false);

            // Create Player with Combat and Defense systems
            playerGo = new GameObject("PlayerWithCombat");
            disposables.Add(playerGo);
            combatSystem = playerGo.AddComponent<PlayerCombatSystem>();
            combatSystem.BlasterPrefab = blasterPrefab;
            combatSystem.MissilePrefab = missilePrefab;
            combatSystem.BombPrefab = bombPrefab;
            combatSystem.EnableSimulatedTime(0f);

            defenseSystem = playerGo.AddComponent<PlayerDefenseSystem>();
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
        public void BaseProjectile_Polymorphism_DerivedTypesInheritKinematicsAndBounds()
        {
            Assert.IsTrue(typeof(BaseProjectile).IsAssignableFrom(typeof(Projectile)));
            Assert.IsTrue(typeof(BaseProjectile).IsAssignableFrom(typeof(HomingMissile)));
            Assert.IsTrue(typeof(BaseProjectile).IsAssignableFrom(typeof(ClusterBomb)));
        }

        [Test]
        public void PlayerCombatSystem_WeaponSelection_SwitchesModesAndFiresEvents()
        {
            WeaponType received = WeaponType.Blaster;
            combatSystem.OnWeaponChanged += (w) => received = w;

            Assert.AreEqual(WeaponType.Blaster, combatSystem.CurrentWeapon);

            combatSystem.SelectWeapon(WeaponType.Missile);
            Assert.AreEqual(WeaponType.Missile, combatSystem.CurrentWeapon);
            Assert.AreEqual(WeaponType.Missile, received);

            combatSystem.SelectNextWeapon();
            Assert.AreEqual(WeaponType.Bomb, combatSystem.CurrentWeapon);

            combatSystem.SelectNextWeapon();
            Assert.AreEqual(WeaponType.Blaster, combatSystem.CurrentWeapon);
        }

        [Test]
        public void PlayerCombatSystem_WeaponCooldowns_IndividualPerWeaponMode()
        {
            combatSystem.EnableSimulatedTime(10f);

            // Fire blaster
            combatSystem.SelectWeapon(WeaponType.Blaster);
            GameObject b1 = combatSystem.FireCurrent(Vector2.right);
            Assert.IsNotNull(b1);
            disposables.Add(b1);

            // Blaster cooldown (0.12s) - fire again at +0.05s should fail
            combatSystem.AdvanceSimulatedTime(0.05f);
            GameObject b2 = combatSystem.FireCurrent(Vector2.right);
            Assert.IsNull(b2);

            // Advance past 0.12s (now +0.15s)
            combatSystem.AdvanceSimulatedTime(0.10f);
            GameObject b3 = combatSystem.FireCurrent(Vector2.right);
            Assert.IsNotNull(b3);
            disposables.Add(b3);

            // Switch to Missile (cooldown 1.0s)
            combatSystem.SelectWeapon(WeaponType.Missile);
            GameObject m1 = combatSystem.FireCurrent(Vector2.right);
            Assert.IsNotNull(m1);
            disposables.Add(m1);

            combatSystem.AdvanceSimulatedTime(0.5f);
            Assert.IsNull(combatSystem.FireCurrent(Vector2.right));

            combatSystem.AdvanceSimulatedTime(0.6f);
            GameObject m2 = combatSystem.FireCurrent(Vector2.right);
            Assert.IsNotNull(m2);
            disposables.Add(m2);
        }

        [Test]
        public void HomingMissile_TargetAcquisition_FindsClosestTargetWithinCone()
        {
            GameObject enemy1 = new GameObject("TargetEnemy_InCone");
            disposables.Add(enemy1);
            enemy1.transform.position = new Vector3(5f, 1f, 0f);
            TargetController tc1 = enemy1.AddComponent<TargetController>();

            GameObject enemy2 = new GameObject("TargetEnemy_Behind");
            disposables.Add(enemy2);
            enemy2.transform.position = new Vector3(-5f, 0f, 0f); // Behind missile
            TargetController tc2 = enemy2.AddComponent<TargetController>();

            GameObject missileGo = new GameObject("MissileInstance");
            disposables.Add(missileGo);
            missileGo.transform.position = Vector3.zero;
            missileGo.AddComponent<CircleCollider2D>();
            HomingMissile missile = missileGo.AddComponent<HomingMissile>();
            missile.Initialize(Vector2.right, 6f);

            TargetController acquired = missile.AcquireNearestTarget();
            Assert.AreEqual(tc1, acquired);
        }

        [Test]
        public void HomingMissile_UpdateKinematics_SteersTowardsTargetAndAccelerates()
        {
            GameObject enemy = new GameObject("TargetToSteerTowards");
            disposables.Add(enemy);
            enemy.transform.position = new Vector3(5f, 5f, 0f); // Up and right
            enemy.AddComponent<TargetController>();

            GameObject missileGo = new GameObject("MissileSteer");
            disposables.Add(missileGo);
            missileGo.transform.position = Vector3.zero;
            missileGo.AddComponent<CircleCollider2D>();
            HomingMissile missile = missileGo.AddComponent<HomingMissile>();
            missile.Initialize(Vector2.right, 6f);

            // Tick 0.5s
            missile.UpdateKinematics(0.5f);

            Assert.Greater(missile.Speed, 6f); // Accelerated
            Assert.Greater(missile.Direction.y, 0f); // Steered upward towards target
        }

        [Test]
        public void ClusterBomb_ArmingAndFuse_ArmsAfterDelayAndDetonatesOnFuse()
        {
            GameObject bombGo = new GameObject("BombInstance");
            disposables.Add(bombGo);
            bombGo.transform.position = Vector3.zero;
            bombGo.AddComponent<CircleCollider2D>();
            ClusterBomb bomb = bombGo.AddComponent<ClusterBomb>();
            bomb.Initialize(Vector2.right, 1f);

            // At t = 0.2s: not armed yet (armDelay = 0.5s)
            bomb.UpdateKinematics(0.2f);
            Assert.IsFalse(bomb.IsArmed);
            Assert.IsFalse(bomb.HasDetonated);

            // At t = 0.6s: armed
            bomb.UpdateKinematics(0.4f);
            Assert.IsTrue(bomb.IsArmed);
            Assert.IsFalse(bomb.HasDetonated);

            // At t = 1.6s: fuse expired (fuseTime = 1.5s)
            bomb.UpdateKinematics(1.0f);
            Assert.IsTrue(bomb.HasDetonated);
        }

        [Test]
        public void PlayerDefenseSystem_ActivateShield_AbsorbsHitsAndExpires()
        {
            Assert.IsTrue(defenseSystem.CanActivateShield);

            bool activated = defenseSystem.ActivateShield(3, 8.0f);
            Assert.IsTrue(activated);
            Assert.IsTrue(defenseSystem.IsShieldActive);
            Assert.AreEqual(3, defenseSystem.RemainingShieldHits);

            // First hit absorbed
            Assert.IsTrue(defenseSystem.TryAbsorbDamage());
            Assert.AreEqual(2, defenseSystem.RemainingShieldHits);
            Assert.IsTrue(defenseSystem.IsShieldActive);

            // Second hit absorbed
            Assert.IsTrue(defenseSystem.TryAbsorbDamage());
            Assert.AreEqual(1, defenseSystem.RemainingShieldHits);

            // Third hit breaks shield
            Assert.IsTrue(defenseSystem.TryAbsorbDamage());
            Assert.AreEqual(0, defenseSystem.RemainingShieldHits);
            Assert.IsFalse(defenseSystem.IsShieldActive);

            // Cannot absorb when shield is broken
            Assert.IsFalse(defenseSystem.TryAbsorbDamage());
        }

        [Test]
        public void PlayerDefenseSystem_TriggerEmpStun_HaltsEnemyKinematicsAndTintsCyan()
        {
            GameObject enemyGo = new GameObject("EmpTestEnemy");
            disposables.Add(enemyGo);
            enemyGo.transform.position = new Vector3(5f, 0f, 0f);
            SpriteRenderer sr = enemyGo.AddComponent<SpriteRenderer>();
            TargetController target = enemyGo.AddComponent<TargetController>();

            Assert.IsTrue(defenseSystem.CanTriggerEmp);
            defenseSystem.TriggerEmpStun();

            Assert.IsTrue(target.IsStunned);
            Assert.AreEqual(new Color(0.4f, 0.8f, 1f, 1f), sr.color);

            // Ticking kinematics while stunned does NOT move the enemy
            Vector3 posBefore = enemyGo.transform.position;
            target.UpdateKinematics(1.0f);
            Assert.AreEqual(posBefore, enemyGo.transform.position);

            // Advance past stun duration (3.0s)
            target.UpdateKinematics(2.5f);
            Assert.IsFalse(target.IsStunned);
            Assert.AreEqual(Color.white, sr.color);
        }

        [Test]
        public void PlayerDefenseSystem_Cooldowns_PreventImmediateReactivation()
        {
            defenseSystem.ActivateShield(3, 8.0f);
            Assert.IsFalse(defenseSystem.CanActivateShield);
            Assert.AreEqual(12f, defenseSystem.ShieldCooldownRemaining, 0.01f);

            defenseSystem.TriggerEmpStun();
            Assert.IsFalse(defenseSystem.CanTriggerEmp);
            Assert.AreEqual(15f, defenseSystem.EmpCooldownRemaining, 0.01f);

            // Tick 5 seconds
            defenseSystem.UpdateDefense(5.0f);
            Assert.AreEqual(7f, defenseSystem.ShieldCooldownRemaining, 0.01f);
            Assert.AreEqual(10f, defenseSystem.EmpCooldownRemaining, 0.01f);
            Assert.IsFalse(defenseSystem.CanActivateShield);
            Assert.IsFalse(defenseSystem.CanTriggerEmp);

            // Tick 11 seconds (total 16s, past both cooldowns)
            defenseSystem.UpdateDefense(11.0f);
            Assert.AreEqual(0f, defenseSystem.ShieldCooldownRemaining, 0.01f);
            Assert.AreEqual(0f, defenseSystem.EmpCooldownRemaining, 0.01f);
            Assert.IsTrue(defenseSystem.CanActivateShield);
            Assert.IsTrue(defenseSystem.CanTriggerEmp);
        }

        [Test]
        public void PlayerCombatSystem_TemporaryWeapon_ExpiresAfterDuration()
        {
            Assert.AreEqual(WeaponType.Blaster, combatSystem.CurrentWeapon);

            combatSystem.GrantTemporaryWeapon(WeaponType.Missile, 10.0f);
            Assert.AreEqual(WeaponType.Missile, combatSystem.CurrentWeapon);
            Assert.IsTrue(combatSystem.IsTemporaryWeaponActive);
            Assert.AreEqual(10.0f, combatSystem.TemporaryWeaponTimeRemaining, 0.01f);

            // Tick 5 seconds
            combatSystem.UpdateCombat(5.0f);
            Assert.AreEqual(WeaponType.Missile, combatSystem.CurrentWeapon);
            Assert.AreEqual(5.0f, combatSystem.TemporaryWeaponTimeRemaining, 0.01f);

            // Tick past expiration
            combatSystem.UpdateCombat(6.0f);
            Assert.AreEqual(WeaponType.Blaster, combatSystem.CurrentWeapon);
            Assert.IsFalse(combatSystem.IsTemporaryWeaponActive);
        }

        [Test]
        public void AnimatedSpriteEffect_TicksFrames_CyclesAndUpdatesSprite()
        {
            GameObject go = new GameObject("TestAnimEffect");
            disposables.Add(go);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            AnimatedSpriteEffect effect = go.AddComponent<AnimatedSpriteEffect>();

            Texture2D tex = new Texture2D(16, 16);
            disposables.Add(tex);
            Sprite s1 = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
            Sprite s2 = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
            disposables.Add(s1);
            disposables.Add(s2);

            effect.Frames = new[] { s1, s2 };
            effect.FramesPerSecond = 10f;
            effect.Loop = true;

            effect.Tick(0.05f); // 0.05s < 0.1s -> frame 0
            Assert.AreEqual(s1, sr.sprite);

            effect.Tick(0.06f); // total 0.11s >= 0.1s -> frame 1
            Assert.AreEqual(s2, sr.sprite);
        }

        [Test]
        public void HomingMissile_Animation_AdvancesFramesAndDetonatesWithExplosion()
        {
            GameObject mGo = new GameObject("TestMissileWithAnim");
            disposables.Add(mGo);
            SpriteRenderer sr = mGo.AddComponent<SpriteRenderer>();
            HomingMissile missile = mGo.AddComponent<HomingMissile>();

            Texture2D tex = new Texture2D(16, 16);
            disposables.Add(tex);
            Sprite f1 = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
            Sprite f2 = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
            Sprite exp = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
            disposables.Add(f1);
            disposables.Add(f2);
            disposables.Add(exp);

            missile.FlyingFrames = new[] { f1, f2 };
            missile.ExplosionFrames = new[] { exp };
            missile.AnimationFps = 10f;
            missile.Initialize(Vector2.right, 6f);

            // Tick kinematics should advance flying animation
            missile.UpdateKinematics(0.12f);
            Assert.AreEqual(f2, sr.sprite);

            // Detonate should trigger explosion
            missile.Detonate();
            Assert.IsTrue(missile.HasDetonated);
        }

        [Test]
        public void HomingMissile_Size_IsSmallerThanObjectA()
        {
            // Object A standard uniform size is 1.5f
            float objectASize = 1.5f;

            GameObject mGo = new GameObject("SizedMissile");
            disposables.Add(mGo);
            SpriteRenderer sr = mGo.AddComponent<SpriteRenderer>();

            // Simulate missile sprite (141 x 502 pixels, 100 PPU => 1.41 x 5.02 world units)
            Texture2D tex = new Texture2D(141, 502);
            disposables.Add(tex);
            Sprite missileSprite = Sprite.Create(tex, new Rect(0, 0, 141, 502), new Vector2(0.5f, 0.5f), 100f);
            disposables.Add(missileSprite);
            sr.sprite = missileSprite;

            HomingMissile missile = mGo.AddComponent<HomingMissile>();
            missile.TargetUniformSize = 1.1f;
            missile.ApplyTargetSize();

            // Missile size (1.1f) must be strictly smaller than Object A (1.5f)
            Assert.Less(missile.TargetUniformSize, objectASize);
            Assert.AreEqual(1.1f, missile.TargetUniformSize, 0.01f);

            // Effective world height = 5.02 * localScale.y
            float worldHeight = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) * mGo.transform.localScale.y;
            Assert.AreEqual(1.1f, worldHeight, 0.01f);
            Assert.Less(worldHeight, objectASize);
        }

        [Test]
        public void HomingMissile_CollisionWithTarget_SpawnsExplosionAnimation()
        {
            // Create TargetController (Object B)
            GameObject targetGo = new GameObject("TargetEnemy_B");
            disposables.Add(targetGo);
            targetGo.transform.position = new Vector3(3f, 0f, 0f);
            BoxCollider2D targetCol = targetGo.AddComponent<BoxCollider2D>();
            targetCol.isTrigger = true;
            TargetController target = targetGo.AddComponent<TargetController>();

            // Create HomingMissile
            GameObject missileGo = new GameObject("MissileInstance");
            missileGo.transform.position = new Vector3(3f, 0f, 0f);
            BoxCollider2D missileCol = missileGo.AddComponent<BoxCollider2D>();
            missileCol.isTrigger = true;
            HomingMissile missile = missileGo.AddComponent<HomingMissile>();

            Texture2D tex = new Texture2D(32, 32);
            disposables.Add(tex);
            Sprite expSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.zero);
            disposables.Add(expSprite);
            missile.ExplosionFrames = new[] { expSprite };

            // When Target (Object B) receives projectile hit from missile
            target.HandleHitByProjectile(missileGo);

            // Explosion effect should be spawned
            GameObject spawnedExplosion = GameObject.Find("Missile_Explosion");
            if (spawnedExplosion != null)
            {
                disposables.Add(spawnedExplosion);
            }
            Assert.IsNotNull(spawnedExplosion, "Missile_Explosion GameObject should be spawned upon collision with Object B");

            AnimatedSpriteEffect effect = spawnedExplosion.GetComponent<AnimatedSpriteEffect>();
            Assert.IsNotNull(effect, "Spawned explosion should have AnimatedSpriteEffect component");
            Assert.AreEqual(1, effect.Frames.Length);
            Assert.AreEqual(expSprite, effect.Frames[0]);
        }
    }
}
