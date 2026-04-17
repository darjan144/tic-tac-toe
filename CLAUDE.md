# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Local multiplayer Tic-Tac-Toe game built in Unity. Two players on a single device, must support both Portrait and Landscape orientations. Full spec lives in `Project Spec/Game Developer Assignment Two Desperados.md`.

## Unity Environment

- **Unity Version**: 6000.3.11f1 (Unity 6)
- **Render Pipeline**: URP 17.3.0
- **UI System**: Unity UI (uGUI via `com.unity.ugui 2.0.0`) — use `UnityEngine.UI.Image` (fully qualified) to avoid namespace conflicts
- **Animation/Tweening**: DOTween (in `Assets/Plugins/Demigiant/DOTween/`)
- **Input**: New Input System 1.19.0
- **Unity MCP**: Configured in `.mcp.json` — use `mcp__unity-mcp__Unity_RunCommand` to execute C# editor scripts

## Unity MCP Conventions

When writing editor scripts via `Unity_RunCommand`:
- Class MUST be named `CommandScript` with `internal` accessibility
- Use `PhysicsMaterial` not `PhysicMaterial` (renamed in Unity 6)
- Fully qualify `UnityEngine.UI.Image` to avoid collision with `System.Drawing`
- Always use `result.RegisterObjectCreation()` / `result.RegisterObjectModification()` / `result.DestroyObject()`
- Save scenes with `EditorSceneManager.SaveScene()`

## Scene Structure

Two scenes needed (in `Assets/Scenes/`):
1. **MainMenu** (`MainMenu.unity`) — Play, Stats, Settings, Exit buttons with popups
2. **Game** — 3x3 grid gameplay, HUD (timer, move counts, settings), game over popup

## Provided Assets

### Art (`Assets/Art/`)
- `XO.psd` — X and O mark sprites
- `buttonNormal.psd`, `buttonOver.psd` — button states
- `genericPopup.psd` — popup background
- `flare.png` + `Particles/particle1-4.png` — VFX particles

### Audio (`Assets/Art/Audio/`)
- `music.wav` — BGM (looping)
- `click1.wav`, `click2.wav` — button click SFX
- `pop.wav` — placement SFX
- `woosh.wav` — popup/strike animation SFX

## Required Features (Priority Order)

### 1. Functionality & Playability (highest priority)
- 3x3 grid, alternating X/O placement
- Win detection with strike animation on 3-in-a-row
- Draw detection
- Game over popup with result, duration, Retry/Exit buttons
- Theme selection popup (choose XO visual style before starting)

### 2. Code Quality
- Clean C# following Unity conventions
- Scripts in `Assets/Scripts/` organized by feature (UI, Game, Audio, Data)

### 3. Popups (from spec)
- **Theme Selection** (Play Scene): choose XO theme, Start button transitions to Game Scene
- **Statistics** (Play Scene): total games, P1 wins, P2 wins, draws, avg duration
- **Settings** (both scenes): BGM toggle, SFX toggle
- **Exit Confirmation** (Play Scene): confirm quit
- **Game Result** (Game Scene): result + duration, Retry + Exit buttons

### 4. Persistence
- Save stats between sessions using `PlayerPrefs` or JSON to `Application.persistentDataPath`
- Track: total games, P1 wins, P2 wins, draws, cumulative game duration

### 5. Audio
- BGM with toggle (persisted in settings)
- SFX: button clicks, mark placement, strike/win, popup animations
- All toggleable from Settings popup

## Architecture Guidelines

- Use a singleton `AudioManager` for BGM/SFX control
- Use a `GameManager` for game state (current turn, board state, win check)
- Use a `StatsManager` or static data class for persistence
- Popups should be reusable prefabs activated/deactivated on the Canvas
- Scene transitions via `SceneManager.LoadScene()`
- DOTween for popup animations, strike line animation, and UI polish
