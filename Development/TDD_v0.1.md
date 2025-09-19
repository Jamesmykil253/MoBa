# MOBA Technical Design Document (TDD) v0.1

This technical design document defines the architecture for the MOBA prototype described in the companion GDD.  The project is built with Unity 6 using Netcode for GameObjects (NGO) and Unity Transport (UTP).  Key systems are designed to be deterministic, server‑authoritative and configurable via ScriptableObject assets【518224335480678†L83-L109】.

## Overview

The game runs a fixed update loop on the server.  Clients simulate locally and predict certain actions but always reconcile based on server snapshots.  Game state is managed through finite state machines for spawning (implemented in `PlayerSpawnStateMachine`), match flow, abilities and jump physics.  Each state machine consists of a set of states, a set of transitions and a variable storing the current state【804165595389957†L82-L94】.

Data used to configure heroes, abilities, match modes and scoring resides in ScriptableObject assets.  ScriptableObjects are serializable Unity types that act as data containers independent of GameObjects【518224335480678†L83-L109】.

## Input System Architecture

### Modern Input Framework
The game utilizes Unity's Input System with comprehensive control scheme support:

**Control Schemes Supported:**
- **Keyboard & Mouse** (Primary PC gaming)
- **Gamepad** (Console-style controllers) 
- **Touch** (Mobile devices)
- **Joystick** (Legacy controller support)
- **XR** (VR/AR headset controllers)

**Input Processing Flow:**
1. Unity Input System captures input across all supported devices
2. Input actions mapped to game functions via `InputSystem_Actions.inputactions`
3. Input buffering (0.1s window) ensures responsive ability casting
4. Server validates all gameplay-affecting inputs for anti-cheat
5. Client prediction with server reconciliation maintains responsiveness

**Key Control Mappings:**
- **Q/E/G**: Ability 1, Ability 2, Ultimate (corrected from previous Q/W/E/R)
- **LMB/RMB**: Smart attack targeting with priority systems
- **1/2 Keys**: Ability evolution path selection (Pokemon Unite-style)
- **Space**: Variable jump height (tap vs hold)
- **Ping Wheel**: Team communication system (implementation pending)

## Match Loop State Machine

The match loop controls the high‑level progression of a game session.  It runs exclusively on the server and replicates its state to clients via a NetworkVariable or an RPC.  The states and transitions are as follows:

| State            | Description                                                                                                    | Transitions                                                                                                                       |
|------------------|----------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------|
| **Lobby**        | Players connect, select heroes and ready up.  The server waits for all clients to load.                        | → **Loading** when all players are ready and the match owner starts the game.                                                     |
| **Loading**      | The server spawns the map and network prefabs, then signals clients to load necessary scenes and assets.        | → **MatchStart** when all clients confirm they have loaded; → **Lobby** on error (e.g., a client disconnects).                    |
| **MatchStart**   | `PlayerSpawnStateMachine` instantiates player objects and assigns base stats.  A countdown appears to clients. | → **InMatch** when the countdown ends; → **Lobby** if the spawn process fails.                                                     |
| **InMatch**      | The core gameplay state.  Players move, attack, score and use abilities.  Timer and score milestones trigger.  | → **MatchEnd** when the timer expires or a score cap is reached; → **Pause** if the host pauses (for testing).                    |
| **MatchEnd**     | The server stops gameplay, calculates the winner and displays results.                                          | → **Results** immediately after final calculations; → **Lobby** after results are viewed.                                          |
| **Results**      | Post‑match summary showing scores, kills, assists and milestones.  Players can exit to lobby or rematch.       | → **Lobby** when all players close results.                                                                                        |
| **Replay (Future)** | An optional state for viewing match replays.                                                                 | → **Lobby** when the replay ends or the player quits viewing.                                                                     |

The state machine ensures that gameplay cannot progress until the appropriate conditions are met.  For example, the server only transitions to InMatch when all clients report that they have loaded the map, and players cannot spawn until the match is in MatchStart.

### Time Synchronization

Timekeeping is critical in networked games.  The server maintains the authoritative clock (tick count) and replicates it to clients.  Clients use this clock for fixed‑step simulation and interpolation.  Each fixed update tick is 0.0167 seconds (60 Hz) for physics and ability resolution.  Network snapshots are sent at 20 Hz, balancing bandwidth and responsiveness.  A configurable interpolation window (e.g., 100 ms) smooths network jitter.

## Ability System Architecture

Abilities are executed through a modular system composed of an `AbilityController` MonoBehaviour attached to each player and a set of `AbilityDef` ScriptableObjects that describe ability data.  The process for an ability usage is:

