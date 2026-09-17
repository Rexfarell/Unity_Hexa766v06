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
- 4v4 Team Architecture
- Enemy Drone Phase
- Delivery Scoring and Victory Flow

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

**v0.3.1**

Major systems implemented:

- 4v4 team structure.
- Eight-unit player turn sequence.
- Procedural map and vertex navigation.
- Combat, shield/energy damage, death, and respawn.
- Objective pickup, carrying, death-drop, delivery, scoring, and victory flow.
- Enemy drone phase with multi-unit targeting.
- Strategy camera and mission intro.
- Binary in-world unit identifiers.
- Blue/Red team scoreboard.



## v0.3.1 — 4v4 team milestone

The current playable roster is:

```text
Blue: B 00, B 01, B 10, B 11
Red:  R 00, R 01, R 10, R 11
```

The current round sequence is:

```text
Blue_1 → Blue_2 → Blue_3 → Blue_4
→ Red_1 → Red_2 → Red_3 → Red_4
→ Enemy Drone Phase
```

The v0.3.1 milestone preserves the established movement/pathfinding architecture while generalizing team membership, spawning, respawning, turn order, enemy targeting, and score presentation for 4v4 play.

## v0.3 design direction — Contested Objective

Version 0.3 formalizes the next gameplay architecture around a **single contested objective**.

The core loop becomes:

1. Exactly one box is in circulation.
2. Blue and Red race for, intercept, steal through combat, and deliver that box.
3. A carried box returns to the board if its carrier dies.
4. A delivered box is consumed and a new contested box is spawned.
5. The first team to complete three deliveries wins.
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

### Hostile drone pressure

The core enemy-drone phase is implemented. Neutral hostile drones act after the eight player units, evaluate all living units, hard-prioritize reachable objective carriers, and otherwise pursue the nearest reachable target. Drone pressure is intended to:

- Make delivery routes dangerous.
- Create contested zones.
- Increase pressure on box carriers.
- Prevent the match from becoming a simple shortest-path race.
- Add a third tactical force beyond the two competing players.

Automatic hive spawning and final drone population/balance rules remain future work.

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
