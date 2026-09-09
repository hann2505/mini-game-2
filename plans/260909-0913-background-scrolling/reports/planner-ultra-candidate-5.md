# Implementation Plan: Background Scrolling System

## Goals
1. Implement a robust, modular background scrolling system (`BackgroundScroller.cs`) to simulate forward movement of Object A.
2. Resolve kinematics: To simulate Object A moving forward (facing right, +X), the background must scroll from right to left (-X). Provide configurable direction, axis, and speed to support multiple orientations (e.g., Horizontal and Vertical).
3. Import and properly configure the target background asset (`Assets/Sprites/Backgrounds/background10.jpg`) by generating its `.meta` file.
4. Integrate the scrolling system with existing architecture (`GameController.cs` and `SceneSetupHelper.cs`).
5. Ensure system reliability with deterministic NUnit tests (`Assets/Editor/Tests/BackgroundScrollerTests.cs`) utilizing a `Tick(float deltaTime)` method.

## Phases

### Phase 1: Asset Configuration
- **Objective:** Configure `background10.jpg` for Unity 2D.
- **Tasks:**
  - Generate/update `Assets/Sprites/Backgrounds/background10.jpg.meta`.
  - Set `textureType` to Sprite (2D and UI).
  - Configure `spriteMode` to Single, wrap mode to Repeat (crucial for seamless scrolling), and ensure proper compression/pixels-per-unit settings.

### Phase 2: Core Scrolling Logic (`BackgroundScroller.cs`)
- **Objective:** Create the background scrolling component.
- **Tasks:**
  - Create `BackgroundScroller.cs`.
  - Expose configuration properties: `Vector2 scrollDirection` (default to `(-1, 0)` for right-to-left), `float scrollSpeed`.
  - Implement a `Tick(float deltaTime)` method to update the scroll state independent of `Update()` for testability. `Update()` will just call `Tick(Time.deltaTime)`.
  - The preferred approach for 2D is managing two SpriteRenderers (leapfrogging) or a Quad with Material offset. Since it's a Sprite asset, a dual-transform or SpriteRenderer `drawMode = Tiled` approach with offset translation will be used. Let's design it with mathematical position wrapping based on Sprite bounds.

### Phase 3: Integration (`GameController.cs` & `SceneSetupHelper.cs`)
- **Objective:** Hook up the scroller to the game lifecycle.
- **Tasks:**
  - Update `GameController.cs`: Modify `ScaleBackground()` to accommodate the scrolling bounds.
  - Update `SceneSetupHelper.cs` to correctly instantiate the background prefab/object with the `BackgroundScroller` component.

### Phase 4: Deterministic Testing (`BackgroundScrollerTests.cs`)
- **Objective:** Verify scrolling logic mathematically.
- **Tasks:**
  - Create `BackgroundScrollerTests.cs` in the Editor Tests folder.
  - Test `Tick(float deltaTime)`: Provide a fixed `deltaTime`, assert that the internal offset or transform position changes by exactly `scrollDirection * scrollSpeed * deltaTime`.
  - Test Wrap-around: Advance time significantly, assert that the position wraps correctly without precision loss or gaps.
  - Test orientation handling (horizontal vs vertical bounds).

## Acceptance Criteria
- [ ] `background10.jpg` has a valid `.meta` file and acts as a tiled/repeating Sprite.
- [ ] `BackgroundScroller.cs` is created and correctly moves the background along the configured axis.
- [ ] For horizontal orientation (Object A moving right), the background scrolls right-to-left (-X) seamlessly.
- [ ] The logic uses a deterministic `Tick(float deltaTime)` method.
- [ ] `BackgroundScrollerTests.cs` passes all unit tests verifying positional translation and wrapping logic.
- [ ] Integration with `GameController.cs` scales the background correctly using `ViewportManager` bounds.
- [ ] No visual gaps or tearing occurs during continuous scrolling.
