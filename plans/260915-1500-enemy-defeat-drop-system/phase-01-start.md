---
phase: 1
title: "InteractiveEntity Arming Delay & Player Guard"
status: completed
priority: P1
effort: "45m"
dependencies: []
---

# Phase 1: InteractiveEntity Arming Delay & Player Guard

## Overview
Adds an arming delay mechanism and visual warning pulsation to `InteractiveEntity` (Object X - Hazard Mine), while patching `PlayerController.OnTriggerEnter2D` so that freshly spawned mines cannot instantly detonate on the player during point-blank kills.

## Requirements
- Functional:
  - `InteractiveEntity` must track arming state via `IsArmed => armingTimer <= 0f`.
  - Dropped Hazard Mines can be initialized with a configurable arming delay (default 0.75s) via `SetArmingDelay(float delay)`.
  - While unready (`!IsArmed`), collisions with `PlayerController` must be completely ignored by both `InteractiveEntity` and `PlayerController`.
  - Visual warning: when arming, if `spriteRenderer != null`, pulse sprite alpha smoothly between 0.35f and 1.0f. Once armed, restore alpha unconditionally to 1.0f.
- Non-functional:
  - Deterministic EditMode testing: provide `public void TickArming(float deltaTime)` so tests can advance arming time without PlayMode.
  - Generic mines not created as enemy drops must default to `armingTimer = 0f` (`IsArmed == true`) to preserve existing standalone mine behavior.

## Architecture
Both `InteractiveEntity` and `PlayerController` participate in 2D trigger detection. To prevent collision bypass from either direction:
1. `InteractiveEntity.CheckPlayerCollision()`: returns early if `!IsArmed`.
2. `InteractiveEntity.OnTriggerEnter2D(Collider2D)`: returns early if `!IsArmed`.
3. `PlayerController.OnTriggerEnter2D(Collider2D)`: verifies `entity.IsArmed` before invoking `CollisionEffectDispatcher.ResolveCollision`.

## Related Code Files
- Modify: `Assets/Scripts/Combat/InteractiveEntity.cs`
- Modify: `Assets/Scripts/Player/PlayerController.cs`

## Implementation Steps
1. In `Assets/Scripts/Combat/InteractiveEntity.cs`:
   - Declare private field `private float armingTimer = 0f;`.
   - Expose public property `public bool IsArmed => armingTimer <= 0f;`.
   - Expose public property `public float ArmingTimer => armingTimer;`.
   - Add public method `public void SetArmingDelay(float delay) { armingTimer = Mathf.Max(0f, delay); }`.
   - Add public method `public void TickArming(float deltaTime)`:
     - If `armingTimer > 0f`: decrement by `deltaTime`.
     - While `armingTimer > 0f` and `spriteRenderer != null`: modulate `spriteRenderer.color` alpha using `Mathf.PingPong(armingTimer * 8f, 0.65f) + 0.35f`.
     - When `armingTimer <= 0f` and `spriteRenderer != null`: clamp alpha back to `1.0f`.
   - In `Update()`: call `TickArming(Time.deltaTime)` before `UpdateKinematics()`.
   - In `OnTriggerEnter2D(Collider2D other)`: add guard `if (!IsArmed) return;`.
   - In `CheckPlayerCollision()`: add guard `if (!IsArmed) return;`.
2. In `Assets/Scripts/Player/PlayerController.cs`:
   - Locate `OnTriggerEnter2D(Collider2D other)` at line 483:
     ```csharp
     InteractiveEntity entity = other.GetComponent<InteractiveEntity>() ?? other.GetComponentInParent<InteractiveEntity>();
     if (entity != null && !entity.IsConsumed && entity.IsArmed)
     {
         entity.IsConsumed = true;
         CollisionEffectDispatcher.ResolveCollision(entity, this);
     }
     ```

## Success Criteria
- [x] `InteractiveEntity.IsArmed` is `true` by default on newly instantiated entities.
- [x] Calling `SetArmingDelay(0.75f)` sets `IsArmed` to `false` until 0.75 seconds elapsed.
- [x] `TickArming(0.5f)` followed by `TickArming(0.3f)` deterministically transitions `IsArmed` from `false` to `true`.
- [x] During the unready window, neither `InteractiveEntity.CheckPlayerCollision` nor `PlayerController.OnTriggerEnter2D` triggers `CollisionEffectDispatcher.ResolveCollision`.
- [x] Sprite alpha is restored to 1.0f when arming finishes.

## Risk Assessment
- **Risk:** NullReferenceException when `spriteRenderer` is missing in test environments.
  - *Mitigation:* Explicit null check on `spriteRenderer != null` inside `TickArming`.
- **Risk:** Delayed trigger dispatch when stationary overlap transitions from unready to armed.
  - *Mitigation:* `InteractiveEntity.CheckPlayerCollision()` runs every frame in `Update()`, ensuring immediate trigger as soon as `IsArmed` becomes true even without relative movement.
