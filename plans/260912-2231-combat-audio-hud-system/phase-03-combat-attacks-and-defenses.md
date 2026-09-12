---
phase: 3
title: "Offensive Arsenal and Defensive Systems"
status: completed
priority: P1
effort: "4h"
dependencies: ["phase-02-player-stats-and-kinematics"]
---

# Phase 3: Offensive Arsenal and Defensive Systems

## Overview
Equips Object A (Player) with a complete offensive and defensive combat loadout. Introduces 3 distinct attack modes (Plasma Blaster, Homing Missile, Cluster Bomb) and 2 tactical defense skills (Energy Shield Barrier, EMP Stun Wave). Implements responsive weapon selection (keys 1, 2, 3 or scroll wheel) and instant skill activation (Q for Shield, E for EMP), accompanied by dedicated visual feedback and short SFX triggers.

## Requirements
- Functional:
  - **Attack 1 (Plasma Blaster)**:
    - High-velocity direct kinetic projectile (`bullet1.png`), speed 14 units/s, cooldown 0.12s.
    - Plays `click.ogg` upon firing.
  - **Attack 2 (Homing Missile)**:
    - Target-tracking missile (`missile1.png`), accelerates from 6 to 14 units/s, steers towards nearest active `TargetController` within 90° forward cone.
    - Radial splash damage radius (1.5m), plays `bomb.mp3` launch SFX and `explosion.wav` on impact. Cooldown 1.0s.
  - **Attack 3 (Cluster Bomb)**:
    - Deployable tactical ordnance (`bomb1.png`), dropped at player location, drifts slowly forward (1.0 units/s).
    - Arms after 0.5s; detonates on contact with enemy or 1.5s timer, dealing 2.5m radial splash damage. Plays `explosion.wav`. Cooldown 2.5s.
  - **Defense 1 (Energy Shield Barrier)**:
    - Displays visual shield overlay (`shield.png`). Absorbs 100% of incoming collision damage for up to 3 hits or 8.0s duration.
    - While active, prevents HP and Armor reduction. Plays shield hit/break SFX. Cooldown 12.0s.
  - **Defense 2 (EMP Stun Wave)**:
    - Emits instantaneous radial pulse. All active `TargetController` instances have their `UpdateKinematics` motion halted (speed multiplier set to 0) for 3.0s, accompanied by a cyan tint (`#66CCFF`). Cooldown 15.0s.
  - **Input Integration**:
    - Weapons selected via keys 1, 2, 3 or mouse scroll; fired via Space / Left Click.
    - Shield triggered via Key Q; EMP Stun triggered via Key E.
- Non-functional:
  - Reuse base projectile abstractions (`BaseProjectile`) to share boundary culling and trigger hit detection.
  - Full NUnit EditMode testability for weapon cooldowns, ammo counters, shield absorption, and stun application.

## Architecture
- `KinematicsGame.Combat.BaseProjectile`: Abstract base for projectiles, handling direction, speed, and viewport despawning.
- `KinematicsGame.Combat.HomingMissile`: Subclass tracking nearest `TargetController`.
- `KinematicsGame.Combat.ClusterBomb`: Delayed fuse explosive with radial trigger overlap.
- `KinematicsGame.Player.PlayerCombatSystem`: Attached to Player, manages current weapon index, cooldowns, and firing dispatch.
- `KinematicsGame.Player.PlayerDefenseSystem`: Attached to Player, manages Shield state, hit counts, and EMP stun propagation.

## Related Code Files
- Create:
  - `Assets/Scripts/Combat/BaseProjectile.cs`
  - `Assets/Scripts/Combat/HomingMissile.cs`
  - `Assets/Scripts/Combat/ClusterBomb.cs`
  - `Assets/Scripts/Player/PlayerCombatSystem.cs`
  - `Assets/Scripts/Player/PlayerDefenseSystem.cs`
  - `Assets/Editor/Tests/CombatAndDefenseTests.cs`
- Modify:
  - `Assets/Scripts/Combat/Projectile.cs` (inherit from `BaseProjectile`)
  - `Assets/Scripts/Player/PlayerController.cs` (wire input events to combat and defense systems)
  - `Assets/Scripts/Enemy/TargetController.cs` (add `ApplyStun(float duration)` and `IsStunned` flag)

## Implementation Steps
1. Create `BaseProjectile.cs` encapsulating movement, orientation alignment, and viewport culling. Refactor `Projectile.cs` to inherit from it.
2. Create `HomingMissile.cs` with target acquisition logic (`Physics2D.OverlapCircleAll` filtering for `TargetController`) and angular steering.
3. Create `ClusterBomb.cs` with fuse timer, arming delay, and radial damage trigger (`Physics2D.OverlapCircleAll`).
4. Update `TargetController.cs`: Add `isStunned`, `stunTimer`, and `ApplyStun(float duration)`. In `UpdateKinematics()`, bypass drift and wave calculations when stunned, tinting sprite cyan.
5. Create `PlayerDefenseSystem.cs` with `ActivateShield(int hits, float duration)` and `TriggerEmpStun(float duration)`. Wire shield visual overlay.
6. Create `PlayerCombatSystem.cs` managing active weapon mode, cooldown timers, and projectile spawning.
7. Update `PlayerController.cs` to delegate attack/defense inputs to `PlayerCombatSystem` and `PlayerDefenseSystem`.
8. Write NUnit EditMode tests in `CombatAndDefenseTests.cs` verifying firing cooldowns, projectile trajectory initialization, shield hit absorption, and target stun state transitions.

## Success Criteria
- [ ] Player can fire Plasma Blaster (bullet1), Homing Missile (missile1), and Cluster Bomb (bomb1).
- [ ] Each attack plays its distinct SFX via `AudioManager` upon release.
- [ ] Energy Shield absorbs up to 3 hits or 8s duration, preserving player HP and Armor.
- [ ] EMP Stun halts all active enemy kinematics for 3.0s, tinting their sprites cyan.
- [ ] All tests in `CombatAndDefenseTests.cs` pass 100% green.

## Risk Assessment
- **Risk**: Homing missiles targeting inactive or pooled targets.
  - *Mitigation*: Filter target search only to active GameObjects (`go.activeInHierarchy`) and valid `TargetController` instances.
- **Risk**: EMP stun coroutines colliding with enemy recycling/despawn.
  - *Mitigation*: Manage stun duration via an explicit `stunTimer` float updated in `UpdateKinematics(deltaTime)` instead of relying on coroutines.
