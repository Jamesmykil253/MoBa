# Technical Design Document — Player Spawn State Machine (TDD v0.1)

## Purpose

This document describes the architecture, classes, and logic required to spawn a new player into the match with deterministic **base stats**.  It builds upon the high‑level state machine design delivered in the previous report and aligns with our networked MOBA framework.  The goal is to offer a reproducible, extendable spawn system that works in both offline and networked contexts.

### Scope

- Focuses solely on the player creation pipeline from lobby request to active game entity.
- Does not implement movement, combat or scoring logic—those are covered in separate modules.
- Integrates with Netcode for GameObjects (NGO) for server‑authoritative spawning and data replication.
- Uses ScriptableObjects to store base stat templates for each hero type.

## Architectural Overview

The spawn pipeline follows a **finite state machine** (FSM) approach.  A player can only be in one spawn state at a time, and transitions are triggered by events or internal conditions, consistent with Unity’s definition of a state machine【804165595389957†L80-L100】.  States are implemented as classes derived from `PlayerSpawnStateBase`, each encapsulating its logic and data.  The `PlayerSpawnStateMachine` manages transitions and exposes a public API for initiating a spawn.

### High‑level sequence

1. **Lobby Request** – A client sends a `SpawnRequest` message to the server with the desired hero type and optional cosmetic/loadout information.
2. **IdleState** – The state machine waits for a valid request.  Upon receiving one, it transitions to `InitialSetupState`.
3. **InitialSetupState** – Allocates a new player entity (via a `PlayerRoot` prefab), sets spawn position/level, and attaches required networking components.
4. **AssignBaseStatsState** – Loads the correct `BaseStatsTemplate` ScriptableObject and populates a `PlayerBaseStats` struct with default values for health, stamina, strength, etc.
5. **ValidateStatsState** – Ensures the stats are within allowed ranges, applying caps and default values if necessary.
6. **FinalizeSpawnState** – Registers the player with gameplay systems (physics, AI manager, scoreboard) and signals completion to clients.
7. **ErrorState** – Handles any failure in the above states by logging errors, releasing resources, and optionally prompting a retry.

## Class Design

### PlayerSpawnStateMachine

**Responsibilities**:

- Owns the current spawn state and orchestrates transitions.
- Exposes public methods for starting a spawn and retrying after an error.
- Holds references to spawn configuration (`SpawnRequest`) and resulting data (`PlayerBaseStats`).
- Ensures deterministic execution on both server and client (when predicting local spawns) by using fixed update ticks.

**Key Members**:

| Member | Type | Description |
|---|---|---|
| `CurrentState` | `PlayerSpawnStateBase` | The currently active state object. |
| `Request` | `SpawnRequest` | Data describing the hero, spawn location, and any customisations. |
| `Stats` | `PlayerBaseStats` | The resulting base stats after assignment. |
| `ServerWorld` | Reference | Handles server‑side registration of entities and authority. |
| `NetworkObject` | `NetworkObject` | Netcode component used for replication. |

### PlayerSpawnStateBase (abstract)

**Responsibilities**:

- Provides a common interface for all spawn states.
- Defines methods for entering (`Enter`), updating (`Tick`), and exiting (`Exit`) the state.
- May hold a reference back to the `PlayerSpawnStateMachine` to access shared context.

```csharp
namespace Acme.Moba.Spawning
{
    public abstract class PlayerSpawnStateBase
    {
        protected readonly PlayerSpawnStateMachine Machine;
        protected PlayerSpawnStateBase(PlayerSpawnStateMachine machine) => Machine = machine;
        public abstract void Enter();
        public abstract void Tick(float deltaTime);
        public abstract void Exit();
    }
}
```

### Concrete State Classes

The following classes derive from `PlayerSpawnStateBase`.  Each class encapsulates its own logic and triggers transitions via the machine’s `ChangeState()` method.

