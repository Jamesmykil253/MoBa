# MOBA Testing Framework

## Overview

The MOBA Testing Framework provides comprehensive testing infrastructure for the Meme Online Battle Arena project. Built on Unity Test Framework and NUnit, it supports unit tests, integration tests, performance tests, and network tests.

## Framework Components

### Core Framework
- **MOBATestFramework.cs** - Base test framework with setup/teardown, test helpers, and assertions
- **MOBANetworkTestBase.cs** - Specialized base for network testing
- **MOBAPerformanceTestBase.cs** - Performance testing with baseline comparisons
- **MOBATestSuiteManager.cs** - Test discovery, execution, and reporting

### Test Suites
- **EndToEndSystemIntegrationTests.cs** - Complete system integration testing
- **RSBCombatSystemTests.cs** - Risk-Skill-Balance combat formula validation
- **NetworkingSystemIntegrationTests.cs** - Network component testing
- **CharacterSystemIntegrationTests.cs** - Character system validation (placeholder)
- **CrossSystemPerformanceIntegrationTests.cs** - Performance benchmarks (placeholder)

## Quick Start

### Running Tests in Unity

1. Open Unity Test Runner: `Window > General > Test Runner`
2. Select "PlayMode" tab for integration tests
3. Click "Run All" to execute the complete test suite
4. View results in the Test Runner window

### Running Specific Test Categories

```csharp
// Run all RSB Combat tests
[TestFixture]
public class RSBCombatSystemTests : MOBATestFramework

// Run all Network tests  
[TestFixture]
public class NetworkingSystemIntegrationTests : MOBANetworkTestBase

// Run performance tests
[TestFixture]
public class PerformanceTests : MOBAPerformanceTestBase
```

### Using Test Helpers

```csharp
[Test]
public void ExampleTest()
{
    // Create test player
    var player = CreateTestPlayer(Vector3.zero);
    
    // Assert vector equality with tolerance
    AssertVector3Equal(expected, actual, 0.1f);
    
    // Assert health within range
    AssertHealthInRange(playerHealth, 0f, 100f);
    
    // Performance assertion
    AssertPerformance(() => SomeOperation(), 0.01f, "Operation description");
}
```

## Test Categories

### 1. Unit Tests
- Individual component functionality
- Method-level validation
- Isolated system testing

### 2. Integration Tests
- System interaction validation
- Component communication testing
- Cross-system functionality

### 3. Performance Tests
- Execution time benchmarks
- Memory usage validation
- Performance regression detection

### 4. Network Tests
- Network component instantiation
- Basic network functionality
- Connection and synchronization

## Performance Baselines

The framework includes performance baselines for critical operations:

| Operation | Baseline | Description |
|-----------|----------|-------------|
| PlayerSpawn | 10ms | Player instantiation time |
| AbilityCast | 5ms | Ability execution time |
| CombatCalculation | 2ms | RSB damage calculation |
| StateTransition | 1ms | State machine transitions |
| NetworkUpdate | 16ms | Network synchronization (60fps) |

## Test Writing Guidelines

### Test Structure
```csharp
[Test]
public void ComponentName_Behavior_ExpectedResult()
{
    // Arrange - Set up test conditions
    var component = CreateTestComponent();
    
    // Act - Execute the behavior being tested
    var result = component.DoSomething();
    
    // Assert - Verify expected results
    Assert.AreEqual(expectedValue, result);
}
```

### Naming Conventions
- Test methods: `ComponentName_Behavior_ExpectedResult`
- Test classes: `ComponentNameTests`
- Test objects: `testComponentName`

### Best Practices
1. **Isolation** - Each test should be independent
2. **Cleanup** - Always clean up test objects in TearDown
3. **Descriptive** - Test names should clearly describe what's being tested
4. **Fast** - Keep tests under performance baselines
5. **Reliable** - Tests should pass consistently

## Assembly Configuration

The testing framework uses the `MOBA.Testing.asmdef` assembly definition:

```json
{
    "name": "MOBA.Testing",
    "references": ["MOBA.Runtime"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

## Integration with CI/CD

The testing framework is designed for integration with continuous integration:

```bash
# Command line test execution
Unity -batchmode -runTests -testPlatform PlayMode -testResults results.xml
```

## Troubleshooting

### Common Issues

1. **"Assembly not found"** - Ensure MOBA.Testing.asmdef is properly configured
2. **"Test not discovered"** - Check TestFixture attribute and namespace
3. **"Setup failed"** - Verify all required components are available
4. **"Performance test failed"** - Check if operation exceeds baseline

### Debug Logging

Enable detailed test logging:
```csharp
Debug.Log($"[TestName] Test state: {description}");
```

## Extending the Framework

### Adding New Test Categories

1. Create new test class inheriting from appropriate base:
   ```csharp
   public class MySystemTests : MOBATestFramework
   ```

2. Add test methods with proper attributes:
   ```csharp
   [Test]
   public void MySystem_DoesExpectedBehavior()
   ```

3. Register with test discovery (automatic via TestFixture attribute)

### Custom Test Helpers

Add helper methods to base classes:
```csharp
protected MyComponent CreateTestMyComponent()
{
    var obj = new GameObject("TestMyComponent");
    return obj.AddComponent<MyComponent>();
}
```

## Results and Reporting

The `MOBATestSuiteManager` provides comprehensive test reporting:

```csharp
// Run all tests programmatically
var results = MOBATestSuiteManager.RunAllTests();

// Get summary report
string summary = MOBATestSuiteManager.GetTestSummary();
```

## Next Steps

1. **Implement remaining test suites** - Complete placeholder test files
2. **Add UI tests** - Test user interface components
3. **Network integration tests** - Full multiplayer scenario testing
4. **Performance profiling** - Deep performance analysis
5. **Automated test execution** - CI/CD pipeline integration

---

**Status**: ✅ **FRAMEWORK IMPLEMENTED**  
**Coverage**: Core systems, RSB Combat, Network components  
**Priority**: Continue with scene generation and prefab creation testing