1. **Input Detection** – The client listens for input (keyboard/mouse/controller) and sends an input command to the server.  Inputs are bound via the `AbilityDef`.
2. **Cast Check** – The server verifies that the ability is off cooldown and that the player meets resource requirements (mana/stamina).  Cooldowns and costs are defined in the `AbilityDef`.
3. **State Transition** – The player enters a casting state in the ability state machine.  Cast time is processed and can be interrupted by damage or movement.
4. **Server Execution** – When cast time completes, the server executes the ability logic: applying damage, healing, knockback or spawning projectiles.  Damage uses the RSB model (see Combat section) and scales with the attacker’s primary stat.
5. **Client Feedback** – The server sends RPCs to all clients to play animations, particle effects and UI cues.  Clients display the ability effect locally.
6. **Cooldown** – The ability enters a cooldown state.  The `AbilityController` tracks remaining cooldown time and updates the UI accordingly.

The `AbilityDef` ScriptableObject includes fields:

* `Name` – Unique string identifier.
* `InputAction` – Input binding (e.g., `MouseButton0`, `Q`, `Shift`).
* `CastTime` – Time in seconds required to channel before execution.
* `Cooldown` – Base cooldown in seconds.
* `CostType` – Mana or stamina.
* `CostAmount` – Points consumed when cast.
* `DamageCoefficient` – Multiplier applied to the caster’s stat in the RSB formula.
* `KnockbackForce` – Strength of knockback applied (optional).
* `EffectPrefab` – VFX prefab reference for visual representation.
* `Description` – Designer description for tooltips.

### Jump Physics Configuration

High jump and double jump are special abilities shared by all heroes.  Their behaviour is controlled by a `JumpPhysicsDef` asset containing:

* `InitialJumpVelocity` – The upward velocity applied when the first jump is executed.
* `GravityScale` – Gravity multiplier used for players while airborne.
* `DoubleJumpWindow` – The time window after the first jump during which the player can trigger a second jump.
* `DoubleJumpVelocity` – The upward velocity applied for the second jump.
* `CoyoteTime` – A small grace period after leaving the ground during which a jump still counts as grounded.

These parameters feed into the ability state machine.  When a jump is triggered, the player transitions from Grounded→Airborne state.  The server applies vertical velocity and replicates the new position using a `NetworkTransform` or custom networked physics component.

## Combat & Damage Model

The combat system uses a Reduced Softcap Breakpoint (RSB) damage model.  Damage dealt is computed as:

```
Damage = (BaseDamage * DamageCoefficient) * (AttackerPrimaryStat / (AttackerPrimaryStat + TargetDefense))
```

Where **BaseDamage** is defined per ability, **DamageCoefficient** comes from the ability definition and **AttackerPrimaryStat** is the caster’s strength, agility or intellect depending on ability type.  This formula produces diminishing returns on defense: as the target’s defense increases, damage reduction approaches but never reaches 100%.  Effective HP (eHP) is computed by dividing a hero’s health by the damage‑reduction factor.  For example, doubling defense roughly doubles eHP for moderate defense values.

Knockback strength uses a similar ratio: `Knockback = KnockbackForce * (1 - TargetDefense / (TargetDefense + KnockbackScalingConstant))`.  This ensures that tanks resist knockback more than fragile heroes.

## Networking & Transport Configuration

The game uses NGO with Unity Transport as the underlying transport.  Network settings include:

* **Server Authoritative** – The server hosts the match and owns all authoritative objects (players, abilities, neutral units, scoring zones).  Client inputs are sent to the server for validation and execution.

* **Tick Rate** – Game logic and physics operate at 60 ticks per second.  Network snapshots (state updates) are sent at 20 Hz.  Interpolation buffers of 100 ms allow smoothing of positional updates.

* **Channels** – Reliable channels are used for critical events (spawns, ability casts, score deposits).  Unreliable channels are used for frequent transform updates.  Sequence numbers detect lost or out‑of‑order packets.

* **Client Prediction & Reconciliation** – Clients simulate player movement locally using input and predicted physics.  When the server sends authoritative positions, the client compares them to predicted positions and corrects discrepancies smoothly.  A ring buffer stores past states to support rewinding for hit detection.

* **Lag Compensation** – On the server, commands are processed using the timestamp from the client plus estimated network delay.  This ensures that shots fired near obstacles are resolved fairly.

## ScriptableObject Schemas

### BaseStatsTemplate

Used to define starting stats for each hero archetype.  Fields include health, mana, stamina, strength, agility, intellect, defense and any passive modifiers.  Instances are created for each hero and referenced by the spawn state machine.

