---
phase: 4
title: "Interactive Hazards and Extended Player HUD"
status: completed
priority: P1
effort: "3h"
dependencies: ["phase-03-combat-attacks-and-defenses"]
---

# Phase 4: Interactive Hazards and Extended Player HUD

## Overview
Introduces the interactive world entity ecosystem consisting of Object X (Hazard Mine), Object Y (Tech Supply Crate), and Object Z (Gem Core), delivering a verifiable suite of 9 distinct gameplay effects upon collision with Object A. Introduces a periodic `HazardSpawner` drifting hazards and pickups into the arena, and expands `GameHUDController` to render live player vitals (HP, Armor/Shield), currencies (Gold, Diamonds), and active weapon/skill readiness.

## Requirements
- Functional:
  - **Entity Matrix (Objects X, Y, Z)**:
    - **Object X (Hazard Mine)**:
      - *Effect 1 (Explosion)*: Spawns explosion VFX keyframes and plays `explosion.wav`.
      - *Effect 2 (Despawn)*: Object X immediately deactivates/destroys self.
      - *Effect 3 (Damage)*: Deducts 25 Armor points (or HP if armor is depleted).
      - *Effect 4 (Speed Debuff)*: Applies a -40% speed penalty (`0.6x`) for 3.0s.
    - **Object Y (Tech Supply Crate)**:
      - *Effect 5 (Shield/Armor Restore)*: Restores +50 Armor and grants active Shield Barrier if depleted.
      - *Effect 6 (Speed Buff)*: Applies a +50% haste speed boost (`1.5x`) for 4.0s.
      - *Effect 7 (Weapon Upgrade)*: Unlocks/switches primary weapon to Heavy Missile mode for 10.0s.
    - **Object Z (Gem Core / Bounty)**:
      - *Effect 8 (Currency Windfall)*: Awards +50 Gold and +5 Diamonds, playing `eat.ogg`.
      - *Effect 9 (Subsidiary Rewards)*: Spawns 3 mini-bonus collectible pickups scattered nearby in the viewport.
  - **Hazard Spawner (`HazardSpawner.cs`)**:
    - Periodically spawns Objects X, Y, Z at the entry edge of the viewport, drifting along the primary kinematic axis alongside enemies.
  - **Expanded HUD (`GameHUDController.cs`)**:
    - Health: TextMeshPro label and slider bar showing `HP: 100/100` (green to red gradient).
    - Armor / Shield: Display showing `ARMOR: 50/50` and `SHIELD: ACTIVE / READY`.
    - Currencies: TextMeshPro counters showing Gold count (`🪙 0`) and Diamond count (`💎 0`).
    - Weapon & Defense: Icon showing currently selected weapon (Blaster/Missile/Bomb) and cooldown overlays for Shield and EMP.
- Non-functional:
  - Total distinct effects triggered across X, Y, Z strictly equals 9 ($\ge 6$).
  - Zero-GC string caching on HUD text formatting (`displayedHP != newHP`).
  - Full NUnit EditMode testability for collision resolution and HUD state updates.

## Architecture
- `KinematicsGame.Combat.InteractiveEntity`: Attached to prefabs of X, Y, Z, carrying enum `EntityType { HazardMine, SupplyCrate, GemCore }`.
- `KinematicsGame.Combat.CollisionEffectDispatcher`: Component on Player (or called by InteractiveEntity) applying the 9 effects to `PlayerStats`, `PlayerCombatSystem`, and `AudioManager`.
- `KinematicsGame.Combat.HazardSpawner`: Manages pooling and periodic boundary entry for X, Y, Z.
- `KinematicsGame.UI.GameHUDController`: Subscribes to events from `PlayerStats` and `PlayerCombatSystem` to update UI elements.

## Related Code Files
- Create:
  - `Assets/Scripts/Combat/InteractiveEntity.cs`
  - `Assets/Scripts/Combat/CollisionEffectDispatcher.cs`
  - `Assets/Scripts/Combat/HazardSpawner.cs`
  - `Assets/Editor/Tests/HazardAndHUDTests.cs`
- Modify:
  - `Assets/Scripts/UI/GameHUDController.cs` (add UI bindings for HP, Armor, Gold, Diamonds, Weapons)
  - `Assets/Scripts/Player/PlayerController.cs` (handle `OnTriggerEnter2D` with InteractiveEntity)

## Implementation Steps
1. Create `InteractiveEntity.cs` defining `EntityType`, point value, and drift kinematics matching current game orientation.
2. Create `CollisionEffectDispatcher.cs` with methods `ResolveCollision(InteractiveEntity entity, PlayerController player)`. Implement the exact 9 effects across X, Y, Z.
3. Create `HazardSpawner.cs` with configurable spawn intervals (default 8.0s), spawning X, Y, Z with weighted random probabilities.
4. Update `GameHUDController.cs`: Add serialized fields for `hpLabel`, `hpSlider`, `armorLabel`, `goldLabel`, `diamondLabel`, and `weaponIcon`. Subscribe to `PlayerStats` and `PlayerCombatSystem` events in `OnEnable()`.
5. Write NUnit EditMode tests in `HazardAndHUDTests.cs` simulating collisions with X, Y, Z and asserting exact HP drops, speed debuffs, shield deployments, currency gains, and HUD text updates.

## Success Criteria
- [ ] Colliding with Object X triggers explosion VFX/SFX, despawns X, deducts 25 HP/Armor, and applies 40% slow debuff.
- [ ] Colliding with Object Y restores 50 Armor, grants Shield, applies 50% haste, and equips Missile mode.
- [ ] Colliding with Object Z awards +50 Gold and +5 Diamonds and drops 3 mini-bonus pickups.
- [ ] Total distinct effects verified equals 9 ($\ge 6$).
- [ ] HUD displays real-time HP, Armor, Gold, Diamonds, and active weapon status.
- [ ] All tests in `HazardAndHUDTests.cs` pass 100% green.

## Risk Assessment
- **Risk**: Player colliding with multiple hazards simultaneously causing frame-rate drops.
  - *Mitigation*: Flag `InteractiveEntity` as consumed immediately on first contact to prevent duplicate triggers in the same frame.
- **Risk**: HUD layout breaking on narrow screen aspect ratios.
  - *Mitigation*: Anchor stat bars to top-left and top-center, keeping right-anchored score/combo labels intact without overlap.
