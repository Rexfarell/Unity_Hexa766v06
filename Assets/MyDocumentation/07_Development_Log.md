# Development Log

---

## 2026-01-08

### Camera System

Implemented Strategy Camera V2.

Removed obsolete CameraRig.

Implemented:

- Orbit
- Zoom
- Recenter

---

### Mission Intro

Corrected orbit interpolation.

Fixed ending position.

---

### Health System

Solved health bar update issue.

Root cause:

Duplicate child transforms with identical names.

---

### Prefabs

Created:

- Player.prefab
- Player2.prefab

---

### Scene Cleanup

Removed:

- TEST_HEX_TILE
- TEST_HEX_VISIBLE
- CameraRig

Created:

Documentation folder.

---

### Documentation

Started Technical Handbook Version 0.1.

Date

2026-01-08

Title

Narrative Redesign – Archive 766

Objective

Transform the game's introduction from a sequence of UI panels into a cinematic historical reconstruction presented as a recovered archive.

Decisions
Narrative Perspective

The narrator is no longer an anonymous voice.

The introduction is presented by The Curator, an archaeologist from a distant future who reconstructs historical events from fragmented expedition records.

The Curator's true identity remains intentionally ambiguous. They may be:

Human
Artificial Intelligence
Extraterrestrial scholar

This ambiguity is considered part of the game's worldbuilding.

Archive Concept

HEXA_766 now possesses two simultaneous meanings.

Development Origin

The suffix 766 originates from the first tabletop prototype, which contained:

7 red hexagonal tiles
6 white hexagonal tiles
6 orange hexagonal tiles

Narrative Meaning

Within the game universe,

Recovered Archive 766

is the designation assigned to the reconstructed expedition records presented to the player.

Canon Established

Holographic Cenotaph

The archaeological monument discovered by humanity.

It protects the archaeological site.

AI Station

The technological system that serves as the physical host of ARCHON.

Recovered alien technology is delivered here to restore the intelligence.

ARCHON

The dormant artificial intelligence.

ARCHON constitutes the central objective of Mission 1.

Players restore ARCHON by recovering alien technology and transporting it to the AI Station.

Screenplay

Approved screenplay sequence:

Recovered Archive 766
Source Integrity
Year 2268
Humanity discovers extraterrestrial remains
Hidden beyond the Holographic Cenotaph...
ARCHON
Description of ARCHON
Expedition convergence
Mission Briefing
Archive Ready

Outcome

This redesign establishes the narrative identity of HEXA_766 and serves as the canonical foundation for future story development, menus, mission briefings, and in-game historical records.


Development Log – Mission Intro Polish (Pre-2.0)

Version: 1.9.9 (Pre-2.0 Stable)
Date: August 2026

Overview

This milestone focused on transforming the mission introduction from a functional prototype into a polished cinematic sequence. The work emphasized visual identity, audiovisual synchronization, and configurability while preserving code stability.

Visual Improvements
Redesigned the HEXA_766 logo as a holographic projection.
Implemented independent RGB channel separation (white, blue, cyan).
Added randomized horizontal offsets to simulate holographic instability.
Added randomized channel visibility to avoid repetitive animation.
Implemented a catastrophic synchronization glitch, producing a brief but dramatic signal failure before stabilization.
Removed the experimental LogoGlass background after determining it detracted from the illusion.
Adjusted the logo composition to reduce overlap with the battlefield and improve visual hierarchy.
Audio Improvements
Replaced the abrupt termination of the intro music with a smooth fade-out.
Began exposing audio timing values for Inspector-based tuning rather than hardcoded constants.
Technical Improvements
Began converting hardcoded cinematic values into configurable Inspector parameters.
Established a development workflow favoring small, isolated, reversible patches over large code rewrites.
Improved maintainability by preparing MissionIntro to function as a configurable cinematic controller.
Lessons Learned

Several debugging sessions revealed that apparent rendering problems originated from UI configuration rather than animation logic. Material assignments, CanvasGroup visibility, and object activation order were identified as critical factors for TextMeshPro holographic effects.

The introduction now serves as a stable baseline for Version 2.0 development.

## [2026-08-XX] Unit Death, Animation, Respawn, and Turn Transfer Fix

### Issue
When a PlayerUnit depleted its energy, the unit correctly entered the death sequence and triggered the death animation, but the turn could be transferred prematurely while the unit was still completing its movement coroutine.

This produced an abrupt death sequence in which the next player's turn started before the intended death/respawn flow had completed.

### Investigation
The death sequence involves three independent processes:

1. `DieRoutine()` triggers the death animation and controls the delayed turn transfer.
2. `RespawnRoutine()` handles the respawn countdown, stat restoration, health-bar refresh, home-base positioning, and animator reset.
3. `SmoothMove()` controls movement and normally calls `TurnManager.HandleMoveFinished()` when movement is completed.

Debug logs showed that when a unit died during movement, `SmoothMove()` detected `isDead` and was also calling `HandleMoveFinished()`. This caused the turn to advance immediately, independently of the delay configured in `DieRoutine()`.

The respawn timer itself was confirmed to be working correctly. `RespawnRoutine()` starts its configured countdown and does not immediately respawn the unit.

