# HEXA_766 Technical Handbook

**Project:** HEXA_766

**Author:** Eric Ávila

**Purpose**

HEXA_766 is a serious game developed as the central software artifact of a PhD dissertation in Computer Science Education.

The project combines:

- Serious Games
- Computational Thinking
- Debugging
- Turn-Based Strategy
- Procedural Generation
- Educational Game Design

---

## Development Philosophy

The project follows the following engineering principles:

- Every gameplay system should have a single responsibility.
- Presentation systems should remain independent from gameplay systems.
- Procedurally generated objects should be generated at runtime.
- Reusable gameplay objects should become prefabs.
- Every completed milestone must be documented.

---

## Current Status

Current milestone:

- Strategy Camera V2
- Mission Intro Cinematic
- Procedural Hexagonal Map
- Turn System
- 4v4 Team Architecture
- Player Prefabs
- TeamController
- Multi-Unit Enemy Drone Targeting
- Health & Shield System

---

This handbook is a living document.

It must evolve together with the project.

# Project Overview

## Project

**Name:** HEXA_766  
**Engine:** Unity 6 / Unity 6.3 LTS  
**Genre:** Turn-based tactical strategy / sci-fi objective game  
**Primary mode:** Competitive two-team prototype (4 units per team)

## Core concept

Players control robotic units moving through a modular hex-grid environment. They compete to recover AI components represented by boxes and deliver them to the AI Station while surviving combat and environmental pressure.

The AI Station is the central objective and includes a special center vertex used for delivery.

## Current gameplay foundation

The project currently supports:

- Vertex-based movement across hex tiles.
- Turn management.
- Player-versus-player attacks.
- Shield-first, energy-second damage.
- Death and respawn.
- Box pickup and carrying.
- Box drop on carrier death.
- AI Station delivery.
- Delivery scoring.
- Delivery visual, camera, UI, and audio feedback.
- Pickup and death audio feedback.
- Intro presentation and match music.


## Version 0.3.1 implementation milestone

Version 0.3.1 expands the competitive prototype from one unit per side to a **4v4 team structure**.

Current team structure:

```text
Blue: B 00, B 01, B 10, B 11
Red:  R 00, R 01, R 10, R 11
```

Each `PlayerUnit` now has:

- `teamID` — identifies Blue or Red.
- `unitID` — identifies the unit within the team.
- A link to its `TeamController`.

The active player-unit sequence is currently:

```text
Blue_1 → Blue_2 → Blue_3 → Blue_4
→ Red_1 → Red_2 → Red_3 → Red_4
→ Enemy Drone Phase
→ repeat
```

`Map1HexGrid` now places and respawns multiple units on distinct vertices around each team's home hex while preserving the established map/vertex graph.

Enemy drones now evaluate all living player units rather than only the original Player1/Player2 references. Same-hex attacks remain immediate, objective carriers remain hard-priority targets, and normal pursuit selects the nearest reachable valid unit.

The score HUD now presents team scores as `Blue: N` and `Red: N`, while individual units use compact binary identifiers.

## Version 0.8 design milestone

Version 0.8 is the transition from a mechanics prototype into a defined competitive match structure.

### Primary objective

There will be **one and only one box in circulation at a time**.

A box may be:

- On the board.
- Carried by a Blue-team unit.
- Carried by a Red-team unit.
- Temporarily transitioning through a delivery sequence.

A second box must not exist while the current box remains active.

### Victory condition

The first team to deliver **three boxes** wins the match.

At the winning delivery:

- The delivery feedback completes.
- No replacement box is spawned.
- No new gameplay turn begins.
- The match enters a game-over state.
- A dedicated win/lose result presentation is shown.

## Tactical fairness philosophy

The game will not define fairness only by equal distance to the objective.

Example:

- Player 1 may be three graph steps from the box.
- Player 2 may be eight graph steps from the box but one step from a valuable power-up.

This can still be a fair tactical state if Player 2 has meaningful counterplay.

Future fairness should consider:

- Distance to the box.
- Route danger.
- Health and shield state.
- Nearby power-ups.
- Interception routes.
- Current score.
- Hostile drone pressure.

The design target is **fair opportunity**, not equal geometry.

## Future tactical layers

### Power-ups

Power-ups should create alternative objectives and counterplay.

Preferred early mechanics:

- Shield Cell.
- Energy Cell.
- Attack Boost.
- Temporary movement boost.

Optional later mechanics:

- Restricted teleportation / Warp Core.
- Temporary disruption effects.

Power-ups should normally be temporary, immediate, or consumable rather than permanent stat escalation.

### Hostile drone pressure

The core hostile-drone system is implemented. Active drones participate in a dedicated enemy phase, pursue living player units, and attack units that share their hex.

The enemy system currently supports:

- Environmental pressure.
- Hard-priority targeting of objective carriers.
- Same-hex attacks.
- One-step graph pursuit toward reachable targets.
- One-drone-per-hex-center occupancy protection.
- Multi-unit targeting across the full 4v4 roster.

Automatic hive spawning rules and final drone balance remain future work.

The enemy system should remain independent from PlayerUnit and should reuse the established map graph wherever possible.

## Development priority

1. Single-box lifecycle.
2. Race to three deliveries.
3. Match-ending logic.
4. Drone hive / enemy pressure.
5. Basic power-up framework.
6. Tactical spawn scenarios.
7. Optional advanced power-ups.
8. Balance and final presentation.
