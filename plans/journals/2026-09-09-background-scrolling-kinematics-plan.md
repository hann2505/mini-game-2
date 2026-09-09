---
title: Background Scrolling Kinematics Plan
date: 2026-09-09
summary: Ultra-mode verified implementation plan for background scrolling kinematics with dual-sprite leapfrog wrapping.
---

# Background Scrolling Kinematics Plan

Ultra-mode verified implementation plan for background scrolling kinematics with dual-sprite leapfrog wrapping.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.

## Context & Problem
The user requested changing the active background to `Assets/Sprites/Backgrounds/background10.jpg` (1920x768) and moving it from left to right to simulate Object A (Player Spaceship) moving forward. 
Analysis revealed a relative motion contradiction: Object A is positioned at the left edge facing right (+X). In relative motion, to create the illusion of forward travel through stationary scenery, the scenery must translate in the opposite direction (-X, right to left). Moving it +X makes Object A appear to drift backward. The user likely visualized camera panning across the image from left to right.

## Workflow Execution (--ultra --advice)
- **Shared Evidence Packet**: Scouted asset properties (missing `.meta` file, 2.5:1 ratio), `GameController.ScaleBackground()` aspect-ratio scaling, `ViewportManager` bounds, and `SceneSetupHelper`.
- **Advisory Checkpoint 1 (Kongming)**: Confirmed GO on the research and endorsed relative motion correction (-X scrolling for +X movement) with configurable direction.
- **5-Candidate Ultra Wave**: Dispatched 5 independent candidate planners in parallel. All 5 generated full candidate reports.
- **Ultra Verifier Selection**: Kongming independently verified and scored the 5 anonymized candidates. Candidate E (Candidate 2) won decisively with a score of 96/100, praised for exact leapfrog math (`position = otherSegment.position + segmentOffset`), aspect-ratio scaling preservation, and mocked deterministic testability.
- **Materialization**: Overwrote `plan.md` and 4 phase files with the winning architecture.
- **Hostile Red Team Review**: 3 reviewer lenses (Security Adversary, Failure Mode Analyst, Assumption Destroyer) surfaced 5 critical/high edge cases:
  1. Initial and dynamic orientation segment alignment (horizontal vs vertical axes).
  2. Window resize destroying scroll state (added `RefreshScale()` method).
  3. Parent SpriteRenderer Z-fighting and double rendering (disabled parent renderer when scroller active).
  4. Explicit `segmentHeight` computation for vertical leapfrog.
  5. Scale isolation preventing squared scale multiplication.
- **Consistency Sweep**: Applied all 5 mitigations across Phase 2, Phase 3, and Phase 4. Verified zero contradictions.
- **Advisory Checkpoint 3 (Kongming)**: Confirmed GO for execution.
