# Prefab Catalog

---

**Document Version:** 0.3.1

---

# Player.prefab

## Purpose

Represents one playable robot.

---

## Contains

- PlayerUnit
- Animator
- HealthCanvas

---

## HealthCanvas

Contains

- ShieldBar
- EnergyBar

---

## Responsibilities

- Movement
- Health
- Shield
- Animation
- Environmental damage
- Team/unit identity (`teamID`, `unitID`)
- Runtime team link
- In-world unit identifier

---

## Depends On

- TurnManager
- CameraFollow
- Map1HexGrid

---

## Known Pitfalls

ShieldBar and EnergyBar must reference the visible child transforms.

Duplicate objects with identical names exist.

Incorrect assignments prevent bar updates.

---

# Player2.prefab

Same base structure as Player.prefab.

The current scene uses these prefabs/instances to build two 4-unit teams. Individual scene units are distinguished by `teamID` and `unitID`, not by requiring eight separate prefab types.

Current scene naming:

```text
Blue_1, Blue_2, Blue_3, Blue_4
Red_1, Red_2, Red_3, Red_4
```

---

# Runtime Objects

Not prefabs:

- Pyramid
- AI Station
- ClickableSphere
- Procedural Tiles
- Procedural Vertices

# Prefab Catalog

## Existing objective-related inventory

### WorldBox

Existing gameplay object used for pickup, carrying, death-drop, and delivery.

**v0.3 role:** single contested objective.

The important change is lifecycle ownership rather than a required visual redesign.

### AI Station

Existing delivery destination.

The special delivery-center vertex and current delivery feedback remain part of the core match loop.

## Planned future prefabs

### Power-up pickup

Possible variants:

- Shield Cell.
- Energy Cell.
- Attack Boost.
- Overclock / movement boost.
- Warp Core / teleport, if implemented.

Recommendation:

Use one shared pickup framework with data/configuration differences rather than unrelated scripts for every power-up.

### Drone

Implemented hostile unit prefab / runtime enemy unit.

Current responsibilities include:

- Drone controller.
- Visual/animation hierarchy.
- Damage/health state if required.
- Graph-navigation reference.
- Attack effect/audio.
- Target/threat state.
- Target evaluation across all living 4v4 player units.
- Same-hex attack priority.
- Objective-carrier hard priority.
- Hex-center occupancy/reservation.

### Drone Hive / Spawn Node

Automatic hive-spawn rules remain future work. `EnemyDroneDirector` currently coordinates the live enemy phase and supports manually/explicitly spawned drone instances.

## Prefab design rule

New tactical objects should avoid embedding map-coordinate logic directly into visual children.

Logical location should remain compatible with the established tile/vertex graph.


## Team-related scene components

### TeamController

Scene-level team registry used by TeamBlue and TeamRed.

It is not an individual robot prefab. It groups units by `teamID` and exposes an ordered team-unit list.

### UnitIdentityLabel

Presentation component attached to player units.

Current visual codes:

```text
B 00 / B 01 / B 10 / B 11
R 00 / R 01 / R 10 / R 11
```