### AbilityDef

Described above in the ability system section.  Instances are grouped by hero or by ability category.  Designers can create new abilities by creating new assets.

### JumpPhysicsDef

Defines global jump parameters used by all heroes.  Designers can experiment with different jump heights and gravity scales by adjusting the asset values rather than modifying code.

### MatchModeDef

Contains match length (in minutes), team size (3 v 3 or 5 v 5), target score to win, and milestone events (e.g., neutral boss spawn at 4 minutes, double score periods).  Each match mode selects a `ScoringDef` and an `UltimateEnergyDef` to use.

### ScoringDef

Defines baseline deposit durations and per‑orb speed modifiers.  For example, depositing 10 orbs might take 3 seconds, while depositing 30 orbs takes 5 seconds minus a 0.2 second reduction for each teammate assisting.  Designers can adjust these values to control risk–reward dynamics.

### UltimateEnergyDef

Specifies energy gain rates from passive regen, damage dealt, assists, kills and deposits.  It also sets the post‑ultimate cooldown period.  If energy regen is too slow or too fast, designers can adjust these values without recompiling.

### ScoringMilestoneDef (Optional)

Defines point thresholds that trigger events like neutral objective spawns or map changes.  This allows scaling difficulty and rewarding teams for reaching certain scores.

## Project Skeleton & Implementation Guidelines

To support the systems above, the repository should be organised as follows:

* `/Assets` – Contains all Unity assets and code.  Under this folder:
  * `/Scripts` – C# source files.  Use Assembly Definition (.asmdef) files to separate runtime code (`Moba.Runtime`), editor tools (`Moba.Editor`) and tests (`Moba.Tests`).
  * `/Data` – ScriptableObject assets (BaseStatsTemplate, AbilityDef, JumpPhysicsDef, MatchModeDef, ScoringDef, UltimateEnergyDef).
  * `/Prefabs` – Prefabs for players (`PlayerRoot`), neutral units, goal zones, jump pads and UI elements.  Prefabs should include network components like `NetworkObject` and `NetworkTransform`.
  * `/Scenes` – Unity scenes.  At minimum: `Bootstrap` (loads `NetworkManager` and persists across scenes), `MainMap` (contains the gameplay area) and `Lobby` (hero selection and match settings).
  * `/AddressableAssets` – Addressable asset groups for heroes, abilities and map chunks.  Use the Addressables system to allow content updates without client patches.

* `/Docs` – Game and technical design documents (`GDD_v0.1.md`, `TDD_v0.1.md`) and additional guidelines.

* `/Tests` – NUnit or UnityTestFramework scripts for both EditMode and PlayMode tests.  Tests should cover state machine transitions, ability execution, scoring logic and network synchronisation.

* `/Tools` – Editor utilities such as a debug overlay to visualise state machine states and network statistics, and menu commands to create new ScriptableObject assets.

* `/ProjectSettings` and `/Packages` – Unity’s configuration and package manifest.  These are generated by Unity when the project is created.

## Implementation Roadmap

1. **Implement MatchLoopStateMachine** – Create the `MatchLoopStateMachine` MonoBehaviour on the server to manage the lobby, loading, match and results states.  Replicate the state via a NetworkVariable.

2. **Set Up ScriptableObjects** – Implement the ScriptableObject classes described above.  Provide custom inspector editors for ease of tuning.

3. **Build Ability System** – Implement `AbilityController` to process inputs and execute abilities.  Create a few sample ability definitions (high jump, double jump, knockback, basic attack, hero ultimates).

4. **Add Jump Physics** – Use `JumpPhysicsDef` to drive jump behaviour.  Ensure deterministic physics by using fixed update and constant gravity.

5. **Integrate Networking** – Configure NGO’s `NetworkManager` with the appropriate tick rate and transport settings.  Create network prefabs and register them in the `NetworkManager` asset.

6. **Prototype Map & Prefabs** – Build a simple map with two lanes, jungle paths and jump pads.  Create prefabs for `PlayerRoot`, `NeutralUnit`, `GoalZone` and `JumpPad` with proper components.

7. **Testing & Iteration** – Write unit and play mode tests.  Conduct local multiplayer sessions to verify spawning, movement, scoring, abilities and network synchronisation.  Use Unity Profiler to monitor performance and adjust tick rates or interpolation windows as needed.

## Conclusion

This TDD outlines the architecture necessary to build a networked MOBA prototype consistent with the design goals.  It emphasises deterministic state machines, server‑authoritative logic and data‑driven configuration through ScriptableObjects.  By following this plan, the team can incrementally develop a working demo while retaining flexibility to tune gameplay without code changes.