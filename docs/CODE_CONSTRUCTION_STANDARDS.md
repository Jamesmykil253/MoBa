# Code Construction Standards for MoBA Me Project

**Reference Material:** Based on "Code Complete" by Steve McConnell (Available: `docs/Books/code complete.pdf`)  
**Project Context:** Unity 6000.0.56f1 MOBA game with C# and Netcode for GameObjects  
**Integration Level:** Enhanced from existing Game Design Bible references  
**Last Updated:** September 9, 2025

---

## 🎯 Software Construction Excellence

### Primary Technical Imperative: Managing Complexity
> "Software's Primary Technical Imperative is managing complexity. This is greatly aided by a design focus on simplicity."

**MOBA Application:**
- Complex multiplayer interactions simplified through clear abstractions
- Game state complexity managed through hierarchical systems
- Player ability combinations handled through composable components

---

## 🏗️ Construction Fundamentals

### 1. Software Quality Characteristics

#### External Quality (User-Visible)
```csharp
// ✅ CORRECTNESS - Does what it's supposed to do
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    
    public void TakeDamage(float damage)
    {
        // Validate inputs - correctness requirement
        if (damage < 0)
        {
            Debug.LogWarning($"Negative damage value: {damage}. Clamping to 0.");
            damage = 0;
        }
        
        // Server-authoritative - correctness in multiplayer
        if (!IsServer) return;
        
        float newHealth = Mathf.Max(0, currentHealth.Value - damage);
        currentHealth.Value = newHealth;
        
        if (newHealth <= 0)
        {
            HandleDeath();
        }
    }
    
    // USABILITY - Clear interface for designers
    [ContextMenu("Test Damage")]
    private void TestDamage() => TakeDamage(10f);
}
```

#### Internal Quality (Developer-Visible)
```csharp
// ✅ MAINTAINABILITY - Easy to understand and modify
public class AbilitySystem : MonoBehaviour
{
    // READABLE - Self-documenting code structure
    [Header("Configuration")]
    [SerializeField] private List<AbilityData> availableAbilities;
    
    [Header("Runtime State")]
    [SerializeField] private Dictionary<AbilityType, float> cooldownTimers;
    
    // MODIFIABLE - Easy to add new abilities
    public void RegisterAbility(AbilityData ability)
    {
        if (!availableAbilities.Contains(ability))
        {
            availableAbilities.Add(ability);
            cooldownTimers[ability.abilityType] = 0f;
        }
    }
    
    // TESTABLE - Pure function for easy testing
    public static bool IsAbilityReady(float cooldownTimer, float cooldownDuration)
    {
        return cooldownTimer <= 0f;
    }
}
```

### 2. Levels of Design

#### Level 1: Software System (Architecture)
```csharp
// SYSTEM LEVEL - Overall MOBA architecture
public class MOBAGameArchitecture
{
    /*
     * HIGH-LEVEL ARCHITECTURE:
     * 
     * NetworkGameManager (Server Authority)
     * ├── PlayerManager (Player lifecycle)
     * ├── MatchManager (Game state, objectives)
     * ├── MapManager (Environment, lanes)
     * └── AbilitySystem (Combat mechanics)
     * 
     * Each system has clear responsibilities and interfaces
     */
}
```

#### Level 2: Division into Subsystems/Packages
```csharp
// SUBSYSTEM LEVEL - Player management subsystem
namespace MOBA.Player
{
    public interface IPlayerManager
    {
        void RegisterPlayer(NetworkPlayer player);
        void RemovePlayer(NetworkPlayer player);
        NetworkPlayer GetPlayer(ulong clientId);
        IReadOnlyList<NetworkPlayer> GetAllPlayers();
    }
    
    public interface IPlayerFactory
    {
        NetworkPlayer CreatePlayer(PlayerData data);
    }
    
    public interface IPlayerRepository
    {
        void SavePlayerData(PlayerData data);
        PlayerData LoadPlayerData(string playerId);
    }
}
```

#### Level 3: Division into Classes
```csharp
// CLASS LEVEL - Cohesive responsibilities
public class PlayerCombat : NetworkBehaviour
{
    // SINGLE RESPONSIBILITY - Only handles combat
    private PlayerStats stats;
    private AbilitySystem abilities;
    private TargetingSystem targeting;
    
    // HIGH COHESION - All methods relate to combat
    public bool CanAttack(Transform target) { }
    public void StartAttack(Transform target) { }
    public void CancelAttack() { }
    public void ApplyDamage(DamageInfo damage) { }
}
```

