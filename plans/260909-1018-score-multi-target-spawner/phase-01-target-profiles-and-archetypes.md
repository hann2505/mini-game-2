---
phase: 1
title: "Target Profiles and Archetypes"
status: complete
priority: P1
effort: "1h"
dependencies: []
---

# Phase 1: Target Profiles and Archetypes

## Overview
Defines the data contract and archetype configurations for the 4 distinct bird enemy types in the game, binding visual sprites, kinematics multipliers, score point values, and spawn weighting.

## Requirements
- Functional: Create `TargetProfile` data model (serializable class / ScriptableObject) encapsulating `Sprite`, `SpeedMultiplier`, `WaveFrequencyMultiplier`, `WaveAmplitudeMultiplier`, `PointValue`, and `SpawnWeight`.
- Functional: Provide factory or default preset configurations for all 4 available bird sprites (`bird1.png`, `bird2.png`, `bird3.png`, `bird4.png`).
- Non-functional: Zero GC allocation during runtime property access.

## Architecture
- `KinematicsGame.Enemy.TargetProfile`:
  - `string profileName`
  - `Sprite sprite`
  - `float speedMultiplier`
  - `float waveFrequencyMultiplier`
  - `float waveAmplitudeMultiplier`
  - `int pointValue`
  - `float spawnWeight`
- Preset Catalog:
  - **Bird 1 (Pigeon / Common):** Speed 1.0x, Freq 1.0x, Amp 1.0x, 10 pts, Weight 50%
  - **Bird 2 (Hummingbird / Agile):** Speed 1.5x, Freq 2.5x, Amp 0.8x, 30 pts, Weight 25%
  - **Bird 3 (Albatross / Sweeper):** Speed 0.7x, Freq 0.5x, Amp 2.2x, 20 pts, Weight 15%
  - **Bird 4 (Golden Eagle / Rare):** Speed 2.2x, Freq 0.0x, Amp 0.0x, 100 pts, Weight 10%

## Related Code Files
- Create: `Assets/Scripts/Enemy/TargetProfile.cs`
- Create: `Assets/Editor/Tests/TargetProfileTests.cs`

## Implementation Steps
1. Create `TargetProfile.cs` in `Assets/Scripts/Enemy/` with full serialization attributes (`[System.Serializable]`).
2. Implement static helper / catalog to create the 4 archetype presets using loaded or fallback sprites.
3. Add Unit Tests in `Assets/Editor/Tests/TargetProfileTests.cs` to verify profile creation, property values, and weighted selection math.
4. Verify compilation with `dotnet build Assembly-CSharp.csproj`.

## Success Criteria
- [ ] `TargetProfile` defines all kinematic and scoring fields.
- [ ] 4 distinct archetype presets are configured and unit-tested.
- [ ] `dotnet build Assembly-CSharp.csproj` compiles with 0 errors.

## Risk Assessment
- Risk: Missing sprite reference causes NullReferenceException in SpriteRenderer.
  - Mitigation: TargetProfile validator guarantees fallback to default target sprite if profile sprite is null.
