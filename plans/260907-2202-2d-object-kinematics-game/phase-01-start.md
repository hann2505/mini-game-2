---
phase: 1
title: "Viewport & Screen Bounds Foundation"
status: completed
priority: P1
effort: "45m"
dependencies: []
---

# Phase 1: Viewport & Screen Bounds Foundation

## Overview
Establishes the mathematical core for dynamic camera viewport-to-world bounds calculations and sprite dimension normalization, ensuring all screen edge positioning and size parity logic work across arbitrary aspect ratios and resolutions without visual distortion.

## Requirements
- Functional:
  - Dynamically calculate min/max X and Y world coordinates for the active Orthographic camera.
  - Provide helper methods to convert Viewport coordinates (e.g. `(0, 0.5)` for Mid-Left, `(1, 0.5)` for Mid-Right) to world points.
  - Provide a normalization method that scales a target GameObject's Transform so its `SpriteRenderer.bounds.size` precisely matches a reference size or bounds.
- Non-functional:
  - Zero per-frame allocations during boundary queries.
  - Recalculate or refresh bounds when screen resolution or camera size changes.

## Architecture
- `ViewportManager` (Singleton / MonoBehaviour):
  - Caches `MinX`, `MaxX`, `MinY`, `MaxY`, `Width`, `Height` based on `Camera.main.ViewportToWorldPoint`.
  - Exposes `Vector3 GetViewportWorldPosition(float viewportX, float viewportY)`.
  - Exposes `void MatchObjectSize(SpriteRenderer targetRenderer, Vector2 desiredWorldSize)`.
  - Implements `OnDrawGizmos()` to visualize the calculated screen borders in the Unity Editor Scene view.

## Related Code Files
- Create: `Assets/Scripts/Core/ViewportManager.cs`

## Implementation Steps
1. Create directory `Assets/Scripts/Core/`.
2. Implement `ViewportManager.cs` deriving from `MonoBehaviour` with camera caching and viewport-to-world conversion logic.
3. Implement `MatchObjectSize` to compute scale factors from `targetRenderer.bounds.size` vs desired size and apply to `target.transform.localScale`.
4. Add debug visualization with `Gizmos.DrawWireCube` to outline the camera boundary in the Scene view.

## Success Criteria
- [x] Camera viewport corners `(0,0)` and `(1,1)` map correctly to world coordinates in any aspect ratio (16:9, 18:9, 4:3).
- [x] Passing two sprites of different native resolutions to `MatchObjectSize` yields identical on-screen bounding boxes.
- [x] Visual gizmos confirm bounds align with Game view edges.

## Risk Assessment
- Risk: `Camera.main` might return null or uninitialized on frame 0.
  - Mitigation: Cache camera explicitly with fallback to `Camera.main` or `FindFirstObjectByType<Camera>()` in `Awake()`.
- Risk: Sprite PPU (pixels per unit) differences causing uneven aspect ratio distortion if non-uniform scale is applied.
  - Mitigation: Compute uniform scale using the dominant dimension (max of width/height) or standardizing target world size.
