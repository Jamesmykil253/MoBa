# MOBA Project Documentation

## 1. Overview
This r## 9. Input Integration & Control System

### Modern Input Architecture
- **Unity Input System**: Global adoption across all player and ability systems
- **Dual Input Support**: New Input System with legacy input fallback for compatibility
- **Smart Input Management**: Systems auto-detect conflicts and disable duplicate listeners

### Player Control Mapping
- **Movement**: WASD + mouse aim for 3D character control
- **Combat Actions**:
  - **Primary Attack (LMB)**: Smart targeting - enemy players → NPCs → no attack
  - **NPC Attack (RMB)**: NPC-focused - NPCs → enemy players → no attack
  - **Attack Rate Limiting**: Both respect character attack speed statistics
- **Abilities**:
  - **Ability 1 (Q)**: First basic ability
  - **Ability 2 (E)**: Second basic ability
  - **Ultimate (G)**: Character signature ability
  - **Input Buffering**: 0.1-second buffer window for responsive casting

### Advanced Input Features
- **Jump System**: 
  - **Tap (Space)**: Quick single jump
  - **Hold (Space)**: Variable height based on hold duration
- **Ability Evolution**: Pokemon Unite-style upgrade paths
  - **Path A (1 key)**: Select first upgrade branch when leveling
  - **Path B (2 key)**: Select second upgrade branch when leveling
- **Communication**: 
  - **Ping Wheel**: Quick team communication with predefined callouts
  - **Implementation**: Code framework complete (`ChatPingSystem.cs`), UI pending
- **Utility Actions**:
  - **Item Use (C)**: Activate consumable items
  - **Home (Ctrl, hold 1.5s)**: Return to base/respawn point
  - **Interact/Score (Alt)**: Deposit orbs at scoring zones

### Input System Integration
- **Component Compatibility**: `SimplePlayerController`, `EnhancedAbilitySystem`, `SimpleAbilitySystem`, and `SimpleInputHandler` all consume `InputActionAsset`/`InputActionReference` instances
- **Input State Management**: Suppress input during death/respawn with `SetInputEnabled(false)`
- **Conflict Resolution**: Enhanced system auto-disables internal bindings when legacy handlers detected via `enableInternalInputActions` toggle
- **Control Schemes**: Full support for Keyboard+Mouse, Gamepad, Touch, Joystick, and XR/VRtory contains a lightweight multiplayer-online-battle-arena (MOBA) prototype built on Unity's modern runtime stack. The codebase embraces the newest Input System, Netcode for GameObjects, and modular runtime systems that decouple gameplay, networking, and diagnostics. This document explains how the major systems interact, how to bootstrap a match instantly, and where to extend gameplay for production use.

> **Key namespaces**
> - `MOBA` – gameplay controllers, pooling, core infrastructure
> - `MOBA.Abilities` – ability runtime and ScriptableObject definitions
> - `MOBA.Configuration` – central configuration ScriptableObjects and validation helpers
> - `MOBA.Networking` – Netcode integration and high-level match orchestration
> - `MOBA.ErrorHandling` – global logging & validation services

## 2. Boot Flow & Match Lifecycle
1. **Startup:** Place `InstantMatchStarter` (`Assets/Scripts/InstantMatchStarter.cs`) in your bootstrap scene. The component searches for an existing `SimpleGameManager` or instantiates a prefab reference and then calls `StartMatch()` automatically (configurable via inspector).
2. **Match Initialization:** `SimpleGameManager.StartMatch()` (`Assets/Scripts/SimpleGameManager.cs`) resets the timer, clears scores, validates spawn points, then asks `ProductionNetworkManager` to spawn/own player avatars (falling back to local instantiation if Netcode is disabled) before deploying enemies. When the match ends (time limit or score), `OnGameEnd` is raised for UI/network consumers.
3. **Respawn & Loop:** `SimplePlayerController` manages player death/resurrection. When defeated, control/input actions are disabled, the respawn timer (from `MasterConfig.playerConfig`) runs, and the player is restored with temporary invulnerability before gameplay resumes. Match timer and team scores are synchronised via Netcode `NetworkVariable`s so every client observes authoritative state.

