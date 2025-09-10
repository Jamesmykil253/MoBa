# Game Design Bible for 3D Platformer MOBA in Unity with C# and Netcode for GameObjects

This document serves as a comprehensive game design bible, drawing from authoritative sources like *Code Complete* by Steve McConnell, focusing on C# patterns, game architecture, physics, networking, and AI. It is structured into core sections: Guardrails, Mechanics, Best Practices, and Implementation. References to books and code are linked where possible.

**Cross-Validation Note**: Validated against [`NetworkGameManager.cs`](Assets/Scripts/Networking/NetworkGameManager.cs:1) and [`MOBATestScene.cs`](Assets/Scripts/MOBATestScene.cs:1). Aligns with Netcode usage, physics (Rigidbody), abilities, and testing setup. No major discrepancies; minor enhancements added.

## Guardrails: Design Principles and Constraints

Guardrails establish the foundational principles to ensure robust, maintainable code. Based on [`Code Complete`](docs/Books/code complete.pdf), key principles include:

- **Managing Complexity**: Software's Primary Technical Imperative is managing complexity through abstraction and encapsulation (Chapter 5). Use loose coupling and information hiding to minimize interdependencies.
- **Information Hiding**: Hide implementation details (e.g., physics calculations or AI states) behind interfaces to allow changes without affecting other systems (pp. 92-97).
- **Error Handling and Defensive Programming**: Use assertions and exceptions to validate inputs and states (Chapter 8). For example, check player positions to prevent invalid movements, as in connection approval in NetworkGameManager.

Constraints:
- Use Unity 6000.0.56f1 LTS.
- Multiplayer via Netcode for GameObjects (server-authoritative).
- Focus on fluid movement, ability-based combat, lane-based maps.

## Mechanics: Core Game Systems

Mechanics cover gameplay elements like movement, combat, objectives, and progression, informed by design heuristics.

- **Physics Simulations**: Use ADTs for physics objects (e.g., Rigidbody wrappers) to abstract simulations (Chapter 6). Handle fluid movement with raycasts for ground detection, as in [`MOBATestScene.cs`](Assets/Scripts/MOBATestScene.cs:1) with Rigidbody and CapsuleCollider.
- **AI Behaviors**: Employ patterns like State (for hero FSM) and Command (for abilities) from game programming patterns. Behavior trees for pathfinding and decision-making.
- **Ability-Based Combat**: Data-driven abilities via ScriptableObjects for balance. Cooldowns and effects managed in [`AbilitySystem.cs`](Assets/Scripts/AbilitySystem.cs:1).
- **Lane-Based Maps and Objectives**: Procedural generation with predefined lanes; objectives like destroying towers.

## Best Practices: Coding and Development Guidelines

Best practices ensure high-quality code, drawn from Code Complete's construction guidelines.

- **High-Quality Routines**: Keep routines short (<200 lines), focused (strong cohesion), and named descriptively (Chapter 7).
- **Refactoring**: Regularly refactor for simplicity; e.g., extract methods for repeated logic (Chapter 24).
- **Testing**: Unit tests for mechanics, integration tests for networking (Chapter 22). Use stubs for isolated testing, as in MOBATestScene's test methods.
- **Code Tuning**: Optimize after profiling; reduce loop complexity and use caching (Chapters 25-26).

## Implementation: Technical Details and Netcode Integration

Detailed implementation notes, incorporating Netcode.

- **C# Patterns**: Use Singleton for managers (e.g., [`NetworkGameManager.cs`](Assets/Scripts/Networking/NetworkGameManager.cs:1)), Factory for object pooling (projectiles in [`ProjectilePool.cs`](Assets/Scripts/ProjectilePool.cs:1)).
- **Networking with Netcode**: Server-authoritative model for synchronization. Use [NetworkBehaviour] for components, RPCs for abilities (e.g., RequestRespawnServerRpc), and NetworkVariables for state (e.g., connectedPlayers). Client prediction and reconciliation for smooth movement (cross-reference [`MOBACameraController.cs`](Assets/Scripts/MOBACameraController.cs:1) for validation).
- **Physics and AI**: Rigidbody for physics with gravity/collision; NetworkTransform for synced movement. AI pathfinding with NavMesh, synced via server.

This bible is now cross-validated and finalized. No implementation updates needed at this time.
