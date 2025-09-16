# MOBA Project Documentation

## 1. Overview
This repository contains a lightweight multiplayer-online-battle-arena (MOBA) prototype built on Unity's modern runtime stack. The codebase embraces the newest Input System, Netcode for GameObjects, and modular runtime systems that decouple gameplay, networking, and diagnostics. This document explains how the major systems interact, how to bootstrap a match instantly, and where to extend gameplay for production use.

> **Key namespaces**
> - `MOBA` – gameplay controllers, pooling, core infrastructure
> - `MOBA.Abilities` – ability runtime and ScriptableObject definitions
> - `MOBA.Configuration` – central configuration ScriptableObjects and validation helpers
> - `MOBA.Networking` – Netcode integration and high-level match orchestration
> - `MOBA.ErrorHandling` – global logging & validation services

## 2. Boot Flow & Match Lifecycle
1. **Startup:** Place `InstantMatchStarter` (`Assets/Scripts/InstantMatchStarter.cs`) in your bootstrap scene. The component searches for an existing `SimpleGameManager` or instantiates a prefab reference and then calls `StartMatch()` automatically (configurable via inspector).
2. **Match Initialization:** `SimpleGameManager.StartMatch()` (`Assets/Scripts/SimpleGameManager.cs`) resets the timer, clears scores, validates spawn points, then spawns configured player and enemy prefabs. When the match ends (time limit or score), `OnGameEnd` is raised for UI/network consumers.
3. **Respawn & Loop:** `SimplePlayerController` manages player death/resurrection. When defeated, control/input actions are disabled, the respawn timer (from `MasterConfig.playerConfig`) runs, and the player is restored with temporary invulnerability before gameplay resumes.

## 3. Player Systems
- **Controller:** `SimplePlayerController` (movement, combat, health) integrates the new Input System by referencing an `InputActionAsset`. Control is gated by `canControl` & `isAlive`, ensuring input is suppressed while the player is dead/respawning.
- **Movement:** Physics-driven via `Rigidbody.velocity`, with ground checks using `Physics.CheckSphere` + raycasts. Moving-platform detection and invulnerability toggles are handled internally.
- **Damage Interface:** Implements `IDamageable`, enabling both abilities and enemies to apply damage generically.

### Configuration Injection
Player stats (health, damage, movement) sync from `MasterConfig.playerConfig`. Designers update balance values in ScriptableObjects (`Assets/Scripts/Configuration/GameConfigurations.cs`) and they propagate automatically at runtime.

## 4. Ability System
- **Enhanced Ability Runtime:** `EnhancedAbilitySystem` (Assets/Scripts/Abilities) reads ability definitions (`EnhancedAbility` ScriptableObjects) and manages mana, cooldowns, combat state, and hit detection. Input actions map to ability triggers; fallback keyboard polling has been removed to ensure parity with the new Input System.
- **Simple Ability Runtime:** `SimpleAbilitySystem` remains for legacy prefabs but now also consumes `InputActionAsset` or `InputActionReference` arrays, aligning with modern input pipelines.
- **Events:** Ability casts publish `AbilityUsedEvent` through `UnifiedEventSystem`, allowing UI or analytics listeners to respond without tight coupling.

## 5. Enemy AI & NPCs
`EnemyController` mirrors player movement/damage patterns, supports manual initialization, and resets state through `Respawn()`. Hit VFX/fx use temporary primitives; consider swapping to pooled VFX for production. Enemy detection uses layer-filtered overlap spheres and verifies line-of-sight before applying damage.

## 6. Networking Stack
- **ProductionNetworkManager** controls Netcode for GameObjects connection lifecycle (host/client/server), reconnection attempts, and player tracking (`PlayerNetworkData`). Debug UI now requires integration through Unity UI or UIToolkit (legacy `OnGUI` removed).
- **SimpleNetworkManager** offers a minimal example (without built-in UI). Both managers publish connection events via `UnifiedEventSystem` for downstream systems.
- **Input Ownership:** Ensure spawned player objects include `NetworkObject` components and align with `ProductionNetworkManager` when scaling beyond local-testing scenarios.

