# MOBA Scene Generator

## Overview

The MOBA Scene Generator is a comprehensive automated system that creates complete scene hierarchies with all necessary components, prefabs, and systems required for the MOBA project. This tool eliminates manual setup and ensures consistent, production-ready scene configurations.

## Features

### ✅ Complete Scene Generation
- **Network Systems**: NetworkManager, NetworkSystemIntegration, NetworkTestSetup
- **Gameplay Systems**: CommandManager, AbilitySystem, FlyweightFactory, ProjectilePool
- **UI Systems**: Canvas, HUD elements, Network test controls
- **Environment**: Ground, test targets, scoring zones, spawn points
- **Test Player**: Fully configured player with all components
- **Auto-Configuration**: Automatic component reference linking

### ✅ Modular Generation
- Generate individual systems independently
- Mix and match components as needed
- Flexible configuration options

### ✅ Production Ready
- Zero compilation errors
- Complete component integration
- Proper hierarchy organization
- Performance optimized

## Quick Start

### Method 1: One-Click Setup

Use the Unity menu for instant setup:

```
MOBA > Quick Setup > Complete Demo Scene
```

This creates a full testing environment in seconds.

### Method 2: Advanced Setup

For detailed configuration:

```
MOBA > Generate Complete Scene
```

This opens the generator window with options to customize which systems to include.

## Menu Options

### Quick Setup Menu (`MOBA > Quick Setup`)

1. **Complete Demo Scene**
   - Creates all systems, UI, environment, and test player
   - Ready for immediate testing
   - Includes networking and gameplay systems

2. **Network Testing Only**
   - Creates NetworkManager, integration, and test setup
   - Perfect for multiplayer testing
   - Lightweight setup

3. **Gameplay Systems Only**
   - Creates core gameplay systems without networking
   - Good for single-player testing
   - Includes abilities, commands, projectiles

4. **Clear Scene**
   - Removes all objects except Main Camera and Directional Light
   - Safe scene cleanup

### Advanced Setup (`MOBA > Generate Complete Scene`)

Opens the MOBASceneGenerator window with options:

- ☑️ **Network Systems**: Full multiplayer infrastructure
- ☑️ **Gameplay Systems**: Core MOBA mechanics
- ☑️ **UI System**: HUD and network controls
- ☑️ **Environment**: Test environment and objects
- ☑️ **Test Player**: Configured player character
- ☑️ **Auto-Configure References**: Automatic component linking

## Generated Hierarchy

When using Complete Demo Scene, the following hierarchy is created:

```
Scene Root
├── MOBA_SceneSetup (MOBASceneSetup, MOBATestScene)
├── NetworkManager (NetworkManager, UnityTransport, NetworkGameManager)
├── NetworkSystemIntegration (NetworkSystemIntegration)
├── NetworkTestSetup (NetworkTestSetup)
├── CommandManager (CommandManager)
├── AbilitySystem (AbilitySystem)
├── FlyweightFactory (FlyweightFactory)
├── ProjectilePool (ProjectilePool)
├── EventBus (EventBusMarker - static class placeholder)
├── InputSystem (PlayerInput)
├── EventSystem (EventSystem, StandaloneInputModule)
├── MainUICanvas (Canvas, CanvasScaler, GraphicRaycaster)
│   ├── StatusDisplay (TextMeshProUGUI)
│   ├── ControlsDisplay (TextMeshProUGUI)
│   └── DebugDisplay (TextMeshProUGUI)
├── NetworkTestUI (NetworkTestSetup, UI Controls)
├── Ground (Cube with physics)
├── TestTarget (Cube with TestTargetHealth)
├── ScoringZone (Cylinder trigger)
├── SpawnPoints (8 spawn points in circle)
│   ├── SpawnPoint_0
│   ├── SpawnPoint_1
│   └── ... (8 total)
└── TestPlayer (Complete player setup)
    ├── Visual (Capsule mesh)
    └── PlayerCamera (Camera, AudioListener, MOBACameraController)
```

## Generated Components

### Network Systems
- **NetworkManager**: Core networking with 60Hz tick rate
- **UnityTransport**: UDP transport layer
- **NetworkGameManager**: Game state management
- **NetworkSystemIntegration**: Production-ready network coordination
- **NetworkTestSetup**: Development testing tools

