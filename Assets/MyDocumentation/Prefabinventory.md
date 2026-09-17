# Prefab Inventory

## UI

IntroCanvas
Status: Stable
Location:
Assets/Prefabs/UI

Contains:
- LogoPanel
- StoryPanel
- MissionPanel
- SkipCaption

---

## Gameplay

PlayerUnit

Status: Stable

Contains:
- Animator
- Movement
- TeamID
- UnitID
- TeamController link
- UnitIdentityLabel
- Audio

# Prefab Inventory

## Current objective-related inventory

### WorldBox

Existing gameplay object used for pickup, carrying, death-drop, and delivery.

**v0.8 role:** single contested objective.

The important change is lifecycle ownership rather than a required visual redesign.

### AI Station

Existing delivery destination.

The special delivery-center vertex and current delivery feedback remain part of the core match loop.

## Planned inventory

### PowerUp_Base

Future shared pickup base for tactical consumables.

Potential configured variants:

- Shield Cell
- Energy Cell
- Attack Boost
- Overclock
- Warp Core (optional)

### Drone_Enemy

Status: Implemented core enemy unit.

Current behavior:

- Hex-center movement.
- Multi-unit target selection.
- Objective-carrier hard priority.
- Same-hex attack.
- Occupancy/reservation checks.
- Attack telegraph, beam, impact, and audio.

### DroneHive / DroneSpawnNode

Status: Partial / future.

`EnemyDroneDirector` is implemented for enemy-phase execution. Automatic hive spawn cadence and final spawn-control rules remain future work.

## Inventory principle

Prefer reusable configurable prefabs over one-off systems when introducing power-ups and enemies.