## 7. Event & Error Handling
- **UnifiedEventSystem_Fixed** (Assets/Scripts/Core) uses weak references to prevent memory-leak issues while supporting static subscribers. Local and network channels are separated, and automatic cleanup runs periodically.
- **ErrorHandler** provides centralized logging, validation helpers, and threshold-based escalation. The runtime `ErrorHandlerMonoBehaviour` now focuses purely on subscription management—legacy `OnGUI` statistics were removed to encourage modern UI-driven dashboards.

## 8. Object Pooling & Performance Utilities
- **UnifiedObjectPool** handles pooling for GameObjects, Components, and NetworkObjects using thread-safe collections.
- **ProjectilePool/UnitFactory:** Manage domain-specific pools. Debug overlays were removed; hook into your preferred profiling UI if you need runtime stats.

## 9. Input Integration
- Global adoption of the Unity Input System:
  - `SimplePlayerController`, `EnhancedAbilitySystem`, `SimpleAbilitySystem`, and `SimpleInputHandler` all consume `InputActionAsset`/`InputActionReference` instances.
  - Suppress input when gameplay should be paused or the entity is inactive by calling `SetInputEnabled(false)` on the relevant system.

## 10. Configuration & Validation
- **ScriptableObjects**: `PlayerConfig`, `GameConfig`, `AbilitySystemConfig`, `NetworkConfig` (nested under `MasterConfig`). Each exposes `Validate()` methods employing `ErrorHandler.ValidateRange` to catch invalid data early.
- **MasterConfig**: Loaded via `Resources.Load` once and cached. Consider preloading or migrating to Addressables for larger projects.

## 11. Diagnostics & Logging
- Replaced all legacy immediate-mode UI (`OnGUI`) with stubs encouraging proper UI integration.
- `ErrorHandler` log buffering writes batched entries to `Application.persistentDataPath`. Rate limiting prevents disk contention.
- `PerformanceProfiler` (if enabled) and other utility scripts can be extended to capture metrics—tie them into the event system for improved observability.

## 12. Extending the Project
- **UI/UX**: Build UIToolkit or UGUI dashboards to consume events from `SimpleGameManager`, `ProductionNetworkManager`, and `EnhancedAbilitySystem`.
- **Networking**: Implement transport RTT queries in `ProductionNetworkManager.UpdatePing()` (marked with TODO-style comments) and broadcast match state over RPCs/NetworkVariables.
- **Abilities**: Extend `EnhancedAbility` ScriptableObject with new effect types or integrate client-side prediction by subscribing to `AbilityUsedEvent` and spawning VFX locally.
- **Testing**: Add Play Mode tests verifying match start/respawn loops and ability consumption. The modular systems are suited for injection/mocking in the Unity Test Framework.

## 13. Quick Start Checklist
1. Assign `MasterConfig` and its nested configs in `Resources/MasterConfig.asset`.
2. Ensure player & enemy prefabs are configured with `SimplePlayerController`, `EnhancedAbilitySystem`, `NetworkObject`, and appropriate colliders/rigidbodies.
3. Add `InstantMatchStarter` to the bootstrap scene, referencing a `SimpleGameManager` prefab.
4. Hook UI widgets (timer, score, ability cooldowns) to the events exposed by `SimpleGameManager` and the ability systems.
5. Run the scene—match bootstraps automatically, players spawn, and abilities are usable through the configured Input Action map.

---
For deeper audits, refer to the existing analytical reports in `docs/` (e.g., `SYSTEM_AUDIT_7_EVENT_SYSTEMS.md`, `COMPREHENSIVE_CODEBASE_AUDIT_FINAL_REPORT_2025.md`). This overview stays focused on current runtime behavior and integration points.
