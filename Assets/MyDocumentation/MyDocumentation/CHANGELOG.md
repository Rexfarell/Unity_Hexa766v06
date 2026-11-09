# Changelog

---

## Version 0.2

### Added

- Strategy Camera V2
- Mouse orbit
- Mouse wheel zoom
- Camera recenter
- Player prefabs
- Technical handbook

### Fixed

- Mission Intro orbit
- Health bar references
- Camera hierarchy

### Removed

- CameraRig
- Test objects

### Verified

- Camera controls
- Turn transitions
- Health bars
- Environmental damage
- Mission Intro

# CHANGELOG

## [Unreleased]

### Added
- Initial technical documentation.
- Added an on-screen "ENTER · Skip Intro" prompt during the mission intro cinematic.
- The prompt is automatically removed when gameplay begins.

## v0.2.0 (Work in Progress)

### Mission Briefing UI Framework
- Added cinematic intro music.
- Added "Press ENTER to Skip" functionality.
- Implemented the UI framework for mission briefing panels.
- Added Logo, Story, and Mission presentation panels.
- Corrected UI hierarchy and panel activation logic.
- Fixed subtitle alignment and clipping issues.
- Fixed version label placement.
- Improved intro presentation stability before gameplay begins.

CHANGELOG — v0.2 "Archive"

Narrative

Introduced the Archive 766 framing device for the introduction.
Replaced omniscient exposition with a historical reconstruction narrated by The Curator.
Established the game's archaeological tone, blending digital humanities, archaeology, and computer science.
Defined the canonical roles of:
Holographic Cenotaph (archaeological monument)
AI Station (physical host of ARCHON)
ARCHON (dormant non-human artificial intelligence)
Connected the title HEXA_766 to its in-universe meaning as a recovered archive while preserving its real-world origin from the first tabletop prototype.
Rewrote the introductory screenplay using the principle "one idea per screen" to improve pacing and dramatic impact.
Shifted the introduction from a conventional game briefing to an archaeological reconstruction of historical events.

## [2026-08-XX] - Fixed Unit Death and Turn Transfer

### Fixed
- Fixed premature turn transfer when a PlayerUnit dies during movement.
- Prevented `SmoothMove()` from calling `TurnManager.HandleMoveFinished()` when the unit is dead.
- Kept normal movement turn completion unchanged.
- Preserved the existing delayed respawn system.
- Preserved energy/shield restoration and health-bar reset behavior.
- Preserved home-base respawn positioning.
- Death turn handling is now controlled by `DieRoutine()`.

08/18/2026
Basic combat system with automatic same-hex attacks and manually selected adjacent attacks using K.
Added shield-first attack damage and attack animation event handling.

# Changelog


## [0.3.1] - 2026-09-17

### Added

- Added `TeamController` and explicit team/unit identity.
- Expanded the playable roster to four Blue units and four Red units.
- Added deterministic `teamID` + `unitID` ordering.
- Added multi-unit spawn and respawn placement around team home hexes.
- Added compact binary in-world unit identifiers:
  - Blue: `B 00`, `B 01`, `B 10`, `B 11`
  - Red: `R 00`, `R 01`, `R 10`, `R 11`
- Updated score presentation to `Blue: N` / `Red: N`.

### Changed

- Generalized enemy-drone target selection from the original Player1/Player2 pair to all living player units.
- Preserved objective-carrier hard priority, same-hex attack priority, nearest-target pursuit, and random equal-distance tie breaking.
- Updated map generation and respawn logic to preserve distinct unit positions in 4v4 play.
- Moved persistent score text to opposite top corners to keep the screen center clear.

### Verified

- 4v4 spawning.
- 8-unit player turn sequence.
- Enemy phase after Red_4.
- 4v4 respawning.
- Team registration and ordering.
- Multi-unit drone targeting.
- Carrier-priority targeting.
- Existing drone occupancy, attack telegraph, beam, impact, audio, and damage flow.


## [0.3.0] - 2026-09-06

### Design milestone

Version 0.3 defines the next complete match structure for HEXA_766.

### Added to design

- Single contested objective box.
- One-box-in-circulation invariant.
- First-to-three-deliveries victory condition.
- Match-ending state and winner presentation requirement.
- Curated reachable box-spawn strategy.
- Fair-opportunity design philosophy.
- Planned tactical power-up layer.
- Optional restricted teleportation concept.
- Planned hostile drone hive / enemy-pressure phase.
- Future tactical spawn scenarios combining box access, power-ups, route danger, and enemy pressure.

### Audio

- Delivery feedback audio is functional.
- Pickup audio is functional.
- Death audio is functional.
- Looping match music is functional.
- Reactive gameplay/music composition concept documented and deferred.

### Preserved

The v0.3 direction explicitly preserves the working:

- Map coordinate and vertex system.
- Movement/pathfinding.
- Turn system.
- Combat.
- Box pickup.
- Box death-drop behavior.
- AI Station delivery.
- Delivery camera and feedback.
- Current score UI.

### Not yet implemented

This version entry records the design/architecture milestone. The single-box manager, win condition, result screen, power-ups, and drone hive remain implementation work.



## [0.3.2] - 2026-09-29

### Added

- Added playable Mass Dampener Subsystem.
- Added playable Ballast Injection Subsystem.
- Added playable Structural Reinforcement Subsystem.
- Added playable Network Stress Subsystem.
- Added `SubsystemController`.
- Added `SubsystemFeedbackUI`.
- Added HEXA_766 themed tactical Subsystem notifications.
- Added NO EFFECT and NO VALID TARGET feedback states.

### Changed

- Unit-based Subsystem targeting now uses screen-space bounds derived
  from visible PlayerUnit Renderers.
- Added targeting padding for easier robot selection.
- Mass Dampener ignores units already at minimum Effective Load.
- Ballast Injection ignores units already at maximum Effective Load.
- Increased Subsystem feedback display duration.

### Rules

- One successful Subsystem may be activated per PlayerUnit turn.
- Failed attempts do not consume the Subsystem.
- Effective Load remains constrained to 1–5.
- Mass Dampener modifies allied Effective Load by -1.
- Ballast Injection modifies enemy Effective Load by +1.
- Structural Reinforcement modifies hex Capacity by +3.
- Network Stress modifies hex Capacity by -3.

### Preserved

- Existing TurnManager sequence.
- Movement and pathfinding.
- Combat.
- Objective lifecycle.
- Enemy phase.
- Structural-collapse system.
- 4v4 player-turn architecture.