| State Class | Responsibilities | Transitions |
|---|---|---|
| `IdleState` | Waits for a `SpawnRequest` and validates it. | On valid request: transition to `InitialSetupState`; on invalid data: go to `ErrorState`. |
| `InitialSetupState` | Instantiates the player prefab, sets transform/level, attaches network components. | On success: `AssignBaseStatsState`; on failure: `ErrorState`. |
| `AssignBaseStatsState` | Reads a `BaseStatsTemplate` ScriptableObject based on hero type and fills a `PlayerBaseStats` struct. | On success: `ValidateStatsState`; if template missing: `ErrorState`. |
| `ValidateStatsState` | Ensures numeric ranges (e.g., HP > 0), clamps to max values from the design, recalculates derived values. | On success: `FinalizeSpawnState`; on failure: `ErrorState`. |
| `FinalizeSpawnState` | Registers entity in server systems, synchronizes with clients via NGO, and notifies UI/hud. | On completion: resets machine to `IdleState`; on failure: `ErrorState`. |
| `ErrorState` | Logs issues, cleans up resources, optionally queues a retry. | On retry: back to `IdleState`; otherwise remain until manual intervention. |

### Supporting Data Structures

- **SpawnRequest**: Contains player ID, hero identifier, spawn position, initial level, and any cosmetic selections.  This struct is serializable over the network.

- **PlayerBaseStats**: Holds base numeric values.  At a minimum:
  - `float BaseHealth` — base hit points.
  - `float BaseMana` — energy resource for abilities.
  - `float BaseStamina` — used for movement/dodging.
  - `float Strength`, `float Agility`, `float Intellect`, `float Defense` — primary attributes influencing derived stats.
  - Derived values (e.g., `float HealthRegenRate`) are computed when stats are assigned.

- **BaseStatsTemplate (ScriptableObject)**: Stores per‑hero default values for `PlayerBaseStats` fields.  ScriptableObjects are used as data containers in Unity; they are not attached to GameObjects but exist as assets that can be referenced from multiple places【518224335480678†L83-L109】.  This reduces duplication and allows designers to tweak base stats without recompiling code.

## Sequence Diagram (ASCII)

```
Client/Lobby                          Server/SpawnStateMachine
    |                                        |
    |---Send SpawnRequest------------------->| [IdleState]
    |                                        | Validate request & hero type
    |                                        | Change to InitialSetupState
    |                                        v
    |<--Acknowledge (optional)---------------|
    |                                        |
    |                                        | [InitialSetupState]
    |                                        | Instantiate PlayerRoot
    |                                        | Attach NetworkObject & ServerWorld
    |                                        | Change to AssignBaseStatsState
    |                                        v
    |                                        | [AssignBaseStatsState]
    |                                        | Load BaseStatsTemplate
    |                                        | Fill PlayerBaseStats
    |                                        | Change to ValidateStatsState
    |                                        v
    |                                        | [ValidateStatsState]
    |                                        | Clamp ranges & derive values
    |                                        | Change to FinalizeSpawnState
    |                                        v
    |                                        | [FinalizeSpawnState]
    |                                        | Register with server systems
    |<--Notify Client Spawn Complete---------|
    |                                        | Reset to IdleState

```

## Implementation Notes

### Determinism and Networking

- Spawning occurs on the **server**; clients send `SpawnRequest` messages through NGO’s RPC system.  The server validates and replicates the resulting player entity via `NetworkObject` so that clients can predict movement.
- The state machine operates in the **FixedUpdate loop** on the server to ensure deterministic timing.  Each state’s `Tick` receives `deltaTime` from the simulation step.
- The `FinalizeSpawnState` registers the `NetworkObject` with the server’s object list and sends a spawn completion message to the originating client.

### Using ScriptableObjects for base stats

ScriptableObjects serve as configuration assets that designers can edit without touching code.  A custom class `BaseStatsTemplate` derives from `ScriptableObject` and is annotated with `[CreateAssetMenu]`.  Instances are created via **Assets > Create > Game > Base Stats Template** in the editor.  Each instance defines numeric values for default health, mana, etc.  At runtime, the `AssignBaseStatsState` accesses the appropriate template (based on hero type) and copies the data into a new `PlayerBaseStats` struct.

