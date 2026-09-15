---
title: Readable health and armor HUD bars
date: 2026-09-14
summary: Add read-only health and armor sliders to the existing HUD with numeric labels and low-health color.
---

# Readable health and armor HUD bars

## Changes
GameHUDController upgrades existing Canvas health and armor labels with compact dark-backed slider bars at startup. Health is green and turns red at 25% or below; armor is blue. Both retain centered numeric labels and do not intercept clicks or keyboard navigation. Existing scenes upgrade without rebuilding them. SceneSetupHelper defaults armor text to zero.

## Verification
Unity EditMode HUD and full integration suites: 24/24 passed. Covered initial values, armor pickup, damage overflow, recovery, low-health color, repeated setup, and non-interactive graphics. Scoped diff whitespace validation passed. The isolated batch Unity process exited. No visual game-view inspection was performed. Earlier unrelated combat test failures remain outside this HUD change. AgentWiki publish skipped.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.
