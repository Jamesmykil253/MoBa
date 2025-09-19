# Network Soak & Latency Test Strategy

## Goals
- Exercise `ProductionNetworkManager` anti-cheat thresholds under sustained player movement spam.
- Measure `EnhancedAbilitySystem` latency (cast request → authoritative approval) across high jitter scenarios.
- Surface regressions automatically on CI via headless PlayMode suites.

## Scenarios
1. **Authority Flood**
   - Spawn host + N clients (2–6) using NGO `UnityTransport` in loopback.
   - Script clients to send high-frequency position updates beyond configured limits.
   - Assert: `ProductionNetworkManager` increments suspicion counters and kicks after `maxSuspicionBeforeKick`.
2. **Ability Burst**
   - Host + client.
   - Client fires abilities back-to-back with simulated RTT (use Unity Transport simulator).
   - Record delta between `RequestAbilityCast` and `AbilityCastResult` receipt. Fail when > configurable ceiling.
3. **Reconnect Storm**
   - Host + client.
   - Client repeatedly disconnects/reconnects inside cooldown window.
   - Assert: `NetworkErrorCode.RapidReconnect` raised and automatic reconnection halted.

## Tooling
- New PlayMode test suite (`MOBA.Tests.PlayMode.NetworkSoak`) driving NGO mock clients via `NetworkManager.Singleton` in headless mode.
- Wrap critical measurements in `PerformanceProfiler` for automatic overlay export.
- Parameterise test intensity via `ScriptableObject` to match build/perf budgets.

## Metrics & Reporting
- Average/peak latency (ms) per ability cast.
- Suspicion events per minute until kick.
- Memory and CPU telemetry via `PerformanceProfiler` snapshot logging to disk.

## Next Steps
1. Implement reusable `NetworkTestHarness` fixture that spins up host/client pairs with deterministic prefabs.
2. Author soak scripts matching scenarios above; integrate with CI lane flagged `network-soak`.
3. Persist summary JSON in `Logs/SoakTests` for per-commit analysis.

## Automation
- `Assets/Tests/PlayMode/NetworkSoakTests.cs` derives from `NetcodeIntegrationTest` to orchestrate host and multi-client sessions for each scenario.
- `ProductionNetworkManager.AllowMultipleInstancesForTesting` toggles enable in-process client managers for automated validation.
- Latency samples leverage `PerformanceProfiler` snapshots surfaced via `PerformanceOverlay` for live inspection.
