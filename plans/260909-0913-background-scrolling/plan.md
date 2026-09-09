---
title: "Background Scrolling Kinematics System"
description: "Continuous dual-sprite leapfrog background scrolling system using background10.jpg to simulate forward player motion in Unity 2D."
status: completed
priority: P1
effort: "3h"
tags: [unity, 2d, kinematics, background, scroller, testing]
created: 2026-09-09
---

# Background Scrolling Kinematics System

## Overview
Implement a modular, deterministic 2D background scrolling system for `mini-game-2` using the wide panoramic asset `Assets/Sprites/Backgrounds/background10.jpg` (1920x768). The system resolves the relative motion physics required to make Object A (Player Spaceship, facing +X) feel like it is moving forward by scrolling the background in the opposite direction (-X, right to left). The system maintains full aspect-ratio coverage with zero black bars across arbitrary viewports, dynamically supports both Horizontal and Vertical orientations, and is testable via deterministic NUnit unit tests without relying on Play mode or `Time.deltaTime`.

## Goals
| # | Goal | Priority |
|---|------|----------|
| 1 | Configure `background10.jpg` with correct Unity 2D Sprite `.meta` settings (Full Rect, Sprite Mode Single). | P1 |
| 2 | Develop modular `BackgroundScroller.cs` in `KinematicsGame.Core` with dual-sprite leapfrog wrapping. | P1 |
| 3 | Resolve kinematic relative motion: Default right-to-left (-X) scrolling in Horizontal mode, configurable speed/axis. | P1 |
| 4 | Integrate seamlessly with `GameController.cs` (adapting `ScaleBackground()`) and `SceneSetupHelper.cs`. | P1 |
| 5 | Deliver deterministic test coverage in `Assets/Editor/Tests/BackgroundScrollerTests.cs` via isolated `Tick(float deltaTime)`. | P1 |

## Phases
| # | Phase | Status | Effort | Dependencies |
|---|-------|--------|--------|--------------|
| 1 | [Phase 1: Asset Preparation & Meta Configuration](./phase-01-start.md) | completed | 30m | [] |
| 2 | [Phase 2: Core BackgroundScroller Component](./phase-02-core-background-scroller.md) | completed | 1h | [1] |
| 3 | [Phase 3: Integration with GameController & SceneSetupHelper](./phase-03-integration-and-scene-setup.md) | completed | 45m | [2] |
| 4 | [Phase 4: Deterministic NUnit Unit Testing](./phase-04-deterministic-unit-testing.md) | completed | 45m | [2, 3] |

## Technical Architecture & Kinematics
- **Kinematic Relative Motion**: Object A (Player) is anchored at the left edge facing right (+X), firing projectiles to +X. In relative motion kinematics, moving forward through an environment requires the environment to translate backward (-X, right to left) relative to the camera frame.
- **Dual-Sprite Leapfrog Translation**:
  - The `BackgroundScroller` manages two identical child `SpriteRenderer` instances (`segmentA` and `segmentB`).
  - Both segments are scaled uniformly by `ScaleBackground()` to exceed viewport height/width and eliminate black bars.
  - In Horizontal mode, segments are placed side-by-side with width = `spriteBounds.size.x`.
  - In each `Tick(float deltaTime)`, both segments translate by `scrollDirection * scrollSpeed * deltaTime`.
  - **Leapfrog Trigger**: When a segment's trailing edge exits the viewport boundary (`segment.position.x + (segmentWidth * 0.5f) <= ViewportManager.Instance.MinX`), it teleports to the forward position: `segment.position = otherSegment.position + new Vector3(segmentWidth, 0f, 0f)`.
  - **Vertical Mode**: In Vertical orientation, Object A faces downward (-Y) from top edge. Forward motion is simulated by scrolling upward (+Y), leapfrogging when `segment.position.y - (segmentHeight * 0.5f) >= ViewportManager.Instance.MaxY`.
- **Determinism & Testability**:
  - All positional math lives in `public void Tick(float deltaTime)`.
  - Unity's `Update()` simply forwards `Time.deltaTime` to `Tick()`.
  - Unit tests directly invoke `Tick(stepDelta)` against mocked or assigned `ViewportManager` bounds without running Unity's game loop.

## Ultra Selection

| Candidate | Anonymized ID | Score (/100) | Core Thesis | Report Link |
|:---|:---:|:---:|:---|:---|
| Candidate 1 | Candidate C | 77 | Dual-sprite scroller replacing ScaleBackground() | [Report 1](./reports/planner-ultra-candidate-1.md) |
| **Candidate 2** | **Candidate E** | **96** | **Dual-sprite leapfrog scroller adapting ScaleBackground() with mocked ViewportManager tests (WINNER)** | [Report 2](./reports/planner-ultra-candidate-2.md) |
| Candidate 3 | Candidate A | 83 | Dual-sprite scroller with shift math, basic orientation | [Report 3](./reports/planner-ultra-candidate-3.md) |
| Candidate 4 | Candidate D | 84 | Mixed shader offset / transform scroller with orientation mapping | [Report 4](./reports/planner-ultra-candidate-4.md) |
| Candidate 5 | Candidate B | 76 | Tiled SpriteRenderer drawMode approach | [Report 5](./reports/planner-ultra-candidate-5.md) |