### Gameplay Systems
- **CommandManager**: Command pattern for undo/redo
- **AbilitySystem**: Skill casting and cooldowns
- **FlyweightFactory**: Efficient object creation
- **ProjectilePool**: Object pooling for projectiles
- **EventBus**: Static event system (Observer pattern)

### Player Components
- **PlayerController**: Core player logic
- **MOBACharacterController**: Movement and physics
- **InputRelay**: Input system integration
- **StateMachineIntegration**: State management
- **MOBATestScene**: Test environment control

### Physics & Colliders
- **CapsuleCollider**: Player collision
- **Rigidbody**: Physics simulation with proper settings
- **Ground Collider**: Environment interaction

### Camera System
- **MOBACameraController**: MOBA-style camera with networking
- **Camera**: Rendering with optimized settings
- **AudioListener**: Audio processing

## Usage Examples

### Example 1: Quick Demo Setup
```csharp
// Menu: MOBA > Quick Setup > Complete Demo Scene
// Result: Full testing environment ready in seconds
```

### Example 2: Network Testing
```csharp
// Menu: MOBA > Quick Setup > Network Testing Only
// Result: Multiplayer testing environment
```

### Example 3: Custom Setup
```csharp
// Menu: MOBA > Generate Complete Scene
// Configure individual systems as needed
// Generate with custom options
```

## Configuration Options

### Auto-Configuration Features
- **Component References**: Automatically links components together
- **UI References**: Connects UI elements to scripts
- **Network References**: Links network components
- **Camera Targeting**: Sets camera target to player
- **Pool References**: Connects projectile pool to factory

### Customization
- Toggle individual systems on/off
- Choose specific components to include
- Configure scene setup name
- Enable/disable auto-configuration

## Development Workflow

### 1. Initial Setup
```
MOBA > Quick Setup > Complete Demo Scene
```

### 2. Testing
- Press Play in Unity
- Use F1-F3 keys for system testing
- Network controls appear in top-right
- All systems operational immediately

### 3. Iteration
```
MOBA > Quick Setup > Clear Scene
MOBA > Generate Complete Scene (custom options)
```

### 4. Production
- Use generated prefabs in final scenes
- Customize components as needed
- Deploy with confidence

## Troubleshooting

### Common Issues

**Q: Scene generation fails with errors**
A: Ensure all required scripts are present in the project. Check console for specific missing components.

**Q: Auto-configuration doesn't work**
A: Verify that target components exist in scene. Some references require specific naming conventions.

**Q: Network systems don't start**
A: Check that NetworkManager is properly configured with transport. Use Network Test UI buttons.

**Q: Player doesn't respond to input**
A: Ensure InputSystem_Actions asset exists and is properly configured in PlayerInput component.

### Performance Notes
- All generated systems are production-optimized
- Object pooling used for projectiles
- Efficient networking with 60Hz tick rate
- Zero-allocation hot paths where possible

## Script References

### Core Generator Scripts
- `MOBASceneGenerator.cs`: Main generation logic
- `MOBASceneGeneratorUsage.cs`: Quick access methods
- `MOBAPrefabGenerator.cs`: Prefab creation utilities

### Helper Components
- `TestTargetHealth.cs`: Simple damageable test object
- `EventBusMarker.cs`: Hierarchy organization for static EventBus

## Integration

The Scene Generator integrates seamlessly with:
- ✅ Unity Netcode for GameObjects
- ✅ Input System
- ✅ TextMeshPro UI
- ✅ Physics System
- ✅ Audio System
- ✅ All MOBA project systems

## Best Practices

1. **Start Clean**: Use "Clear Scene" before generation
2. **Test Immediately**: Generated scenes are ready for testing
3. **Customize Gradually**: Modify generated components as needed
4. **Save Prefabs**: Convert generated objects to prefabs for reuse
5. **Version Control**: Generated scenes work perfectly with Git

## Support

For issues or questions:
1. Check console for detailed error messages
2. Verify all required components exist
3. Use "Clear Scene" and regenerate if needed
4. Review the generated hierarchy for missing components

---

**The MOBA Scene Generator provides a complete, production-ready scene setup in seconds, eliminating manual configuration and ensuring consistent, high-quality results.**