## 3. Player Systems
- **Controller:** `SimplePlayerController` (movement, combat, health) integrates the new Input System by referencing an `InputActionAsset`. Control is gated by `canControl` & `isAlive`, ensuring input is suppressed while the player is dead/respawning.
- **Movement Core:** `UnifiedMovementSystem` is the single authoritative movement/anti-cheat layer reused across players and AI. Legacy façades such as `JumpController` and `PlayerMovement` have been removed.
- **Damage Interface:** Implements `IDamageable`, enabling both abilities and enemies to apply damage generically.
- **Health Snapshots:** `SimplePlayerController` additionally implements `IDamageSnapshotReceiver` so server-authored damage results immediately adjust local UI and state.

### Configuration Injection
Player stats (health, damage, movement) sync from `MasterConfig.playerConfig`. Designers update balance values in ScriptableObjects (`Assets/Scripts/Configuration/GameConfigurations.cs`) and they propagate automatically at runtime.

## 4. Ability System
- **Enhanced Ability Runtime:** `EnhancedAbilitySystem` (Assets/Scripts/Abilities) reads ability definitions (`EnhancedAbility` ScriptableObjects) and manages mana, cooldowns, combat state, and hit detection. Input actions map to ability triggers; fallback keyboard polling has been removed to ensure parity with the new Input System.
- **Simple Ability Runtime:** `SimpleAbilitySystem` remains for legacy prefabs but now also consumes `InputActionAsset` or `InputActionReference` arrays, aligning with modern input pipelines.
- **Events:** Ability casts publish `AbilityUsedEvent` through `UnifiedEventSystem`, allowing UI or analytics listeners to respond without tight coupling.
- **Network Authority:** `AbilityNetworkController` bridges ability casts to Netcode for GameObjects. All requests flow through the server, which applies damage and pushes health snapshots to components implementing `IDamageSnapshotReceiver`.
- **Input ownership:** To prevent duplicate listeners the enhanced system now auto-detects `SimpleInputHandler` and disables its internal bindings (configurable via the `enableInternalInputActions` toggle). Follow this workflow whenever migrating prefabs—keep exactly one input bridge active per actor, mirroring AAA best practices from *Game Programming Patterns* and *Clean Code*.

## 5. Enemy AI & NPCs
`EnemyController` composes `UnifiedMovementSystem` with AI steering so chase/return logic shares the same physics and teleport safeguards as the player. It implements `IDamageSnapshotReceiver` so server-issued health snapshots immediately update remote clients. Hit VFX/fx use temporary primitives; consider swapping to pooled VFX for production. Enemy detection uses layer-filtered overlap spheres and verifies line-of-sight before applying damage.

## 6. Networking Stack
- **ProductionNetworkManager** is the authoritative networking entry point and **must** be used for MOBA gameplay. It drives the Netcode for GameObjects lifecycle (host/client/server), reconnection attempts, anti-cheat validation toggles, and player tracking (`PlayerNetworkData`). Hook its events into UIToolkit/UGUI dashboards for disconnect prompts, ping warnings, and error surfaced to players.
- **SimpleNetworkManager** is retained solely as a learning sample. Do not ship competitive builds with it—the class lacks reconnection, validation, and player bookkeeping.
- **Scene Integration:** Every playable scene should host exactly one `ProductionNetworkManager` (plus `UnityTransport`). If you bootstrap the scene through `SimpleSceneSetup`, replace the auto-added `SimpleNetworkManager` with the production component before testing multiplayer flows.
- **Input Ownership:** Ensure spawned player objects include `NetworkObject` components and align with `ProductionNetworkManager` ownership rules when scaling beyond local-testing scenarios.

## 7. Event & Error Handling
- **UnifiedEventSystem_Fixed** (Assets/Scripts/Core) uses weak references to prevent memory-leak issues while supporting static subscribers. Local and network channels are separated, and automatic cleanup runs periodically.
- **ErrorHandler** provides centralized logging, validation helpers, and threshold-based escalation. The runtime `ErrorHandlerMonoBehaviour` now focuses purely on subscription management—legacy `OnGUI` statistics were removed to encourage modern UI-driven dashboards.

