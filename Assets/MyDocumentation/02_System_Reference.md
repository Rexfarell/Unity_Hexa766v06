# HEXA_766 System Reference

**Version:** 0.3.1  
**Author:** Eric Ávila  
**Project:** HEXA_766

---

# Purpose

This document describes every major gameplay system implemented in **HEXA_766**.

Its purpose is to explain:

- what each system is responsible for;
- how it interacts with other systems;
- where future modifications should be made.

This document complements the source code and should always be updated whenever a core system changes significantly.

---

# Core Systems

---

# TurnManager

## Purpose

The **TurnManager** is the central gameplay controller.

It manages:

- match flow;
- turn order;
- movement permissions;
- player selection;
- environmental effects;
- communication between gameplay systems.

---

## Responsibilities

- Start the match after `MissionIntro`
- Sequence four Blue units, four Red units, then the enemy-drone phase
- Maintain the current active `PlayerUnit`
- Reset movement points
- Highlight valid destinations
- Validate movement
- Apply environmental hazards
- Notify `CameraFollow` when the active player changes

---

## Dependencies

- `PlayerUnit`
- `Map1HexGrid`
- `ClickableSphere`
- `CameraFollow`
- `EnemyDroneDirector`

---

## Important Methods

```csharp
BeginMatch()

StartNextTurn()

HandleMoveFinished()

HighlightValidMoves()

OnSphereClicked()
```

---

# PlayerUnit

## Purpose

Represents one playable robot.

Each player robot is an instance of this class.

---

## Responsibilities

- Team identity (`teamID`)
- Unit identity (`unitID`)
- TeamController link
- Health
- Shield
- Movement
- Animation
- Environmental damage
- Carrying boxes
- Health bar updates

---

## Components

- Animator
- HealthCanvas
- ShieldBar
- EnergyBar

---

## Important Methods

```csharp
SmoothMove()

TakeEnvironmentalDamage()

UpdateHealthBars()

SetMovementPoints()

ConsumeMovementPoint()
```

---


# TeamController

## Purpose

Represents team-level membership without absorbing individual robot behavior.

## Current teams

- Team 1 = Blue.
- Team 2 = Red.

Each team currently contains four `PlayerUnit` instances.

## Responsibilities

- Discover all active `PlayerUnit` objects that match its `teamID`.
- Maintain an ordered unit list.
- Link each `PlayerUnit` back to its team controller.
- Provide a future home for team-level strategy, cards, possession context, or AI playbooks.

`PlayerUnit` remains responsible for individual movement, animation, damage, carrying, and actions.

---

# UnitIdentityLabel

## Purpose

Provides lightweight in-world identification for the eight player units.

Current display codes:

```text
Blue: B 00, B 01, B 10, B 11
Red:  R 00, R 01, R 10, R 11
```

The label is presentation-only and does not determine gameplay identity.

---

# EnemyDroneDirector

## Purpose

Executes the neutral enemy phase after all eight player units complete their actions.

## Current responsibilities

- Iterate through active `EnemyDrone` instances.
- Execute each drone action and wait for completion.
- Maintain enemy-phase sequencing.

---

# EnemyDrone

## Purpose

Neutral hostile unit that moves between hex centers and creates route pressure.

## Current targeting

1. Evaluate all living `PlayerUnit` instances.
2. If one or more valid targets share the drone's hex, attack immediately.
3. If a reachable objective carrier exists, carriers receive hard priority.
4. Otherwise select the nearest reachable unit.
5. Resolve equal-distance ties randomly.
6. Move only one tile toward the selected target per enemy action.

## Preserved constraints

- Drones use hex centers, not player vertices.
- AI Station center remains forbidden.
- Only one drone may occupy or reserve a destination hex center.
- Attack requires the player and drone to share a hex.
- Existing telegraph, beam, impact, audio, and damage sequence remain independent from target selection.

---

# CameraFollow

## Purpose

Controls the gameplay camera after the cinematic introduction.

---

## Features

