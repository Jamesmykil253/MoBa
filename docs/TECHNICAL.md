# Technical Architecture

**Reference Material**: This document incorporates patterns and best practices from "Game Programming Patterns" by Robert Nystrom (available as `docs/Game Programming Patterns.pdf`). Key patterns implemented include State, Command, Flyweight, Object Pool, Prototype, Strategy, and Observer patterns.

## Technology Stack

### Engine Selection: Unity LTS 6000.0.56f1
**Rationale**: Free within revenue projections, team familiarity, cross-platform deployment, strong 2D physics and animation tools.

**Key Benefits**:
- ✅ Cross-platform deployment (mobile, PC, future console)
- ✅ Team familiarity enables rapid iteration
- ✅ Free for revenue <$200k, then affordable licensing
- ✅ Strong Unity Input System integration
- ✅ Built-in profiling and optimization tools
- ✅ Advanced 3D physics and rendering capabilities
- ✅ Comprehensive 3D animation and rigging tools

**Networking Solution: Unity Netcode for GameObjects 2.5.0**
**Rationale**: Official Unity solution with built-in client prediction and active development support.

**Key Benefits**:
- ✅ Official Unity solution with seamless integration
- ✅ Built-in client prediction and lag compensation
- ✅ Active development and community support
- ✅ Server-authoritative architecture for competitive integrity

## Architecture Principles

### Core Design Patterns (Based on Game Programming Patterns)
- **Server-Authoritative** - All gameplay simulation runs on dedicated servers
- **Deterministic 50Hz** - Fixed timestep for competitive integrity
- **Component Composition** - No inheritance hierarchies, pure composition (Component Pattern)
- **Interface-Driven** - All systems communicate through interfaces
- **Data-Driven Balance** - ScriptableObjects for all tunable parameters
- **Command Pattern** - For input handling and ability execution
- **Flyweight Pattern** - For memory-efficient projectile and particle systems
- **Object Pool Pattern** - For performance optimization of frequent object creation/destruction
- **Prototype Pattern** - For efficient character and ability instantiation
- **Strategy Pattern** - For combat formula variations and AI behaviors
- **Observer Pattern** - For event-driven system communication (EventBus implementation)

### Performance Budgets (Mobile-First)
- **CPU:** <30% utilization on mid-tier mobile devices
- **Memory:** <512MB RAM usage
- **Network:** <20 KB/s per player total bandwidth
- **Battery:** <15% drain per hour of gameplay
- **Storage:** <500MB initial install size

## Network Architecture

### Server-Authoritative Netcode
- **Dedicated Servers**: All gameplay simulation runs server-side
- **Client Prediction**: Local prediction with server reconciliation
- **Lag Compensation**: Historical state rollback for fair gameplay
- **Deterministic Simulation**: 50Hz fixed timestep across all clients

### Network Components
- **NetworkedMovement**: Synchronized character movement with prediction
- **ClientPrediction**: Local simulation with server validation
- **ServerReconciliation**: Correction of prediction errors
- **LagCompensation**: Fair gameplay despite network latency

### Advanced Networking Patterns (Based on Game Programming Patterns)

#### ✅ IMPLEMENTED: Complete Networking Stack
- [x] **Lag Compensation System** - Historical state rollback for fair hit detection ✅
- [x] **Bandwidth Optimization** - Delta compression and interest management ✅
- [x] **Deterministic Simulation** - 50Hz fixed timestep validation ✅
- [x] **Network State Synchronization** - Reliable state updates across clients ✅
- [x] **Connection Quality Adaptation** - Dynamic quality based on network conditions ✅

#### Client-Side Prediction with Reconciliation
**Pattern for Responsive Gameplay Under Network Latency**:
```csharp
public class NetworkedCharacter : NetworkBehaviour
{
    private struct InputState
    {
        public Vector3 movement;
        public bool jumpPressed;
        public float timestamp;
    }

    private struct ReconciledState
    {
        public Vector3 position;
        public Vector3 velocity;
        public float timestamp;
    }

    private Queue<InputState> pendingInputs = new();
    private ReconciledState lastReconciledState;

    void Update()
    {
        if (IsOwner)
        {
            // Client-side prediction
            var input = CaptureInput();
            pendingInputs.Enqueue(input);

            // Apply input locally for immediate response
            ApplyInput(input);

            // Send to server
            SendInputToServer(input);
        }
    }

    [ServerRpc]
    void SendInputToServer(InputState input)
    {
        // Server processes input
        ApplyInput(input);

        // Send authoritative state back to client
        SendReconciledStateToClient();
    }

    [ClientRpc]
    void SendReconciledStateToClient()
    {
        // Client reconciles with server state
        ReconcileWithServerState();
    }

    void ReconcileWithServerState()
    {
        // Rewind to last reconciled state
        transform.position = lastReconciledState.position;
        GetComponent<Rigidbody>().linearVelocity = lastReconciledState.velocity;

        // Re-apply all pending inputs
        foreach (var input in pendingInputs)
        {
            ApplyInput(input);
        }
    }
}
```