## 8. Object Pooling & Performance Utilities
- **UnifiedObjectPool** handles pooling for GameObjects, Components, and NetworkObjects using thread-safe collections, now backed by hash-based lookups to avoid duplicate returns.
- **ProjectilePool/UnitFactory:** Manage domain-specific pools. Projectile prefabs are cloned at runtime instead of being modified in place, and any missing components are reported rather than patched onto the source asset.
- **Editor Tooling:** Use `Tools ▸ MOBA ▸ Validate Pooled Prefabs` to audit pooled prefabs for required components before entering play mode.

## 9. Input Integration
- Global adoption of the Unity Input System:
  - `SimplePlayerController`, `EnhancedAbilitySystem`, `SimpleAbilitySystem`, and `SimpleInputHandler` all consume `InputActionAsset`/`InputActionReference` instances.
  - Suppress input when gameplay should be paused or the entity is inactive by calling `SetInputEnabled(false)` on the relevant system.
  - Prefer the enhanced system’s internal bindings as the single source of truth; if a legacy handler must be used, disable `enableInternalInputActions` to avoid duplicated callbacks.

## 10. Configuration & Validation
- **ScriptableObjects**: `PlayerConfig`, `GameConfig`, `AbilitySystemConfig`, `NetworkConfig` (nested under `MasterConfig`). Each exposes `Validate()` methods employing `ErrorHandler.ValidateRange` to catch invalid data early.
- **MasterConfig**: Loaded via `Resources.Load` once and cached. Consider preloading or migrating to Addressables for larger projects.

## 11. Diagnostics & Logging
- Replaced all legacy immediate-mode UI (`OnGUI`) with stubs encouraging proper UI integration.
- `ErrorHandler` log buffering writes batched entries to `Application.persistentDataPath`. Rate limiting prevents disk contention.
- `PerformanceOverlay` can be attached to runtime cameras to visualise frame timings and live network stats, following profiling guidance from *Real-Time Rendering*.
- `PerformanceProfiler` (if enabled) and other utility scripts can be extended to capture metrics—tie them into the event system for improved observability.

## 12. Extending the Project
- **UI/UX**: Build UIToolkit or UGUI dashboards to consume events from `SimpleGameManager`, `ProductionNetworkManager`, and `EnhancedAbilitySystem`.
- **Networking**: Implement transport RTT queries in `ProductionNetworkManager.UpdatePing()` (marked with TODO-style comments) and broadcast match state over RPCs/NetworkVariables.
- **Abilities**: Extend `EnhancedAbility` ScriptableObject with new effect types or integrate client-side prediction by subscribing to `AbilityUsedEvent` and spawning VFX locally.
- **Testing**: Add Play Mode tests verifying match start/respawn loops and ability consumption. The modular systems are suited for injection/mocking in the Unity Test Framework.
- **System Test Scenes**: Regenerate the dedicated system sandboxes via `Tools ▸ System Tests ▸ Generate Test Scenes`. The editor utility now scaffolds scenes with a `SystemTestSceneBootstrap` component so lighting, camera, and prefab spawns follow AAA-style isolation and instrumentation (*Game Programming Patterns*, *Real-Time Rendering*).

## 13. Quick Start Checklist
1. Assign `MasterConfig` and its nested configs in `Resources/MasterConfig.asset`.
2. Ensure player & enemy prefabs are configured with `SimplePlayerController`, `EnhancedAbilitySystem`, `NetworkObject`, and appropriate colliders/rigidbodies.
3. Add `InstantMatchStarter` to the bootstrap scene, referencing a `SimpleGameManager` prefab, and place a `ProductionNetworkManager` + `UnityTransport` alongside it to drive host/client workflows.
4. Hook UI widgets (timer, score, ability cooldowns) to the events exposed by `SimpleGameManager` and the ability systems.
5. Run the scene—match bootstraps automatically, players spawn, and abilities are usable through the configured Input Action map.

---
For deeper audits, refer to the existing analytical reports in `docs/` (e.g., `SYSTEM_AUDIT_7_EVENT_SYSTEMS.md`, `COMPREHENSIVE_CODEBASE_AUDIT_FINAL_REPORT_2025.md`). This overview stays focused on current runtime behavior and integration points.
