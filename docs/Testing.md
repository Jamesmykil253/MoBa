# Testing Strategy

## PlayMode Tests
- `Assets/Tests/PlayMode/TestMatchLifecycle.cs` runs end-to-end match lifecycle checks (host start, match start, score to win, end game) using the `MOBASceneSetup` scene.
- Additional PlayMode tests should cover ability synchronization under latency, pooling stress tests, and respawn flows.

## Running Tests
- From the Unity Test Runner, select **PlayMode** and run all tests.
- Or via CLI: `unity -projectPath <path> -runTests -testPlatform PlayMode -testResults results.xml`.

## Upcoming Work
- Add EditMode tests for configuration validation.
- Extend PlayMode coverage for network ability casting and pooling diagnostics.
