---
phase: 3
title: "Combat System & Input Integration"
status: pending
priority: P1
effort: "1h"
dependencies: ["phase-02-laser-beam-controller"]
---

# Phase 3: Combat System & Input Integration

## Overview
Connects `PlayerCombatSystem` and `PlayerController` to maintain the `LaserBeam` when `isAttackHeld` is active, seamlessly handling weapon switching, temporary buff expiration, and discrete projectile coexistence.

## Requirements
- Functional:
  - `PlayerCombatSystem` owns an instance of `LaserBeam` (instantiated at runtime or attached as child).
  - Expose `MaintainLaserBeam(bool isHeld, Vector2 direction, Transform firePoint)` in `PlayerCombatSystem`.
  - In `PlayerController.Update()`:
    - If `currentWeapon == WeaponType.Laser`, call `MaintainLaserBeam(isAttackHeld, ...)`.
    - If `currentWeapon != WeaponType.Laser`, ensure the laser beam is deactivated.
  - When the 10.0s temporary weapon timer expires in `PlayerCombatSystem.UpdateCombat()`, immediately deactivate the beam and transition to `WeaponType.Blaster`.
  - If the player manually changes weapons, deactivate the laser beam immediately.
- Non-functional:
  - Responsive, frame-accurate start and cutoff without audio clipping or visual lingering.

## Architecture
- `PlayerCombatSystem`:
  - Adds `[SerializeField] private LaserBeam laserBeamInstance`.
  - In `GrantTemporaryWeapon(WeaponType.Laser, 10.0f)`, equips the laser.
  - In `SelectWeapon()`, calls `laserBeam?.SetBeamActive(false)` if changing away from Laser.
- `PlayerController`:
  - In `Update()`, checks if `combatSystem.CurrentWeapon == WeaponType.Laser`.
  - Passes `isAttackHeld` to `MaintainLaserBeam()`.

## Related Code Files
- Modify: `Assets/Scripts/Player/PlayerCombatSystem.cs`
- Modify: `Assets/Scripts/Player/PlayerController.cs`

## Implementation Steps
1. In `PlayerCombatSystem.cs`:
   - Add reference and lazy initialization for `LaserBeam`.
   - Add `MaintainLaserBeam(bool isHeld, Vector2 direction, Transform firePoint)`.
   - In `UpdateCombat(float deltaTime)`, ensure expiry immediately shuts down beam.
   - In `SelectWeapon(WeaponType weapon)`, ensure non-laser weapons shut down beam.
2. In `PlayerController.cs`:
   - In `Update()`, route `isAttackHeld` into `combatSystem.MaintainLaserBeam` when laser is equipped.
   - When firing discrete weapons, continue using `FireProjectile()`.

## Success Criteria
- [ ] Holding attack with Laser equipped keeps the beam firing continuously.
- [ ] Releasing attack stops the beam immediately.
- [ ] When the 10-second timer hits 0 while holding attack, the laser cuts off instantly and reverts to Blaster autofire.

## Risk Assessment
- *Risk:* Double firing (instantiating discrete bullets while also firing laser).
- *Mitigation:* Explicit check: if `currentWeapon == WeaponType.Laser`, bypass `FireCurrent()` and only drive `MaintainLaserBeam()`.
