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

---

## Thesis: Design Patterns in Mobile Game Development

This tic-tac-toe project doubles as a **demonstration codebase** for a bachelor thesis (diplomski rad, FTN Novi Sad) on design and architectural patterns in mobile game development. The thesis covers 8 patterns — each must be demonstrable with working code in this project (or the companion patterns demo project where that's clearer).

**Full brief:** `SCREENSHOT_ILLUSTRATIONS_BRIEF.md`

### The 8 Patterns

| # | Pattern | Likely already in TTT | May need adding/extending |
|---|---------|----------------------|--------------------------|
| 1 | **Singleton** | GameManager, AudioManager | Ensure DontDestroyOnLoad, console logging of access |
| 2 | **Observer** | Events for UI updates (win, turn change) | Make event wiring visible in Inspector (UnityEvents) |
| 3 | **Command** | — | Add undo/redo for moves, command history |
| 4 | **State Machine** | Game flow (menu → playing → game over) | Make states explicit classes, log transitions |
| 5 | **Object Pool** | — | Pool X/O markers or VFX instead of Instantiate/Destroy |
| 6 | **Flyweight** | — | SharedData ScriptableObject for cell/marker properties (or use existing demo scene) |
| 7 | **Decorator / Factory** | — | Factory for marker creation; decorator for marker visual variants |
| 8 | **Service Locator / DI** | — | Audio via Service Locator, NullService fallback |

### Companion Demo Project

A separate patterns demo project has isolated scenes for each pattern (see brief for paths). For some patterns (Flyweight profiler comparison, Object Pool gun demo), those scenes may produce better illustrations than tic-tac-toe.

### Screenshot Guidelines (for the student)

- FTN template: figures numbered as "Slika 1", "Slika 2", etc., captioned below, referenced by number in text
- Unity Editor light theme preferred for print readability
- Clean, cropped, high-res — no personal info visible
- Size: 1/8 to 1/2 of A4; larger goes to appendix
- Save to `Project Materials/Screenshots/` with descriptive filenames (e.g., `singleton_inspector_gamemanager.png`)
- The student writes all thesis text and captures all screenshots — Claude assists with code only

### Code Guidelines for Pattern Implementations

- Add `Debug.Log` statements at pattern-relevant points (singleton access, event firing, command execution, state transitions) so Console screenshots show the pattern working
- Keep pattern code clean and clearly separated — the reader should see the pattern, not game complexity
- Use `[Header]` and `[Tooltip]` attributes on serialized fields so Inspector screenshots are self-documenting
- Prefer explicit, textbook-style implementations over clever shortcuts — clarity for the thesis reader matters more than production elegance
