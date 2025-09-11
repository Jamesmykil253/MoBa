# 🔧 Network Issues Resolution Guide

## 🚨 Issues Identified

### 1. Adaptive Performance Warning ⚠️
```
[Adaptive Performance] No Provider was configured for use. Make sure you added at least one Provider in the Adaptive Performance Settings.
```

### 2. NetworkProjectile Duplicate Hash Error ❌
```
NetworkPrefab (NetworkProjectile) has a duplicate GlobalObjectIdHash source entry value of: 3501571144!
```

### 3. Invalid Network Prefabs Warning ⚠️
```
[Netcode] Removing invalid prefabs from Network Prefab registration: {SourceHash: 0, TargetHash: 0}
```

### 4. Camera Success Message ✅
```
[CAMERA] Initialized - Target: MOBA_Player, Distance: 12.0m
```

## 🛠️ Solutions

### Fix 1: Adaptive Performance (Optional - Safe to Ignore)
This warning is harmless for MOBA games. To remove it:

**Option A: Disable Adaptive Performance**
```
1. Edit → Project Settings
2. XR Plug-in Management → Adaptive Performance
3. Uncheck "Initialize Adaptive Performance on Startup"
```

**Option B: Add Provider (if you want performance optimization)**
```
1. Window → Package Manager
2. Search "Adaptive Performance"
3. Install provider for your platform
```

### Fix 2: Network Prefab Duplicates (Critical)
The `DefaultNetworkPrefabs.asset` has duplicate entries causing hash conflicts.

**Solution:**
```
1. In Project window, find "DefaultNetworkPrefabs.asset"
2. Right-click → Delete (Unity will regenerate it)
3. Or replace with the clean version I created: "DefaultNetworkPrefabs_Clean.asset"
```

**Manual Fix:**
```
1. Select DefaultNetworkPrefabs.asset
2. In Inspector, remove duplicate entries
3. Keep only unique prefabs:
   - NetworkPlayer
   - NetworkProjectile  
   - NetworkEnemy
   - etc.
```

### Fix 3: Invalid Network Prefabs
This happens when prefabs have missing NetworkObject components.

**Solution:**
```
1. Go to Assets/Prefabs/Network/
2. Check each prefab has NetworkObject component
3. Remove any prefabs that don't need networking
4. Ensure all network prefabs are properly configured
```

## 🎯 Step-by-Step Network Setup Fix

### Step 1: Clean Network Prefabs List
```
1. Delete: Assets/DefaultNetworkPrefabs.asset
2. Rename: DefaultNetworkPrefabs_Clean.asset → DefaultNetworkPrefabs.asset
3. Unity will use the clean version
```

### Step 2: Verify Network Prefabs
```
1. Open Assets/Prefabs/Network/NetworkProjectile.prefab
2. Ensure it has NetworkObject component
3. Check "Enable Static Hash" is unchecked (to avoid hash conflicts)
4. Repeat for all network prefabs
```

### Step 3: Rebuild Network Configuration
```
1. In Hierarchy, find NetworkManager
2. Select it, go to Inspector
3. Network Prefabs → Clear the list
4. Add only the prefabs you actually use:
   - NetworkPlayer.prefab
   - NetworkProjectile.prefab
   - Any other network objects you need
```

### Step 4: Test Network Setup
```
1. Press Play in Unity
2. Check Console - should see:
   - [CAMERA] Initialized ✅
   - No duplicate hash errors ✅
   - No invalid prefab warnings ✅
```

## 🧪 Network Testing Script

Let me create a network validation script for you:
