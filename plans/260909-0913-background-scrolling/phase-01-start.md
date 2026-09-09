---
phase: 1
title: "Asset Preparation & Meta Configuration"
status: pending
priority: P1
effort: "30m"
dependencies: []
---

# Phase 1: Asset Preparation & Meta Configuration

## Overview
Generate and configure the Unity `.meta` file for `Assets/Sprites/Backgrounds/background10.jpg` so that Unity recognizes the 1920x768 panoramic JPEG asset as a 2D Sprite with optimal settings for performance and clean 2D rendering.

## Requirements
- Functional:
  - Create `Assets/Sprites/Backgrounds/background10.jpg.meta` with proper TextureImporter settings.
  - Set `textureType: 8` (`Sprite (2D and UI)`).
  - Set `spriteMode: 1` (`Single`).
  - Set `spriteMeshType: 1` (`FullRect`) to minimize mesh generation overhead for a full-screen rectangular background.
  - Set `wrapMode: 1` (`Clamp`) or `0` (`Repeat`) with proper mipmap and compression flags.
- Non-functional:
  - Clean YAML formatting matching Unity 2022+ / 6000+ TextureImporter schema.
  - Deterministic GUID generation for asset traceability.

## Architecture
Unity requires every asset in the `Assets/` directory to have an associated `.meta` file containing its unique GUID and importer settings. When Unity loads or tests run in batchmode/editor, this file instructs Unity how to generate the `Sprite` object from the raw JPEG data.

## Related Code Files
- Create: `Assets/Sprites/Backgrounds/background10.jpg.meta`
- Modify: None
- Delete: None

## Implementation Steps
1. Inspect `Assets/Sprites/Backgrounds/background1.png.meta` and existing sprite meta files to determine exact Unity version serializer structure.
2. Generate `Assets/Sprites/Backgrounds/background10.jpg.meta` with:
   - Unique GUID (32 hex characters).
   - TextureImporter serializedVersion: 13.
   - Texture type: 8 (Sprite 2D and UI).
   - Sprite mode: 1 (Single).
   - spritePixelsToUnits: 100.
   - spriteMeshType: 1 (FullRect).
   - maxTextureSize: 2048 (covers 1920 width without downsampling).
   - textureCompression: 1 (Normal quality) or 0 (Uncompressed).
3. Verify file syntax and validate that the sprite asset is accessible by Unity AssetDatabase in Editor utilities.

## Success Criteria
- [x] `Assets/Sprites/Backgrounds/background10.jpg.meta` exists on disk.
- [x] GUID is properly declared and unique across all `.meta` files in the project.
- [x] TextureImporter specifies Sprite (2D and UI) with 100 pixels per unit.
- [x] No YAML syntax errors.

## Risk Assessment
- Risk: Unity re-imports the texture upon opening and overrides the meta file with defaults.
  - Observable signal: Git diff on `.meta` file when opening Unity.
  - Mitigation: Match the exact serializer schema used by `background1.png.meta` and `background2.png.meta`.
