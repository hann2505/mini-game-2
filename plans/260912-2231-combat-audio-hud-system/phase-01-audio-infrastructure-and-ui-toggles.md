---
phase: 1
title: "Audio Infrastructure, UI Toggles, and Warning Zone"
status: completed
priority: P1
effort: "3h"
dependencies: []
---

# Phase 1: Audio Infrastructure, UI Toggles, and Warning Zone

## Overview
Delivers the foundational audio architecture and user interface controls. Establishes a dedicated 3-channel `AudioManager` (BGM, SFX pool, Warning siren) to prevent acoustic voice starvation, implements in-place sprite-swapping `AudioToggleButton` components with identical dimensions (64x64) and anchor coordinates, and introduces an orientation-aware `RestrictedZoneTrigger` that alerts the player with 3 to 6 repeated warning pulses upon enemy intrusion.

## Requirements
- Functional:
  - `AudioManager` manages 3 isolated `AudioSource` channels: BGM loop, polyphonic SFX pool (4 channels), and dedicated warning siren.
  - Mute controls for SFX and Music with state persistence via `PlayerPrefs` (`"Audio_SFX_Muted"`, `"Audio_Music_Muted"`).
  - Sound toggle: Clicking `SoundOff` mutes SFX and swaps displayed sprite to `SoundOn` in-place; clicking `SoundOn` restores SFX and swaps back to `SoundOff`.
  - Music toggle: Clicking `MusicOn` plays BGM and swaps displayed sprite to `MusicOff` in-place; clicking `MusicOff` halts BGM and swaps back to `MusicOn`.
  - Both toggle buttons share identical `RectTransform` dimensions (64x64) and positions.
  - `RestrictedZoneTrigger` detects when `TargetController` (Object B) enters its 2D trigger volume and initiates a random 3–6 pulse warning alarm; each pulse finishes before the next one starts.
  - 3.0s anti-spam debounce timer prevents audio cacophony when multiple enemies breach simultaneously.
- Non-functional:
  - Zero audio voice clipping during high-rate weapon firing.
  - In-place sprite swapping on a single `RectTransform` eliminates layout rebuilds and pixel offset glitches.
  - Full EditMode testability via explicit `Tick(float dt)` simulation methods without waiting for real-time coroutines.

## Architecture
- `KinematicsGame.Audio.AudioManager`: Singleton managing channels, mute states, and alarm coroutines.
- `KinematicsGame.UI.AudioToggleButton`: Generic component attached to single `Button` with `Image` component; handles click events and sprite swapping.
- `KinematicsGame.Combat.RestrictedZoneTrigger`: 2D Trigger box anchored relative to `ViewportManager` (Left 20% in Horizontal, Top 20% in Vertical).

## Related Code Files
- Create:
  - `Assets/Scripts/Audio/AudioManager.cs`
  - `Assets/Scripts/UI/AudioToggleButton.cs`
  - `Assets/Scripts/Combat/RestrictedZoneTrigger.cs`
  - `Assets/Editor/Tests/AudioSystemTests.cs`
- Modify:
  - `Assets/Scripts/Core/GameController.cs` (bind audio sources and trigger zone references)

## Implementation Steps
1. Create `AudioManager.cs` with `musicSource`, `sfxSourcePool` (array of 4 AudioSources), and `warningSource`. Add `ToggleSfx()`, `ToggleMusic()`, `PlaySfx()`, and `PlayWarningAlarm(int pulses, float interval)`.
2. Create `AudioToggleButton.cs` with serialized fields for `activeSprite`, `inactiveSprite`, `fixedDimensions` (64x64), and `ToggleType` (SFX or Music). Wire in-place `Image.sprite` swap.
3. Create `RestrictedZoneTrigger.cs` with `BoxCollider2D` (isTrigger = true). In `OnTriggerEnter2D`, check for `TargetController` and trigger alarm with 3.0s debounce. Add `AlignToViewport(GameOrientation orientation)`.
4. Create NUnit EditMode unit tests in `AudioSystemTests.cs` verifying mute toggling, PlayerPrefs persistence, sprite swapping, and warning burst counter logic.

## Success Criteria
- [ ] Clicking Sound button toggles SFX mute and swaps sprite between `sound_off.png` and `sound_on.png` without moving.
- [ ] Clicking Music button toggles BGM and swaps sprite between `music_TurnOn.png` and `music_TurnOff.png` without moving.
- [ ] Both buttons have identical size (64x64) and stable positions across orientation changes.
- [x] Restricted Zone detects Object B ingress and triggers 3 to 6 warning beeps with 3.0s debounce.
- [x] All new unit tests in `AudioSystemTests.cs` pass 100% green.

## Risk Assessment
- **Risk**: Rapid enemy wave triggering overlapping warning coroutines.
  - *Mitigation*: Enforce a hard 3.0-second debounce timestamp check in `RestrictedZoneTrigger` and cancel any existing warning coroutine before starting a new one.
- **Risk**: EditMode tests failing because Unity Coroutines do not tick outside PlayMode.
  - *Mitigation*: Provide an explicit `SimulatePulseSequence(int count)` method in `AudioManager` to test counter logic synchronously in NUnit.