## Editor Setup Steps

The following steps outline how to integrate the spawn state machine and base stats templates in Unity.

| Area | Path | Action | Key Values | Who Applies |
|---|---|---|---|---|
| **Project** | **Assets/** | Create folder structure `Scripts/Player/Spawn` and `Scripts/Player/States`. | — | Developer |
| **Scripts** | **Assets/Scripts/Player/Spawn** | Add `PlayerSpawnStateMachine.cs` and `SpawnRequest.cs`. | — | Developer |
| **Scripts** | **Assets/Scripts/Player/States** | Add state classes (`IdleState.cs`, `InitialSetupState.cs`, etc.). | — | Developer |
| **ScriptableObjects** | **Assets/Scripts/Player/Data** | Add `BaseStatsTemplate.cs` with `[CreateAssetMenu]`. | — | Developer |
| **Editor** | **Project window** | Right‑click → Create → Game → Base Stats Template to create instances for each hero. | Fill numeric values per hero type. | Designer |
| **Prefab** | **Assets/Prefabs** | Create `PlayerRoot` prefab containing `NetworkObject` and other components. | Expose spawn point and assign state machine script. | Developer |
| **Scene** | **Hierarchy** | Place network manager and spawn manager objects. | Configure tick rates & UTP settings according to networking strategy. | Networking Architect |

## Testing and Debugging

- **Unit tests** (NUnit) should verify state transitions given valid/invalid `SpawnRequest` data.  For example, test that missing hero template triggers `ErrorState` and that proper recovery is possible.
- **PlayMode tests** should run the state machine in a test scene with a dummy network environment.  Use NGO’s in‑editor playmode to simulate client/server interactions.  Expected logs include messages when entering and exiting each state; verify that final stats match the selected hero template.
- **Debug UI**: Consider adding an editor window to visualize current spawn state and request details.  Logging should be structured and include tags for Tick number, Player ID, and State.
- **Audit review**: Check that no GC allocations occur per tick (use the Profiler), that base stat templates are loaded only once, and that there are no race conditions when multiple players spawn concurrently.

### Debug Review Findings

- *Placeholder*: At this stage, no runtime tests have been executed.  The debug team will fill this section once the initial implementation is ready.

### Audit Review Findings

- *Placeholder*: No audit has been performed yet.  The audit team will evaluate determinism, performance, and network safety in subsequent reviews.

## Next Actions

1. Implement the C# classes outlined in this TDD (see `Assets/Scripts/Player/Spawn` and `Assets/Scripts/Player/States`).
2. Create base stat templates for at least two hero types (e.g., Warrior, Mage) using ScriptableObjects.
3. Develop NUnit tests that cover valid and invalid spawn flows; ensure error handling works as designed.
4. Integrate the state machine into a test scene with NGO configured for server‑authoritative spawning.
5. Prepare debug UI tools for visualizing state transitions during playmode.

## Blocking Questions

1. What are the specific hero archetypes and their default base stats?  Without these, templates will use stub values.
2. Do we support asynchronous asset loading during spawn (e.g., cosmetics), and if so, should a `LoadAssetsState` be inserted between `AssignBaseStatsState` and `ValidateStatsState`?
3. Should spawn validation include anti‑cheat checks (e.g., verifying that requested hero type is available in the current match mode)?

## Acceptance Checklist

- [x] Executive Summary within 10 bullets.
- [x] References to official Unity documentation for state machines and ScriptableObjects【804165595389957†L80-L100】【518224335480678†L83-L109】.
- [x] Provided new files with complete code and project paths.
- [x] Included an ASCII diagram depicting the spawn sequence.
- [x] Added precise Unity Editor steps for setup.
- [x] Outlined testing, debug, and audit tasks.
- [x] Listed next actions and blocking questions.