### Winner & Selection Rationale
**Candidate 2 (Candidate E)** was selected by Kongming (the independent strongest-model verifier) with a dominant score of **96/100**:
1. **Kinematic Precision**: Flawlessly solves the forward motion illusion by scrolling -X for right-facing movement, while preserving directional configurability.
2. **Zero-Black-Bar Safety**: Rather than dangerously replacing `ScaleBackground()`, it adapts it to scale both leapfrog segments uniformly, ensuring arbitrary aspect ratios remain completely covered.
3. **Exact Leapfrog Math**: `segment.position = otherSegment.position + segmentOffset` triggered when boundary is exceeded.
4. **Deterministic Testing**: Explicitly specifies isolating `Tick(float deltaTime)` with mocked `ViewportManager` bounds in NUnit.

### Rejected Alternatives
- **Candidate 5 (Candidate B - 76)**: Proposing `SpriteRenderer.drawMode = Tiled` conflicts with arbitrary transform scaling and non-power-of-two sprites, risking stretching artifacts.
- **Candidate 1 (Candidate C - 77)**: Suggests replacing `ScaleBackground()` entirely, introducing massive risk of black bars on ultra-wide or narrow screens.
- **Candidate 3 (Candidate A - 83)**: Lacked explicit handling for vertical orientation switching.
- **Candidate 4 (Candidate D - 84)**: Indecisive on implementation mechanism (shader offset vs transform translation); shader offset testing in headless NUnit is brittle.

## Red Team Review

### Session — 2026-09-09
**Findings:** 5 (5 accepted, 0 rejected)
**Severity breakdown:** 2 Critical, 3 High

| # | Finding | Severity | Disposition | Applied To |
|---|---------|----------|-------------|------------|
| 1 | Hardcoded horizontal adjacency breaks vertical orientation | Critical | Accept | Phase 2, Phase 3 |
| 2 | Window resize invokes Initialize() and destroys scroll state | High | Accept | Phase 2, Phase 3 |
| 3 | Parent SpriteRenderer causes Z-fighting and double rendering | High | Accept | Phase 3 |
| 4 | Omission of segmentHeight calculation breaks vertical leapfrog | High | Accept | Phase 2 |
| 5 | Double scaling of child segments under scaled parent | High | Accept | Phase 2, Phase 3 |

### Applied Mitigations
1. **Orientation-Aware Placement**: Added `GameOrientation` parameter to `Initialize()`. In Horizontal mode, segments are placed along X; in Vertical mode, segments are stacked along Y. `SetOrientation()` immediately snaps segments into the new alignment.
2. **Resize Continuity**: Introduced `RefreshScale(float newScaleFactor)` called by `ScaleBackground()` during screen resizes. It updates segment scales and bounding thresholds without resetting current world positions.
3. **Z-Fighting & Double Rendering**: Explicitly set `backgroundRenderer.enabled = false` when `BackgroundScroller` is attached.
4. **segmentHeight Math**: Added explicit `segmentHeight = (sprite.rect.height / sprite.pixelsPerUnit) * scaleFactor`.
5. **Scale Isolation**: Set child segment local scale to `scaleFactor` while keeping parent local scale at `Vector3.one`.

### Whole-Plan Consistency Sweep
- **Decision Deltas**: Verified that all method signatures across Phase 2, Phase 3, and Phase 4 match:
  - `BackgroundScroller.Initialize(Sprite, float, ViewportManager, GameOrientation)`
  - `BackgroundScroller.RefreshScale(float)`
  - `BackgroundScroller.SetOrientation(GameOrientation)`
  - `BackgroundScroller.Tick(float)`
- **Reconciliation**: Zero unresolved contradictions. All stubs, phases, and plan documentation agree on the dual-sprite leapfrog architecture.

## Success Criteria
- [x] `background10.jpg.meta` exists and correctly imports the panoramic texture as a 2D Sprite.
- [x] `BackgroundScroller.cs` provides a modular, configurable scrolling component with `Tick(float deltaTime)`.
- [x] In Horizontal mode, the background continuously scrolls right-to-left (-X), creating an intuitive sensation of Object A moving forward.
- [x] In Vertical mode, the background scrolls in the appropriate axis (+Y) for downward forward motion.
- [x] Zero black bars are visible across 16:9, 4:3, and 21:9 aspect ratios.
- [x] At least 6 NUnit unit tests in `Assets/Editor/Tests/BackgroundScrollerTests.cs` pass, validating deterministic translation, leapfrog wrapping, orientation adaptability, and resize continuity.
- [x] `SceneSetupHelper.cs` assigns `background10.jpg` and attaches `BackgroundScroller` when configuring the scene.
