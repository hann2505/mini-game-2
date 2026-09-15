---
title: Snug hitboxes and physical touch collision
date: 2026-09-14
summary: Fix ghost hitboxes on Player and Objects X, Y, Z so effects only trigger on actual physical contact.
---

# Snug hitboxes and physical touch collision

## Root Cause
1. In `Player.prefab`, `CircleCollider2D` had an unnormalized radius of `7.68`. At runtime scale (~0.57), this produced a massive world radius of **4.38 units** (nearly the entire height of the screen). Any object drifting within 4.5+ units of the player was immediately collided with, detonating or collecting far away from the player ship.
2. In `PlayerController.cs`, `col.radius` was never calculated or clamped against the visual sprite or uniform scale.
3. In `InteractiveEntity.cs`, `col.radius` on Object X (Hazard Mine / Bomb 3) was calculated as `targetSize * 0.45 = 0.45` world units, which was 35% larger than the visible bomb sprite (actual visible radius is ~0.33 world units). Additionally, proximity check logic permitted `dist.distance <= 0.05f` tolerances rather than strict overlap.

## Fixes Implemented
1. **PlayerController Hitbox Enforcement**:
   - Added `hitboxRadius = 0.38f` (snug world radius centered within the 1.50 x 1.15 visual ship).
   - Added `EnsureCollider()` which dynamically computes `col.radius = hitboxRadius / currentScale`, ensuring exactly 0.38 world units radius across all scales and orientations.
   - Hooked `EnsureCollider()` into `Awake()`, `Start()`, `OnValidate()`, and `GameController.NormalizeEntitySizes()`.
2. **Player Prefab Updated**:
   - Updated `Player.prefab`'s `CircleCollider2D` radius to `0.6663`, eliminating the bloated `7.68` radius.
   - Serialized `hitboxRadius = 0.38` and direct reference to `playerCollider`.
3. **Object X, Y, Z Snug Hitbox Alignment**:
   - Added `GetSnugWorldRadius()` in `InteractiveEntity.cs`:
     - Object X (Hazard Mine / Bomb 3): `0.30f` (snug inside the 0.335 visible bomb radius).
     - Object Y (Supply Crate): `0.40f` (snug inside the 0.50 crate radius).
     - Object Z (Gem Core): `0.20f` (snug inside the 0.25 diamond radius).
   - `EnsureCollider()` and `ApplyTargetSize()` now apply these snug radii.
   - Updated `Hazard_Mine.prefab` (`m_Radius: 0.906`), `Supply_Crate.prefab` (`m_Radius: 1.20`), and `Gem_Core.prefab` (`m_Radius: 0.096`).
4. **Strict Overlap Collision**:
   - In `CheckPlayerCollision()`, removed artificial distance buffers and enforce `dist.isOverlapped == true`.
5. **Rebalanced Spawner Rates (Not Spawning as Many as Target B)**:
   - Target B (enemy birds) has `initialSpawnInterval = 2.5f`, `minSpawnInterval = 1.0f`, spawning continuously.
   - HazardSpawner interval rebalanced from `0.4s` to `3.5s` (min `2.0s`).
   - Multi-spawn disabled (`maxSpawnPerCycle = 1`, `multiSpawnChance = 0.0f`).
   - Viewport prewarm disabled (`prewarmViewport = false`, `initialPrewarmCount = 0`).
   - Enemy defeat drop chance reduced from `0.65f` to `0.15f` (15% rare drop).
6. **Testing & Verification**:
   - Updated `HazardAndHUDTests.cs` to test that Player world collider radius is ~0.38, Object X is ~0.30, separated objects (distance 0.85 units) do NOT collide, and touching objects (distance 0.50 units) DO collide.
   - Verified that HazardSpawner rates are balanced relative to Target B.
   - Clean compilation on `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` with zero warnings and zero errors.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.
