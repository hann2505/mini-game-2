---
phase: 2
title: "HazardSpawner Drop Pipeline & Scene Synchronization"
status: completed
priority: P1
effort: "45m"
dependencies: [1]
---

# Phase 2: HazardSpawner Drop Pipeline & Scene Synchronization

## Overview
Reconfigures `HazardSpawner` to disable passive map/perimeter spawning and viewport prewarming, guarantees 100% item drop on enemy takedown with reward-favored weights (50% Gem, 25% Crate, 25% Mine), attaches the 0.75s arming delay to dropped mines, and synchronizes `SampleScene.unity` and `SceneSetupHelper.cs`.

## Requirements
- Functional:
  - `HazardSpawner.AutoSpawn` defaults to `false`.
  - `HazardSpawner.PrewarmViewport` defaults to `false` with `initialPrewarmCount = 0`.
  - `HazardSpawner.EnemyDropChance` defaults to `1.0f` (100%).
  - Spawn weights default to:
    - `GemCoreWeight = 0.50f` (50%)
    - `SupplyCrateWeight = 0.25f` (25%)
    - `HazardMineWeight = 0.25f` (25%)
  - `HazardSpawner.HandleTargetHit`: on target defeat, rolls an entity type and calls `SpawnHazard(type, hitPosition, armingDelay: 0.75f)`.
  - If a `HazardMine` is spawned as a drop, its arming delay is initialized to 0.75 seconds.
- Non-functional:
  - `SpawnInterval` remains <= 3.0f to satisfy `FullSystemIntegrationTests.cs:463`.
  - `SampleScene.unity` YAML serialized values match code defaults to prevent editor override drift.

## Architecture
`HazardSpawner` acts as the event-driven factory:
1. Subscribes to `TargetController.OnTargetHit` during `OnEnable()`.
2. Upon hit event at `hitPosition`:
   - Checks `enemyDropChance >= 1.0f || Random.value <= enemyDropChance`.
   - Selects type via `GetRandomEntityType()` (weighted 50/25/25).
   - Instantiates entity via `SpawnHazard(type, hitPosition, armingDelay)`.
   - If `type == EntityType.HazardMine`, initializes `SetArmingDelay(0.75f)`.
3. Does not run `UpdateSpawner` when `autoSpawn == false`.

## Related Code Files
- Modify: `Assets/Scripts/Combat/HazardSpawner.cs`
- Modify: `Assets/Editor/SceneSetupHelper.cs`
- Modify: `Assets/Scenes/SampleScene.unity`

## Implementation Steps
1. In `Assets/Scripts/Combat/HazardSpawner.cs`:
   - Update serialized field defaults:
     ```csharp
     [SerializeField] private bool autoSpawn = false;
     [SerializeField] private bool prewarmViewport = false;
     [SerializeField] [Range(0, 8)] private int initialPrewarmCount = 0;
     [SerializeField] [Range(0f, 1f)] private float enemyDropChance = 1.0f;
     [SerializeField] private float hazardMineWeight = 0.25f;
     [SerializeField] private float supplyCrateWeight = 0.25f;
     [SerializeField] private float gemCoreWeight = 0.50f;
     ```
   - Update `SpawnHazard` signature to accept optional arming delay:
     ```csharp
     public GameObject SpawnHazard(EntityType type, Vector3? explicitPosition = null, float armingDelay = 0f)
     ```
   - In `SpawnHazard`, after getting `InteractiveEntity entity = go.GetComponent<InteractiveEntity>();`:
     ```csharp
     if (entity != null && type == EntityType.HazardMine && armingDelay > 0f)
     {
         entity.SetArmingDelay(armingDelay);
     }
     ```
   - Update `HandleTargetHit`:
     ```csharp
     private void HandleTargetHit(TargetController target, TargetProfile profile, Vector3 hitPosition)
     {
         if (enemyDropChance > 0f && (enemyDropChance >= 1f || Random.value <= enemyDropChance))
         {
             EntityType type = GetRandomEntityType();
             SpawnHazard(type, hitPosition, armingDelay: 0.75f);
         }
     }
     ```
2. In `Assets/Editor/SceneSetupHelper.cs`:
   - Locate lines 407–418:
     ```csharp
     hazardSpawner.SpawnInterval = 3.0f;
     hazardSpawner.MinSpawnInterval = 2.0f;
     hazardSpawner.SpawnChance = 0.8f;
     hazardSpawner.MaxSpawnPerCycle = 1;
     hazardSpawner.MultiSpawnChance = 0.0f;
     hazardSpawner.PrewarmViewport = false;
     hazardSpawner.InitialPrewarmCount = 0;
     hazardSpawner.AutoSpawn = false;
     hazardSpawner.EnemyDropChance = 1.0f;
     hazardSpawner.HazardMineWeight = 0.25f;
     hazardSpawner.SupplyCrateWeight = 0.25f;
     hazardSpawner.GemCoreWeight = 0.50f;
     ```
3. In `Assets/Scenes/SampleScene.unity`:
   - Update `HazardSpawner` component block (around line 133–175) to reflect `autoSpawn: 0`, `enemyDropChance: 1`, `hazardMineWeight: 0.25`, `supplyCrateWeight: 0.25`, `gemCoreWeight: 0.5`, `spawnInterval: 3`.

## Success Criteria
- [x] In `HazardSpawner.cs`, `AutoSpawn` is `false` and `EnemyDropChance` is `1.0f` by default.
- [x] Drops created via `HandleTargetHit` receive `armingDelay = 0.75f` when type is `HazardMine`.
- [x] Scene configuration in `SceneSetupHelper.cs` and `SampleScene.unity` match the new defaults.
- [x] No compilation errors or warnings.

## Risk Assessment
- **Risk:** Existing `SampleScene.unity` YAML properties overriding C# defaults when opened in Unity.
  - *Mitigation:* Explicitly update the scene YAML and `SceneSetupHelper.cs` in sync.
- **Risk:** `FullSystemIntegrationTests.cs:463` checks `Assert.LessOrEqual(hazardSpawner.SpawnInterval, 3.0f)`.
  - *Mitigation:* Keep `SpawnInterval = 3.0f` so the spawner config remains compliant with existing integration tests.
