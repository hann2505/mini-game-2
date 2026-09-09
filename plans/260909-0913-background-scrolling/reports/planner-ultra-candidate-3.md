# Background Scrolling Implementation Plan

## 1. Goals
- **Simulate Forward Motion:** Create the illusion that Object A is moving forward by scrolling the background in the opposite direction. Specifically, for a player facing right (+X), the background must scroll right-to-left (-X).
- **Implement a Modular Scrolling System:** Create `BackgroundScroller.cs` as a reusable component that supports deterministic updates, configurable axes, directions, and speeds.
- **Asset Configuration:** Properly configure `background10.jpg` with a `.meta` file to ensure it's imported correctly as a 2D Sprite in Unity.
- **Orientation Support:** Ensure the scrolling system supports both horizontal and vertical orientations according to the game's existing design.
- **Deterministic Testing:** Provide full NUnit test coverage for the scrolling logic using a deterministic `Tick(float deltaTime)` approach.

## 2. Technical Architecture & Kinematics
- **Component Structure:**
  - `BackgroundScroller` will manage two `SpriteRenderer` instances.
  - One sprite represents the main background, the other acts as a seamless extension.
- **Kinematics Math:**
  - Position is updated linearly: `Position += Direction * Speed * DeltaTime`.
  - Wrapping threshold logic: If `ScrollDirection` is -X (left), once a sprite's right edge passes the camera's left bounds (defined by `ViewportManager.MinX`), it is instantly translated to the right edge of the trailing sprite.
  - Translation distance: `Shift = SpriteWidth * 2`. 
- **Deterministic Update:** The update logic will be isolated in `public void Tick(float deltaTime)` so it can be called explicitly from the `GameController` or a custom loop, enabling precise step-by-step unit testing.

## 3. Phases

### Phase 1: Asset Configuration
- **Task:** Create the `.meta` file for `Assets/Sprites/Backgrounds/background10.jpg`.
- **Details:** The texture importer settings must be set to `Sprite` mode, `Texture2D`, with appropriate wrap modes.

### Phase 2: Core System Implementation (`BackgroundScroller.cs`)
- **Task:** Create `Assets/Scripts/Core/BackgroundScroller.cs`.
- **Details:**
    - Expose `ScrollSpeed`, `ScrollDirection` (Vector2), and a `Tick(float deltaTime)` method.
    - Implement a dual-sprite looping logic (two `SpriteRenderer` components). 
    - As they move based on `ScrollSpeed` and `deltaTime`, if one goes out of the camera's view bounds, it gets repositioned seamlessly to the trailing edge.

### Phase 3: Integration
- **Task:** Integrate with `GameController.cs` and `SceneSetupHelper.cs`.
- **Details:**
    - Update `GameController.cs` to instantiate or configure `BackgroundScroller`.
    - Adjust the initial scaling logic (currently in `ScaleBackground()`) to ensure both background segments are scaled properly to cover the viewport, maintaining the 2.5:1 aspect ratio.
    - Link the `BackgroundScroller.Tick(Time.deltaTime)` call in the `GameController`'s `Update` loop (or deterministic game tick loop).

### Phase 4: Unit Testing
- **Task:** Create `Assets/Editor/Tests/BackgroundScrollerTests.cs`.
- **Details:**
    - Write NUnit tests to verify background movement per `Tick()`.
    - Test wrapping logic: ensure that when a background sprite exceeds the bound threshold, it snaps correctly to the trailing position without losing distance accuracy.

## 4. Acceptance Criteria
1. **Asset:** `background10.jpg.meta` exists and configures the texture as a 2D Sprite.
2. **Scrolling Logic:** `BackgroundScroller.cs` provides a deterministic `Tick` method with correct wrapping kinematics.
3. **Integration:** `GameController.cs` uses `BackgroundScroller` and updates it. The static `ScaleBackground` logic is replaced or adapted to support multiple scrolling components.
4. **Visual Behavior:** In Horizontal mode, the background scrolls right-to-left (-X) to simulate Object A moving right (+X).
5. **Testing:** `BackgroundScrollerTests.cs` contains passing NUnit tests validating position updates and wrap-around mechanics.
