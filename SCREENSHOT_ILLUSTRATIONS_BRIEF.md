# Screenshot & Illustration Brief for Thesis Patterns

## Context

This document is a briefing for a Claude Code session that will work on capturing
screenshot illustrations from a **tic-tac-toe Unity game** to include in a bachelor
thesis (diplomski rad) at FTN Novi Sad.

**Thesis topic:** Design and architectural patterns in mobile game development.
The thesis covers 8 design patterns, each demonstrated with Unity code examples.
The thesis is engine-agnostic in framing — Unity is used as a means of demonstration,
not the subject.

**The tic-tac-toe game** is a separate Unity project that the student built. Some of
the 8 thesis patterns are likely already implemented in this game. The goal is to:
1. Identify which patterns are already present in the tic-tac-toe codebase
2. Extend the project where needed to demonstrate missing patterns
3. Capture screenshots from the Unity Editor (Game view, Inspector, Console, Profiler)
   that illustrate each pattern in action within a real, working game

**Why tic-tac-toe?** It's a complete, simple game that makes pattern usage easy to
see and understand — ideal for thesis illustrations where the reader should focus on
the pattern, not the game complexity.

---

## Thesis Figure Requirements (FTN Template Rules)

Screenshots going into the thesis must follow these formatting rules:
- Each figure is numbered sequentially: "Слика 1", "Слика 2", etc.
- Caption centered directly below the figure
- Figures must be referenced by number in the text (never "the image above")
- Size: between ~1/8 and ~1/2 of an A4 page; larger goes to an appendix
- Screenshots should be clean, cropped to the relevant area, high-resolution
- Prefer light theme in Unity Editor for better print readability
- No personal/identifying information visible in screenshots

---

## The 8 Patterns — What to Illustrate

### 1. Singleton
**What the thesis discusses:** 5 variants (classic C#, Unity MonoBehaviour, thread-safe
Lazy<T>, generic base class, scene-scoped). Anti-pattern discussion, lifecycle.

**Screenshots needed:**
- Inspector showing a Singleton MonoBehaviour component (e.g., GameManager or
  AudioManager) with DontDestroyOnLoad behavior
- Console output showing singleton access from multiple scripts
- Optionally: Hierarchy view showing only one instance persists across scenes

**How it might appear in tic-tac-toe:** A GameManager singleton controlling game
state, turn management, or score tracking. An AudioManager singleton for sound effects.

---

### 2. Observer
**What the thesis discusses:** C# events, delegates, UnityEvents, static events.
Event-driven architecture for UI updates and decoupling.

**Screenshots needed:**
- Inspector showing event wiring (UnityEvent fields or references)
- Console output showing event firing and multiple subscribers reacting
- Game view showing a UI element that updates in response to an event (e.g.,
  score display updating when a player wins)

**How it might appear in tic-tac-toe:** Events fired when a cell is clicked, when a
player wins, when the game resets. UI elements (score, turn indicator, win message)
subscribing to game state changes.

---

### 3. Command
**What the thesis discusses:** Encapsulated actions, undo/redo capability, input
rebinding. Command queue / event queue.

**Screenshots needed:**
- Game view showing undo/redo in action (e.g., undoing a move on the board)
- Inspector showing command-related components
- Console output showing command execution sequence

**How it might appear in tic-tac-toe:** Each cell placement as a Command object.
Undo button that reverts the last move. Command history visible in console.

---

### 4. State Machine
**What the thesis discusses:** Menu/screen flow, game states. State transition
diagrams. Clean state management vs. if-else chains.

**Screenshots needed:**
- Game view showing different game states (e.g., Main Menu → Playing → Game Over)
- Inspector showing state machine component with current state visible
- Console output showing state transitions

**How it might appear in tic-tac-toe:** Game states: MainMenu, PlayerXTurn,
PlayerOTurn, GameOver/Draw. UI changes based on current state. Possibly a
settings or pause menu state.

---

### 5. Object Pool
**What the thesis discusses:** Pooling vs. instantiation, GC pressure, memory
allocation. Simple pool, optimized pool, Unity native ObjectPool<T>. Profiler
comparison.

**Screenshots needed:**
- Profiler screenshot: pooled vs. non-pooled GC allocation comparison
- Hierarchy view showing pooled objects (active/inactive)
- Inspector showing pool configuration (pool size, prefab reference)
- Game view showing pooled objects in action

**How it might appear in tic-tac-toe:** X and O marker prefabs being pooled instead
of Instantiate/Destroy each round. Visual effects (win line, particle effects) pooled.
This pattern may need to be added — a simple tic-tac-toe might not pool anything.

---

