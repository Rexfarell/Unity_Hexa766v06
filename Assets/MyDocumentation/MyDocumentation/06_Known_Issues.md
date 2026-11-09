# Known Issues

---

**Document Version:** 0.3.1

---

# Current Issues

None considered game-breaking.

---

# Historical Issues

## Health Bars

Problem

Health bars stopped updating.

Cause

Wrong ShieldBar / EnergyBar transform assigned.

Resolution

Assign the visible child transform.

---

## Mission Intro

Problem

Camera finished underneath the map.

Cause

Incorrect serialized reference after camera refactor.

Resolution

Reassign procedural object references.

---

## CameraRig

Problem

Obsolete camera hierarchy remained after CameraFollow rewrite.

Resolution

CameraRig removed.

---

## Procedural Objects

Pyramid and AI Station are generated procedurally.

Serialized references must be assigned after generation.

---

## Scene Helpers

Camera markers are scene objects.

They should never become prefabs.


# Known Issues and Planned Gaps

## v0.3.1 remaining gaps

No current issue is considered game-breaking.

The following systems remain incomplete or intentionally deferred after the v0.3.1 4v4 milestone.

### Single-objective lifecycle

Implemented in the current gameplay loop: the delivered objective is redeployed when the match continues, while the winning delivery does not redeploy the box.

### Win condition

Implemented: score-to-win is three deliveries, the winning delivery stops normal continuation, and victory UI is triggered.

### Result presentation

A functional victory panel exists. Final visual/audio polish for the match-ending presentation remains open.

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

The core enemy-drone phase is implemented and tested with the 4v4 roster.

Current working behavior includes:

- Pursuit of all living player units.
- Objective-carrier hard priority.
- Same-hex attacks.
- One-drone-per-hex-center occupancy/reservation.

Automatic hive spawning, final population cadence, and final balance remain incomplete.


## Deferred audio concept

The current audio system is functional and sufficient for the gameplay-completion phase.

A reactive musical system in which gameplay SFX become rhythmically integrated with the match soundtrack is intentionally deferred until the overall game is complete.

Do not prioritize this over core gameplay.
