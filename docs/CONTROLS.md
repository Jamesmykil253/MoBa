# Controls & Camera System

## Input System Architecture

### Unity Input System Integration
- **Action-Based Input**: Platform-agnostic input handling
- **Cross-Platform Support**: Keyboard, mouse, gamepad, touch
- **Dynamic Rebinding**: Runtime input customization
- **Accessibility Features**: Motor and visual impairment support

### Implemented Design Patterns

**Complete Technical Implementation:** See [TECHNICAL.md](TECHNICAL.md) for detailed pattern implementations and architectural details.

**Cross-Reference:** For UI-specific input handling, see [UI-UX.md - Command Pattern for UI Actions](UI-UX.md#command-pattern-for-ui-actions).

- **Command Pattern**: Input handling with undo/redo capabilities
- **Observer Pattern**: Input event broadcasting to state machines  
- **Strategy Pattern**: Platform-specific input processing strategies
- **State Pattern**: Input routing based on character state

### Input Action Map Structure

#### Gameplay ActionMap
```
Movement - WASD/Left Stick (3D Vector)
Jump - Space/A Button (Button)
DoubleJump - Space Double-Tap/A Button Multi-Tap (Button)
PrimaryAttack - Left Mouse/Right Trigger (Button, Hold)
SecondaryAttack - Right Mouse/Left Trigger (Button, Hold)
Ability1 - Q/Left Shoulder (Button, Hold-to-Aim)
Ability2 - E/Right Shoulder (Button, Hold-to-Aim)
Ultimate - R/Both Shoulders (Button, Hold-to-Aim)
Interact - Left Alt/Right Stick Press (Button)
CenterCamera - Space/Y Button (Button)
CameraPan - Tab/Right Stick (Button, Hold)
Ping - V/D-Pad Up (Button)
OpenMainMenu - Escape/Start (Button)
```

#### UI ActionMap
```
Navigate - WASD/D-Pad (3D Vector)
Submit - Enter/A Button (Button)
Cancel - Escape/B Button (Button)
Previous - Tab/Left Shoulder (Button)
Next - Shift+Tab/Right Shoulder (Button)
```

### Hold-to-Aim Mechanics
- **Targeting Reticle**: Visual feedback during ability hold
- **Auto-Lock Starting**: Begins with auto-target position if available
- **Drag Adjustment**: Mouse/touch drag or gamepad stick adjusts aim
- **Release-to-Cast**: Button release executes ability at current aim position
- **Timeout Cancel**: 3-second hold limit prevents ability locking
- **Skill Expression**: 20% damage bonus for manual aim accuracy

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
- **Strategic Visibility**: Camera extends toward targeting direction during manual aim
- **Platform Scaling**: Different extension distances per platform
- **Smooth Transitions**: Easing curves for natural movement
- **Collision Detection**: Prevents camera clipping through geometry

### Platform-Specific Scaling
| Platform | Extension Multiplier | Control Method | Features |
|----------|---------------------|----------------|----------|
| PC | 1.8x | Precise mouse | Advanced rebinding, macro prevention |
| Console | 1.9x | Gamepad stick | Haptic feedback, adaptive triggers |
| Mobile | 2.0x | Touch gestures | Virtual joysticks, battery optimization |

## Accessibility Features

### Motor Accessibility
- **Hold-to-Toggle**: Convert hold inputs to toggle for limited dexterity
- **Custom Hold Thresholds**: Adjustable timing (0.1s to 2.0s)
- **One-Hand Mode**: Remap inputs for single-hand play
- **Button Remapping**: Full customization of all input bindings
- **Auto-Aim Assist**: Enhanced targeting assistance (0% to 80% strength)

### Visual Accessibility
- **Colorblind Support**: Alternative indicators (shapes instead of colors)
- **High Contrast Mode**: Enhanced visibility for low vision users
- **Scalable UI**: Text and UI scaling (75% to 150%)
- **Motion Reduction**: Option to disable screen shake and particles
- **Enlarged Indicators**: Bigger targeting reticles and UI elements

### Audio Accessibility
- **Audio Cues**: Sound feedback for input actions and target acquisition
- **Input Confirmation**: Audible feedback for successful inputs
- **Spatial Audio**: 3D sound cues for off-screen events
- **Volume Controls**: Independent accessibility audio volume

### Cognitive Accessibility
- **Simplified HUD**: Reduce information density for focus
- **Tutorial Replay**: Re-watch training content at any time
- **Terminology Glossary**: In-game definitions for all game terms
- **Visual Indicators**: Clear telegraphs for all abilities and interactions

## Platform-Specific Adaptations

### PC (Keyboard & Mouse)
- **Input Latency**: <16ms
- **Aim Assist**: Minimal (0.1 strength)
- **Mouse Sensitivity**: Fully customizable
- **Rebinding**: Full keyboard and mouse customization
- **Features**: Precise aiming, advanced shortcuts, spectator controls

### Console (Gamepad)
- **Input Latency**: <20ms
- **Aim Assist**: Moderate (0.25 strength)
- **Stick Sensitivity**: Optimized curves
- **Deadzone**: 12.5% deadzone for precision
- **Features**: Haptic feedback, adaptive triggers, ergonomic design

### Mobile (Touch)
- **Input Latency**: <33ms
- **Aim Assist**: Strong (0.4 strength)
- **Touch Sensitivity**: 80% of default
- **Gesture Threshold**: 10px minimum gesture distance
- **Features**: Touch gestures, virtual controls, battery optimization

## Input Persistence & Rebinding

### Binding Storage Format
```json
{
  "version": "1.0",
  "controlScheme": "Keyboard&Mouse",
  "customBindings": {
    "Gameplay": {
      "Movement": "<Keyboard>/wasd",
      "Jump": "<Keyboard>/space",
      "PrimaryAttack": "<Mouse>/leftButton",
      "Interact": "<Keyboard>/leftAlt",
      "Ability1": "<Keyboard>/q",
      "Ability2": "<Keyboard>/e",
      "Ultimate": "<Keyboard>/r"
    }
  },
  "platformSettings": {
    "aimAssist": 0.1,
    "mouseSensitivity": 1.0,
    "holdThresholds": {
      "abilities": 0.1,
      "interaction": 0.5
    }
  }
}
```

### Rebinding System Architecture
```csharp
public class InputRebindingManager : MonoBehaviour
{
    public void StartRebinding(string actionName, int bindingIndex)
    {
        // Disable current binding
        // Listen for new input
        // Validate new binding
        // Apply and save changes
    }

    public void SaveBindings()
    {
        // Persist bindings to PlayerPrefs
        // Include platform-specific settings
        // Handle corrupted data gracefully
    }
}
```

## Advanced Input Features

### MOBA-Style Targeting Integration
- **Hold-to-Aim Pattern**: Q/E/R buttons for manual ability targeting
- **Target Acquisition**: Auto-target feeds into manual aim starting point
- **Smart Aim**: Slight magnetism toward valid targets within tolerance cone
- **Accuracy Tracking**: Records manual aim precision for skill expression

### Left Alt Interaction System
- **Contextual Scoring**: Hold Left Alt near scoring zones
- **Progress Feedback**: Visual progress bar with time scaling
- **Team Synergy**: Multiple players can assist simultaneously
- **Risk/Reward**: More carried coins = longer scoring time = higher vulnerability

### Spectator Mode (Future)
```
FreeCamera - WASD/Left Stick (3D Vector)
CameraZoom - Mouse Wheel/Triggers (Axis)
FollowPlayer1-5 - 1-5/D-Pad (Button)
ToggleUI - H/X Button (Button)
```

## Performance Considerations

### Input Processing
- **Update Frequency**: 50Hz fixed timestep for deterministic input
- **Latency Budget**: <16ms total input-to-display latency
- **Prediction**: Client-side input prediction with server reconciliation
- **Buffering**: Input buffering for consistent 50Hz processing

### Camera Performance
- **Frame Rate**: 60fps target with smooth camera transitions
- **Collision Detection**: Efficient raycasting for camera collision
- **Extension Calculations**: Optimized vector math for camera positioning
- **Platform Optimization**: Reduced complexity on mobile devices

## Testing Framework Integration

### Input System Tests
- **Action Map Loading**: Verify all actions load correctly
- **Binding Validation**: Confirm Left Alt and other key bindings
- **Cross-Platform Detection**: Test input detection across platforms
- **Hold-to-Aim Mechanics**: Validate targeting behavior

### Camera System Tests
- **Extension Functionality**: Test camera extension during targeting
- **Collision Detection**: Verify camera collision prevention
- **Platform Scaling**: Confirm platform-specific multipliers
- **Smooth Transitions**: Validate easing curve behavior

### Accessibility Tests
- **Motor Features**: Test hold-to-toggle and remapping
- **Visual Features**: Verify colorblind and high contrast modes
- **Audio Features**: Confirm audio cue functionality
- **Cognitive Features**: Test simplified HUD options

## Implementation Notes

### Unity Input System Setup
1. **Create InputSystem_Actions.inputactions** file
2. **Configure Action Maps** with appropriate bindings
3. **Add Interact Action** to Gameplay ActionMap
4. **Implement Hold-to-Aim Interactions** for abilities
5. **Setup Platform-Specific Overrides**

### Camera System Implementation
1. **Create CameraController Component**
2. **Implement CameraExtension System**
3. **Add Collision Detection**
4. **Configure Cinemachine Virtual Camera**
5. **Setup Platform Scaling**

### Accessibility Implementation
1. **Add Accessibility Settings UI**
2. **Implement Motor Accessibility Options**
3. **Create Visual Accessibility Features**
4. **Add Audio Accessibility Cues**
5. **Test with Assistive Technologies**

## Key Performance Indicators (KPIs)

### Input Performance
- **Responsiveness**: <16ms input lag on PC, <20ms on console, <33ms on mobile
- **Binding Success**: >95% successful custom binding saves and loads
- **Cross-Platform Parity**: <10% win rate variance between platforms

### Camera Performance
- **Extension Utility**: >60% of manual aim casts utilize camera extension
- **Collision Prevention**: 100% camera collision detection success
- **Transition Smoothness**: <1 frame camera transition artifacts

### Accessibility Adoption
- **Feature Usage**: >15% of players use at least one accessibility feature
- **Satisfaction Rating**: >4.5/5 accessibility feature user satisfaction
- **Retention Impact**: Positive retention correlation with accessibility usage

## Dependencies
- **Unity Input System** package for action-based input
- **Cinemachine** for camera management and collision
- **Platform-specific SDKs** for haptic feedback and accessibility
- **PlayerPrefs/File System** for binding persistence
- **Accessibility Frameworks** for inclusive design compliance