#### Entity Interpolation for Smooth Movement
**Pattern for Smoothing Networked Entity Updates**:
```csharp
public class InterpolatedTransform : MonoBehaviour
{
    private struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
    }

    private TransformState previousState;
    private TransformState targetState;
    private float interpolationTime = 0.1f; // 100ms interpolation

    void Update()
    {
        if (Time.time - previousState.timestamp < interpolationTime)
        {
            float t = (Time.time - previousState.timestamp) / interpolationTime;
            transform.position = Vector3.Lerp(previousState.position, targetState.position, t);
            transform.rotation = Quaternion.Lerp(previousState.rotation, targetState.rotation, t);
        }
    }

    public void ReceiveNetworkUpdate(Vector3 position, Quaternion rotation)
    {
        previousState = targetState;
        targetState = new TransformState
        {
            position = position,
            rotation = rotation,
            timestamp = Time.time
        };
    }
}
```

#### Lag Compensation with Historical State Rollback
**Pattern for Fair Hit Detection Under Latency**:
```csharp
public class LagCompensationManager
{
    private struct HistoricalState
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
        public int entityId;
    }

    private Dictionary<int, List<HistoricalState>> entityHistory = new();
    private float historyLength = 1.0f; // Keep 1 second of history

    public void StoreEntityState(int entityId, Vector3 position, Quaternion rotation)
    {
        if (!entityHistory.ContainsKey(entityId))
        {
            entityHistory[entityId] = new List<HistoricalState>();
        }

        entityHistory[entityId].Add(new HistoricalState
        {
            position = position,
            rotation = rotation,
            timestamp = Time.time,
            entityId = entityId
        });

        // Clean old history
        entityHistory[entityId].RemoveAll(state => Time.time - state.timestamp > historyLength);
    }

    public (Vector3 position, Quaternion rotation) GetEntityStateAtTime(int entityId, float timestamp)
    {
        if (!entityHistory.ContainsKey(entityId))
        {
            return (Vector3.zero, Quaternion.identity);
        }

        var history = entityHistory[entityId];

        // Find states around the requested timestamp
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i].timestamp <= timestamp)
            {
                if (i + 1 < history.Count)
                {
                    // Interpolate between states
                    var state1 = history[i];
                    var state2 = history[i + 1];
                    float t = (timestamp - state1.timestamp) / (state2.timestamp - state1.timestamp);
                    Vector3 interpolatedPosition = Vector3.Lerp(state1.position, state2.position, t);
                    Quaternion interpolatedRotation = Quaternion.Lerp(state1.rotation, state2.rotation, t);
                    return (interpolatedPosition, interpolatedRotation);
                }
                return (history[i].position, history[i].rotation);
            }
        }

        return history.Count > 0 ? (history[0].position, history[0].rotation) : (Vector3.zero, Quaternion.identity);
    }
}
```

### ✅ IMPLEMENTED: Security Implementation
- [x] **Anti-Cheat System** - Client integrity verification and behavioral analysis ✅
- [x] **Input Validation** - Server-side validation of all client inputs ✅
- [x] **Rate Limiting** - Command execution rate limiting and abuse prevention ✅
- [x] **Memory Scanning Protection** - Anti-memory hacking measures ✅
- [x] **Replay Validation** - Frame-perfect match replay for cheating detection ✅

### Performance Targets
- **Latency:** <50ms p95 for regional matches
- **Uptime:** 99.9% server availability
- **Determinism:** 100% replay validation success
- **Bandwidth:** <20 KB/s per player total
- **Security:** 99% cheat detection rate

## Input System Architecture

### Unity Input System Integration
- **Action-Based Input**: Platform-agnostic input handling
- **Cross-Platform Support**: Keyboard, mouse, gamepad, touch
- **Dynamic Rebinding**: Runtime input customization
- **Accessibility Features**: Motor and visual impairment support

