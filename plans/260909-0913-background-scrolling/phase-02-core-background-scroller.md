---
phase: 2
title: "Core BackgroundScroller Component"
status: pending
priority: P1
effort: "1h"
dependencies: [1]
---

# Phase 2: Core BackgroundScroller Component

## Overview
Implement the `BackgroundScroller.cs` component in namespace `KinematicsGame.Core`. This component encapsulates the dual-sprite leapfrog translation logic, exposes configurable velocity and direction, dynamically adapts to game orientations, and provides a deterministic `Tick(float deltaTime)` method for testing and update cycles.

## Requirements
- Functional:
  - Manage two child `SpriteRenderer` segments (`segmentA` and `segmentB`) that loop seamlessly.
  - Expose properties: `ScrollSpeed` (float, e.g. 3.0f), `ScrollDirection` (Vector2), `IsScrolling` (bool).
  - Explicit Dimension Calculation:
    - `segmentWidth = (sprite.rect.width / sprite.pixelsPerUnit) * scaleFactor;`
    - `segmentHeight = (sprite.rect.height / sprite.pixelsPerUnit) * scaleFactor;`
  - Scale Isolation:
    - Keep parent `Background` GameObject scale at `Vector3.one` and apply `new Vector3(scaleFactor, scaleFactor, 1f)` to child segments (or vice-versa) to prevent double-scaling (scale squared).
  - Orientation-Aware Initial Placement:
    - Accept `GameOrientation` in `Initialize(Sprite sprite, float scaleFactor, ViewportManager viewport, GameOrientation orientation)`.
    - Horizontal: `segmentA` at viewport center; `segmentB` at `center + new Vector3(segmentWidth, 0f, 0f)`.
    - Vertical: `segmentA` at viewport center; `segmentB` at `center - new Vector3(0f, segmentHeight, 0f)` (stacked below to scroll upward).
  - Dynamic Orientation Switching (`SetOrientation(GameOrientation orientation)`):
    - Update `scrollDirection` (Horizontal: `Vector2.left`, Vertical: `Vector2.up`).
    - Immediately snap and re-align `segmentA` and `segmentB` along the newly selected axis (X-axis for Horizontal, Y-axis for Vertical) to prevent empty voids and boundary glitches.
  - Window Resize Handling:
    - Implement `public void RefreshScale(float newScaleFactor)` that recalculates `segmentWidth` and `segmentHeight` and updates segment local scales WITHOUT resetting their current world scroll positions.
  - Implement `public void Tick(float deltaTime)`:
    - Advances both segments by `ScrollDirection.normalized * (ScrollSpeed * deltaTime)`.
    - Horizontal Leapfrog (-X): When a segment's right edge exits left bounds (`pos.x + (segmentWidth * 0.5f) <= ViewportManager.Instance.MinX`), snap it behind the other segment: `pos.x = otherSegment.position.x + segmentWidth`.
    - Vertical Leapfrog (+Y): When a segment's bottom edge exits top bounds (`pos.y - (segmentHeight * 0.5f) >= ViewportManager.Instance.MaxY`), snap it behind the other segment: `pos.y = otherSegment.position.y - segmentHeight`.
- Non-functional:
  - Zero heap allocation in `Tick()` or `Update()`.
  - Clean separation: Scroller only handles background positioning; does not interfere with player input or collision systems.

## Architecture
```
+-------------------------------------------------------------+
|                      BackgroundScroller                     |
+-------------------------------------------------------------+
| - segmentA: SpriteRenderer                                  |
| - segmentB: SpriteRenderer                                  |
| - scrollSpeed: float                                        |
| - scrollDirection: Vector2                                  |
| - currentOrientation: GameOrientation                       |
| - segmentWidth: float                                       |
| - segmentHeight: float                                      |
+-------------------------------------------------------------+
| + Initialize(Sprite, float, ViewportManager, GameOrientation)|
| + RefreshScale(float newScaleFactor)                        |
| + SetOrientation(GameOrientation orientation)               |
| + Tick(float deltaTime)                                     |
| - CheckAndLeapfrogSegments()                                |
+-------------------------------------------------------------+
```

## Related Code Files
- Create: `Assets/Scripts/Core/BackgroundScroller.cs`
- Modify: None
- Delete: None

## Implementation Steps
1. Create `Assets/Scripts/Core/BackgroundScroller.cs`.
2. Declare serialized fields for `segmentA`, `segmentB`, `scrollSpeed` (default 3f), `scrollDirection` (default `Vector2.left`), and `isScrolling` (default true).
3. Implement `Initialize(Sprite sprite, float scaleFactor, ViewportManager viewport, GameOrientation orientation)`:
   - Instantiates or assigns two child GameObjects with `SpriteRenderer`.
   - Assigns sprite, sorting order (-100), and sets child local scale to `new Vector3(scaleFactor, scaleFactor, 1f)`. Parent scale remains `Vector3.one`.
   - Computes world dimensions:
     - `segmentWidth = (sprite.rect.width / sprite.pixelsPerUnit) * scaleFactor;`
     - `segmentHeight = (sprite.rect.height / sprite.pixelsPerUnit) * scaleFactor;`
   - Positions segments based on `orientation`:
     - Horizontal: `segmentA` at `viewport.Center`, `segmentB` at `viewport.Center + new Vector3(segmentWidth, 0f, 0f)`.
     - Vertical: `segmentA` at `viewport.Center`, `segmentB` at `viewport.Center - new Vector3(0f, segmentHeight, 0f)`.
4. Implement `RefreshScale(float newScaleFactor)`:
   - Updates `segmentWidth` and `segmentHeight`.
   - Updates local scale on both child segments without resetting world positions.
5. Implement `SetOrientation(GameOrientation orientation)`:
   - Updates `currentOrientation`.
   - Updates `scrollDirection` (`Vector2.left` for Horizontal, `Vector2.up` for Vertical).
   - Repositions `segmentB` relative to `segmentA` along the new active axis.
6. Implement `public void Tick(float deltaTime)`:
   - Guard against `!isScrolling` or `deltaTime <= 0`.
   - Translate both segment transforms by `scrollDirection.normalized * (scrollSpeed * deltaTime)`.
   - Execute leapfrog check using relative snapping (`otherSegment.position + offset`) to avoid floating-point drift.
7. In `Update()`, call `Tick(Time.deltaTime)`.

## Success Criteria
- [x] `BackgroundScroller.cs` compiles with zero warnings or errors.
- [x] `Tick(float deltaTime)` advances segment positions deterministically.
- [x] Segments leapfrog seamlessly when passing beyond viewport edges in both Horizontal and Vertical modes.
- [x] Orientation switching correctly snaps segments into the new axis without black voids.
- [x] Calling `RefreshScale()` on window resize does not reset or jump the background scroll position.

## Risk Assessment
- Risk: Floating-point precision error causes a 1-pixel gap after hundreds of leapfrog cycles.
  - Observable signal: A flicker or vertical hairline visible between segments.
  - Mitigation: Calculate leapfrog destination by relative offset to the other segment (`otherSegment.position + segmentWidth * direction`) rather than purely accumulating self position.