#### Level 4: Division into Routines
```csharp
// ROUTINE LEVEL - Small, focused methods
public class DamageCalculation
{
    // Each method has one clear purpose
    public static float CalculatePhysicalDamage(float baseDamage, float armor)
    {
        // Simple formula, easy to understand and test
        float damageReduction = armor / (armor + 100f);
        return baseDamage * (1f - damageReduction);
    }
    
    public static float CalculateCriticalDamage(float baseDamage, float critMultiplier)
    {
        return baseDamage * critMultiplier;
    }
    
    public static bool IsCriticalHit(float critChance)
    {
        return UnityEngine.Random.value < critChance;
    }
}
```

---

## 📝 High-Quality Routines

### Characteristics of High-Quality Routines

#### 1. Strong Functional Cohesion
```csharp
// ✅ STRONG COHESION - Everything relates to ability casting
public class AbilityCaster : MonoBehaviour
{
    public bool CanCastAbility(AbilityData ability)
    {
        return HasSufficientMana(ability.manaCost) &&
               IsAbilityOffCooldown(ability.abilityType) &&
               IsValidTarget(ability.targetType);
    }
    
    public void CastAbility(AbilityData ability, Vector3 targetPosition)
    {
        ConsumeMana(ability.manaCost);
        StartCooldown(ability.abilityType, ability.cooldown);
        ExecuteAbilityEffect(ability, targetPosition);
    }
    
    private bool HasSufficientMana(float required) { }
    private bool IsAbilityOffCooldown(AbilityType type) { }
    private bool IsValidTarget(TargetType type) { }
}

// ❌ WEAK COHESION - Mixed responsibilities
public class PlayerManager : MonoBehaviour
{
    public void UpdatePlayerHealth() { }     // Health responsibility
    public void HandlePlayerInput() { }      // Input responsibility  
    public void RenderPlayerModel() { }      // Rendering responsibility
    public void SavePlayerProgress() { }     // Persistence responsibility
    // Too many unrelated concerns!
}
```

#### 2. Loose Coupling
```csharp
// ✅ LOOSE COUPLING - Minimal dependencies
public class SpellEffect : MonoBehaviour
{
    // Depends only on what it needs
    public void Execute(Vector3 position, float damage, float radius)
    {
        var affectedTargets = Physics.OverlapSphere(position, radius);
        foreach (var target in affectedTargets)
        {
            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage);
        }
    }
}

// ❌ TIGHT COUPLING - Too many dependencies
public class TightlyCoupledSpell : MonoBehaviour
{
    public PlayerManager playerManager;      // Depends on player system
    public UIManager uiManager;             // Depends on UI system
    public AudioManager audioManager;       // Depends on audio system
    public ParticleManager particleManager; // Depends on effects system
    public NetworkManager networkManager;   // Depends on network system
    
    // Changes to any of these systems could break this class
}
```

#### 3. Small Size and Clear Purpose
```csharp
// ✅ GOOD SIZE - Under 200 lines, single purpose
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    
    public void UpdateHealth(float current, float maximum)
    {
        UpdateSlider(current, maximum);
        UpdateText(current, maximum);
    }
    
    private void UpdateSlider(float current, float maximum)
    {
        healthSlider.value = current / maximum;
    }
    
    private void UpdateText(float current, float maximum)
    {
        healthText.text = $"{current:F0}/{maximum:F0}";
    }
}
```

---

## 🎨 Defensive Programming

### Input Validation
```csharp
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float maxSpeed = 10f;
    
    public void SetMovementVector(Vector3 movement)
    {
        // VALIDATE INPUTS - Defensive programming principle
        if (movement.magnitude > 1f)
        {
            Debug.LogWarning($"Movement vector too large: {movement.magnitude}. Normalizing.");
            movement = movement.normalized;
        }
        
        // Validate reasonable bounds
        if (movement.magnitude * maxSpeed > 100f)
        {
            Debug.LogError($"Computed speed too high: {movement.magnitude * maxSpeed}. Check maxSpeed configuration.");
            return;
        }
        
        ApplyMovement(movement);
    }
    
    private void ApplyMovement(Vector3 movement)
    {
        // ASSERTIONS - Check internal consistency
        Debug.Assert(movement.magnitude <= 1.01f, "Movement should be normalized");
        
        Vector3 velocity = movement * maxSpeed;
        GetComponent<Rigidbody>().velocity = velocity;
    }
}
```

### Error Handling Strategies
```csharp
public class SaveSystem
{
    public bool TrySavePlayerData(PlayerData data, out string errorMessage)
    {
        errorMessage = null;
        
        try
        {
            // VALIDATE FIRST - Fail fast principle
            if (data == null)
            {
                errorMessage = "Player data is null";
                return false;
            }
            
            if (string.IsNullOrEmpty(data.playerId))
            {
                errorMessage = "Player ID is required";
                return false;
            }
            
            // ATTEMPT OPERATION
            string json = JsonUtility.ToJson(data);
            string filePath = GetSaveFilePath(data.playerId);
            File.WriteAllText(filePath, json);
            
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = "Insufficient permissions to save file";
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            errorMessage = "Save directory not found";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error: {ex.Message}";
            Debug.LogError($"Save failed: {ex}");
            return false;
        }
    }
}
```