- Smooth follow
- Mouse-wheel zoom
- Right Mouse orbit
- Automatic target switching
- Recenter (`R`)

---

## Input

| Input | Action |
|--------|--------|
| Mouse Wheel | Zoom |
| Right Mouse | Orbit |
| R | Recenter |

---

# MissionIntro

## Purpose

Runs the introductory cinematic before gameplay begins.

---

## Camera Sequence

1. CAM_Overview
2. CAM_Pyramid
3. CAM_AIStation
4. CAM_Player1
5. CAM_Player2
6. MAP_CENTER

---

## Dependencies

- MissionDirector
- TurnManager
- CameraFollow

---

# Map1HexGrid

## Purpose

Procedurally generates the playable board.

---

## Responsibilities

- Board generation
- Vertex graph generation
- Coordinate conversion
- Reachable vertices
- Pathfinding support
- Hazard spawning
- AI Station spawning
- Pyramid spawning
- Multi-unit team spawn placement
- Team/unit-aware respawn placement

---

# ClickableSphere

## Purpose

Represents one legal movement destination.

---

## Responsibilities

- Receive mouse click
- Notify TurnManager
- Destroy itself when movement finishes

---

# HealthCanvas

## Purpose

Displays the current health status of a `PlayerUnit`.

---

## Contains

- Background
- ShieldBar
- EnergyBar

---

## Known Pitfalls

⚠ **ShieldBar** and **EnergyBar** must reference the visible child transforms.

Some objects share identical names, but only one is the actual visible bar.

Assigning the wrong transform causes the health bars to stop updating even though the gameplay values change correctly.

---

# Architecture Diagram

```text
MissionDirector
        │
        ▼
MissionIntro
        │
        ▼
TurnManager
        │
        ├──────────────┐
        ▼              ▼
PlayerUnit        CameraFollow
        │
        ▼
HealthCanvas

TurnManager
        │
        ▼
Map1HexGrid
        │
        ▼
ClickableSphere
```

---

# Documentation Rule

Whenever a system receives:

- a new responsibility;
- a new dependency;
- a major refactor;

this document must be updated during the same work session.

# System Reference

## Existing stable systems

### Map1HexGrid

Provides the established hex/vertex graph and pathfinding model.

Important constraint:

> The existing coordinate system, tile naming, vertex mapping, and AI Station special center vertex are intentional and must not be normalized or redesigned during v0.8 work.

### PlayerUnit

Currently owns player-level behavior including:

- Movement.
- Animation state.
- Combat animation hooks.
- Shield and energy.
- Death and respawn.
- Box pickup.
- Carried box state.
- Box drop on death.
- AI Station delivery sequence.
- Pickup/death audio hooks.

The v0.8 systems should call into or receive events from PlayerUnit instead of relocating all gameplay state into it.

### TurnManager

Currently owns turn sequencing, movement highlighting, occupancy checks, and attack resolution.

### ScoreUI

Currently provides team delivery scoring and score presentation. The live HUD displays `Blue: N` and `Red: N` while preserving `teamID`-based scoring.

v0.8 should reuse the existing score flow, adding a match-level victory check after a successful score increment.

### AIStationFeedback

Provides delivery visual feedback and synchronized delivery audio.

### CameraFollow

Provides normal player following and delivery focus behavior.

### MissionIntro / MatchMusic

The intro owns the transition from intro presentation into the match. Match music is a separate looping AudioSource.

## Planned systems

### BoxManager

**Status:** planned for v0.8.

Primary invariant:

> At most one objective box may exist in circulation.

Responsibilities:

- Spawn the first box.
- Hold the current objective reference.
- Spawn a replacement after delivery.
- Do not spawn after the winning delivery.
- Select only valid candidate locations.

### MatchManager

**Status:** planned for v0.8.

Suggested responsibilities:

```text
scoreToWin = 3
matchEnded
winnerTeam
CheckVictory()
EndMatch()
RestartMatch()
```

The first implementation should be intentionally small.

### Spawn scenario system

**Status:** later v0.8 / post-core loop.

