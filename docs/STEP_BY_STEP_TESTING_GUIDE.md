# MOBA Game Testing Guide - Step by Step

## 🎮 How to Test Your MOBA Game From Empty Scene

### Prerequisites Checklist ✅
Before starting, ensure you have:
- Unity 6000.2.2f1 or compatible version
- Netcode for GameObjects package installed
- Input System package installed
- Your MOBA project loaded in Unity

## Step 1: Scene Setup (Start Here) 🏗️

### 1.1 Create/Open Test Scene
```
1. In Unity, go to File → New Scene
2. Choose "3D Core" template
3. Save as "MOBATestScene" in Assets/Scenes/
```

### 1.2 Add Essential GameObjects
```
1. Right-click in Hierarchy → Create Empty
2. Name it "MOBA_GameManager"
3. Right-click in Hierarchy → Create Empty  
4. Name it "MOBA_NetworkSystems"
5. Right-click in Hierarchy → Create Empty
6. Name it "MOBA_Testing"
```

## Step 2: Add Core MOBA Components 🎯

### 2.1 Network System Setup
```
1. Select "MOBA_NetworkSystems" GameObject
2. In Inspector, click "Add Component"
3. Search and add: "NetworkSystemIntegration"
4. Search and add: "NetworkGameManager" 
5. Search and add: "LobbySystem"
```

### 2.2 Game Management Setup
```
1. Select "MOBA_GameManager" GameObject
2. In Inspector, click "Add Component"
3. Search and add: "AnalyticsSystem"
4. Search and add: "PerformanceProfiler"
5. Search and add: "AntiCheatSystem"
```

### 2.3 Testing Framework Setup
```
1. Select "MOBA_Testing" GameObject
2. In Inspector, click "Add Component"
3. Search and add: "MOBASystemTester"
4. Search and add: "Priority1FixesTester"
5. Search and add: "RuntimeTestRunner"
```

## Step 3: Player Setup 🏃‍♂️

### 3.1 Create Player GameObject
```
1. Right-click in Hierarchy → 3D Object → Capsule
2. Name it "MOBA_Player"
3. Position at (0, 1, 0)
4. Add Component: "PlayerController"
5. Add Component: "Rigidbody"
6. Add Component: "CapsuleCollider"
```

### 3.2 Camera Setup
```
1. Select Main Camera in Hierarchy
2. Add Component: "MOBACameraController"
3. Position camera at (0, 5, -10)
4. Rotate camera to look at player (X: 20, Y: 0, Z: 0)
```

### 3.3 Input System Setup
```
1. In Assets, find "InputSystem_Actions.inputactions"
2. Double-click to open Input Actions window
3. Click "Auto-Save" if prompted
4. Close Input Actions window
```

## Step 4: Scene Environment 🌍

### 4.1 Create Ground Plane
```
1. Right-click in Hierarchy → 3D Object → Plane
2. Name it "Ground"
3. Scale to (10, 1, 10)
4. Position at (0, 0, 0)
```

### 4.2 Add Lighting
```
1. Window → Rendering → Lighting
2. In Lighting window, click "Generate Lighting"
3. Wait for baking to complete
```

## Step 5: Testing Phase 🧪

### 5.1 Manual Component Testing
```
1. Select "MOBA_Testing" GameObject
2. In Inspector, find "MOBASystemTester" component
3. Check "Run Tests On Start" = false (manual control)
4. Check "Enable Detailed Logging" = true
5. Click "Run Basic Validation" button in Inspector
```

### 5.2 Priority Fixes Validation
```
1. In same GameObject, find "Priority1FixesTester"
2. Click "Run Priority 1 Tests" button
3. Check Console for test results
```

### 5.3 Network Testing Setup
```
1. Go to Window → Netcode for GameObjects → Netcode Manager
2. If not present, create NetworkManager:
   - Right-click Hierarchy → Create Empty → "NetworkManager"
   - Add Component: "NetworkManager"
   - Add Component: "UnityTransport"
```

## Step 6: Play Testing 🎮

### 6.1 Enter Play Mode
```
1. Press Play button in Unity
2. Check Console for initialization messages
3. Look for "MOBA systems validation successful!" message
```

### 6.2 Basic Movement Testing
```
1. Use WASD keys to move player
2. Use mouse to look around
3. Press Space to jump
4. Check movement is smooth and responsive
```

### 6.3 Camera Testing
```
1. Verify camera follows player
2. Test mouse look functionality
3. Check camera doesn't clip through objects
```

## Step 7: Network Testing 🌐

### 7.1 Local Server Test
```
1. In Play mode, open Console
2. Look for network initialization messages
3. Check for "Server started" or similar messages
```

### 7.2 Build and Test
```
1. File → Build Settings
2. Add current scene to build
3. Click "Build and Run"
4. Test built version alongside Unity editor
```

## Step 8: Performance Validation ⚡

### 8.1 Runtime Testing
```
1. Select "MOBA_Testing" GameObject
2. Find "RuntimeTestRunner" component  
3. Click "Start Runtime Tests"
4. Monitor performance metrics in Console
```

### 8.2 Frame Rate Check
```
1. In Game view, click "Stats" button
2. Monitor FPS (should be >30 consistently)
3. Check memory usage stays reasonable
```

## Troubleshooting Common Issues 🔧

### Issue: Player doesn't move
**Solution:**
1. Check PlayerController component is attached
2. Verify Input System Actions are properly set
3. Ensure Rigidbody is not kinematic

### Issue: Camera doesn't follow
**Solution:**
1. Check MOBACameraController is attached to Main Camera
2. Verify camera target is set to player
3. Check camera position and rotation

### Issue: Network errors
**Solution:**
1. Ensure NetworkManager exists in scene
2. Check Netcode for GameObjects package is installed
3. Verify transport component is attached

### Issue: Tests fail
**Solution:**
1. Check all required components are attached
2. Verify scene has necessary GameObjects
3. Review Console for specific error messages

## Expected Test Results ✅

### Successful Setup Should Show:
- ✅ "GameObject setup - PASSED"
- ✅ "Scene has cameras - PASSED"  
- ✅ "Basic components found - PASSED"
- ✅ "All tests PASSED - MOBA systems validation successful!"
- ✅ Frame rate >30 FPS
- ✅ Smooth player movement
- ✅ Responsive camera controls

## Next Steps After Basic Testing 🚀

1. **Add more players** - Test multiplayer functionality
2. **Create abilities** - Test skill system
3. **Add game objectives** - Test victory conditions
4. **Performance tuning** - Optimize for your target specs
5. **Build testing** - Test on target platforms

Your MOBA game should now be fully testable with all core systems working!
