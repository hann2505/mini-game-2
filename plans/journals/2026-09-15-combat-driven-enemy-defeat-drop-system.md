---
title: Combat-Driven Enemy Defeat Drop System
date: 2026-09-15
summary: "Refactored item/hazard generation from passive perimeter drift into 100% deterministic enemy kill drops with Hazard Mine arming grace period."
---

# Combat-Driven Enemy Defeat Drop System

## What happened

Ambient map-edge hazard and pickup generation in `HazardSpawner` created background clutter rather than rewarding active combat engagement. Furthermore, spawning Hazard Mines at point-blank range upon enemy defeat previously posed an instant unavoidable collision hazard to the player.

## Changes

- Configured `HazardSpawner` with `autoSpawn = false`, `prewarmViewport = false`, and `enemyDropChance = 1.0f`.
- Rebalanced drop distribution to reward-favored weights: 50% Gem Core (Object Z), 25% Supply Crate (Object Y), 25% Hazard Mine (Object X).
- Implemented a 0.75-second arming delay (`SetArmingDelay`) and alpha pulsation warning on `InteractiveEntity` for dropped Hazard Mines.
- Guarded collision resolution bidirectionally in both `InteractiveEntity` (`CheckPlayerCollision`, `OnTriggerEnter2D`) and `PlayerController.OnTriggerEnter2D` against `!IsArmed`.
- Synchronized code defaults across `SceneSetupHelper.cs` and `SampleScene.unity`.
- Added 4 comprehensive EditMode unit tests in `HazardAndHUDTests.cs` verifying passive spawn elimination, 100% kill drop triggering, arming grace period immunity, and alpha restoration.

## Verification

Both runtime and editor assemblies compiled cleanly with 0 errors via `dotnet build mini-game-2.sln` and Unity's background compilation. Code review approved with no regressions.

## Next steps

Playtest combat drop pacing and visual feedback under diverse enemy wave densities.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.
