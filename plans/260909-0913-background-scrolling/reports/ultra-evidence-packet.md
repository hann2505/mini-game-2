# Ultra Evidence Packet: Background Scrolling Kinematics

## 1. Verbatim User Request
> "@Assets/Sprites/Backgrounds/background10.jpg has a long width, I want to change the background, move the background from left to right to make the game feel like the object A is moving forward. --ultra --advice"

---

## 2. Asset & Codebase Inventory

### A. Asset Analysis: Assets/Sprites/Backgrounds/background10.jpg
- **Resolution**: 1920 x 768 pixels (Aspect ratio 2.5:1, wide panorama).
- **Current State**: File is present on disk (138 KB, JPEG).
- **Missing Meta**: `background10.jpg.meta` does not exist yet in git / workspace.
- **Required TextureImporter Settings**:
  - `textureType`: 8 (`Sprite (2D and UI)`)
  - `spriteImportMode`: 1 (`Single`)
  - `wrapMode`: 0 (`Repeat`) or `Clamp`
  - `spriteMeshType`: 1 (`FullRect`) to avoid tight polygonal mesh overhead on a large background rectangle
  - `alphaIsTransparency`: 0 (JPEG has no alpha channel; RGB)
  - `npotScale`: 0 (`None`)

### B. Current Background Architecture in GameController.cs
- `backgroundRenderer`: Serialized `SpriteRenderer` reference.
- `ScaleBackground()`:
  Scales background uniformly using `scaleFactor = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y)` and positions at `ViewportManager.Instance.Center` at z = 5.
- Background is currently static.
- `orientation`: Controls `GameOrientation.Horizontal` vs `GameOrientation.Vertical`. In Horizontal mode, Player is at left edge facing right (+X). In Vertical mode, Player is at top edge facing down (-Y).

### C. Viewport Management in ViewportManager.cs
- Manages dynamic orthographic camera bounds: `MinX`, `MaxX`, `MinY`, `MaxY`, `Width`, `Height`, `Center`.
- Calculates world bounds from camera viewport points without per-frame garbage collection.

### D. Scene Construction in SceneSetupHelper.cs
- `SetupSceneHierarchy()` creates the `Background` GameObject with `SpriteRenderer` (sorting order -100).
- Currently hardcodes `background1.png` as default background sprite.
- In `SampleScene.unity`, `Background` is currently using `background2.png`.

### E. Existing Test Suite (Assets/Editor/Tests/)
- `GameControllerTests.cs`: Tests `ScaleBackground_EncompassesCameraView`, `NormalizeEntitySizes`, `ApplyOrientation`.
- `ViewportManagerTests.cs`: Tests camera bounds calculation across aspect ratios (16:9, 4:3, etc.).
- `PlayerControllerTests.cs`, `TargetControllerTests.cs`, `ProjectileTests.cs`.

---

## 3. Kinematics & Relative Motion Analysis

### The Direction Dilemma & Resolution
- **User phrasing**: "move the background from left to right to make the game feel like the object A is moving forward."
- **Physics / Visual Kinematics**:
  - In Horizontal orientation, Object A (Player Spaceship) is anchored on the left side of the screen facing right (`+X`), shooting projectiles towards `+X`.
  - In relative motion, when an observer/vehicle moves forward (`+X`), stationary background scenery moves backward relative to the camera (`-X`, from right to left).
  - If the background sprite literally moves from left to right (`+X`), Object A will appear to drift backward or reverse!
  - **Reconciliation**: The user's goal is explicitly stated: *"to make the game feel like the object A is moving forward"*. The user likely visualized the camera/viewport panning from left to right across the wide panoramic texture, which causes the background in camera space to shift right-to-left.
  - **Requirement**: The scrolling system must ensure the forward illusion by default (`scrollVelocity = (-speed, 0)` in Horizontal mode), while exposing configurable parameters (speed, direction, invert flag) so the user/designer has complete control. In Vertical mode, forward motion corresponds to downward scrolling `(0, -speed)`.

---

## 4. Technical Approaches Evaluated

### Approach 1: Dual-Sprite World-Space Wrapping Loop
- **Mechanism**: The Background GameObject contains two identical child SpriteRenderers (or two adjacent sprites). Both translate by `velocity * Time.deltaTime`. When a sprite moves completely beyond the viewport edge, its position wraps around to snap behind the other sprite.
- **Pros**: 100% native Unity 2D SpriteRenderer pipeline, works with standard Sprite materials, no texture wrap mode or custom shader required, crisp visual rendering, exact sizing with `ViewportManager`.
- **Cons**: Requires seam alignment management (precision snapping) so no sub-pixel gaps appear.

### Approach 2: Texture UV Offset Scrolling (Quad + Material)
- **Mechanism**: Use a 3D Quad with a Material (e.g. Unlit/Texture or Sprites/Default) and animate `material.mainTextureOffset`. Requires texture wrap mode = `Repeat`.
- **Pros**: Single quad, zero transform repositioning, mathematically seamless repeat.
- **Cons**: `background10.jpg` is a panoramic photograph/matte illustration that may not be seamlessly tileable horizontally (a visible vertical seam line may appear where left edge meets right edge unless mirrored or blended). Also diverges from the `SpriteRenderer` pattern used throughout the rest of the project.

### Approach 3: Panoramic Pan & Clamp (Camera / Scroller Window)
- **Mechanism**: Background is scaled to fit camera height, leaving extra width (1920x768 has ~2.5:1 aspect ratio vs 16:9 ~1.78:1). Background translates until the right/left boundary of the sprite is reached, then stops or ping-pongs.
- **Pros**: Shows the original non-repeating image without tiling artifacts.
- **Cons**: Finite duration. Once the edge is reached, the forward movement illusion stops unless looping is handled.

### Approach 4: Seamless Looping Dual-Sprite BackgroundScroller Component
- **Mechanism**: A dedicated `BackgroundScroller` component in `KinematicsGame.Core`.
  - Supports continuous scrolling with configurable `scrollSpeed` and `scrollDirection`.
  - Integrates with `GameOrientation` (switches between horizontal and vertical scroll axes).
  - Automatically sizes sprites via `ViewportManager` to ensure zero black bars vertically and seamless horizontal tiling.
  - Implements a deterministic `Tick(float deltaTime)` method for Editor unit testing without running Play mode.

---

## 5. Architectural & Quality Constraints
1. **Separation of Concerns**: Do not bloat `GameController.cs`. Create a specialized `BackgroundScroller.cs` component that manages background movement, while `GameController` orchestrates lifecycle and orientation events.
2. **KISS & DRY**: Reuse existing `ViewportManager` for bounds and sizing.
3. **Deterministic Testing**: Write unit tests in `Editor/Tests/BackgroundScrollerTests.cs` verifying position updates, wrap-around logic, orientation switching, and edge snapping.
4. **Asset Integrity**: Generate and commit proper `background10.jpg.meta` ensuring Sprite (2D and UI) import settings. Update `SceneSetupHelper.cs` so scene recreation uses `background10.jpg`.
