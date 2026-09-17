# Gameplay Rules

---

**Document Version:** 0.3.1

**Last Updated:** 2026-09-17

---

# Purpose

This document describes the gameplay mechanics of HEXA_766 independently of the source code.

---

# Objective

Blue and Red each control four robotic units on a procedurally generated hexagonal battlefield.

Players must navigate hazards, manipulate objects, and complete mission objectives while preserving their unit.

---

# Turn Structure

Each player-unit turn follows this sequence:

1. Current `PlayerUnit` becomes active.
2. Valid movement locations are highlighted.
3. The controlling player selects a destination or action.
4. The unit resolves its movement/action.
5. Environmental hazards and combat effects are resolved.
6. Camera changes to the next active unit.
7. The unit turn ends.

The current round order is:

```text
Blue_1 → Blue_2 → Blue_3 → Blue_4
→ Red_1 → Red_2 → Red_3 → Red_4
→ Enemy Drone Phase
→ repeat
```

---

# Movement

Movement is vertex-based.

The board is generated procedurally.

Movement destinations are generated dynamically every turn.

---


## 4v4 team structure

Each team currently contains four units.

Gameplay identity is stored as:

- `teamID`: Blue = 1, Red = 2.
- `unitID`: 1 through 4 within the team.

In-world presentation uses binary-style labels:

```text
Blue: B 00, B 01, B 10, B 11
Red:  R 00, R 01, R 10, R 11
```

These labels are presentation only; gameplay logic uses `teamID` and `unitID`.

### Spawn and respawn

Unit 1 keeps the team's configured home vertex. Units 2–4 use successive vertices around the same home hex.

Respawn uses the same team/unit placement rule so teammates do not return to the same vertex.


# Health System

Each unit has:

- Shield
- Energy

Damage is applied in this order:

1. Shield absorbs damage.
2. Remaining damage reduces Energy.

When Energy reaches zero, the unit is considered depleted.

---

# Environmental Hazards

Current hazards include:

- Corrosion

Corrosion damages shields before energy.

---

# Camera

The gameplay camera automatically follows the active player.

Players may:

- Orbit with Right Mouse Button.
- Zoom using Mouse Wheel.
- Recenter using the R key.

---

# Mission Intro

Gameplay begins only after the Mission Intro cinematic finishes.

MissionIntro transfers control to TurnManager.

---

# Procedural Objects

The following gameplay objects are generated at runtime:

- Pyramid
- AI Station
- Map vertices
- Tiles
- Hazards

These objects are not stored as prefabs.

---

# Future Gameplay

Planned additions include:

- AI opponent
- Additional hazards
- Card effects
- Victory conditions
- Save system

## Unit Death and Respawn

When a PlayerUnit's energy is depleted, the unit enters its death sequence.

The death sequence follows these rules:

1. The unit stops its walking animation.
2. The `Die` animation trigger is activated.
3. A respawn countdown begins.
4. The unit's current turn is considered consumed.
5. If the unit dies while completing a movement, the movement coroutine must not independently advance the turn.
6. Turn advancement during the death sequence is handled by the death routine rather than by `SmoothMove()`.
7. After the respawn delay, the unit:
   - Restores energy to its starting value.
   - Restores shield to its starting value.
   - Updates its health bars.
   - Returns to its assigned home-base tile and vertex.
   - Resets its animator state.
8. The next player's turn begins according to the normal `TurnManager` sequence.

### Death During Movement

A unit may deplete its energy as a consequence of environmental damage encountered during movement.

In this situation, `SmoothMove()` must terminate without calling `TurnManager.HandleMoveFinished()`.

This prevents the turn from being advanced twice or prematurely. The death routine remains responsible for completing the death sequence and transferring the turn.

### Normal Movement

If the unit survives the movement:

- `SmoothMove()` completes normally.
- The walking animation stops.
- Box pickup logic is evaluated.
- `TurnManager.HandleMoveFinished()` is called when appropriate.
- The next player's turn begins normally.

Basic Attack Mechanics

Same-hex attack: When a unit finishes movement on a hex containing hostile unit(s) occupying different vertices, an automatic short-range attack is triggered. The attack plays once and all hostile units sharing that hex receive the attack damage.

Adjacent-hex attack: A hostile unit occupying a vertex of an adjacent hex can be selected as an attack target. The player then presses K to perform Attack1 against the selected target. Only the selected hostile unit receives the damage.

Vertex occupancy: Two units cannot occupy the same vertex. Multiple units may occupy different vertices of the same hex.

Damage: Attack damage is currently 25. Damage is applied to Shield first; remaining damage reduces Energy.

Current status: Implemented and tested.

# Gameplay Rules

## Match objective

Blue and Red compete to recover and deliver AI components to the AI Station.

## v0.8 primary rule

There is exactly **one objective box in circulation at a time**.

The box may be on the map or carried by a player, but a second objective box must not be generated until the active box has been successfully delivered.

## Box lifecycle

### Spawn

At match start, one box is placed at a valid reachable board location.

### Pickup

A player picks up the box by reaching its exact tile and vertex under the existing pickup rules.

### Carrying

While carried:

- The unit becomes the current carrier.
- The original objective remains the same box in circulation.
- No replacement box is spawned.

### Carrier death

If the carrier dies:

- The carried box returns to the board using the existing death-drop behavior.
- The box is not consumed.
- No replacement box is spawned.
- The contest continues around the dropped box.

### Delivery

A player who reaches the AI Station delivery center while carrying the box completes delivery.

On successful delivery:

- The box is consumed.
- The carrier's team receives +1 delivery score.
- Existing delivery animation, camera, pulse, banner, and audio feedback play.
- If the player's new score is below 3, a replacement objective box is spawned.
- If the player's new score reaches 3, the match ends instead.

## Victory

The first team to deliver **three boxes** wins.

Recommended final sequence:

```text
Third delivery
→ BoxDown
→ delivery pulse/audio
→ score becomes 3
→ brief camera/score feedback
→ no replacement box
→ no next gameplay turn
→ result screen
```

## Fairness

Fairness does not require both players to be equally distant from the objective.

A deliberately asymmetric state is valid when meaningful counterplay exists.

Example:

```text
Player 1:
3 steps from box

Player 2:
8 steps from box
1 step from useful power-up
```

The game should permit these situations when the second player has a credible tactical alternative.

## Power-ups — planned

Power-ups are secondary objectives intended to create counterplay.

Potential effects:

- Shield restoration.
- Energy restoration.
- Attack boost.
- Temporary movement boost.
- Restricted teleportation if development time permits.
- Temporary disruption effects if development time permits.

Power-ups should usually be immediate, temporary, or consumable.

## Hostile drone pressure — implemented core

Neutral hostile drones now act after all eight player units have completed their turns.

Their current purpose is to:

- Threaten carriers.
- Complicate routes.
- Create zones of danger.
- Prevent objective play from becoming a pure shortest-path race.
- Add tactical pressure independent of the competing player.

Carrying the objective currently gives a unit hard-priority threat status for reachable drones. Same-hex attacks take immediate priority; otherwise drones pursue the nearest reachable valid unit. Automatic hive-spawn cadence and final balance remain future work.

## Strategic design target

Players should repeatedly face meaningful choices between:

- Racing for the box.
- Attacking the opponent.
- Intercepting a carrier.
- Seeking a power-up.
- Denying a power-up.
- Choosing a safer route.
- Exploiting or avoiding drone pressure.

The board should create counterplay, not guaranteed equality.