---

## 🔄 Pseudocode Programming Process

### Step-by-Step Construction
```csharp
// STEP 1: Write pseudocode comments first
public void ProcessPlayerTurn()
{
    // Check if it's actually the player's turn
    // Validate the player's chosen action
    // Execute the action if valid
    // Update game state
    // Check for win/lose conditions
    // Switch to next player's turn
}

// STEP 2: Refine pseudocode with more detail
public void ProcessPlayerTurn()
{
    // Check if it's actually the player's turn
    //   - Compare current player ID with active player
    //   - Return early if not this player's turn
    
    // Validate the player's chosen action
    //   - Check if action is legal in current game state
    //   - Verify player has resources for action
    //   - Confirm target is valid
    
    // Execute the action if valid
    //   - Apply immediate effects
    //   - Schedule delayed effects
    //   - Update player resources
    
    // Update game state
    //   - Recalculate derived values
    //   - Update UI elements
    //   - Sync to network if multiplayer
    
    // Check for win/lose conditions
    //   - Evaluate victory conditions
    //   - Check for game over states
    //   - Trigger end game if necessary
    
    // Switch to next player's turn
    //   - Advance turn counter
    //   - Notify next player
    //   - Reset turn-specific state
}

// STEP 3: Replace comments with actual code
public void ProcessPlayerTurn(ulong playerId, PlayerAction action)
{
    // Check if it's actually the player's turn
    if (currentActivePlayer != playerId)
    {
        Debug.LogWarning($"Player {playerId} tried to act out of turn");
        return;
    }
    
    // Validate the player's chosen action
    var validation = ValidateAction(playerId, action);
    if (!validation.isValid)
    {
        SendActionRejected(playerId, validation.reason);
        return;
    }
    
    // Execute the action if valid
    ExecutePlayerAction(action);
    
    // Update game state
    UpdateGameState();
    
    // Check for win/lose conditions
    var gameResult = CheckGameEndConditions();
    if (gameResult.gameEnded)
    {
        EndGame(gameResult);
        return;
    }
    
    // Switch to next player's turn
    AdvanceToNextPlayer();
}
```

---

## 🧪 Construction Testing

### Unit Testing Approach
```csharp
[TestFixture]
public class DamageCalculationTests
{
    [Test]
    public void CalculatePhysicalDamage_WithZeroArmor_ReturnsFullDamage()
    {
        // Arrange
        float baseDamage = 100f;
        float armor = 0f;
        
        // Act
        float result = DamageCalculation.CalculatePhysicalDamage(baseDamage, armor);
        
        // Assert
        Assert.AreEqual(100f, result, 0.01f);
    }
    
    [Test]
    public void CalculatePhysicalDamage_WithHighArmor_ReducesDamageSignificantly()
    {
        // Arrange
        float baseDamage = 100f;
        float armor = 100f; // Should reduce damage by 50%
        
        // Act
        float result = DamageCalculation.CalculatePhysicalDamage(baseDamage, armor);
        
        // Assert
        Assert.AreEqual(50f, result, 0.01f);
    }
    
    [Test]
    public void CalculatePhysicalDamage_WithNegativeValues_HandlesGracefully()
    {
        // Test edge cases and error conditions
        float result = DamageCalculation.CalculatePhysicalDamage(-10f, 50f);
        Assert.GreaterOrEqual(result, 0f, "Damage should never be negative");
    }
}
```

### Integration Testing
```csharp
[TestFixture]
public class PlayerCombatIntegrationTests
{
    private GameObject playerObject;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    
    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        playerHealth = playerObject.AddComponent<PlayerHealth>();
        playerCombat = playerObject.AddComponent<PlayerCombat>();
        
        // Initialize with test data
        playerHealth.SetMaxHealth(100f);
    }
    
    [Test]
    public void PlayerCombat_AttackingEnemy_ReducesEnemyHealth()
    {
        // Arrange
        var enemyObject = new GameObject("TestEnemy");
        var enemyHealth = enemyObject.AddComponent<PlayerHealth>();
        enemyHealth.SetMaxHealth(100f);
        
        // Act
        playerCombat.Attack(enemyObject.transform, 25f);
        
        // Assert
        Assert.AreEqual(75f, enemyHealth.CurrentHealth);
        
        // Cleanup
        Object.DestroyImmediate(enemyObject);
    }
    
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
    }
}
```

