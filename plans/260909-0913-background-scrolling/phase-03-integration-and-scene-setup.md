---
phase: 3
title: "Integration with GameController & SceneSetupHelper"
status: pending
priority: P1
effort: "45m"
dependencies: [2]
---

# Phase 3: Integration with GameController & SceneSetupHelper

## Overview
Connect the `BackgroundScroller` component to the master `GameController.cs` orchestrator and update the Editor utility `SceneSetupHelper.cs` so that scene recreation automatically equips the `Background` GameObject with `BackgroundScroller` using `background10.jpg`. Adapt `ScaleBackground()` to scale and position both leapfrog segments uniformly, ensuring zero black bars across arbitrary aspect ratios while preventing double-scaling, Z-fighting, and resize-induced teleportation glitches.

## Requirements
- Functional:
  - Add `BackgroundScroller` reference to `GameController`:
    - `[SerializeField] private BackgroundScroller backgroundScroller;`
  - Update `GameController.ScaleBackground()`:
    - Reads sprite from `backgroundSprite` field (fallback to `backgroundRenderer.sprite`).
    - Calculates uniform `scaleFactor = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y)`.
    - If `BackgroundScroller` is present:
      - Explicitly disables the parent `SpriteRenderer` component (`backgroundRenderer.enabled = false;`) to prevent double-rendering and Z-fighting.
      - Keeps the parent transform local scale at `Vector3.one` so child segments are not double-scaled.
      - If scroller is not yet initialized, calls `backgroundScroller.Initialize(bgSprite, scaleFactor, ViewportManager.Instance, orientation)`.
      - If scroller is already initialized, calls `backgroundScroller.RefreshScale(scaleFactor)` to update bounds without resetting current scroll position.
    - If `BackgroundScroller` is absent, maintains legacy single-renderer scaling for backwards compatibility.
  - Orientation Integration in `ApplyOrientation(orientation)`:
    - Calls `backgroundScroller.SetOrientation(newOrientation)` to smoothly re-align segments along the new axis.
  - Update `SceneSetupHelper.SetupSceneHierarchy()`:
    - Assigns `Assets/Sprites/Backgrounds/background10.jpg` as the background sprite.
    - Ensures `Background` GameObject has `BackgroundScroller` component attached.
    - Serializes scroller reference into `GameController.backgroundScroller`.
- Non-functional:
  - Zero black bars across any aspect ratio.
  - Backwards compatibility with existing unit tests.

## Architecture
```
+---------------------+           +------------------------+
|   GameController    | --------> |   BackgroundScroller   |
+---------------------+           +------------------------+
           |                                  |
           v                                  v
+---------------------+           +------------------------+
|   ViewportManager   | <-------- |    Dual Segments       |
+---------------------+           +------------------------+
```

## Related Code Files
- Modify: `Assets/Scripts/Core/GameController.cs`
- Modify: `Assets/Editor/SceneSetupHelper.cs`
- Modify: `Assets/Scenes/SampleScene.unity`

## Implementation Steps
1. In `GameController.cs`:
   - Add `[SerializeField] private BackgroundScroller backgroundScroller;` and property.
   - Refactor `ScaleBackground()`:
     - If `backgroundScroller != null`:
       - Disable parent renderer: `if (backgroundRenderer != null) backgroundRenderer.enabled = false;`.
       - Parent scale: `backgroundRenderer.transform.localScale = Vector3.one;`.
       - If not initialized: `backgroundScroller.Initialize(sprite, scaleFactor, ViewportManager.Instance, orientation);`.
       - Else: `backgroundScroller.RefreshScale(scaleFactor);`.
     - Else: execute legacy static scaling on `backgroundRenderer`.
   - In `ApplyOrientation(newOrientation)`:
     - If `backgroundScroller != null`: `backgroundScroller.SetOrientation(newOrientation);`.
2. In `SceneSetupHelper.cs`:
   - Update background sprite loading to load `Assets/Sprites/Backgrounds/background10.jpg`.
   - Ensure `bgGo.GetComponent<BackgroundScroller>()` is added if null.
   - Link `serializedGc.FindProperty("backgroundScroller")`.
3. In Editor / Scene:
   - Run setup utility to update `SampleScene.unity` with `background10.jpg` and save the scene.

## Success Criteria
- [x] Running the scene in Play mode shows `background10.jpg` smoothly scrolling from right to left.
- [x] Changing orientation in `GameController` smoothly flips the scrolling vector to the vertical axis and re-aligns segments vertically.
- [x] Resizing the game view window does NOT cause the background to jump or reset its scroll position.
- [x] No double-rendering, Z-fighting, or squared-scaling artifacts occur.
- [x] `Kinematics Game -> Setup Scene & Prefabs` successfully sets up the new scroller.

## Risk Assessment
- Risk: Background segments overlap player or target sorting layers.
  - Observable signal: Background renders in front of spaceships or targets.
  - Mitigation: Explicitly enforce `sortingOrder = -100` on both child segments during `Initialize()`.
