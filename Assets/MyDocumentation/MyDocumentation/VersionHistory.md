### Version 1.9.9

Mission Intro Polish

- RGB hologram effect
- Catastrophic glitch
- Inspector configuration
- Audio fade-out


# Version History

## v0.2 — Mechanics foundation

The existing prototype established the major low-level gameplay systems required for a complete match:

- Hex/vertex movement.
- Turn handling.
- Combat.
- Health/shield damage.
- Death/respawn.
- Box pickup/carry/drop.
- AI Station delivery.
- Delivery scoring.
- Camera and UI feedback.
- Core gameplay audio.
- Intro and match music.

## v0.3 — Contested Objective

**Design milestone: 2026-09-06**

Version 0.3 defines the first complete competitive match loop.

### Identity of the version

> One contested objective, meaningful counterplay, first to three.

### Core changes

- Exactly one box in circulation.
- Replacement box after successful non-winning delivery.
- Death returns the active box to play.
- First player to three deliveries wins.
- Match ends cleanly after final delivery feedback.

### Tactical philosophy

Version 0.3 rejects strict symmetry as the sole measure of fairness.

Instead, players may receive different opportunities:

- One may have better objective access.
- The other may have better power-up access.
- One route may be shorter but more dangerous.
- Future drones may alter route value dynamically.

The intended result is a match built around counterplay.

### Planned expansion

After the single-box match loop is stable:

1. Hostile drone hive.
2. Basic power-ups.
3. Tactical spawn scenarios.
4. Optional teleportation/disruption mechanics.
5. Final victory presentation and balance refinement.

### Deferred

Reactive breakbeat-style musical integration between soundtrack and SFX remains a post-gameplay polish idea.


## v0.3.1 — 4v4 Team Architecture

**Implementation milestone: 2026-09-17**

Version 0.3.1 expands the contested-objective prototype into a stable 4v4 team structure.

### Core changes

- Four Blue units and four Red units.
- `TeamController` team registry.
- `teamID` + `unitID` identity.
- Deterministic 8-unit turn ordering.
- Distinct team-unit spawn and respawn vertices.
- Enemy-drone targeting across all living player units.
- Objective-carrier hard priority preserved.
- Binary in-world unit IDs:
  - `B 00`, `B 01`, `B 10`, `B 11`
  - `R 00`, `R 01`, `R 10`, `R 11`
- Team score HUD updated to `Blue: N` / `Red: N`.

### Verified sequence

```text
Blue_1 → Blue_2 → Blue_3 → Blue_4
→ Red_1 → Red_2 → Red_3 → Red_4
→ Enemy Drone Phase
→ repeat
```

### Status

The 4v4 structural baseline and multi-unit drone targeting are operational. Automatic hive-spawn rules, power-ups, and later programmable/AI strategy layers remain subsequent work.



## v0.3.2 — Core Subsystems

**Gameplay milestone: 2026-09-29**

Version 0.3.2 introduces the first playable implementation of the
HEXA_766 Subsystem layer.

### Identity of the version

> Manipulate load. Manipulate capacity. Change the structural state
> without replacing the underlying graph.

### Core Subsystems

- Mass Dampener: allied Effective Load -1.
- Ballast Injection: enemy Effective Load +1.
- Structural Reinforcement: hex Capacity +3.
- Network Stress: hex Capacity -3.

### Effective Load

Robot Effective Load is now directly manipulated through gameplay.

Normal value:

3

Allowed range:

1–5

### Interaction Model

Unit Subsystems use forgiving screen-space targeting based on the
visible robot geometry.

Hex Subsystems continue to operate directly on HexCapacity targets.

Only successful Subsystem activations consume the unit's Subsystem
opportunity for the turn.

### Interface

A dedicated HEXA_766 tactical feedback card now reports Subsystem
results and invalid-state conditions.

### Next Development Step

Implement the planned team-regroup mechanic in which all four living
members of a team occupying one logical hex restore their shields.
