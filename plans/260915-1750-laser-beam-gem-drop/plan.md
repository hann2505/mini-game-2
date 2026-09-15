---
title: "Hold-to-Shoot Continuous Laser Beam & 50/50 Gem Drop Mechanic"
description: "Implementation plan for hold-to-fire piercing laser beam weapon, 50/50 drop chance on Gem Core, and full test suite coverage."
status: complete
priority: P1
effort: "4h"
tags: ["combat", "player", "weapons", "gem-drop", "kinematics"]
created: 2026-09-15
completed: 2026-09-16
---

# Hold-to-Shoot Continuous Laser Beam & 50/50 Gem Drop Mechanic

## Overview
Adds a sustained, piercing Laser Beam attack mechanic and integrates it into the temporary powerup system. When picking up a Gem Core (Object Z), players have a 50/50 chance to receive either the existing Rapid Homing Missile or the new Laser Beam for 10 seconds. The Laser Beam is fired continuously by holding the attack button (Mouse, Touch, or Space), piercing all enemies along its path with tick-based damage and instant deactivation upon release or buff expiration.

## Goals

| # | Goal | Priority |
|---|------|----------|
| 1 | Extend `WeaponType` with `Laser = 3` and guard manual weapon cycling (Keys 1-3) | P1 |
| 2 | Create `LaserBeam` controller with `LineRenderer` and non-allocating 2D circle casting for piercing continuous damage | P1 |
| 3 | Connect `PlayerCombatSystem` and `PlayerController` to maintain the laser beam while `isAttackHeld` is active | P1 |
| 4 | Implement deterministic 50/50 Gem Core drop selection in `CollisionEffectDispatcher` | P1 |
| 5 | Deliver comprehensive unit tests in `CombatAndDefenseTests` and `HazardAndHUDTests` ensuring 100% test pass rate | P1 |

## Phases

| # | Phase | Status | Priority | Effort |
|---|-------|--------|----------|--------|
| 1 | [Phase 1: Weapon Data Contracts & HUD Alignment](./phase-01-start.md) | complete | P1 | 30m |
| 2 | [Phase 2: Laser Beam Controller Component](./phase-02-laser-beam-controller.md) | complete | P1 | 1h |
| 3 | [Phase 3: Combat System & Input Integration](./phase-03-combat-and-input-integration.md) | complete | P1 | 1h |
| 4 | [Phase 4: Gem Drop 50/50 Mechanics & Verification Tests](./phase-04-gem-drop-and-tests.md) | complete | P1 | 1h30m |

## Success Criteria

- [x] `WeaponType.Laser` is recognized by HUD and correctly displays `WEAPON: Laser (X.Xs)` when active.
- [x] Keyboard cycling (Keys 1-3, `SelectNextWeapon`) restricts manual switching to standard weapons (Blaster, Missile, Bomb).
- [x] Laser Beam draws cleanly from `firePoint` to viewport edge and pierces all enemies in its line without runtime GC allocations.
- [x] Laser applies damage on a tick timer (10 damage / 0.1s = 100 DPS) to all pierced targets.
- [x] Releasing the attack button or buff expiry immediately shuts off beam rendering and audio loop.
- [x] Gem Core collection deterministically rolls 50% Missile and 50% Laser, with test hooks to avoid flaky CI runs.
- [x] All existing and new tests pass cleanly with zero regression.

<!-- slug: laser-beam-gem-drop -->