Initial recommendation:

Use curated legal spawn candidates rather than unrestricted procedural placement.

Candidates may later be grouped into tactical scenarios containing:

- Box position.
- Power-up position.
- Relative advantage.
- Route danger.
- Drone pressure.

The system should validate reachability using existing pathfinding before accepting a candidate.

### Power-up system

**Status:** future.

Initial low-risk effects:

- Restore shield.
- Restore energy.
- Boost next attack.
- Temporary movement bonus.

Advanced optional effect:

- Restricted teleportation to highlighted legal graph destinations.

### EnemyDirector / DroneHive

**Status:** core drone director and enemy phase implemented; automatic hive spawning and final balance remain future work.

Responsibilities:

- Execute active enemies during the enemy phase.
- Maintain a maximum drone population.
- Provide neutral environmental pressure.
- Increase tactical pressure around carriers and routes.
- Avoid hard-locking the AI Station.
- Generalize targeting across all living units in the 4v4 roster.

The carrier may have increased threat/aggro, but drones should not be a hidden rubber-band system that automatically punishes the leader.

## Fairness model

Do not evaluate fairness only as:

```text
distance(Player1, Box) ≈ distance(Player2, Box)
```

Instead, future tactical evaluation should consider effective opportunity:

```text
Objective access
+ nearby power-up value
+ route safety / danger
+ current health and shield
+ interception potential
+ current score context
+ drone pressure
```

This is a design model, not yet a required numeric formula.


v0.3.2

# SubsystemController

## Purpose

`SubsystemController` manages the playable HEXA_766 Subsystem layer.

Subsystems are tactical actions that manipulate unit Effective Load
or hex Capacity without replacing the existing movement, combat,
objective, or turn systems.

## Current Subsystems

### 1 — Mass Dampener

Target: allied PlayerUnit

Effect:

Effective Load -1

Minimum Effective Load: 1

A unit already at Effective Load 1 cannot receive an additional
Mass Dampener effect.

### 2 — Ballast Injection

Target: enemy PlayerUnit

Effect:

Effective Load +1

Maximum Effective Load: 5

A unit already at Effective Load 5 cannot receive an additional
Ballast Injection effect.

### 3 — Structural Reinforcement

Target: HexCapacity

Effect:

Capacity +3

### 4 — Network Stress

Target: HexCapacity

Effect:

Capacity -3

## Turn Rule

Each PlayerUnit may successfully activate one Subsystem during its turn.

An unsuccessful attempt does not consume the Subsystem opportunity.

Subsystems cannot be activated while:
- the active unit is dead;
- the active unit is moving;
- the Enemy Phase is running.

## Unit Targeting

Unit-based Subsystems use screen-space targeting derived from the
visible Renderers belonging to each PlayerUnit.

The target area receives additional screen-space padding to make
selection practical without depending on a precise collider hit.

When several units are visually close, invalid units that have already
reached the applicable Effective Load limit can be excluded from the
candidate set.

## Dependencies

- TurnManager
- PlayerUnit
- HexCapacity
- EnemyDroneDirector
- SubsystemFeedbackUI

---

# SubsystemFeedbackUI

## Purpose

Displays temporary tactical feedback after Subsystem attempts.

The UI uses a HEXA_766 system-readout presentation.

Feedback includes:
- Subsystem identity;
- selected target when applicable;
- Effective Load or Capacity change;
- resulting value;
- hex load/state where applicable;
- NO EFFECT conditions;
- NO VALID TARGET conditions.

The display duration is intentionally longer than the original
prototype notification to allow the player to read the tactical result.

---

# PlayerUnit — Effective Load

Each PlayerUnit has:

- Base Mass;
- Load Modifier;
- Effective Load.

Effective Load is constrained to the range:

1–5

The normal base value is:

3

`ApplyLoadModifier()` provides the gameplay entry point used by
unit-based Subsystems.

Effective Load represents the amount of load that the robot contributes
to its current logical hex. It is not the robot's health and does not
directly represent structural overload.
