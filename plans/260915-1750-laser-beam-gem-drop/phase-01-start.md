---
phase: 1
title: "Weapon Data Contracts & HUD Alignment"
status: pending
priority: P1
effort: "30m"
dependencies: []
---

# Phase 1: Weapon Data Contracts & HUD Alignment

## Overview
Extends the combat data structures to support `WeaponType.Laser = 3`, adjusts weapon cycling logic to protect standard loadout boundaries, and ensures the UI HUD formats temporary laser powerups cleanly.

## Requirements
- Functional:
  - Add `Laser = 3` to `WeaponType` in `PlayerCombatSystem.cs`.
  - Fix `SelectNextWeapon()` and `SelectPreviousWeapon()` so manual cycling only iterates over standard weapons (0..2), or gracefully supports temporary states.
  - In `GameHUDController.cs`, ensure `boundCombat.CurrentWeapon` formats `Laser` cleanly alongside the existing `WEAPON: <Type> (<Timer>s)` text.
- Non-functional:
  - Backward compatibility with existing weapon selection tests.

## Architecture
- `WeaponType`: enum in `KinematicsGame.Player`.
- Standard loadout remains: `Blaster (0)`, `Missile (1)`, `Bomb (2)`.
- `Laser (3)` is a temporary powerup weapon granted via `GrantTemporaryWeapon(WeaponType.Laser, 10.0f)`.

## Related Code Files
- Modify: `Assets/Scripts/Player/PlayerCombatSystem.cs`
- Modify: `Assets/Scripts/UI/GameHUDController.cs`

## Implementation Steps
1. In `PlayerCombatSystem.cs`:
   - Add `Laser = 3` to `WeaponType`.
   - Update `SelectNextWeapon()` / `SelectPreviousWeapon()` so player cycling between basic weapons (Keys 1-3) doesn't enter `Laser` unless desired, or keeps cycling across standard weapons while defaulting back to `Blaster` upon buff expiry.
2. In `GameHUDController.cs`:
   - Verify label formatting for `boundCombat.CurrentWeapon` handles `WeaponType.Laser` with clean string output.

## Success Criteria
- [ ] `WeaponType.Laser` is defined and compiles cleanly.
- [ ] Standard cycling keys (1, 2, 3) work as before without skipping or index out-of-range errors.
- [ ] HUD displays active temporary laser countdown when granted.

## Risk Assessment
- *Risk:* Hardcoded modulo in cycling methods breaking on enum expansion.
- *Mitigation:* Explicitly clamp/cycle standard weapons [0..2] for manual keyboard inputs.
