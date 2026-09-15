---
phase: 2
title: "Laser Beam Controller Component"
status: pending
priority: P1
effort: "1h"
dependencies: ["phase-01-start"]
---

# Phase 2: Laser Beam Controller Component

## Overview
Develops a high-performance, zero-GC `LaserBeam` MonoBehaviour that renders a continuous piercing visual beam using `LineRenderer` and detects targets along its trajectory using `Physics2D.CircleCastNonAlloc`.

## Requirements
- Functional:
  - Anchor start point at `firePoint` (or player ship muzzle).
  - Project beam forward towards `direction` (default right / +X) until reaching viewport boundary or max beam distance.
  - Pierce all targets along the beam path.
  - Apply tick-based damage (e.g., 10 damage every 0.1s = 100 DPS) to all detected targets.
  - Provide immediate `Activate(...)` and `Deactivate()` methods.
  - Manage looping laser audio with instant cutoff on deactivation.
- Non-functional:
  - Zero heap allocation per frame during continuous firing (use preallocated `RaycastHit2D[16]` buffer).
  - Visual vibrancy with customizable inner/outer beam colors or width.

## Architecture
- `LaserBeam : MonoBehaviour` in namespace `KinematicsGame.Combat`.
- Fields:
  - `LineRenderer lineRenderer`
  - `AudioSource audioSource` / `AudioClip laserLoopClip`
  - `float beamRadius = 0.15f`
  - `float tickInterval = 0.1f`
  - `int damagePerTick = 10`
  - `RaycastHit2D[] hitBuffer = new RaycastHit2D[16]`
  - `Dictionary<Collider2D, float> lastHitTimes` (or simple sweep timer) for tick cooldowns.

## Related Code Files
- Create: `Assets/Scripts/Combat/LaserBeam.cs`

## Implementation Steps
1. Create `LaserBeam.cs`:
   - Initialize `LineRenderer` programmatically if not assigned (2 positions, unlit/additive material or sprite default, start/end width).
   - Set up `SetBeamActive(bool active)` to toggle line renderer and looping sound.
   - Implement `UpdateBeam(Vector2 origin, Vector2 direction)`:
     - Determine beam endpoint using `ViewportManager.Instance.MaxX` or max range.
     - Set LineRenderer positions (origin and endpoint).
     - Run `Physics2D.CircleCastNonAlloc(origin, beamRadius, direction, hitBuffer, distance)`.
     - For each hit collider, verify target (`TargetController` or `InteractiveEntity`), and apply tick damage if elapsed time >= `tickInterval`.

## Success Criteria
- [ ] `LaserBeam` activates and renders cleanly between origin and screen edge.
- [ ] Pierces through multiple enemy colliders in a single frame.
- [ ] Applies damage ticks at the configured interval.
- [ ] Shuts down immediately when `SetBeamActive(false)` is invoked.

## Risk Assessment
- *Risk:* Memory leaks or dictionary bloat from storing hit collider references.
- *Mitigation:* Clean up stale dictionary entries or use a frame-based tick accumulator.
