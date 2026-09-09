# Candidate Plan: Background Scrolling System

## 1. Goals
- **Objective:** Create a modular and deterministic background scrolling system that simulates forward motion for the player character.
- **Visuals:** Replace the current static background with `background10.jpg` and configure it properly.
- **Kinematics:** Implement a relative motion approach where the background moves opposite to the intended direction of travel (e.g., right-to-left to simulate moving right).
- **Architecture:** Develop `BackgroundScroller.cs`, integrate with `GameController` and `SceneSetupHelper`.
- **Testing:** Ensure deterministic unit testing by exposing a `Tick(float deltaTime)` method for manual test updates.

## 2. Technical Approach & Design
- **Asset Configuration:** Generate or update `background10.jpg.meta` ensuring `textureType` is `Sprite` (or Default with appropriate wrap mode), and `wrapMode` is `Repeat` so the material can tile.
- **Scroller Component (`BackgroundScroller.cs`):** 
  - Manage a material offset or duplicate sprites to create a seamless scrolling effect. Given it's a 2D kinematic game using sprites, moving the transform and wrapping it, or manipulating material `mainTextureOffset` are valid. Manipulating `mainTextureOffset` is usually easier if using a MeshRenderer/Quad, but if using SpriteRenderer, duplicating the sprite (or utilizing a custom shader/material) is common. For modularity, we will use a two-sprite wrapping technique or shader offset. Since we want a `Tick()` method, standard translation of transforms is robust.
  - Implement configurable `scrollSpeed` and `scrollDirection`.
  - The `Tick(float deltaTime)` method updates the position/offset. The Unity `Update()` will just call `Tick(Time.deltaTime)`.
- **Integration:** 
  - `GameController.cs` should instantiate/setup the background via `BackgroundScroller` instead of purely static scaling.
  - Apply appropriate scale to fit `ViewportManager.Instance.Height` (or Width) based on orientation, while allowing the length to scroll.
- **Orientation Support:** 
  - If Horizontal (moving +X), background scrolls -X.
  - If Vertical (moving -Y), background scrolls +Y.

## 3. Phases

### Phase 1: Asset Configuration
- **Task:** Create/update `Assets/Sprites/Backgrounds/background10.jpg.meta`.
- **Details:** Set importer to Sprite (2D and UI) or Default. If using Sprite wrapping, set Wrap Mode to Repeat. Ensure pixel per unit (PPU) is set reasonably (e.g., 100).

### Phase 2: Core Scroller Component
- **Task:** Implement `BackgroundScroller.cs` in `Assets/Scripts/Core/`.
- **Details:**
  - Create a class that handles scrolling.
  - Expose `public Vector2 ScrollVelocity`.
  - Implement `public void Tick(float deltaTime)`.
  - Handle seamless wrapping (e.g., when the background moves past a threshold based on its width/height calculated via `ViewportManager` and Sprite bounds, snap it back).

### Phase 3: Integration
- **Task:** Update `GameController.cs` and `SceneSetupHelper.cs`.
- **Details:**
  - Refactor the existing `ScaleBackground()` logic.
  - Attach `BackgroundScroller` to the background object.
  - Set the scroll velocity based on the current orientation (e.g., Horizontal -> `new Vector2(-speed, 0)`).

### Phase 4: Deterministic Testing
- **Task:** Implement `Assets/Editor/Tests/BackgroundScrollerTests.cs`.
- **Details:**
  - Instantiate `BackgroundScroller`.
  - Set specific scroll velocities.
  - Call `Tick(1.0f)` multiple times.
  - Assert that the internal offset or transform position updates exactly according to `Velocity * deltaTime`.
  - Assert that the wrapping logic triggers correctly when the threshold is crossed.

## 4. Acceptance Criteria
- [ ] `background10.jpg.meta` exists and configures the image correctly for Unity 2D.
- [ ] `BackgroundScroller.cs` is implemented with a deterministic `Tick(float)` method.
- [ ] In Horizontal orientation, the background visually scrolls from right to left (-X) to simulate forward (+X) movement.
- [ ] In Vertical orientation, the background visually scrolls bottom to top (+Y) to simulate forward (-Y) movement.
- [ ] Background wraps seamlessly without visual gaps.
- [ ] `GameController.cs` successfully instantiates and manages the scrolling background instead of a static one.
- [ ] NUnit tests in `BackgroundScrollerTests.cs` pass and verify mathematical correctness of scrolling and wrapping without relying on Unity's internal `Time.deltaTime`.
