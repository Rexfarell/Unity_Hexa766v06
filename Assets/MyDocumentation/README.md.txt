# HEXA_766

HEXA_766 is a turn-based serious game developed as the software artifact of a PhD dissertation in Computer Science Education.

The project focuses on teaching debugging and computational thinking through game-based learning.

---

## Technologies

- Unity
- C#
- Universal Render Pipeline (URP)

---

## Main Systems

- Procedural Hexagonal Map
- Vertex Navigation
- Turn Manager
- Mission Intro
- Strategy Camera
- Health and Shield System
- Environmental Hazards

---

## Documentation

Technical documentation is located in:

Documentation/

Every gameplay milestone should be accompanied by documentation updates.

---

## Repository Structure

Assets/

Documentation/

Packages/

ProjectSettings/

---

## Current Status

Current Version

Alpha 0.1

Major systems implemented:

- Turn System
- Camera System
- Procedural Map
- Player Prefabs
- Mission Intro
- Health System

Alpha 0.2

[Next version] Added initial melee/adjacent attack mechanics, basic combat damage, scoring system, and delivery mechanics


## v0.3 design direction — Contested Objective

Version 0.3 formalizes the next gameplay architecture around a **single contested objective**.

The core loop becomes:

1. Exactly one box is in circulation.
2. Both players race for, intercept, steal through combat, and deliver that box.
3. A carried box returns to the board if its carrier dies.
4. A delivered box is consumed and a new contested box is spawned.
5. The first player to complete three deliveries wins.
6. The match ends with a dedicated victory/defeat presentation.

The design goal is not perfect geometric symmetry. HEXA_766 will target **fair opportunity and meaningful counterplay**.

Future systems such as power-ups and hostile drone pressure are expected to create situations where one player may be closer to the box while the other has access to compensating tactical opportunities.

## Planned tactical layers

### Power-ups

Initial low-cost power-up candidates:

- Shield restoration.
- Energy restoration.
- Attack boost.
- Temporary movement boost.

Optional advanced mechanics, if development time allows:

- Restricted teleportation / Warp Core.
- Disruption effects.
- Other one-use tactical abilities.

### Hostile drone hive

A future enemy system will deploy neutral hostile drones that stalk and attack players. Drone pressure is intended to:

- Make delivery routes dangerous.
- Create contested zones.
- Increase pressure on box carriers.
- Prevent the match from becoming a simple shortest-path race.
- Add a third tactical force beyond the two competing players.

The drone hive should be treated as part of the fairness model, even before it is implemented.

## Design principle

HEXA_766 should not attempt to make every turn equally balanced.

Instead, each board state should provide meaningful choices among:

- Objective access.
- Power-up access.
- Route safety.
- Combat.
- Interception.
- Denial.
- Survival.

The target is **counterplay**, not perfect symmetry. 