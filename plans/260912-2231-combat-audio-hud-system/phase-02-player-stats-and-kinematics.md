---
phase: 2
title: "Player Stats and Dynamic Kinematics Modifiers"
status: completed
priority: P1
effort: "3h"
dependencies: ["phase-01-audio-infrastructure-and-ui-toggles"]
---

# Phase 2: Player Stats and Dynamic Kinematics Modifiers

## Overview
Establishes the state and vital statistics for Object A (Player Spacecraft) through a decoupled `PlayerStats` component, and upgrades the locomotion engine in `PlayerController` to support dynamic speed buffs (+50% haste) and debuffs (-40% slow) with automatic countdown decay timers, while preserving strict viewport clamping.

## Requirements
- Functional:
  - `PlayerStats` tracks Current/Max Health (`100/100`), Current/Max Armor (`50/50`), Gold (`int`), and Diamonds (`int`).
  - Damage absorption pipeline: Incoming damage is absorbed by Armor first; remaining excess damage depletes Health.
  - Armor/Health healing and currency accumulation methods.
  - Stackable / timed dynamic speed modifier system: `ApplySpeedModifier(float factor, float duration)`.
  - Effective speed calculation: `EffectiveMoveSpeed = BaseMoveSpeed * PlayerStats.EffectiveSpeedMultiplier`.
  - Automatic decay of speed modifiers back to baseline `1.0f` when duration expires.
  - Strongly-typed C# events: `OnHealthChanged(int current, int max)`, `OnArmorChanged(int current, int max)`, `OnCurrencyChanged(int gold, int diamonds)`, `OnSpeedModifierChanged(float multiplier)`.
- Non-functional:
  - Zero GC allocation in per-frame velocity calculation and timer ticking.
  - Backward compatibility: `PlayerControllerTests` must continue to pass without regressions.
  - Full NUnit EditMode testability via explicit `Tick(float deltaTime)` parameter support.

## Architecture
- `KinematicsGame.Player.PlayerStats`: Component on Player GameObject managing vitals, currency, and speed modifiers.
- `KinematicsGame.Player.PlayerController`: Consumes `PlayerStats.EffectiveSpeedMultiplier` inside `HandleMovement()`.

## Related Code Files
- Create:
  - `Assets/Scripts/Player/PlayerStats.cs`
  - `Assets/Editor/Tests/PlayerStatsTests.cs`
- Modify:
  - `Assets/Scripts/Player/PlayerController.cs` (reference `PlayerStats`, scale speed by multiplier)

## Implementation Steps
1. Create `PlayerStats.cs` with fields for `currentHealth`, `maxHealth`, `currentArmor`, `maxArmor`, `gold`, `diamonds`, `speedMultiplier`, and `speedModifierDuration`.
2. Implement `TakeDamage(int damage)`, `RestoreHealth(int amount)`, `RestoreArmor(int amount)`, `AddCurrency(int gold, int diamonds)`, and `ApplySpeedModifier(float multiplier, float duration)`. Add `UpdateModifiers(float deltaTime)`.
3. Update `PlayerController.cs`: In `Awake()`, resolve `PlayerStats` via `GetComponent<PlayerStats>()`. In `HandleMovement()`, compute movement scaled by `MoveSpeed * (playerStats != null ? playerStats.EffectiveSpeedMultiplier : 1f)`.
4. Create NUnit EditMode tests in `PlayerStatsTests.cs` validating damage pipeline (armor absorbs before HP), currency addition, speed debuff application, and timer expiration after simulated delta times.

## Success Criteria
- [ ] Taking damage correctly depletes Armor before reducing Health.
- [ ] Speed debuffs (e.g. 0.6x) and speed buffs (e.g. 1.5x) scale Player velocity accurately.
- [ ] Speed modifiers expire automatically after their duration elapses, restoring multiplier to 1.0x.
- [ ] Position clamping strictly restrains player inside camera bounds regardless of current speed.
- [ ] All tests in `PlayerStatsTests.cs` and existing `PlayerControllerTests.cs` pass 100% green.

## Risk Assessment
- **Risk**: Player speed becoming 0 or negative during intense debuff stacking.
  - *Mitigation*: Clamp `EffectiveSpeedMultiplier` to a minimum floor of `0.2f` so the player never becomes completely stuck unless an explicit stun effect is applied.
- **Risk**: Regression in legacy `PlayerControllerTests` that instantiate `PlayerController` without `PlayerStats`.
  - *Mitigation*: Fall back gracefully to `1.0f` multiplier if `PlayerStats` component is null on the GameObject.
