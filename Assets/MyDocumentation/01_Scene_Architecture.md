# Scene Architecture

## Environment

- Directional Light
- Global Volume

## Gameplay

- TurnManager
- MissionDirector
- Map1

## Players

- TeamBlue (`TeamController`)
- TeamRed (`TeamController`)
- Blue_1
- Blue_2
- Blue_3
- Blue_4
- Red_1
- Red_2
- Red_3
- Red_4

The legacy `Player1` / `Player2` map references still point to Blue_1 and Red_1 for compatibility with older helper/debug code.

## Camera

- Main Camera

## Scene Helpers

- CAM_Overview
- CAM_Pyramid
- CAM_AIStation
- CAM_Player1
- CAM_Player2
- MAP_CENTER

---

## Notes

Pyramid and AI Station are procedurally generated.

Camera helper objects are scene-only objects and are not prefabs.

Global Volume contains the post-processing profile used by the game.

The Main Camera is controlled by CameraFollow.





# Mission Scene Architecture

## Scene Objects (Not Prefabs)

These objects define the current mission and should remain scene-specific.

### Main Camera
- Scene object
- Controlled by CameraFollow
- Uses SceneAnchors for cinematic positions.

### TurnManager
- Scene object
- Coordinates the 8-unit player turn sequence and the enemy-drone phase.
- References PlayerUnits and HexGrid.

### MissionDirector
- Scene object
- Controls mission flow.
- References UI, TurnManager, and gameplay events.

### HexGrid
- Scene object
- Generates the playable map.
- Contains graph and pathfinding information.

### SceneHelpers (recommended: SceneAnchors)
Purpose:
Stores Transform markers used by cinematics and camera movement.

Contains:
- IntroCamera
- MissionCamera
- PlayerFocus
- ObjectiveFocus
- Other helper transforms

Notes:
Scene-specific.
Should NOT be converted into a prefab.
Contains no gameplay logic.

# Scene Architecture

## Current high-level structure

The scene currently contains the established map, players, managers, UI, AI Station presentation, and audio systems.

The `Managers` hierarchy is the preferred home for persistent orchestration components.

Known manager-side objects include:

- TurnManager
- MissionDirector
- DeliveryPulse
- MatchMusic
- Existing delivery feedback/audio controller

## Planned v0.8 manager architecture

```text
Managers
├── TurnManager                 [existing]
├── MissionDirector             [existing]
├── MatchMusic                  [existing]
├── DeliveryPulse               [existing]
├── MatchManager                [planned]
├── BoxManager                  [planned]
├── PowerUpManager              [future]
└── EnemyDirector / DroneHive   [future]
```

## Responsibility boundaries

### TurnManager

Keeps responsibility for:

- Active player.
- Turn sequencing.
- Move processing.
- Existing combat flow.
- Movement highlights.

It should not become the owner of the box lifecycle, victory condition, or drone spawning.

### MatchManager — planned

Responsible for:

- Match state.
- Score-to-win value.
- Winner.
- Game-over state.
- Preventing continuation after the winning delivery.
- Triggering result presentation.

Avoid freezing the whole game with `Time.timeScale = 0` unless later proven necessary.

### BoxManager — planned

Responsible for:

- Ensuring exactly one box is in circulation.
- Initial box spawn.
- Replacement spawn after a successful non-winning delivery.
- Validating candidate spawn locations.
- Avoiding duplicate box creation.

It should not replace the already-working PlayerUnit pickup/death-drop behavior.

### PowerUpManager — future

Responsible for:

- Power-up lifecycle.
- Spawn scenario selection.
- Preventing duplicate/invalid placement.
- Coordinating consumable tactical opportunities.

### EnemyDirector / DroneHive — future

Responsible for:

- Drone spawning.
- Maximum active drone count.
- Target selection / threat logic.
- Hive spawn rules.
- Preventing invalid or unfair spawn locations.

Individual drones should reuse the existing graph/navigation model where practical.

## Architectural constraint

Do not redesign or normalize the established map coordinate, tile, or vertex systems as part of these additions.

New systems should orchestrate the working map rather than replace it.


## v0.3.1 team and enemy hierarchy

The current gameplay hierarchy now includes a team layer:

```text
Players
├── TeamBlue                  [TeamController]
├── TeamRed                   [TeamController]
├── Blue_1                    [PlayerUnit: team 1, unit 1]
├── Blue_2                    [PlayerUnit: team 1, unit 2]
├── Blue_3                    [PlayerUnit: team 1, unit 3]
├── Blue_4                    [PlayerUnit: team 1, unit 4]
├── Red_1                     [PlayerUnit: team 2, unit 1]
├── Red_2                     [PlayerUnit: team 2, unit 2]
├── Red_3                     [PlayerUnit: team 2, unit 3]
└── Red_4                     [PlayerUnit: team 2, unit 4]

EnemyUnits
└── EnemyDroneDirector
    └── EnemyDrone instance(s)
```

### TeamController

`TeamController` is a team-level registry. It discovers and stores every `PlayerUnit` whose `teamID` matches the controller's team and links each unit back to its team.

It does not replace `PlayerUnit`. Individual movement, damage, carrying, animation, and local actions remain unit responsibilities.

### EnemyDroneDirector

The director now participates in the live turn loop after all eight player units have acted.

Current enemy responsibility includes:

- Executing each active drone during the enemy phase.
- Preserving one-drone-per-hex-center occupancy rules.
- Allowing individual drones to select targets from all living player units.

Automatic hive-spawn cadence remains separate future work.
