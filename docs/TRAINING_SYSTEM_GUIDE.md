# Local Training Lobby System 🎯

## Overview

The Local Training Lobby system provides a complete solution for single-player training in your MOBA game. It automatically creates and manages a local server environment that allows players to practice without needing online connectivity or other players.

## Quick Start

### Method 1: Instant Setup (Easiest)
1. Open your `SampleScene`
2. Create an empty GameObject
3. Add the `InstantTrainingScene` component
4. Press Play - everything will be set up automatically!

### Method 2: Manual Setup
1. Add `TrainingSceneSetup` component to any GameObject in your scene
2. Right-click on the component → "Setup Training Scene"
3. Configure player prefab if needed
4. Press Play to start training

### Method 3: Runtime Setup
1. Add `LocalTrainingLobby` component to a GameObject
2. Configure settings in inspector
3. Call `StartTrainingLobby()` to begin

## Core Components

### 1. LocalTrainingLobby
**Purpose**: Main controller for local training sessions
**Features**:
- Automatic local server creation
- Host-based networking (server + client in one)
- State management and error handling
- Debug UI for monitoring

**Key Settings**:
- `autoStartOnAwake`: Automatically start when scene loads
- `localPort`: Port for local server (default: 7777)
- `spawnPlayerImmediately`: Spawn player as soon as connected

### 2. TrainingGameManager
**Purpose**: Manages training-specific game logic
**Features**:
- Player spawning and respawning
- Training statistics tracking
- God mode and unlimited resources
- Instant respawn for rapid practice

**Training Features**:
- **Instant Respawn**: No waiting time for practice
- **God Mode**: Optional invulnerability
- **Unlimited Resources**: Focus on mechanics, not resource management
- **Session Stats**: Track spawns, deaths, and training time

### 3. TrainingLobbyUI
**Purpose**: User interface for training system
**Features**:
- Main menu with training start button
- Lobby progress indicators
- In-game training controls
- Settings for customization

### 4. TrainingSceneSetup
**Purpose**: Automatic scene configuration
**Features**:
- Creates spawn points in circular pattern
- Sets up camera and lighting
- Configures networking components
- Adds visual indicators for spawn points

### 5. InstantTrainingScene
**Purpose**: One-click scene conversion
**Features**:
- Converts any scene to training scene
- Self-configures all required components
- No manual setup required

## How It Works

### 1. Initialization Sequence
```
1. Initialize Network Components
   ├── Find/Create NetworkManager
   ├── Find/Create NetworkSystemIntegration  
   └── Configure for local training

2. Start Local Server
   ├── Start as Host (server + client)
   ├── Configure transport for localhost
   └── Wait for connection

3. Setup Training Environment
   ├── Create TrainingGameManager
   ├── Configure spawn points
   └── Spawn training player
```

### 2. State Management
The system uses a clear state machine:
- **Disconnected**: Ready to start
- **Initializing**: Setting up components
- **StartingLocalServer**: Creating local server
- **ConnectingAsClient**: Connecting to own server
- **Connected**: Ready for training
- **InTraining**: Active training session
- **Error**: Something went wrong

### 3. Local Networking
- Uses Unity Netcode for GameObjects
- Host mode (server + client combined)
- No external connections required
- Perfect for offline training

## Configuration Options

### Training Settings
```csharp
[Header("Training Settings")]
public bool autoStartOnAwake = true;        // Auto-start when scene loads
public bool enableDebugUI = true;           // Show debug interface
public float autoStartDelay = 1f;           // Delay before auto-start

[Header("Training Environment")]
public bool spawnPlayerImmediately = true;  // Spawn player on connect
public bool enableInstantRespawn = true;    // No respawn delay
public bool enableGodMode = false;          // Invulnerability
public bool unlimitedResources = true;      // Infinite resources
```

### Spawn Configuration
```csharp
[Header("Scene Configuration")]
public int numberOfSpawnPoints = 4;         // How many spawn points
public float spawnPointSpacing = 5f;        // Distance between spawns
public Vector3 spawnAreaCenter = Vector3.zero; // Center of spawn area
```

## Integration with Existing Systems

