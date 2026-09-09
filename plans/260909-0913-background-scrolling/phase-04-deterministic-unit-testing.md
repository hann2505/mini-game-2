---
phase: 4
title: "Deterministic NUnit Unit Testing"
status: pending
priority: P1
effort: "45m"
dependencies: [2, 3]
---

# Phase 4: Deterministic NUnit Unit Testing

## Overview
Develop a complete, fast, deterministic NUnit test suite in `Assets/Editor/Tests/BackgroundScrollerTests.cs`. The tests validate linear translation, precision leapfrog boundary snapping, orientation switching (including segment re-alignment), resize continuity (`RefreshScale`), and scale isolation without relying on Unity Play mode or runtime `Time.deltaTime`.

## Requirements
- Functional:
  - Test 1: `BackgroundScroller_Tick_AdvancesSegmentsByExactKinematicDistance`
    - Invokes `Tick(0.1f)` with `scrollSpeed = 4f` and asserts that segment X positions change by exactly `-0.4f`.
  - Test 2: `BackgroundScroller_Tick_LeapfrogsWhenExitingViewportMinX`
    - Simulates time steps until a segment passes `ViewportManager.MinX` and asserts that it teleports to `otherSegment.position.x + segmentWidth`.
  - Test 3: `BackgroundScroller_SetOrientation_SwitchesScrollAxisAndRealiginsSegments`
    - Verifies that setting `GameOrientation.Vertical` redirects movement to the Y axis (+Y), re-aligns segments vertically, and activates vertical leapfrog thresholds.
  - Test 4: `BackgroundScroller_RefreshScale_PreservesScrollPosition`
    - Simulates active scrolling, calls `RefreshScale(newScale)`, and asserts that segment positions do not reset to center while dimensions and scales update.
  - Test 5: `BackgroundScroller_ZeroDeltaTime_DoesNotMoveSegments`
    - Confirms that paused / zero-delta updates produce zero movement.
  - Test 6: `BackgroundScroller_ScaleBackground_DisablesParentRenderer`
    - Verifies that when `GameController.ScaleBackground()` initializes `BackgroundScroller`, the parent `SpriteRenderer.enabled` is set to false to prevent Z-fighting.
- Non-functional:
  - Tests run in Editor mode in < 500ms.
  - Proper fixture teardown cleans up all created GameObjects and textures.

## Architecture
```
+----------------------------+
| BackgroundScrollerTests.cs |
+----------------------------+
  |-- SetUp: Create Mock Camera (16:9) & ViewportManager
  |-- Instantiate BackgroundScroller with procedural 192x76 mock sprite
  |-- Execute Tick(step) deterministically
  |-- Assert segment transforms, relative distance, and orientation realignment
  |-- TearDown: DestroyImmediate all objects
```

## Related Code Files
- Create: `Assets/Editor/Tests/BackgroundScrollerTests.cs`
- Modify: None
- Delete: None

## Implementation Steps
1. Create `Assets/Editor/Tests/BackgroundScrollerTests.cs`.
2. Implement `SetUp` and `TearDown` using `List<Object> disposables` pattern consistent with `GameControllerTests.cs`.
3. Create helper method to construct a mock `Sprite` with 192x76 dimensions matching the 2.5:1 ratio of `background10.jpg`.
4. Write test cases for:
   - Linear translation accuracy.
   - Leapfrog wrap-around trigger and snap position.
   - Horizontal and Vertical orientation response and segment re-alignment.
   - Resize continuity via `RefreshScale`.
   - Scale isolation and parent renderer disabling.
   - Zero-delta / pause behavior.
5. Run the tests via Unity test runner or batchmode test command to verify 100% pass rate.

## Success Criteria
- [x] `BackgroundScrollerTests.cs` compiles cleanly under Assembly-CSharp-Editor.
- [x] All 6 unit tests pass deterministically.
- [x] No memory leaks or leftover GameObjects in the scene hierarchy after test execution.

## Risk Assessment
- Risk: In isolated test runs, `ViewportManager.Instance` is null or not initialized.
  - Observable signal: `NullReferenceException` when checking `MinX` or `MaxX`.
  - Mitigation: In `SetUp`, explicitly initialize `ViewportManager` or allow `BackgroundScroller.Initialize()` to accept an explicit `ViewportManager` parameter.
