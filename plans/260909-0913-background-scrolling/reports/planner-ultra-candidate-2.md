# Background Scrolling Implementation Plan (Ultra Candidate 2)

## 1. Goals
- **Modular Scrolling System:** Develop `BackgroundScroller.cs`, a robust component to handle seamless background scrolling.
- **Kinematics & Relative Motion:** Achieve the illusion of Object A moving forward (facing right, +X) by scrolling the background in the opposite direction (right-to-left, -X). The system will be configurable (speed, direction, axis).
- **Asset Preparation:** Properly configure `Assets/Sprites/Backgrounds/background10.jpg` (1920x768, 2.5:1 ratio) with appropriate `TextureImporter` settings via a `.meta` file.
- **Integration:** Hook the new scrolling system into `GameController` and `SceneSetupHelper`, replacing or augmenting the existing static `ScaleBackground()` logic.
- **Deterministic Testing:** Write deterministic NUnit tests in `Assets/Editor/Tests/BackgroundScrollerTests.cs` leveraging a `Tick(float deltaTime)` method.

## 2. Technical Architecture
### Kinematics and Math
While the initial request mentioned moving "left to right", physical relative motion implies that to feel like moving forward (right, +X), the background must translate backward (left, -X). The `BackgroundScroller` will default to a `-X` translation vector for horizontal mode.
The background texture has a wide aspect ratio. To achieve seamless scrolling, the scroller will manage at least two instances (segments) of the background sprite, leapfrogging them when they exit the `ViewportManager`'s orthographic bounds.
Leapfrog Math: When `segment.position.x + (width/2) <= ViewportManager.Instance.MinX`, the segment will be repositioned to `segment.position.x + (width * 2)`.

### Component Structure
- `BackgroundScroller`: Monobehaviour component attached to a background parent.
  - Maintains a list of spawned sprite segments.
  - Exposes `Vector2 ScrollDirection`, `float ScrollSpeed`.
  - Exposes `Tick(float deltaTime)` for updates (called in `Update()` during normal play).

## 3. Phases & Implementation Steps

### Phase 1: Asset Configuration
- Create `Assets/Sprites/Backgrounds/background10.jpg.meta`.
- Configure the asset as a `Sprite (2D and UI)` with `SpriteMode` set to Single, and appropriate PPU.

### Phase 2: Core BackgroundScroller Implementation
- Create `Assets/Scripts/Core/BackgroundScroller.cs`.
- Define properties: `ScrollDirection` (default `(-1, 0)`), `ScrollSpeed` (e.g. `2.0f`).
- Implement `public void Tick(float deltaTime)` to manually advance the scroll state.
- Implement the leapfrog logic inside `Tick`:
  - Advance all segments by `ScrollDirection * ScrollSpeed * deltaTime`.
  - Check bounds against `ViewportManager` and reposition segments that fall completely off-screen.

### Phase 3: Integration with GameController
- Update `Assets/Scripts/Core/GameController.cs`.
- Refactor `ScaleBackground()` to instantiate the segments required for `BackgroundScroller` instead of a single sprite.
- Connect the `BackgroundScroller` to the game's lifecycle by allowing it to use `Update()` which delegates to `Tick(Time.deltaTime)`.
- Ensure integration handles both Horizontal and Vertical orientations.

### Phase 4: Deterministic NUnit Testing
- Create `Assets/Editor/Tests/BackgroundScrollerTests.cs`.
- Instantiate an isolated test environment with mocked `ViewportManager` bounds.
- Use `BackgroundScroller.Tick(deltaTime)` to advance the system frame-by-frame (e.g., `Tick(0.1f)`).
- Assert that segments move accurately according to kinematics logic.
- Assert that wrap-around occurs deterministically at the correct boundary.

## 4. Acceptance Criteria
- [ ] `background10.jpg.meta` exists and sets the asset as a Sprite.
- [ ] `BackgroundScroller.cs` provides a configurable scrolling API with a `Tick(float deltaTime)` method.
- [ ] Default configuration moves the background right-to-left (-X) to simulate Object A moving forward.
- [ ] `GameController.cs` initializes the scrolling system correctly and respects viewport bounds.
- [ ] `BackgroundScrollerTests.cs` passes completely, successfully validating deterministic movement and wrapping.