---

## 📊 Code Tuning and Optimization

### Performance Measurement First
```csharp
public class PerformanceOptimization : MonoBehaviour
{
    [Header("Performance Monitoring")]
    [SerializeField] private bool enableProfiling = false;
    
    private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    
    public void UpdateEnemyAI()
    {
        if (enableProfiling) stopwatch.Start();
        
        // AI update logic here
        foreach (var enemy in enemies)
        {
            enemy.UpdateBehavior();
        }
        
        if (enableProfiling)
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 16) // More than one frame at 60fps
            {
                Debug.LogWarning($"AI update took {stopwatch.ElapsedMilliseconds}ms - too slow!");
            }
            stopwatch.Reset();
        }
    }
}
```

### Optimization Strategies
```csharp
// BEFORE OPTIMIZATION - Measure first
public class EnemyManager : MonoBehaviour
{
    private List<Enemy> enemies = new List<Enemy>();
    
    public Enemy FindNearestEnemy(Vector3 position)
    {
        // O(n) search - measure if this is actually a bottleneck
        Enemy nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
}

// AFTER OPTIMIZATION - Only if measurement showed it was slow
public class OptimizedEnemyManager : MonoBehaviour
{
    private List<Enemy> enemies = new List<Enemy>();
    
    public Enemy FindNearestEnemy(Vector3 position)
    {
        // Optimized only after profiling showed this was slow
        Enemy nearest = null;
        float nearestDistanceSquared = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            // Use sqrMagnitude to avoid expensive sqrt calculation
            float distanceSquared = (position - enemy.transform.position).sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
}
```

---

## 🎮 Unity-Specific Construction Guidelines

### MonoBehaviour Lifecycle Management
```csharp
public class ProperMonoBehaviour : MonoBehaviour
{
    // INITIALIZATION ORDER - Follow Unity lifecycle
    private void Awake()
    {
        // Initialize THIS object's internal state
        InitializeComponents();
        ValidateConfiguration();
    }
    
    private void Start()
    {
        // Initialize interactions with OTHER objects
        RegisterWithSystems();
        SubscribeToEvents();
    }
    
    private void OnEnable()
    {
        // Subscribe to events that should only be active when enabled
        GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
    }
    
    private void OnDisable()
    {
        // Clean up event subscriptions
        GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
    }
    
    private void OnDestroy()
    {
        // Clean up resources
        UnregisterFromSystems();
        CleanupReferences();
    }
    
    private void InitializeComponents()
    {
        // Cache expensive GetComponent calls
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        
        // Validate required components
        Debug.Assert(playerRigidbody != null, "PlayerController requires Rigidbody");
        Debug.Assert(playerAnimator != null, "PlayerController requires Animator");
    }
}
```

### Serialization Best Practices
```csharp
public class SerializationExample : MonoBehaviour
{
    [Header("Required Configuration")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private LayerMask groundLayers = -1;
    
    [Header("Optional Settings")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private Color debugColor = Color.red;
    
    [Header("Runtime References")]
    [SerializeField] private Transform targetTransform; // Set in inspector
    
    // Private fields that shouldn't be serialized
    private Rigidbody cachedRigidbody;
    private float lastUpdateTime;
    
    // Properties for controlled access
    public float MovementSpeed 
    { 
        get => movementSpeed; 
        set => movementSpeed = Mathf.Max(0f, value); 
    }
    
    public bool IsDebugging => debugMode;
}
```

---

## 📚 Code Complete Integration Summary

### Key Principles Applied
1. **Managing Complexity** - Through clear abstractions and modular design
2. **High-Quality Routines** - Small, cohesive, loosely coupled methods
3. **Defensive Programming** - Input validation and error handling
4. **Construction Process** - Pseudocode-driven development
5. **Testing Integration** - Unit and integration tests for reliability
6. **Performance Awareness** - Measure before optimizing

### Team Standards Established
- Method size limits (≤30 lines ideal, ≤50 maximum)
- Class responsibility clarity (single purpose)
- Error handling consistency (validation + graceful failure)
- Testing requirements (critical logic must have tests)
- Documentation standards (public APIs documented)

### Continuous Improvement
- Regular code reviews using these standards
- Refactoring sessions to improve existing code
- Team training on construction techniques
- Measurement and optimization of critical paths

---

**Document Status:** ACTIVE  
**Integration Level:** Enhanced from Game Design Bible  
**Team Training:** Scheduled for October 2025  
**Cross-References:** `CLEAN_CODE_STANDARDS.md`, `REFACTORING_GUIDELINES.md`, `PRAGMATIC_DEVELOPMENT.md`  
**Quality Gates:** Integrated into code review process
