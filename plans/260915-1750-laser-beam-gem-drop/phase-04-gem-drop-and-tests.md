---
phase: 4
title: "Gem Drop 50/50 Mechanics & Verification Tests"
status: pending
priority: P1
effort: "1h30m"
dependencies: ["phase-03-combat-and-input-integration"]
---

# Phase 4: Gem Drop 50/50 Mechanics & Verification Tests

## Overview
Updates `CollisionEffectDispatcher.ResolveGemCore` to grant either Rapid Missile or Laser Beam with 50/50 probability, adds a test hook for deterministic unit testing, and writes comprehensive NUnit tests covering both weapons, continuous firing, and expiration lifecycles.

## Requirements
- Functional:
  - In `CollisionEffectDispatcher.ResolveGemCore`, roll a 50% chance for `WeaponType.Laser` and 50% for `WeaponType.Missile`.
  - Provide public static delegate `Func<WeaponType> GemWeaponSelector` with default `() => UnityEngine.Random.value < 0.5f ? WeaponType.Laser : WeaponType.Missile`.
  - Update `HazardAndHUDTests.cs:228` and add new tests in `CombatAndDefenseTests.cs` verifying:
    - Gem pickup granting Laser Beam for 10.0s.
    - Gem pickup granting Rapid Missile for 10.0s.
    - Holding attack maintaining laser beam state.
    - Laser beam piercing multiple targets and applying tick damage.
    - Instant cutoff on timer expiration.
- Non-functional:
  - 100% test pass rate with zero flaky or stochastic tests.

## Architecture
- `CollisionEffectDispatcher`:
  - `public static Func<WeaponType> GemWeaponSelector`
  - In `ResolveGemCore()`:
    ```csharp
    WeaponType granted = GemWeaponSelector != null ? GemWeaponSelector() : WeaponType.Missile;
    player.CombatSystem.GrantTemporaryWeapon(granted, 10.0f);
    ```

## Related Code Files
- Modify: `Assets/Scripts/Combat/CollisionEffectDispatcher.cs`
- Modify: `Assets/Editor/Tests/HazardAndHUDTests.cs`
- Modify: `Assets/Editor/Tests/CombatAndDefenseTests.cs`

## Implementation Steps
1. In `CollisionEffectDispatcher.cs`:
   - Declare `GemWeaponSelector`.
   - Update `ResolveGemCore()` to grant the selected weapon.
2. In `HazardAndHUDTests.cs`:
   - In `SetUp`/`TearDown`, ensure `GemWeaponSelector` is cleanly configured and reset.
   - Assert `GemCore_Collision` grants temporary weapon cleanly.
3. In `CombatAndDefenseTests.cs`:
   - Add `PlayerCombatSystem_LaserBeam_ActivatesOnHold_AndDeactivatesOnRelease`.
   - Add `PlayerCombatSystem_LaserBeam_ExpiresAfterTenSeconds`.
   - Add `PlayerCombatSystem_LaserBeam_PiercesAndDamagesMultipleTargets`.
   - Add `CollisionEffectDispatcher_GemCore_GrantsMissileOrLaserBasedOnRoll`.
4. Run static analysis and verify all tests pass.

## Success Criteria
- [ ] Collecting Gem Core grants Missile or Laser with 50/50 probability.
- [ ] Unit tests test both 10s Missile and 10s Laser outcomes deterministically.
- [ ] All tests pass in `Assets/Editor/Tests/`.

## Risk Assessment
- *Risk:* Static selector state bleeding across different unit test runs.
- *Mitigation:* Explicitly reset `CollisionEffectDispatcher.GemWeaponSelector = null` in `[TearDown]`.