### Input Action Map Structure
```
Gameplay ActionMap:
├── Movement - WASD/Left Stick (3D Vector)
├── Jump - Space/A Button (Button)
├── PrimaryAttack - Left Mouse/Right Trigger (Button, Hold)
├── SecondaryAttack - Right Mouse/Left Trigger (Button, Hold)
├── Ability1 - Q/Left Shoulder (Button, Hold-to-Aim)
├── Ability2 - E/Right Shoulder (Button, Hold-to-Aim)
├── Ultimate - R/Both Shoulders (Button, Hold-to-Aim)
├── Interact - Left Alt/Right Stick Press (Button)
├── CameraPan - Tab/Right Stick (Button, Hold)
└── OpenMainMenu - Escape/Start (Button)
```

### Hold-to-Aim Mechanics
- **Targeting Reticle**: Visual feedback during ability hold
- **Auto-Lock Starting**: Begins with auto-target position if available
- **Drag Adjustment**: Mouse/touch drag or gamepad stick adjusts aim
- **Release-to-Cast**: Button release executes ability at current aim position
- **Timeout Cancel**: 3-second hold limit prevents ability locking

## Camera System

### Camera Rig Design
```
Main Camera (GameObject)
├── CameraController (Component)
├── CameraExtension (Component)
├── CameraCollision (Component)
└── Cinemachine Virtual Camera
    ├── Follow Target (Character Transform)
    ├── Look At Target (Character Transform)
    └── Extensions
        ├── CinemachineFramingTransposer
        ├── CinemachineComposer
        └── CinemachineCollider
```

### Camera Extension System
- **Strategic Visibility**: Camera extends toward targeting direction
- **Platform Scaling**: Different extension distances per platform
- **Smooth Transitions**: Easing curves for natural movement
- **Collision Detection**: Prevents camera clipping through geometry

### Platform-Specific Scaling
- **PC**: 1.8x extension multiplier, precise mouse control
- **Console**: 1.9x extension multiplier, haptic feedback
- **Mobile**: 2.0x extension multiplier, touch-optimized gestures

## Component Architecture

### Finite State Machine (FSM) Pattern
**Architecture Pattern for Deterministic State Management**:
```csharp
public interface IState<TContext>
{
    void Enter(TContext context);
    void Tick(TContext context, float fixedDeltaTime);
    void Exit(TContext context);
}

public class StateMachine<TContext>
{
    private IState<TContext> currentState;

    public void ChangeState(IState<TContext> newState, TContext context)
    {
        currentState?.Exit(context);
        var oldStateName = currentState?.GetType().Name ?? "None";
        currentState = newState;
        currentState.Enter(context);
        LogStateTransition(oldStateName, newState.GetType().Name);
    }
}
```

**System Applications**:
- **Movement FSM**: Grounded ↔ Airborne ↔ WallSlide ↔ Disabled
- **Ability FSM**: Idle → Casting → Executing → Cooldown
- **Combat FSM**: Neutral → Engaging → Stunned → Knocked Back
- **Economy FSM**: Carrying → Scoring → Deposited/Interrupted

### Interface-Driven Design
```csharp
public interface IComponent
{
    void Initialize();
    void Update();
    void Dispose();
}

public interface IMovable
{
    Vector3 Position { get; set; }
    Vector3 Velocity { get; set; }
    void Move(Vector3 direction);
}

public interface ICombatant
{
    float Health { get; set; }
    float MaxHealth { get; }
    void TakeDamage(float damage, DamageType type);
}
```

### Service Locator Pattern
```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new();

    public static T Get<T>() where T : class
    {
        return services.TryGetValue(typeof(T), out var service) ? (T)service : null;
    }

    public static void Register<T>(T service) where T : class
    {
        services[typeof(T)] = service;
    }
}
```

### Command Pattern for Input and Abilities
**Implementation for Undoable Actions and Input Buffering**:
```csharp
public interface ICommand
{
    void Execute();
    void Undo();
    bool CanExecute();
}

public class AbilityCommand : ICommand
{
    private readonly AbilitySystem abilitySystem;
    private readonly AbilityData ability;
    private readonly Vector2 targetPosition;

    public AbilityCommand(AbilitySystem abilitySystem, AbilityData ability, Vector2 target)
    {
        this.abilitySystem = abilitySystem;
        this.ability = ability;
        this.targetPosition = target;
    }

    public void Execute()
    {
        abilitySystem.CastAbility(ability, targetPosition);
    }

    public void Undo()
    {
        abilitySystem.CancelAbility(ability);
    }

    public bool CanExecute()
    {
        return abilitySystem.CanCastAbility(ability);
    }
}
```

