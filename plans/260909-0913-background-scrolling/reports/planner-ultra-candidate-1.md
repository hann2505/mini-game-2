# Background Scrolling Implementation Plan

## Goals
1.  **Parallax/Scrolling Effect**: Implement a continuous background scrolling mechanism using `background10.jpg` to simulate the player object (Object A) moving forward.
2.  **Kinematic Correctness**: Ensure the background scrolls in the opposite direction of the player's facing/movement direction (e.g., right-to-left scrolling for right-facing movement) to create an accurate illusion of forward motion.
3.  **Deterministic Logic**: Extract the core scrolling logic into a deterministic, testable method (`Tick(float deltaTime)`) independent of Unity's `Update()`.
4.  **Asset Configuration**: Properly configure `background10.jpg` with a `.meta` file to ensure correct texture settings (e.g., Sprite, appropriate wrap mode or pivot).
5.  **Robust Architecture**: Create a modular `BackgroundScroller` component integrated cleanly with `GameController` and `SceneSetupHelper`.

## Technical Architecture & Kinematics Math
- **Kinematics**: When Object A (Player) moves right (+X) relative to the world, the camera also moves right. For a static background relative to the world, its position in screen space should move left (-X). Thus, moving the background `background.position += Vector2.left * speed * deltaTime` simulates forward motion. The speed should be proportional to the player's speed, or an independent parallax factor.
- **Component Structure**: `BackgroundScroller` will hold references to two `SpriteRenderer` elements. It manages an offset variable updated deterministically via `Tick(float deltaTime)`. When the offset exceeds the width of the background, it wraps around. 

## Phases

### Phase 1: Asset Preparation
-   **Target**: `Assets/Sprites/Backgrounds/background10.jpg.meta`
-   **Actions**:
    -   Generate a `.meta` file for the background image if missing, or update the existing one.
    -   Set `textureType` to Sprite (2D and UI).
    -   Set `spriteMode` to Single.
    -   Depending on the chosen scrolling technique (dual-sprite wrapping vs. material UV offset), ensure `wrapMode` is set to `Repeat` if using UV scrolling, or keep standard settings if using a dual-sprite transform-based scroller. (Transform-based dual-sprite scrolling is recommended for standard 2D SpriteRenderers).

### Phase 2: Core Background Scroller System
-   **Target**: `Assets/Scripts/Core/BackgroundScroller.cs`
-   **Actions**:
    -   Create a new MonoBehaviour `BackgroundScroller`.
    -   **Properties**: `ScrollSpeed`, `ScrollDirection` (Vector2), `ResetThreshold` (float), `SpriteWidth` (float).
    -   **State**: Track current position or offset.
    -   **Core Method**: Implement `public void Tick(float deltaTime)` to update the background's state deterministically.
    -   **Visuals**: Implement a dual-sprite system (two child GameObjects with `SpriteRenderer`). `Tick` will move both transforms by `ScrollDirection * ScrollSpeed * deltaTime`. When a sprite moves past the `ResetThreshold` (e.g., off-screen to the left), it is repositioned to the back of the queue (to the right of the other sprite).

### Phase 3: Integration with Game Systems
-   **Target**: `Assets/Scripts/Core/GameController.cs` and `SceneSetupHelper`
-   **Actions**:
    -   Modify `GameController` to initialize `BackgroundScroller` instead of statically scaling the background.
    -   Calculate `SpriteWidth` based on `background10.jpg` dimensions and Viewport bounds (`ViewportManager.Instance`).
    -   Set the default `ScrollDirection` to `Vector2.left` (-X) when the orientation is `Horizontal` and the player is facing right (+X). This corrects the user's "left to right" intuition to match realistic kinematics.
    -   Call `BackgroundScroller.Tick(Time.deltaTime)` within the main game loop update.

### Phase 4: Deterministic Unit Testing
-   **Target**: `Assets/Editor/Tests/BackgroundScrollerTests.cs`
-   **Actions**:
    -   Create a new NUnit test script.
    -   Write tests for `BackgroundScroller.Tick(float deltaTime)` to verify:
        -   Background moves by the correct amount given a specific `deltaTime` and `ScrollSpeed`.
        -   Background resets/loops correctly when crossing the `ResetThreshold`.
        -   Scroll direction respects both horizontal and vertical axes (configurability check).

## Acceptance Criteria
-   [ ] `background10.jpg` imports without errors and has correct Unity `.meta` settings.
-   [ ] `BackgroundScroller.cs` contains a pure `Tick(float deltaTime)` method that governs motion.
-   [ ] In `Horizontal` mode, if the player implies rightward (+X) forward motion, the background scrolls continuously from right to left (-X).
-   [ ] The scrolling loops infinitely without visual gaps.
-   [ ] `BackgroundScrollerTests.cs` contains at least 3 passing NUnit tests validating the `Tick` math (motion and looping threshold).
-   [ ] `GameController` delegates background updates to the new modular system instead of static placement.
