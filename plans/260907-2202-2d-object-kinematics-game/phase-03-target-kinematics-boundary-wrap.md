---
phase: 3
title: "Target Kinematics & Boundary Wrap"
status: pending
priority: P1
effort: "1h"
dependencies: ["phase-01-start"]
---

# Phase 3: Target Kinematics & Boundary Wrap

## Overview
Implements Object B (Target enemy) with autonomous, flexible 4-way movement and seamless screen boundary wrapping with randomized repositioning when crossing the boundary where Object A originated.

## Requirements
- Functional:
  - Object B moves autonomously with flexible 4-way kinematics (combining drift along the primary axis and oscillation/sinusoidal motion along the perpendicular axis).
  - Movement speed, oscillation frequency, and amplitude are exposed in the Inspector for easy tuning.
  - Screen boundary detection: When Object B completely crosses the screen edge where Object A originated (e.g. Left edge in Horizontal mode, or Top edge in Vertical mode), it immediately teleports to the opposite screen edge (Right edge or Bottom edge).
  - Upon teleporting/respawning, its position along the perpendicular axis (e.g. Y-axis for Horizontal orientation) is randomized within the visible camera viewport limits.
  - Boundary check includes a sprite extents buffer (`spriteRenderer.bounds.extents`) so Object B completely leaves the screen before wrapping, preventing visual popping.
  - Trigger collider detects collisions with Object C (Projectile), playing hit SFX (`explosion.wav`) and optionally triggering a respawn.
- Non-functional:
  - Smooth frame-rate independent movement via `Time.deltaTime`.

## Architecture
- `TargetController.cs`:
  - Fields:
    - `float baseSpeed = 3f`: forward drift velocity.
    - `float waveFrequency = 2f`: oscillation speed.
    - `float waveAmplitude = 1.5f`: perpendicular movement range.
    - `bool isHorizontal = true`: orientation alignment.
  - In `Update()`:
    - Calculates primary movement (e.g. `Vector3.left * baseSpeed * Time.deltaTime`).
    - Calculates perpendicular movement via `Mathf.Sin(Time.time * waveFrequency) * waveAmplitude`.
    - Checks position against `ViewportManager.Instance`:
      - If `transform.position.x < (ViewportManager.Instance.MinX - extents.x)`:
        - Repositions `transform.position` to `(ViewportManager.Instance.MaxX + extents.x, Random.Range(minY + extents.y, maxY - extents.y), 0)`.
  - `OnTriggerEnter2D(Collider2D other)`:
    - If `other.CompareTag("Projectile")`, destroys projectile, plays `explosion.wav`, and triggers boundary respawn.

## Related Code Files
- Create: `Assets/Scripts/Enemy/TargetController.cs`

## Implementation Steps
1. Create `Assets/Scripts/Enemy/` directory.
2. Implement `TargetController.cs` with configurable kinematics, boundary detection, and randomized repositioning.
3. Configure Object B prefab using `Assets/Sprites/Characters/Enemies/Birds/bird1.png`, attaching `SpriteRenderer`, `CircleCollider2D` (Trigger), Kinematic `Rigidbody2D`, and `TargetController`.
4. Add audio trigger on hit referencing `Assets/Audio/SFX/explosion.wav`.

## Success Criteria
- [ ] Object B moves smoothly across the screen demonstrating flexible 4-way motion (up, down, left, right).
- [ ] When Object B crosses the Left screen boundary, it completely exits view before wrapping.
- [ ] Object B reappears on the Right screen boundary at a newly randomized vertical (Y) coordinate.
- [ ] Object C colliding with Object B triggers explosion sound effect and resets Object B.

## Risk Assessment
- Risk: Object B spawns too close to the edge or outside screen viewport on respawn.
  - Mitigation: Clamp randomized perpendicular coordinate with inner padding (`bounds.extents`).
- Risk: Visual popping when wrapping.
  - Mitigation: Factor in `spriteRenderer.bounds.extents` so wrapping only occurs when `position + extents` is completely off-screen.