### Fix
The death branch in `SmoothMove()` was changed so that it no longer calls `TurnManager.HandleMoveFinished()`.

The death branch now exits the movement coroutine and leaves turn management to `DieRoutine()`.

Normal movement completion remains unchanged:

- `SmoothMove()` completes the movement.
- `TryPickupBox()` is executed when appropriate.
- `turnAlreadyFinished` is set.
- `TurnManager.HandleMoveFinished()` transfers the turn.

When a unit dies during movement:

- The death animation is triggered.
- `RespawnRoutine()` starts independently.
- `SmoothMove()` detects `isDead` and exits without advancing the turn.
- `DieRoutine()` remains responsible for the delayed turn transfer.

### Result
The death sequence and normal movement sequence are now separated.

Normal movement continues to transfer turns normally, while a unit that dies during movement does not prematurely advance the turn from inside `SmoothMove()`.

The existing respawn system remains responsible for restoring:

- Energy
- Shield
- Health bars
- Home-base position
- Animator state

This fix preserves the previously working respawn and health-bar behavior while preventing duplicate or premature turn advancement.

Basic Attack System Implemented

Implemented and tested the first combat system. Same-hex hostile units trigger an automatic Attack1 animation when movement finishes. 
Adjacent hostile units require explicit target selection followed by K input. Attack targeting is vertex-specific for adjacent targets 
while movement restrictions remain vertex-based. Shield-first damage was implemented at 25 damage per attack. Animation Events were configured 
through PlayerAnimationEvents because the Animator is on the Robot_Blue Variant child rather than the PlayerUnit parent. 
Tested successfully for both same-hex and adjacent-target attacks.



# Known Issues and Planned Gaps

## v0.3 design gaps

The following items are intentionally documented as **not implemented yet** at the v0.8 design milestone.

### Single-objective lifecycle

Current box mechanics work, but the new match rule requiring exactly one objective box in circulation still needs a dedicated lifecycle controller.

### Win condition

Delivery scoring works, but first-to-three match termination and winner presentation still need implementation.

### Result screen

Victory/defeat UI and final-match audio presentation are planned but not implemented.

### Spawn fairness

Curated valid box-spawn candidates and path-based reachability validation are planned.

Do not attempt to solve fairness by changing the existing map coordinate or vertex systems.

### Power-ups

Power-up mechanics are planned for a later pass.

Initial candidates:

- Shield.
- Energy.
- Attack boost.
- Movement boost.

Teleportation and disruption remain optional scope.

### Drone hive

Hostile autonomous enemies are planned for the next major gameplay phase after the core match loop.

This future system must be considered when evaluating fairness, because route danger will alter the effective value of box and power-up locations.

## Deferred audio concept

The current audio system is functional and sufficient for the gameplay-completion phase.

A reactive musical system in which gameplay SFX become rhythmically integrated with the match soundtrack is intentionally deferred until the overall game is complete.

Do not prioritize this over core gameplay.

## 2026-09-17 — v0.3.1 4v4 Team Architecture

### Team Structure

Expanded the playable roster from one robot per side to four units per team.

Added:

- `TeamController` for Blue and Red.
- `teamID` for team membership.
- `unitID` for deterministic identity within each team.
- Unit-to-team linking.
- Automatic team discovery and ordered team lists.

Current unit structure:

```text
Blue_1 → B 00
Blue_2 → B 01
Blue_3 → B 10
Blue_4 → B 11

Red_1  → R 00
Red_2  → R 01
Red_3  → R 10
Red_4  → R 11
```

### Turn Sequence

Verified the existing turn architecture with eight playable units.

Current round:

```text
Blue_1 → Blue_2 → Blue_3 → Blue_4
→ Red_1 → Red_2 → Red_3 → Red_4
→ Enemy Drone Phase
→ Blue_1
```

No TurnManager rewrite was required after the multi-unit registration/order changes.

### Spawn and Respawn

Updated `Map1HexGrid` so map generation preserves and positions every `PlayerUnit`, not only the original Player1/Player2 pair.

Each team's units now receive distinct home-base vertices based on `unitID`.

Respawn uses the same rule, preventing teammates from returning to the same vertex.

### Enemy Targeting

Generalized `EnemyDrone` targeting from the original Player1/Player2 references to all living `PlayerUnit` instances.

Preserved behavior:

- Immediate same-hex attack priority.
- Objective-carrier hard priority.
- Nearest reachable target selection.
- Random tie-break at equal distance.
- One-step movement per enemy action.
- One-drone-per-hex-center occupancy/reservation.
- Existing telegraph, beam, impact, audio, and damage behavior.

Multi-unit targeting was tested successfully against the 4v4 roster.

### UI and Identification

Added compact in-world binary unit identifiers:

```text
Blue: B 00, B 01, B 10, B 11
Red:  R 00, R 01, R 10, R 11
```

Updated the score HUD from Player1/Player2 terminology to:

```text
Blue: 0                                      Red: 0
```

The score texts are now anchored to opposite top corners, keeping the center of the screen clear for gameplay.

### Result

Version 0.3.1 establishes a stable 4v4 structural baseline for future team tactics, active-unit feedback, power-ups, and programmable/AI behavior.
