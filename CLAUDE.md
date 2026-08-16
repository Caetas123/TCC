# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A Unity 2D fighting game (Portuguese-language codebase) built as the practical component of a TCC (undergraduate thesis) about menu-interface navigation and accessibility (gamepad/keyboard focus, remappable controls, localization). The fighting mechanics exist to give the menu/UI-navigation work something real to sit in front of. Most identifiers, comments, and in-editor strings are in Portuguese — match that language and tone when editing existing scripts.

- Engine: Unity **6000.0.59f2** (Unity 6), Universal Render Pipeline.
- There is no README, no test suite, and no CI config in this repo — verification is manual, in-editor.

## Working in this repo

This is a Unity project, not a typical CLI-buildable codebase:

- **No command-line build/lint/test commands exist.** Unity projects are built and run from the Unity Editor (Windows → open the project, press Play; File → Build Settings to build a player). There is a `com.unity.test-framework` package dependency but no test assemblies/`.asmdef` under `Assets/` currently define any tests to run.
- `Library/`, `Temp/`, `Logs/`, and `UserSettings/` are Unity-generated caches/local state — never hand-edit or rely on their contents; they're not meaningful to the actual project.
- Scene files (`Assets/Scenes/*.unity`) and prefabs wire up which script fields point at which GameObjects (Inspector references). Reading a script in isolation often isn't enough to see the full behavior — the same class is frequently reused across scenes with different Inspector wiring (e.g. `LutadorController2D` is attached to both `player1` and `player2`, distinguished only by the `numeroJogador` field).
- A `com.coplaydev.unity-mcp` package is referenced in `Packages/manifest.json`, and an empty `.mcp.json` exists at the repo root — if a Unity MCP server is configured, prefer it for anything requiring live inspection of scene/GameObject state over guessing from source alone.

## Architecture

### Scene flow

`TelaInicial` (main menu) → `TelaModoJogador` (mode select) → `TelaSelecaoArena` (arena select) → `SelecaoPlayer` (character/controls select) → fight scene (`GameManagerLuta` + `cena1`/`cena2`). Each scene has a same-named controller script (`TelaInicial.cs`, `TelaModoJogador.cs`, `TelaSelecaoArena.cs`, `TelaSelecaoPlayer.cs`) driving that screen's menu logic and UI navigation.

Screens hand off state to each other via `PlayerPrefs` (selected characters, control bindings, round count, time limit, selected language, video/audio settings) rather than passed objects or a persistent game-state singleton — expect scripts to read/write `PlayerPrefs` directly rather than a shared state container.

### UI navigation/focus layer

This is the core subject of the thesis, split across several small single-purpose utilities rather than one monolithic input system:

- `UIFocusUtility` — static helpers to clear/set the `EventSystem`'s selected GameObject, including a "select next frame" coroutine (needed because selecting immediately on scene load / panel-open is frequently one frame too early to take effect).
- `UIInputUtility`, `SincronizarHoverComFoco`, `EventoHoverUI`, `Manterfocoeventsystem`, `DesligarNavegacaoNativaTelaSelecaoPlayer` — keep mouse hover state and keyboard/gamepad focus state in sync, and in some screens deliberately disable Unity's built-in automatic Selectable navigation in favor of custom logic (explicit navigation is needed where automatic neighbor-finding picks the wrong element).
- `ButtonVisualPersonalizado` — custom visual state handling for buttons beyond Unity's default `Selectable` transitions.
- `ControleManager` — key/button rebinding UI; persists bindings per-player to `PlayerPrefs` under keys like `P1_Esquerda`, `P2_Ataque`, etc., with hardcoded defaults (P1 = WASD+FGH, P2 = arrows+KL+`;`).
- `PauseManager` — Esc-driven pause overlay; during the fight scene it consults `GameManagerLuta.PodePausar()` before opening, so pause can't stack on top of the round-win/victory screens.

When touching navigation/focus code, preserve the existing pattern of small dedicated scripts with a narrow, commented responsibility rather than merging logic into a bigger controller.

### Localization

`LanguageManager` is a `DontDestroyOnLoad` singleton (`LanguageManager.Instance`) holding two in-memory `Dictionary<string,string>` tables (`pt`/`en`), persisting the chosen language to `PlayerPrefs`, and broadcasting a static `OnLanguageChanged` event. UI scripts that show composed/dynamic text (scores, round labels, control hints, victory text) subscribe to `OnLanguageChanged` in `Start`/unsubscribe in `OnDestroy` and rebuild their strings from `LanguageManager.Instance.GetText(key)` — there is no separate localization asset system; all strings live in these two dictionaries in code. `LocalizedText` and `Localizedimage` components apply this per-UI-element declaratively; `LanguageButton` toggles/sets the language.

### Fighting game layer

- `DadosPersonagem` (ScriptableObject, `Jogo/Dados do Personagem` asset menu) is the data-driven definition of a character: stats, per-animation-state sprite arrays + fps, attack/special/ultimate ranges/costs/damage, and per-state "can move while X" flags with movement multipliers. New characters are authored as `DadosPersonagem` assets, not new C# classes.
- `LutadorController2D` is the fighter controller, shared by both players and by AI. It owns a custom frame-based animation state machine (`EstadoAnim` enum, ordered by priority — higher-priority states preempt lower ones), reads either human input (configurable keys/gamepad axis, loaded per-player from `PlayerPrefs`) or AI-driven input fields (`IA_DefinirMovimento`, `IA_SolicitarAtaque`, etc., gated by `controladoPorIA`), and applies all combat rules (energy costs, defense damage reduction, attack hit/follow-through frame windows) from the fighter's `DadosPersonagem`.
- `AIControllerLuta` drives `controladoPorIA` fighters through a layered behavioral state machine (documented in-file): a low-frequency "think" layer picks a behavioral state (`Aproximar`, `Pressionar`, `Recuar`, `Defender`, `Flanquear`, `Espacamento`, `Finalizar`, `Recuperar`), a per-frame "move" layer translates that into movement using live distance, and a separately-clocked "buttons" layer fires actions — this separation (not just difficulty scaling numbers) is deliberate and documented; preserve it when touching AI behavior. Difficulty changes reaction time/imprecision/tactics, never attack frequency or character stats (see the class doc comment for the full rationale).
- `GameManagerLuta` owns round/match flow (best-of-N from `PlayerPrefs["RoundsSelecionados"]`, timer from `PlayerPrefs["TempoSelecionado"]`, win/draw detection, round-win and match-end panels) for the active fight scene, and exposes `PodePausar()` for `PauseManager` to query.
- `AudioManager` centralizes volume settings (master/music/effects sliders → `PlayerPrefs`) and exposes an `OnVolumeChanged` event; per-fighter looped sounds (footsteps) subscribe to it since one-shot sounds already read the current volume at play time.

### Editor tooling

`Assets/Editor/` has two Unity Editor scripts (`CorrigirHoverFocoEmMassa.cs`, `CriadorHUDLuta.cs`) — editor-only utilities for bulk-fixing hover/focus wiring across scenes and for scaffolding the fight HUD, not part of the runtime game.
