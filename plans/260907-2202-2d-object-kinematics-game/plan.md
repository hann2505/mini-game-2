---
title: "2d-object-kinematics-game"
description: "Implementation of responsive 2D kinematics game featuring Object A (Player), Object B (Target), and Object C (Projectile) with dynamic axis spawning, size parity, 4-way movement, touch/mouse firing, and boundary wrapping."
status: pending
priority: P1
effort: "4h"
tags: ["unity", "2d", "kinematics", "input-system", "urp"]
created: 2026-09-07
---

# 2D Object Kinematics Game

## Overview
A functional 2D Unity scene demonstrating precise screen-boundary spawning, input-driven mechanics, and edge-wrapping kinematics in Unity 6 (`6000.0.58f2`) with URP 2D and the New Input System (`com.unity.inputsystem` v1.14.2).

- **Object A (Player Spaceship)**: Spawns at the center of one screen edge (e.g., Mid-Left) and moves freely in 4 directions.
- **Object B (Target Enemy)**: Spawns at the opposing edge (e.g., Mid-Right), visually sized identically to A, and moves flexibly with autonomous kinematics.
- **Object C (Projectile Bullet)**: Spawns from Object A upon touch/click on screen (AVD mouse/touch), traveling with customizable speed and direction.
- **Boundary Wrapping**: When Object B crosses the screen boundary where Object A originated (e.g., Left edge), it immediately respawns on the opposite edge (Right edge) at a randomized coordinate.

## Goals

| # | Goal | Priority |
|---|------|----------|
| 1 | Dynamic Viewport bounds calculation and runtime size parity normalization | P1 |
| 2 | Player 4-way movement and touch/mouse projectile firing (Object A & C) | P1 |
| 3 | Target autonomous 4-way kinematics with seamless edge-wrapping (Object B) | P1 |
| 4 | Game orchestration with configurable Horizontal/Vertical axis and scene integration | P1 |

## Phases

| # | Phase | Status | Priority | Dependencies |
|---|-------|--------|----------|--------------|
| 1 | [Phase 1: Viewport & Screen Bounds Foundation](./phase-01-start.md) | Completed | P1 | None |
| 2 | [Phase 2: Player Controller & Projectile Mechanics](./phase-02-player-projectile-mechanics.md) | Pending | P1 | Phase 1 |
| 3 | [Phase 3: Target Kinematics & Boundary Wrap](./phase-03-target-kinematics-boundary-wrap.md) | Pending | P1 | Phase 1 |
| 4 | [Phase 4: Game Orchestration, Dynamic Axis Config & Scene Setup](./phase-04-orchestration-and-scene-setup.md) | Pending | P1 | Phase 1, 2, 3 |

## Success Criteria

- [ ] Object A spawns exactly at `Viewport(0, 0.5)` (Mid-Left) and Object B at `Viewport(1, 0.5)` (Mid-Right) in Horizontal mode (or Top/Bottom equivalent in Vertical mode).
- [ ] Object A and Object B have identical bounding box dimensions on screen regardless of sprite resolution.
- [ ] Object A responds to WASD/Touch controls with 4-way movement clamped within the screen view.
- [ ] Object B moves autonomously in 4 directions with configurable speeds.
- [ ] Touching the screen or clicking the mouse fires Object C from Object A with customizable trajectory and speed.
- [ ] When Object B crosses the boundary behind Object A, it respawns at the opposite edge with a randomized perpendicular position without visual popping.

<!-- slug: 2d-object-kinematics-game -->