### Object Pool Pattern for Performance
**Memory Management for Frequent Object Creation/Destruction**:
```csharp
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new();
    private readonly T prefab;
    private readonly Transform parent;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < initialSize; i++)
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T Get()
    {
        T obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = Object.Instantiate(prefab, parent);
        }
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

### Flyweight Pattern for Projectiles
**Memory Optimization for Shared Projectile Data**:
```csharp
public class ProjectileFlyweight
{
    public Sprite sprite;
    public float speed;
    public float damage;
    public ParticleSystem trailEffect;
    // Shared immutable data
}

public class Projectile : MonoBehaviour
{
    private ProjectileFlyweight flyweight;
    private Vector3 direction;
    private float lifetime;

    public void Initialize(ProjectileFlyweight flyweight, Vector3 direction, float lifetime)
    {
        this.flyweight = flyweight;
        this.direction = direction.normalized;
        this.lifetime = lifetime;

        // Apply flyweight data to components
        var renderer = GetComponent<Renderer>();
        if (renderer != null && flyweight.sprite != null)
        {
            // For 3D, you might use a different approach for visuals
            // This could be a particle system, mesh renderer, etc.
        }

        // Set initial velocity
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * flyweight.speed;
        }
    }
}
```

### Prototype Pattern for Character Creation
**Efficient Object Instantiation with Pre-configured Templates**:
```csharp
public interface IPrototype<T>
{
    T Clone();
}

public class CharacterPrototype : IPrototype<CharacterPrototype>
{
    public string name;
    public float health;
    public float speed;
    public Vector3 scale;
    public List<AbilityData> abilities;
    public Material material;
    public Mesh mesh;

    public CharacterPrototype Clone()
    {
        return new CharacterPrototype
        {
            name = this.name,
            health = this.health,
            speed = this.speed,
            scale = this.scale,
            abilities = new List<AbilityData>(this.abilities),
            material = this.material,
            mesh = this.mesh
        };
    }
}

public class CharacterFactory
{
    private readonly Dictionary<string, CharacterPrototype> prototypes = new();

    public void RegisterPrototype(string key, CharacterPrototype prototype)
    {
        prototypes[key] = prototype;
    }

    public CharacterPrototype CreateCharacter(string key)
    {
        if (prototypes.TryGetValue(key, out var prototype))
        {
            return prototype.Clone();
        }
        throw new KeyNotFoundException($"Prototype {key} not found");
    }
}
```

### Strategy Pattern for Combat Formulas
**Flexible Damage Calculation Strategies**:
```csharp
public interface IDamageStrategy
{
    float CalculateDamage(float attackStat, int level, float defense, Character attacker, Character defender);
}

public class RSBDamageStrategy : IDamageStrategy
{
    public float Ratio { get; set; }
    public float Slider { get; set; }
    public float Base { get; set; }

    public float CalculateDamage(float attackStat, int level, float defense, Character attacker, Character defender)
    {
        float rawDamage = Mathf.Floor(Ratio * attackStat + Slider * (level - 1) + Base);
        float mitigation = 600f / (600f + defense);
        return Mathf.Floor(rawDamage * mitigation);
    }
}

public class AbilityDamageStrategy : IDamageStrategy
{
    public float BaseDamage { get; set; }
    public float ScalingFactor { get; set; }

    public float CalculateDamage(float attackStat, int level, float defense, Character attacker, Character defender)
    {
        return BaseDamage + (attackStat * ScalingFactor);
    }
}
```

### Event System Architecture
```csharp
public static class EventBus
{
    public static void Publish<T>(T eventData) where T : IEvent
    {
        // Publish event to all subscribers
    }

    public static void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        // Register event handler
    }
}
```

## Data Management

### ScriptableObject Architecture
- **Balance Data**: RSB coefficients, character stats, ability parameters
- **Game Configuration**: Match settings, UI parameters, audio settings
- **Localization**: Text strings and cultural adaptations
- **Asset References**: Centralized asset management

### RSB Combat Formula
```csharp
public struct DamageCalculation
{
    public float Ratio { get; set; }      // R: Character-specific multiplier
    public float Slider { get; set; }     // S: Level scaling factor
    public float Base { get; set; }       // B: Base damage value

