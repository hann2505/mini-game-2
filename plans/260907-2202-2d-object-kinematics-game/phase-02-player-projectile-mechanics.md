---
phase: 2
title: "Player Controller & Projectile Mechanics"
status: completed
priority: P1
effort: "1h"
dependencies: ["phase-01-start"]
---

# Phase 2: Player Controller & Projectile Mechanics

## Overview
Implements Object A (Player spaceship) with responsive 4-way movement clamped to screen bounds, and Object C (Projectile bullet) instantiated at Object A's position upon touch or mouse click with customizable speed and direction.

## Requirements
- Functional:
  - Object A moves in all 4 directions (Up, Down, Left, Right) using the New Input System `Move` action (WASD / Arrow keys / Gamepad / Touch).
  - Object A's movement is strictly clamped within the camera's world bounds using `ViewportManager` and the sprite's extents.
  - Object C is instantiated at Object A's position when a touch or mouse click occurs (New Input System `Attack` action).
  - Object C has customizable direction (`Vector2`) and speed (`float`) exposed in the Inspector.
  - Object C automatically destroys itself once it leaves the camera viewport to prevent memory leaks.
- Non-functional:
  - Use `context.performed` on the `Attack` action to prevent double-firing on devices that simulate mouse clicks from touch taps.
  - Zero garbage generation during steady-state movement.

## Architecture
- `PlayerController.cs`:
  - Uses `PlayerInput` referencing `InputSystem_Actions.inputactions`.
  - Reads `Vector2 moveInput` in `OnMove` and translates `transform.position` with configurable `moveSpeed`.
  - Clamps position using `ViewportManager.Instance` limits minus `spriteRenderer.bounds.extents`.
  - Subscribes to `OnAttack` to instantiate `projectilePrefab` at `transform.position` (or dedicated `firePoint`).
  - Plays audio SFX (`click.ogg` / `bomb.mp3`) on fire.
- `Projectile.cs`:
  - Fields: `[SerializeField] private Vector2 direction = Vector2.right`, `[SerializeField] private float speed = 10f`.
  - Moves via `transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World)`.
  - Checks position against `ViewportManager.Instance` bounds + margin; calls `Destroy(gameObject)` when outside.
  - Carries a 2D Trigger Collider (`CircleCollider2D` or `BoxCollider2D`) and Kinematic `Rigidbody2D`.

## Related Code Files
- Create: `Assets/Scripts/Player/PlayerController.cs`
- Create: `Assets/Scripts/Combat/Projectile.cs`

## Implementation Steps
1. Create `Assets/Scripts/Player/` and `Assets/Scripts/Combat/` directories.
2. Implement `Projectile.cs` with configurable speed, direction, movement, and viewport boundary exit destruction.
3. Implement `PlayerController.cs` with New Input System callback integration, clamped 4-way movement, and projectile instantiation.
4. Construct Object A prefab using `Assets/Sprites/Characters/Players/Ships/spaceship1.png` and Object C prefab using `Assets/Sprites/Weapons/Bullets/bullet1.png`.

## Success Criteria
- [x] Pressing W/A/S/D or arrow keys moves Object A smoothly in all 4 directions.
- [x] Object A stops at screen edges and cannot move off-screen.
- [x] Clicking mouse or tapping screen fires Object C from Object A immediately.
- [x] Object C travels continuously in its configured direction and disappears cleanly when leaving the screen.
- [x] No double-firing occurs on single tap events on touch devices.

## Risk Assessment
- Risk: Rapid clicking spawns hundreds of projectile instances leading to frame drops.
  - Mitigation: Add a small fire cooldown timer (e.g. 0.15s) in `PlayerController.cs`.
- Risk: Input System action map not loaded or enabled.
  - Mitigation: Use `PlayerInput` component in "Send Messages" or "Unity Events" mode with auto-enabled default map.
