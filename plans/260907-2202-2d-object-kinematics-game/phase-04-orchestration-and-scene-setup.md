---
phase: 4
title: "Game Orchestration, Dynamic Axis Config & Scene Setup"
status: pending
priority: P1
effort: "1h"
dependencies: ["phase-01-start", "phase-02-player-projectile-mechanics", "phase-03-target-kinematics-boundary-wrap"]
---

# Phase 4: Game Orchestration, Dynamic Axis Config & Scene Setup

## Overview
Coordinates the initial placement of Object A and Object B at opposing mid-edges, dynamically normalizes their visual dimensions for exact size parity, configures the movement axis (Horizontal vs Vertical), and wires up the Unity scene.

## Requirements
- Functional:
  - `GameController.cs` manages initial entity spawning, placement, and sizing.
  - Exposes an enum `GameOrientation { Horizontal, Vertical }`:
    - In `Horizontal` mode: Object A starts at Mid-Left `Viewport(0, 0.5)` facing right, Object B starts at Mid-Right `Viewport(1, 0.5)` facing left.
    - In `Vertical` mode: Object A starts at Mid-Top `Viewport(0.5, 1)` facing down, Object B starts at Mid-Bottom `Viewport(0.5, 0)` facing up.
  - Dynamically forces scale parity between Object A and Object B: adjusts Object B's `transform.localScale` so that its `SpriteRenderer.bounds.size` precisely matches Object A's bounding box dimensions.
  - Propagates the chosen orientation and bounds configuration to `PlayerController` and `TargetController`.
  - Configures the active scene `SampleScene.unity`:
    - Orthographic Camera with URP 2D renderer.
    - Background sprite from `Assets/Sprites/Backgrounds/background1.png` scaled to fit the camera view.
    - AudioSource with background music `Assets/Audio/Music/music.mp3` or SFX channels.
    - GameController GameObject with references to Prefab A, Prefab B, and Prefab C.
- Non-functional:
  - Clean Inspector exposure with tooltips and serialized fields.

## Architecture
- `GameController.cs`:
  - Enums: `public enum GameOrientation { Horizontal, Vertical }`.
  - References:
    - `[SerializeField] private GameObject playerPrefab;` (Object A)
    - `[SerializeField] private GameObject targetPrefab;` (Object B)
    - `[SerializeField] private GameObject projectilePrefab;` (Object C)
    - `[SerializeField] private GameOrientation orientation = GameOrientation.Horizontal;`
    - `[SerializeField] private float targetUniformSize = 1.5f;` (target world size in units)
  - Lifecycle:
    - In `Start()`:
      1. Calls `ViewportManager.Instance.Initialize()`.
      2. Spawns/positions Object A at `Viewport(0, 0.5)` or `Viewport(0.5, 1)`.
      3. Spawns/positions Object B at `Viewport(1, 0.5)` or `Viewport(0.5, 0)`.
      4. Normalizes Object A's size to `targetUniformSize`, then normalizes Object B's size to match Object A's bounds exactly.
      5. Passes orientation settings to controllers.

## Related Code Files
- Create: `Assets/Scripts/Core/GameController.cs`
- Modify: `Assets/Scenes/SampleScene.unity`

## Implementation Steps
1. Implement `GameController.cs` in `Assets/Scripts/Core/`.
2. Configure prefab variants or instances for Object A (Player), Object B (Target), and Object C (Projectile).
3. Set up `SampleScene.unity` with Main Camera, ViewportManager, GameController, and Background.
4. Verify dynamic axis switching and scale parity in both Horizontal and Vertical configurations.

## Success Criteria
- [ ] On Play, Object A and Object B spawn directly opposite each other at the exact center of opposing edges.
- [ ] In the Scene view and Game view, Object A and Object B have identical dimensions on screen.
- [ ] Toggling between Horizontal and Vertical orientation places objects correctly (Left-Right vs Top-Bottom).
- [ ] Clicking/tapping on AVD fires projectiles from A towards B; B wraps seamlessly when reaching the opposite edge.

## Risk Assessment
- Risk: `SampleScene.unity` meta or YAML conflict if modified externally.
  - Mitigation: Create self-bootstrapping runtime GameController that can dynamically spawn or configure scene GameObjects on Awake if prefabs are assigned.
