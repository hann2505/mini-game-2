---
phase: 2
title: "TargetController Additive Refactor"
status: complete
priority: P1
effort: "2h"
dependencies: [1]
---

# Phase 2: TargetController Additive Refactor

## Overview
Enhances `TargetController` to support `TargetProfile` archetypes, event-based hit notifications, and pooled lifecycle management while strictly preserving 100% backward compatibility with all legacy NUnit editor tests.

## Requirements
- Functional: Add `Initialize(TargetProfile profile, ...)` overload that applies archetype kinematic multipliers and sprite.
- Functional: Add `public static event Action<TargetController, TargetProfile, Vector3> OnTargetHit` fired on projectile collision with impact world position.
- Functional: Add `bool IsPooled { get; set; }` property:
  - If `IsPooled == false` (legacy/test mode): preserves original behavior (`RespawnAtOppositeEdge()`, local audio playback).
  - If `IsPooled == true` (pool mode): deactivates target (`gameObject.SetActive(false)`) on hit or boundary wrap, handing recycling control to `TargetSpawner`.
- Non-functional: 100% pass rate on existing `TargetControllerTests` and `GameControllerTests` without altering test code.

## Architecture
```mermaid
flowchart TD
    Hit["Projectile Trigger Hit"] --> CheckPooled{"IsPooled?"}
    CheckPooled -->|false (Legacy/Test)| A["Play Local Sound & RespawnAtOppositeEdge()"]
    CheckPooled -->|true (Pooled)| B["Fire OnTargetHit Event & SetActive(false)"]
    
    Wrap["CheckBoundaryWrap()"] --> CheckWrapPooled{"IsPooled?"}
    CheckWrapPooled -->|false (Legacy/Test)| C["RespawnAtOppositeEdge()"]
    CheckWrapPooled -->|true (Pooled)| D["SetActive(false) -> Spawner Recycles"]
```

## Related Code Files
- Modify: `Assets/Scripts/Enemy/TargetController.cs`
- Modify / Extend: `Assets/Editor/Tests/TargetControllerTests.cs` (add tests for `IsPooled` mode and `TargetProfile` initialization)

## Implementation Steps
1. Add `TargetProfile currentProfile` field and property to `TargetController`.
2. Implement `public void ApplyProfile(TargetProfile profile)`: sets sprite, updates speed/frequency/amplitude by multipliers.
3. Expose `public bool IsPooled { get; set; } = false;`.
4. Refactor `HandleHitByProjectile(GameObject projectileObj)`:
   - Always destroy projectile.
   - Fire `OnTargetHit?.Invoke(this, currentProfile, transform.position)`.
   - If `IsPooled`, deactivate `gameObject.SetActive(false)`. If not pooled, call `PlayExplosionSound()` and `RespawnAtOppositeEdge()`.
5. Refactor `CheckBoundaryWrap()`:
   - If past exit threshold: if `IsPooled`, deactivate `gameObject.SetActive(false)`. If not pooled, call `RespawnAtOppositeEdge()`.
6. Run `dotnet test` or execute NUnit tests to prove zero test regressions on legacy behaviors.

## Success Criteria
- [ ] `TargetController` supports `TargetProfile` assignment and kinematic scaling.
- [ ] `OnTargetHit` event fires with target instance, profile, and impact coordinate.
- [ ] `IsPooled` toggle prevents zombie self-respawns in pool mode.
- [ ] All existing `TargetControllerTests` pass without modification.

## Risk Assessment
- Risk: Inverting legacy behavior breaks automated test suite.
  - Mitigation: `IsPooled` defaults strictly to `false`; legacy constructors and initializers set multipliers to 1.0f.
