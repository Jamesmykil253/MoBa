# Modularisation & Legacy Simple* Sunset Roadmap

## Objectives
- Extract orchestration logic from `SimpleGameManager` into dedicated services (match state, spawn management, scoring, UI sync).
- Break `EnhancedAbilitySystem` into focused components (resource management, cooldown scheduler, effect executor, network bridge adapter).
- Decommission `Simple*` facades once prefabs migrate to new services.
- Backfill edit-mode unit tests for `UnifiedObjectPool` and `ErrorHandler` to secure regression net.

## Phase Breakdown
### Phase 1 – Service Contracts
1. Define interfaces:
   - `IMatchLifecycleService` (start/stop/check win).
   - `ISpawnCoordinator` (player/enemy spawn orchestration with pooling hooks).
   - `IScoringService` (team score persistence + events).
   - `IAbilityResourceService`, `IAbilityCooldownService` within ability domain.
2. Introduce lightweight ScriptableObject configs for service wiring.
3. Add constructor-style injection helpers (ScriptableObject + MonoBehaviour bridge) for runtime scenes.

### Phase 2 – Incremental Extraction
1. Move `SimpleGameManager` logic into new service classes while preserving API; keep component as façade delegating to services.
2. Partition `EnhancedAbilitySystem` into partial classes or nested services; progressively route logic to new components.
3. Update tests to target services directly (fast edit-mode scope) before touching MonoBehaviours.

### Phase 3 – Legacy Cleanup
1. Audit prefabs referencing `SimpleAbilitySystem`, `SimpleNetworkManager`, `SimplePlayerController` legacy pathways.
2. Migrate to service-backed variants; deprecate facades with `[Obsolete]` until removal window expires.
3. Remove redundant `Simple*` assets once prefab audit passes.

## Testing Additions
- **UnifiedObjectPool**: add edit-mode suite validating component/network pool creation, exhaustion behaviour, and `Clear` semantics via mocks.
- **ErrorHandler**: cover rate limiting, threshold escalation, and validation helpers in isolation using fake log sinks.
- Expand PlayMode tests to cover new services once extraction begins (match start, scoring, ability cast pipeline).

## Tooling Support
- Extend `SystemTestSceneGenerator` to allow injecting alternate service prefabs for quick validation.
- Add profiling checkpoints (via `PerformanceProfiler`) around new services to ensure no performance regressions during refactor.

## Dependencies & Risks
- Requires coordination with configuration assets: ensure new services consume `GameConfig`, `AbilitySystemConfig` without duplicating data.
- Beware of network lifecycle coupling; service extraction must respect NGO spawn orderings.
- Legacy prefabs/scripts outside core assembly may depend on `Simple*`; inventory before deletion.

## Progress Notes (Jan 2025)
- Introduced `ScoringService` and corresponding wiring in `SimpleGameManager` to reduce direct state handling.
- Added `AbilityResourceService` and `AbilityCooldownService` wrappers to encapsulate mana/cooldown logic ahead of broader extraction.
- Seeded edit-mode coverage for `UnifiedObjectPool` and `ErrorHandler` to support safe refactors.