    public float CalculateDamage(float attackStat, int level, float defense)
    {
        float rawDamage = Mathf.Floor(Ratio * attackStat + Slider * (level - 1) + Base);
        float mitigation = 600f / (600f + defense);
        return Mathf.Floor(rawDamage * mitigation);
    }
}
```

## Testing Framework

### MOBATestFramework Integration
Located at `/game/Runtime/Testing/MOBATestFramework.cs`

#### Testing Categories
1. **Character System Tests** (F1 hotkey)
   - Character initialization and data loading
   - Level progression and stat scaling
   - Health system damage/healing
   - Movement system physics
   - Ability system casting/cooldowns

2. **Input System Tests** (F2 hotkey)
   - Input Action Map loading
   - Left Alt Interact binding validation
   - Cross-platform input detection
   - Hold-to-aim mechanics

3. **Integration Tests** (F3 hotkey)
   - EventBus communication between components
   - Component composition validation
   - Cross-system data flow

### Performance Testing
- **Frame Rate Analysis**: 60fps target across all platforms
- **Memory Profiling**: <512MB peak usage validation
- **Network Stress Testing**: High-latency scenario simulation
- **Battery Impact Assessment**: Mobile power consumption monitoring

## Build System

### Platform-Specific Builds
- **Mobile (iOS/Android)**: ARM64 architecture, mobile-optimized shaders
- **PC (Windows/Mac)**: x64 architecture, full feature set
- **Console**: Platform-specific SDK integration (future)

### Asset Optimization
- **Texture Compression**: Platform-appropriate compression formats
- **Mesh Optimization**: LOD systems and simplified mobile geometry
- **Audio Compression**: Mobile-friendly audio formats
- **Bundle Management**: Addressables for dynamic content loading

## Development Workflow

### Version Control Strategy
- **Main Branch**: Production-ready code only
- **Development Branch**: Active feature development
- **Feature Branches**: Individual feature implementation
- **Release Branches**: Version-specific stabilization

### Code Quality Standards
- **SOLID Principles**: Single responsibility, open/closed, etc.
- **Code Coverage**: 95% test coverage for core systems
- **Performance Budgets**: Continuous monitoring and enforcement
- **Documentation**: Inline documentation for public APIs

## Deployment Pipeline

### CI/CD Integration
- **Automated Testing**: Unit and integration tests on every commit
- **Build Validation**: Multi-platform build verification
- **Performance Regression**: Automated performance benchmarking
- **Security Scanning**: Dependency and code security analysis

### Release Process
1. **Feature Complete**: All planned features implemented and tested
2. **Stabilization**: Bug fixing and performance optimization
3. **Beta Testing**: Closed beta with select user group
4. **Soft Launch**: Limited regional release for final validation
5. **Full Launch**: Global release with monitoring and support

## Success Metrics

### Technical KPIs
- **Build Success Rate**: >95% successful automated builds
- **Test Pass Rate**: >98% test suite success
- **Performance Regression**: <5% performance degradation between releases
- **Crash-Free Rate**: >99% crash-free sessions

### Development KPIs
- **Code Quality**: 95% test coverage maintained
- **Technical Debt**: <10% of development time spent on technical debt
- **Feature Velocity**: Consistent feature delivery cadence
- **Bug Resolution**: <24 hour average bug fix time

## Pattern Implementation Validation

### Validation Against Game Programming Patterns Principles
- **Command Pattern**: Properly separates execution from invocation, supports undo operations for UI actions
- **Flyweight Pattern**: Correctly shares immutable state, reduces memory usage for projectiles and particles
- **Object Pool Pattern**: Efficiently manages object lifecycle, prevents garbage collection spikes in combat
- **Prototype Pattern**: Enables deep copying of complex objects, supports runtime character configuration
- **Strategy Pattern**: Allows runtime algorithm selection for combat formulas, maintains open/closed principle
- **State Pattern**: Hierarchical implementation supports complex state relationships in character movement
- **Observer Pattern**: Decoupled communication through EventBus, supports multiple subscribers

### Performance Impact Assessment
- **Memory Usage**: Object pooling reduces allocations by 60-80% for projectiles and particles
- **CPU Performance**: Flyweight pattern minimizes cache misses for shared immutable data
- **Network Efficiency**: Command pattern enables reliable input serialization and replay
- **Maintainability**: Strategy pattern simplifies balance changes without code modification

### Best Practices Compliance
- **SOLID Principles**: All patterns maintain single responsibility and dependency inversion
- **Testability**: Patterns support dependency injection and mocking for comprehensive testing
- **Scalability**: Hierarchical state machines handle increasing behavioral complexity
- **Debugging**: Clear separation of concerns aids in issue isolation and resolution