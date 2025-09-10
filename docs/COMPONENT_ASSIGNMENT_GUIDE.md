# MOBA Component Assignment Guide

This comprehensive guide details exactly which components should be attached to which GameObjects and how they should be configured in the MOBA project.

## Table of Contents
1. [Player Prefab Component Assignment](#player-prefab-component-assignment)
2. [Enemy Prefab Component Assignment](#enemy-prefab-component-assignment)  
3. [Network Components](#network-components)
4. [Scene GameObjects](#scene-gameobjects)
5. [Component Dependencies](#component-dependencies)
6. [Missing Script Resolution](#missing-script-resolution)

---

## Player Prefab Component Assignment

### Player.prefab (`/Assets/Prefabs/Gameplay/Player.prefab`)

**Required Components:**
1. **Transform** ✅ (automatically added)
2. **CapsuleCollider** ✅ (already present)
   - Radius: 0.5
   - Height: 2
   - Center: (0, 1, 0)
   - IsTrigger: false
3. **Rigidbody** ✅ (already present)
   - Mass: 1
   - LinearDamping: 0
   - AngularDamping: 0.05
   - Constraints: 112 (freeze rotation X, Z)
4. **PlayerController** ✅ (already present)
   - Script: `MOBA.PlayerController`
   - Fields to assign:
     - inputRelay: (will auto-find if null)
     - characterController: (will auto-find if null)
     - commandManager: (will auto-find if null)
     - abilitySystem: (will auto-find if null)
     - maxHealth: 1000
     - baseMoveSpeed: 350
     - jumpForce: 8
     - doubleJumpForce: 6
     - damageMultiplier: 1
     - scoringZone: (assign in scene)
5. **MOBACharacterController** ✅ (already present)
   - Script: `MOBA.MOBACharacterController`
   - Fields to assign:
     - moveSpeed: 350
     - jumpForce: 8
     - doubleJumpForce: 6
     - rb: (will auto-find if null)
     - groundCollider: (will auto-find if null)
6. **PlayerInput** ✅ (already present)
   - Script: `Unity.InputSystem.PlayerInput`
   - m_Actions: InputSystem_Actions asset
7. **InputRelay** ✅ (already present)
   - Script: `MOBA.InputRelay`
   - Fields to assign:
     - characterController: (will auto-find if null)
     - commandManager: (will auto-find if null)
     - abilitySystem: (will auto-find if null)
8. **StateMachineIntegration** ✅ (already present)
   - Script: `MOBA.StateMachineIntegration`
   - Fields to assign:
     - characterController: (will auto-find if null)
     - playerController: (will auto-find if null)
     - inputRelay: (will auto-find if null)
     - enableStateLogging: true
     - enableStateEvents: true

**Optional Components (for enhanced functionality):**
- **SpriteRenderer** (for 2D sprites)
- **Animator** (for animations)

**Child Objects:**
- **Visual** (child GameObject)
  - MeshFilter: Default Capsule mesh
  - MeshRenderer: Material assignment needed

---

## Enemy Prefab Component Assignment

### Enemy.prefab (`/Assets/Prefabs/Gameplay/Enemy.prefab`)

**Current State:** ❌ Missing essential scripts

**Required Components:**
1. **Transform** ✅ (already present)
2. **BoxCollider** ✅ (already present)
   - Size: (1, 1, 1)
   - Center: (0, 0, 0)
   - IsTrigger: false
3. **Rigidbody** ✅ (already present)
   - Mass: 1
   - Constraints: 112 (freeze rotation X, Z)
4. **MeshFilter** ✅ (already present)
   - Mesh: Default Cube
5. **MeshRenderer** ✅ (already present)
   - Material: Red material (guid: aa077ec4c3d8a483baa3ec4e43abbe8c)
6. **EnemyController** ❌ MISSING - ADD THIS SCRIPT
   - Script: `MOBA.EnemyController`
   - Fields to configure:
     - maxHealth: 500
     - damage: 50
     - attackRange: 3
     - attackCooldown: 2
     - moveSpeed: 200
     - detectionRange: 8
     - chaseRange: 12
     - targetLayerMask: Default
     - enemyColor: Red

**What was missing:** The Enemy prefab had component slots but no actual controller script attached. This is why you saw "missing script" references.

---

## Network Components

### NetworkPlayer (for multiplayer)

When creating networked players, use these components:

**Required Components:**
1. **NetworkObject** (Unity Netcode)
   - Don't Destroy With Owner: false
   - Network Rigidbody Settings: recommended
2. **NetworkPlayerController**
   - Script: `MOBA.Networking.NetworkPlayerController`
   - Requires: NetworkObject, Rigidbody
3. **All Player components listed above**

### NetworkManager Setup

**Single Scene GameObject with:**
- **NetworkManager** (Unity Netcode)
- **NetworkObjectPoolManager** (singleton)
- **NetworkEventBus** (singleton)
- **NetworkSystemIntegration**

---

## Scene GameObjects

### Essential Scene Objects

1. **Game Manager**
   - **CommandManager** script
   - **AbilitySystem** script
   - **ProjectilePool** script
   - **MOBACameraController** script

2. **Ground Objects**
   - Layer: "Ground"
   - Colliders for physics detection

3. **Spawn Points**
   - Empty GameObjects at spawn locations
   - Assign to PlayerController.scoringZone

4. **UI Canvas**
   - Event system for input handling

---

## Component Dependencies

### Automatic Discovery Pattern
Most scripts use `FindAnyObjectByType<T>()` for automatic component discovery:

```csharp
// PlayerController.InitializeComponents()
if (inputRelay == null) inputRelay = GetComponent<InputRelay>();
if (characterController == null) characterController = GetComponent<MOBACharacterController>();
if (commandManager == null) commandManager = FindAnyObjectByType<CommandManager>();
if (abilitySystem == null) abilitySystem = FindAnyObjectByType<AbilitySystem>();
```

### RequireComponent Attributes
These components are automatically added:

- **PlayerController**: requires Rigidbody, Collider
- **NetworkPlayerController**: requires NetworkObject, Rigidbody  
- **StateMachineIntegration**: requires MOBACharacterController
- **EnemyController**: requires Rigidbody, Collider

### Manual Assignment Needed
These must be manually assigned in Inspector:

- **PlayerInput.m_Actions**: InputSystem_Actions asset
- **PlayerController.scoringZone**: Scene transform reference
- **Material assignments**: For visual appearance

---

## Missing Script Resolution

### Problem: "Missing Script" on Enemy Prefab
**Cause:** Enemy.prefab exists but lacks controller script
**Solution:** Add `EnemyController` script (now created)

### Problem: Empty Network Component References  
**Cause:** Trying to assign scene components to singleton managers
**Solution:** Use singleton pattern `NetworkObjectPoolManager.Instance`

### Problem: Null Reference Exceptions
**Cause:** Components not found during initialization
**Solution:** Check FindAnyObjectByType results and ensure scene setup

---

## Component Assignment Checklist

### Before Scene Testing:
- [ ] Player prefab has all 8 required scripts
- [ ] Enemy prefab has EnemyController script added
- [ ] Scene has CommandManager GameObject
- [ ] Scene has AbilitySystem GameObject  
- [ ] Scene has Ground layer objects
- [ ] InputSystem_Actions asset assigned to PlayerInput
- [ ] Materials assigned to renderers

### For Network Testing:
- [ ] NetworkManager in scene
- [ ] NetworkObject on player prefabs
- [ ] NetworkPlayerController script added
- [ ] NetworkObjectPoolManager singleton exists
- [ ] NetworkEventBus singleton exists

### Validation:
- [ ] No "Missing Script" errors in console
- [ ] All RequireComponent dependencies satisfied
- [ ] Automatic component discovery working
- [ ] Manual assignments completed in Inspector

---

## Quick Fix Commands

If you need to quickly resolve the missing scripts:

1. **Add EnemyController to Enemy.prefab:**
   - Open Enemy.prefab
   - Add Component → Scripts → MOBA → EnemyController
   - Configure the exposed fields as listed above

2. **Verify Player.prefab:**
   - All scripts should already be present
   - Check that fields are either assigned or will auto-discover

3. **Scene Setup:**
   - Create empty GameObject named "GameManager"
   - Add CommandManager, AbilitySystem, ProjectilePool scripts
   - Ensure Ground objects have "Ground" layer

This guide ensures all components are properly assigned and configured for full MOBA functionality.
