---
title: Bomb 3 animated Object X
date: 2026-09-14
summary: Object X now uses Bomb_3 idle frames and plays a one-shot Bomb_3 explosion on player contact.
---

# Bomb 3 animated Object X

## What changed
Object X now uses the ten Bomb_3 idle sprite frames as a looping 12 FPS animation. Contact spawns a separate nine-frame Bomb_3 explosion at 15 FPS before Object X despawns, allowing the complete one-shot animation to remain visible.

The collision still applies five points of armor-first damage and the existing slowdown. When shield immunity is active, it absorbs the contact and prevents both damage and slowdown.

## Assets and tooling
Added Assets/Prefabs/Bomb_3_Explosion.prefab and updated Assets/Prefabs/Hazard_Mine.prefab. SceneSetupHelper can regenerate both through Kinematics Game > Generate Bomb 3 Hazard and also refreshes them during normal scene setup.

## Verification
The focused Unity EditMode suites passed 25/25, including actual prefab frame counts and names, looping idle configuration, one-shot auto-destroy explosion configuration, collision spawning, armor-first damage, health overflow, slowdown, and shield immunity. The solution also builds with zero warnings and zero errors. Diff whitespace validation passed.

AgentWiki publish skipped.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.
