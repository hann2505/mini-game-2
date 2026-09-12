---
phase: 5
title: "Scene Setup Automation, Integration, and Verification"
status: completed
priority: P1
effort: "3h"
dependencies: ["phase-01-audio-infrastructure-and-ui-toggles", "phase-02-player-stats-and-kinematics", "phase-03-combat-attacks-and-defenses", "phase-04-interactive-hazards-and-hud"]
---

# Phase 5: Scene Setup Automation, Integration, and Verification

## Overview
Automates the assembly and linking of all newly created prefabs, audio sources, UI toggle buttons, restricted zone boundaries, and HUD panels in `SampleScene.unity` via `SceneSetupHelper.cs`. Validates full dual-orientation compatibility (`Horizontal` and `Vertical`), and executes the complete NUnit EditMode test suite to guarantee zero regressions and 100% test pass rate.

## Requirements
- Functional:
  - Extend `SceneSetupHelper.SetupAll()` menu item (`Kinematics Game/Setup Scene & Prefabs`) to construct and save:
    - `AudioManager` instance with configured `musicSource`, `sfxSourcePool`, and `warningSource`.
    - Prefabs: `Projectile.prefab` (Blaster), `Missile.prefab`, `Bomb.prefab`, `Hazard_Mine.prefab` (X), `Supply_Crate.prefab` (Y), `Gem_Core.prefab` (Z), `ShieldOverlay.prefab`.
    - HUD Canvas: `AudioToggleButton` instances for Sound and Music (64x64, anchored top-left), `RestrictedZoneTrigger` (anchored to viewport perimeter), and player stats widgets (HP slider/text, Armor bar, Gold/Diamond counters).
    - Player GameObject with attached `PlayerController`, `PlayerStats`, `PlayerCombatSystem`, `PlayerDefenseSystem`, and `CollisionEffectDispatcher`.
    - `HazardSpawner` linked to X, Y, Z prefabs.
  - Dual-Orientation Verification:
    - Verify that flipping `GameController.Orientation` between `Horizontal` and `Vertical` correctly re-anchors the `RestrictedZoneTrigger`, updates Player fire vectors, and preserves UI toggle layout without visual distortion.
- Non-functional:
  - 100% green pass on all existing tests (`BackgroundScrollerTests`, `GameControllerTests`, `PlayerControllerTests`, `ProjectileTests`, `ScoreManagerTests`, `TargetControllerTests`, `TargetProfileTests`, `UITests`, `ViewportManagerTests`) plus all newly added combat/audio test fixtures ($\ge 25$ new tests, $\ge 100$ total tests).
  - Clean compilation with 0 compiler warnings or errors.

## Architecture
- `KinematicsGame.Editor.SceneSetupHelper`: Static Editor utility constructing scene hierarchy, setting serialized properties, and calling `EditorSceneManager.SaveOpenScenes()`.
- `KinematicsGame.Tests.FullSystemIntegrationTests`: End-to-end NUnit EditMode test fixture verifying system coherence.

## Related Code Files
- Modify:
  - `Assets/Editor/SceneSetupHelper.cs`
- Create:
  - `Assets/Editor/Tests/FullSystemIntegrationTests.cs`

## Implementation Steps
1. Extend `SceneSetupHelper.EnsurePrefabsExist()` to generate and save `Missile.prefab`, `Bomb.prefab`, `Hazard_Mine.prefab`, `Supply_Crate.prefab`, and `Gem_Core.prefab`.
2. Extend `SceneSetupHelper.SetupSceneHierarchy()` to instantiate `AudioManager`, wire `AudioToggleButton` pairs onto the HUD Canvas, instantiate `RestrictedZoneTrigger`, and attach player combat/defense/stats components.
3. Wire audio clips from `Assets/Audio/SFX/` and `Assets/Audio/Music/` to `AudioManager` and entity inspectors.
4. Create `FullSystemIntegrationTests.cs` validating:
   - AudioManager channel assignments and mute states.
   - RestrictedZone orientation alignment upon switching `GameController.Orientation`.
   - Player firing all 3 weapons and activating both defense skills.
   - Resolving collisions with X, Y, Z and verifying that all 9 effects trigger as expected.
5. Execute full test suite via terminal/editor commands and verify 100% green pass.

## Success Criteria
- [ ] Running `Kinematics Game/Setup Scene & Prefabs` configures `SampleScene.unity` without errors or missing references.
- [ ] Sound and Music toggle buttons render with exact 64x64 size at top-left.
- [ ] Restricted Zone updates its bounds correctly when switching between Horizontal and Vertical orientations.
- [ ] All 3 attack types and 2 defense skills function in-game with visual and audio feedback.
- [ ] Interactive Objects X, Y, Z spawn and trigger 9 distinct effects on collision.
- [ ] 100% green pass on all NUnit EditMode tests (all existing tests + all new tests pass).

## Risk Assessment
- **Risk**: Missing asset references or broken serialized properties after scene rebuild.
  - *Mitigation*: Use `SerializedObject` and `FindProperty` in `SceneSetupHelper` with null-checks and `EditorUtility.SetDirty()` to guarantee persistent serialization.
- **Risk**: Test conflicts caused by static state lingering in singletons (`AudioManager`, `ScoreManager`).
  - *Mitigation*: Include explicit cleanup in `[TearDown]` methods in all test classes.