### NetworkSystemIntegration
The training system works seamlessly with your existing `NetworkSystemIntegration`:
- Automatically configures networking for local use
- Maintains compatibility with your network architecture
- Uses existing player prefabs and spawn systems

### MOBACharacterController & MOBACameraController
Your existing character and camera controllers work without modification:
- Full movement and combat mechanics available
- Camera lag compensation still functions
- All abilities and skills work normally

### Object Pooling
The training system uses your existing object pools:
- Projectiles spawn and despawn normally
- Performance optimizations maintained
- No special training-only pools needed

## Training Features

### 1. Instant Practice
- Start training in seconds
- No lobby waiting or matchmaking
- Practice specific scenarios immediately

### 2. Development-Friendly
- Debug UI shows connection status
- Error handling with clear messages
- Easy to integrate into development workflow

### 3. Customizable Environment
- Configurable spawn points
- Optional training aids (god mode, etc.)
- Scalable from solo to small group training

### 4. Performance Optimized
- Local-only networking reduces latency to near zero
- Full 60Hz simulation for responsive practice
- All optimizations from main game preserved

## Usage Examples

### Basic Training Session
```csharp
// Get the training lobby
LocalTrainingLobby lobby = FindFirstObjectByType<LocalTrainingLobby>();

// Start training
lobby.StartTrainingLobby();

// Stop when done
lobby.StopTrainingLobby();
```

### Custom Training Setup
```csharp
// Create custom training environment
TrainingSceneSetup setup = gameObject.AddComponent<TrainingSceneSetup>();
setup.numberOfSpawnPoints = 8;
setup.spawnPointSpacing = 10f;
setup.autoStartTraining = true;
setup.SetupTrainingScene();
```

### Monitor Training Stats
```csharp
TrainingGameManager manager = FindFirstObjectByType<TrainingGameManager>();
TrainingStats stats = manager.GetSessionStats();

Debug.Log($"Training Time: {stats.sessionDuration}s");
Debug.Log($"Spawns: {stats.totalSpawns}");
Debug.Log($"Deaths: {stats.totalDeaths}");
```

## Troubleshooting

### Common Issues

**Q: Training doesn't start automatically**
A: Check that `autoStartOnAwake` is enabled and no errors in console

**Q: Player doesn't spawn**
A: Ensure player prefab is assigned and has NetworkObject component

**Q: UI not showing**
A: Make sure Canvas and EventSystem exist in scene

**Q: Network errors**
A: Check that port 7777 is available, or change `localPort` setting

### Debug Information

The system provides extensive debug output:
```
[LocalTrainingLobby] 🎯 Starting Local Training Lobby...
[LocalTrainingLobby] Initializing network components...
[LocalTrainingLobby] ✅ Local server started as host
[LocalTrainingLobby] ✅ Training lobby ready!
```

Enable debug UI to see real-time status and controls.

## API Reference

### LocalTrainingLobby
- `StartTrainingLobby()`: Begin training session
- `StopTrainingLobby()`: End training session
- `OnStateChanged`: Event for state transitions
- `OnTrainingStarted`: Event when training begins
- `OnTrainingEnded`: Event when training ends

### TrainingGameManager
- `SpawnTrainingPlayer()`: Spawn/respawn player
- `GetSessionStats()`: Get training statistics
- `OnPlayerSpawned`: Event for player spawn
- `OnPlayerDied`: Event for player death

### TrainingSceneSetup
- `SetupTrainingScene()`: Complete scene setup
- `GetTrainingLobby()`: Access created lobby
- `GetSpawnPoints()`: Access created spawn points

## Best Practices

1. **Scene Organization**: Use `InstantTrainingScene` for quick setup
2. **Player Prefabs**: Ensure your player prefab has all required networking components
3. **Performance**: Training uses same optimizations as main game
4. **Debugging**: Enable debug UI during development
5. **Iteration**: Use instant respawn for rapid skill practice

## Next Steps

1. **Add this to your SampleScene** with `InstantTrainingScene`
2. **Configure your player prefab** in the training settings
3. **Press Play** and start practicing immediately!
4. **Customize training features** based on your specific needs
5. **Scale up** to multi-player training when ready

The training system is designed to grow with your development needs - start simple and add features as required!
