---
phase: 3
title: "Unit Testing & Integration Verification"
status: completed
priority: P1
effort: "30m"
dependencies: [1, 2]
---

# Phase 3: Unit Testing & Integration Verification

## Overview
Updates existing test assertions in `HazardAndHUDTests.cs` to match the new 100% drop rate and reward-favored weights, and introduces new NUnit test fixtures validating zero passive spawns, 100% defeat drop rate, and mine arming grace period immunity.

## Requirements
- Functional:
  - Update `HazardAndHUDTests.cs:551` to assert `EnemyDropChance == 1.0f` and `AutoSpawn == false`.
  - Add test `HazardSpawner_ZeroPassiveSpawns_WhenAutoSpawnDisabled` verifying that running `UpdateSpawner` with `AutoSpawn = false` produces 0 entities.
  - Add test `HazardSpawner_100PercentDropRate_SpawnsOnTargetHit` verifying that multiple enemy hits consistently spawn drops at the exact death positions.
  - Add test `HazardMine_GracePeriod_ImmuneToPlayerContactUntilArmed` verifying that a mine with 0.75s arming delay does not damage or consume during arming, but detonates immediately once `TickArming` advances past 0.75s.
  - Add test `HazardMine_ArmingExpiry_RestoresFullAlpha` verifying sprite alpha modulation and complete restoration to 1.0f.
- Non-functional:
  - All existing test fixtures (`HazardAndHUDTests`, `FullSystemIntegrationTests`, `CombatAndDefenseTests`, `TargetControllerTests`) compile cleanly and pass without regressions.

## Architecture
Tests run under Unity EditMode (`[Test]`), utilizing manual delta-time injection (`UpdateSpawner(float dt)`, `TickArming(float dt)`) and direct component invocation for deterministic verification without entering PlayMode.

## Related Code Files
- Modify: `Assets/Editor/Tests/HazardAndHUDTests.cs`

## Implementation Steps
1. In `Assets/Editor/Tests/HazardAndHUDTests.cs`:
   - Update `HazardSpawner_BalancedRate_DoesNotSpawnAsManyAsTargetB`:
     - Change line 551:
       ```csharp
       Assert.IsFalse(spawner.AutoSpawn, "AutoSpawn must default to false so hazards do not spawn passively on map.");
       Assert.AreEqual(1.0f, spawner.EnemyDropChance, 0.001f, "EnemyDropChance must default to 1.0 (100%) so every defeated enemy drops an item.");
       Assert.AreEqual(0.25f, spawner.HazardMineWeight, 0.001f, "Hazard Mine (X) weight must be 0.25.");
       Assert.AreEqual(0.25f, spawner.SupplyCrateWeight, 0.001f, "Supply Crate (Y) weight must be 0.25.");
       Assert.AreEqual(0.50f, spawner.GemCoreWeight, 0.001f, "Gem Core (Z) weight must be 0.50.");
       ```
   - Add new test:
     ```csharp
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
     ```
   - Add new test:
     ```csharp
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
     ```
   - Add new test:
     ```csharp
     [Test]
     public void HazardMine_GracePeriod_ImmuneToPlayerContactUntilArmed()
     {
         GameObject playerGo = new GameObject("Player");
         disposables.Add(playerGo);
         PlayerController player = playerGo.AddComponent<PlayerController>();
         PlayerStats stats = playerGo.AddComponent<PlayerStats>();
         playerGo.AddComponent<CircleCollider2D>();
         int startingHp = stats.CurrentHp;

         GameObject mineGo = new GameObject("Mine");
         disposables.Add(mineGo);
         InteractiveEntity mine = mineGo.AddComponent<InteractiveEntity>();
         mine.Type = EntityType.HazardMine;
         mine.EnsureComponents();
         mine.SetArmingDelay(0.75f);

         Assert.IsFalse(mine.IsArmed);

         // Place mine at player position during arming period
         mineGo.transform.position = playerGo.transform.position;
         mine.TickArming(0.3f);
         mine.CheckPlayerCollision();

         Assert.IsFalse(mine.IsConsumed, "Mine must not detonate while arming.");
         Assert.AreEqual(startingHp, stats.CurrentHp, "Player must not take damage while mine is arming.");

         // Advance time past remaining arming window
         mine.TickArming(0.5f);
         Assert.IsTrue(mine.IsArmed, "Mine must be armed after 0.8s total.");

         mine.CheckPlayerCollision();
         Assert.IsTrue(mine.IsConsumed, "Mine must detonate once fully armed.");
         Assert.Less(stats.CurrentHp, startingHp, "Player must take damage once mine is armed.");
     }
     ```
   - Add new test:
     ```csharp
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
     ```
2. Verify all tests pass via `ak plan validate`.

## Success Criteria
- [x] All 4 new tests compile cleanly and pass.
- [x] Modified `HazardSpawner_BalancedRate_DoesNotSpawnAsManyAsTargetB` test passes.
- [x] Existing `FullSystemIntegrationTests` suite passes without error.

## Risk Assessment
- **Risk:** Existing tests asserting on old default weights or drop chances.
  - *Mitigation:* Inspected all grep occurrences of `EnemyDropChance` and `HazardSpawner` in `Editor/Tests/`; only `HazardAndHUDTests.cs:551` asserts on the old threshold.
