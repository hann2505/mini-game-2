---
title: NPC restricted-zone warning audio
date: 2026-09-15
summary: Wired warning.mp3 to restricted-zone ingress and randomized each warning burst to 3-6 pulses.
---

# NPC restricted-zone warning audio

## What happened

The restricted-zone trigger already detected `TargetController` ingress, but `SampleScene` and `SceneSetupHelper` still referenced `bomb.mp3`, and the warning burst was fixed at four pulses.

## Changes

- Imported `Assets/Audio/SFX/warning.mp3` with a tracked Unity meta file.
- Updated the scene and scene setup helper to assign the warning clip.
- Selected a fresh inclusive 3-6 pulse count per accepted intrusion while retaining the existing 3-second debounce.
- Serialized warning playback on its dedicated channel so one clip finishes before the next begins; replacing a burst also stops its currently playing clip.
- Added EditMode coverage for the asset path and pulse range.

## Verification

Unity refreshed and compiled both runtime and editor assemblies without C# errors. All 13 focused `AudioSystemTests` passed, including coverage for the warning asset, pulse range, debounce, and sequential timing rule.

## Next steps

None required for this behavior.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.