### 6. Flyweight
**What the thesis discusses:** Shared intrinsic data vs. per-instance extrinsic data.
Memory savings. Profiler comparison (heavy 1M objects vs. flyweight 1M objects).

**Screenshots needed:**
- Profiler memory comparison: with and without flyweight (already exists in the
  patterns demo project — may reuse or recreate in tic-tac-toe context)
- Inspector showing shared ScriptableObject asset referenced by multiple objects
- Hierarchy showing multiple objects sharing data

**How it might appear in tic-tac-toe:** Cell data (sprite, color, player symbol)
stored in a ScriptableObject shared across all cells of the same type. This pattern
may be demonstrated better with the existing Flyweight demo scene in the patterns
project rather than forced into tic-tac-toe.

**Note:** The patterns demo project already has a Flyweight scene with Heavy vs.
Flyweight profiler comparison (1M objects, ~160 bytes shared Data class). Profiler
screenshots from that scene may be sufficient — check if tic-tac-toe adds value here
or if the existing demo is clearer.

---

### 7. Decorator / Factory (Combined Chapter)
**What the thesis discusses:** Decorator wrapping behavior dynamically. Factory
creating objects without specifying exact class. These share a class hierarchy in
the demo (car order system, car factories, sound system factory).

**Screenshots needed:**
- Console output showing decorated object with layered properties (e.g., base +
  extras)
- Inspector showing factory configuration
- Game view or console showing factory producing different products based on input

**How it might appear in tic-tac-toe:**
- **Decorator:** Player markers with optional visual decorations (glow, animation,
  color variation) applied at runtime. Or game rules decorated with optional
  modifiers (timer, scoring multiplier).
- **Factory:** A factory that creates the correct marker type (X or O) based on
  whose turn it is, or creates different AI difficulty strategies.

---

### 8. Service Locator / Dependency Injection (Combined Chapter)
**What the thesis discusses:** Service Locator as a pattern and anti-pattern
comparison with Singleton. Manual constructor-based DI. Null service pattern.
Comparison table: Singleton vs. SL vs. DI.

**Screenshots needed:**
- Console output showing service resolution (locating an audio service, etc.)
- Inspector showing DI wiring (injected dependencies visible as serialized fields)
- Console output demonstrating NullService fallback (no crash when service missing)

**How it might appear in tic-tac-toe:** Audio service located via SL for sound
effects. AI player logic injected via DI (easy to swap AI strategies). Null audio
service when sound is disabled.

---

## Existing Demo Project Assets

The thesis already has a patterns demo project at this repo with isolated demo scenes:

| Pattern | Scene(s) | Status |
|---------|----------|--------|
| Singleton | `Assets/Patterns Scenes/Singleton/singleton.unity` | Has 5 variants |
| Observer | `Assets/Patterns Scenes/Observer/Static events/`, `Different events/` | Working |
| Command | `Assets/Patterns Scenes/Command/Rebind keys/`, `Command Queue/` | Working |
| State Machine | `Assets/Patterns Scenes/State/Menu/state-menu.unity` | Working |
| Object Pool | `Assets/Patterns Scenes/Object Pool/Gun/object pool gun.unity` | Working |
| Flyweight | `Assets/Patterns Scenes/Flyweight/flyweight.unity` | Has Heavy vs Flyweight toggle |
| Decorator/Factory | `Assets/Patterns Scenes/Decorator/`, `Factory/` | Working |
| Service Locator/DI | `Assets/Patterns Scenes/Service Locator/` (3 scenes + Manual DI) | Working |

**Decision for each pattern:** Determine whether the tic-tac-toe game gives a better,
more cohesive illustration than the existing isolated demo scene. For some patterns
(Flyweight profiler comparison, Object Pool gun demo), the existing scenes may be
more effective. For others (State Machine, Observer, Command), tic-tac-toe provides
a more realistic, interconnected example.

---

## Task Summary for Next Session

1. **Open the tic-tac-toe Unity project** and audit its codebase for existing pattern
   usage
2. **Map each of the 8 patterns** to what exists vs. what needs to be added/extended
3. **Implement missing patterns** where it makes sense in the tic-tac-toe context
4. **Capture screenshots** from Unity Editor (Game view, Inspector, Console, Profiler)
   for each pattern
5. **Screenshots should be thesis-quality:** clean, cropped, high-res, light theme,
   no clutter
6. For patterns where the existing demo project illustrates better (e.g., Flyweight
   profiler), capture those screenshots from the demo project instead
7. Save all screenshots to `Project Materials/Screenshots/` with descriptive filenames
   like `singleton_inspector_gamemanager.png`, `observer_console_event_firing.png`, etc.

**Important:** The student writes all thesis text themselves. This session is purely
about Unity project work (code + screenshots) — do not generate thesis prose.
