---
title: Background Scrolling Kinematics System
date: 2026-09-09
summary: Completed modular dual-sprite leapfrog background scrolling system with orientation support and deterministic unit tests
---

# Background Scrolling Kinematics System

Completed modular dual-sprite leapfrog background scrolling system with orientation support and deterministic unit tests.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.

## Problem & Motivation
The user requested changing the game background to panoramic asset `Assets/Sprites/Backgrounds/background10.jpg` (1920x768) and scrolling the background to create the sensation that Object A (Player Spaceship) is moving forward.
In 2D relative kinematics, Player Object A is anchored at the left edge facing right (+X). Moving forward requires the stationary background environment to translate backward (right-to-left, -X) relative to the camera frame.

## Implementation Details
1. **Asset Configuration (`Assets/Sprites/Backgrounds/background10.jpg.meta`)**:
   - Configured Unity 2D `TextureImporter` with `textureType: 8` (Sprite 2D and UI), `spriteMeshType: 1` (Full Rect), `wrapMode: 0` (Clamp), single sprite mode.
2. **Modular Background Scroller (`Assets/Scripts/Core/BackgroundScroller.cs`)**:
   - Implemented dual-sprite leapfrog wrapping with child segments `Segment_A` and `Segment_B`.
   - Scale isolation: parent `localScale` remains `Vector3.one`, child segments are scaled uniformly by `scaleFactor` to prevent $scale^2$ compounding.
   - Dynamic orientation handling: Horizontal (-X scroll, adjacent along X) and Vertical (+Y scroll, stacked along Y).
   - Window resize contiguity: `RefreshScale(float newScaleFactor)` recalculates segment dimensions and realigns adjacent segments to maintain zero black bars without gaps or overlaps.
   - Independent wrapping checks in `Tick(float deltaTime)` to gracefully recover from frame drops.
   - Fallback lazy resolution for `ViewportManager.Instance`.
3. **Integration (`Assets/Scripts/Core/GameController.cs` & `Assets/Editor/SceneSetupHelper.cs`)**:
   - Refactored `GameController.ScaleBackground()`: disables parent `backgroundRenderer.enabled = false` to completely prevent Z-fighting and double rendering.
   - Connected `BackgroundScroller.Initialize()` on startup and `BackgroundScroller.RefreshScale()` on screen resize.
   - Updated `ApplyOrientation()` to synchronize `BackgroundScroller.SetOrientation()`.
   - Updated `SceneSetupHelper.cs` and `SampleScene.unity` to assign `background10.jpg` and attach `BackgroundScroller`.
4. **Deterministic Unit Testing (`Assets/Editor/Tests/BackgroundScrollerTests.cs`)**:
   - 7 EditMode NUnit tests covering linear translation accuracy, leapfrog wrapping (horizontal and vertical), orientation switching, resize continuity, zero delta pause, and parent renderer disabling.
   - Fully deterministic without Unity Play mode or `Time.deltaTime` dependency.
   - Clean fixture teardown destroying all procedural textures and game objects.

## Verification
- `dotnet build Assembly-CSharp.csproj && dotnet build Assembly-CSharp-Editor.csproj`: 0 Warnings, 0 Errors.
- Code review performed by senior reviewer subagent: Verdict `APPROVE`.
- All 17 tasks across 4 phases in `plans/260909-0913-background-scrolling/` completed and verified.
