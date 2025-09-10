# Refactoring Guidelines for MoBA Me Project

**Reference Material:** Based on "Refactoring: Improving the Design of Existing Code" by Martin Fowler (Available: `docs/Books/Refactoring Improving the Design of Existing Code.pdf`)  
**Project Context:** Unity 6000.0.56f1 MOBA game with C# and Netcode for GameObjects  
**Last Updated:** September 9, 2025

---

## 🎯 Refactoring Philosophy

### Core Principle
> "Refactoring is a disciplined technique for restructuring an existing body of code, altering its internal structure without changing its external behavior."

**In MOBA Development Context:**
- Improve code readability and maintainability
- Enhance performance for real-time gameplay
- Reduce technical debt without breaking gameplay
- Enable easier feature additions and balance changes

---

## 🚨 Code Smells Catalog

### 1. Long Method
**Symptoms:** Methods exceeding 30 lines, multiple responsibilities  
**Unity Context:** Common in Update loops and ability systems

```csharp
// 🚨 CODE SMELL - Long method doing too much
public void Update()
{
    // Input handling (10 lines)
    HandleMovementInput();
    HandleAbilityInput();
    HandleUIInput();
    
    // State management (15 lines)
    UpdatePlayerState();
    UpdateAbilityCooldowns();
    UpdateBuffs();
    
    // Visual updates (20 lines)
    UpdateAnimations();
    UpdateParticleEffects();
    UpdateUI();
    
    // Audio updates (10 lines)
    UpdateSoundEffects();
    UpdateMusic();
    // Total: 55+ lines in Update!
}

// ✅ REFACTORED - Extracted methods
public void Update()
{
    HandleInput();
    UpdateGameState();
    UpdateVisuals();
    UpdateAudio();
}

private void HandleInput()
{
    HandleMovementInput();
    HandleAbilityInput();
    HandleUIInput();
}
```

### 2. Large Class (God Object)
**Symptoms:** Classes with 300+ lines, multiple responsibilities  
**Unity Context:** Common in Player controllers and Game managers

```csharp
// 🚨 CODE SMELL - God object doing everything
public class PlayerController : NetworkBehaviour
{
    // Movement (50 lines)
    private void HandleMovement() { }
    
    // Combat (80 lines)
    private void HandleCombat() { }
    
    // Inventory (60 lines)
    private void ManageInventory() { }
    
    // UI (40 lines)
    private void UpdateUI() { }
    
    // Networking (70 lines)
    private void HandleNetworking() { }
    
    // Audio (30 lines)
    private void ManageAudio() { }
    // Total: 330+ lines!
}

// ✅ REFACTORED - Separated concerns
public class PlayerController : NetworkBehaviour
{
    private PlayerMovement movement;
    private PlayerCombat combat;
    private PlayerInventory inventory;
    private PlayerUI playerUI;
    private PlayerNetworking networking;
    
    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        // Initialize other components
    }
}
```

### 3. Primitive Obsession
**Symptoms:** Using primitives instead of small objects  
**Unity Context:** Common with damage values, positions, and game stats

```csharp
// 🚨 CODE SMELL - Primitive obsession
public void DealDamage(float physicalDamage, float magicalDamage, float trueDamage,
                      bool isCritical, bool isFromAbility, string damageSource)
{
    // Complex damage calculation with many parameters
}

// ✅ REFACTORED - Value object
public struct DamageInfo
{
    public float physicalDamage;
    public float magicalDamage;
    public float trueDamage;
    public bool isCritical;
    public bool isFromAbility;
    public string source;
    
    public float TotalDamage => physicalDamage + magicalDamage + trueDamage;
}

public void DealDamage(DamageInfo damageInfo)
{
    // Cleaner method signature, easier to extend
}
```

### 4. Duplicate Code
**Symptoms:** Similar code blocks in different locations  
**Unity Context:** Common in ability implementations and UI updates

```csharp
// 🚨 CODE SMELL - Duplicate code
public void CastFireball()
{
    if (currentMana >= fireballManaCost)
    {
        currentMana -= fireballManaCost;
        StartCoroutine(FireballCastTime());
        PlayCastSound();
        ShowCastEffect();
    }
}

public void CastIceShard()
{
    if (currentMana >= iceShardManaCost)
    {
        currentMana -= iceShardManaCost;
        StartCoroutine(IceShardCastTime());
        PlayCastSound();
        ShowCastEffect();
    }
}

// ✅ REFACTORED - Template method pattern
public abstract class Ability : ScriptableObject
{
    [SerializeField] protected float manaCost;
    [SerializeField] protected float castTime;
    
    public bool TryCast(PlayerMana manaSystem, MonoBehaviour caster)
    {
        if (!manaSystem.HasMana(manaCost)) return false;
        
        manaSystem.ConsumeMana(manaCost);
        caster.StartCoroutine(CastSequence(caster));
        PlayCastEffects();
        return true;
    }
    
    protected abstract IEnumerator CastSequence(MonoBehaviour caster);
    
    private void PlayCastEffects()
    {
        PlayCastSound();
        ShowCastEffect();
    }
}
```

### 5. Feature Envy
**Symptoms:** Class using another class's data more than its own  
**Unity Context:** Common when components over-access other components

```csharp
// 🚨 CODE SMELL - Feature envy
public class PlayerAttack : MonoBehaviour
{
    private PlayerStats stats;
    
    public void CalculateDamage()
    {
        float baseDamage = stats.GetAttackPower();
        float critChance = stats.GetCriticalChance();
        float critMultiplier = stats.GetCriticalMultiplier();
        float attackSpeed = stats.GetAttackSpeed();
        
        // This class knows too much about PlayerStats internals
        float finalDamage = baseDamage;
        if (Random.value < critChance)
            finalDamage *= critMultiplier;
    }
}

// ✅ REFACTORED - Move logic to appropriate class
public class PlayerStats : MonoBehaviour
{
    public float CalculateAttackDamage()
    {
        float damage = GetAttackPower();
        if (Random.value < GetCriticalChance())
            damage *= GetCriticalMultiplier();
        return damage;
    }
}

public class PlayerAttack : MonoBehaviour
{
    private PlayerStats stats;
    
    public void CalculateDamage()
    {
        float damage = stats.CalculateAttackDamage();
        // Much cleaner!
    }
}
```

---

## 🔧 Refactoring Techniques Catalog

### 1. Extract Method
**When to Use:** Long methods, duplicate code blocks  
**Unity Specific:** Common in Update loops and ability systems

```csharp
// Before refactoring
public void Update()
{
    // Movement input (10 lines)
    Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    input = transform.TransformDirection(input);
    input *= movementSpeed;
    GetComponent<Rigidbody>().velocity = input;
    
    // Ability input (8 lines)
    if (Input.GetKeyDown(KeyCode.Q) && qAbilityReady)
    {
        CastQAbility();
        qAbilityReady = false;
        StartCoroutine(QAbilityCooldown());
    }
}

// After refactoring
public void Update()
{
    HandleMovementInput();
    HandleAbilityInput();
}

private void HandleMovementInput()
{
    Vector3 input = GetMovementInput();
    ApplyMovement(input);
}

private Vector3 GetMovementInput()
{
    return new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
}

private void ApplyMovement(Vector3 input)
{
    Vector3 worldInput = transform.TransformDirection(input) * movementSpeed;
    GetComponent<Rigidbody>().velocity = worldInput;
}
```

### 2. Extract Class
**When to Use:** Large classes with multiple responsibilities  
**Unity Specific:** Break up monolithic controllers

```csharp
// Before - Monolithic PlayerController
public class PlayerController : NetworkBehaviour
{
    // Health system
    private float currentHealth;
    public void TakeDamage(float damage) { }
    public void Heal(float amount) { }
    
    // Inventory system
    private List<Item> inventory;
    public void AddItem(Item item) { }
    public void RemoveItem(Item item) { }
    
    // Movement system
    private Vector3 velocity;
    public void Move(Vector3 direction) { }
}

// After - Separated responsibilities
public class PlayerController : NetworkBehaviour
{
    private PlayerHealth healthSystem;
    private PlayerInventory inventorySystem;
    private PlayerMovement movementSystem;
    
    private void Awake()
    {
        healthSystem = GetComponent<PlayerHealth>();
        inventorySystem = GetComponent<PlayerInventory>();
        movementSystem = GetComponent<PlayerMovement>();
    }
}

public class PlayerHealth : NetworkBehaviour
{
    private float currentHealth;
    public void TakeDamage(float damage) { /* Implementation */ }
    public void Heal(float amount) { /* Implementation */ }
}
```

### 3. Replace Magic Number with Named Constant
**When to Use:** Unclear numeric values throughout code  
**Unity Specific:** Game balance values, physics constants

```csharp
// Before - Magic numbers
public class AbilitySystem : MonoBehaviour
{
    public void CastFireball()
    {
        if (mana >= 25)  // What is 25?
        {
            DealDamage(80);  // What is 80?
            StartCooldown(3.5f);  // What is 3.5?
        }
    }
    
    public void Update()
    {
        if (Time.time - lastCast > 0.1f)  // What is 0.1?
        {
            allowNextCast = true;
        }
    }
}

// After - Named constants
public class AbilitySystem : MonoBehaviour
{
    private const float FIREBALL_MANA_COST = 25f;
    private const float FIREBALL_DAMAGE = 80f;
    private const float FIREBALL_COOLDOWN_SECONDS = 3.5f;
    private const float CAST_BUFFER_TIME_SECONDS = 0.1f;
    
    public void CastFireball()
    {
        if (mana >= FIREBALL_MANA_COST)
        {
            DealDamage(FIREBALL_DAMAGE);
            StartCooldown(FIREBALL_COOLDOWN_SECONDS);
        }
    }
    
    public void Update()
    {
        if (Time.time - lastCast > CAST_BUFFER_TIME_SECONDS)
        {
            allowNextCast = true;
        }
    }
}
```

### 4. Replace Conditional with Polymorphism
**When to Use:** Large switch statements or if-else chains  
**Unity Specific:** Different ability types, damage types, AI behaviors

```csharp
// Before - Large conditional
public class DamageCalculator
{
    public float CalculateDamage(DamageType type, float baseDamage, float resistance)
    {
        switch (type)
        {
            case DamageType.Physical:
                return baseDamage * (1 - resistance * 0.01f);
            case DamageType.Magical:
                return baseDamage * (1 - resistance * 0.005f);
            case DamageType.True:
                return baseDamage;
            case DamageType.Elemental:
                float elementalResist = resistance * 0.008f;
                return baseDamage * (1 - elementalResist);
            default:
                return baseDamage;
        }
    }
}

// After - Polymorphism with Strategy pattern
public interface IDamageStrategy
{
    float CalculateDamage(float baseDamage, float resistance);
}

public class PhysicalDamageStrategy : IDamageStrategy
{
    public float CalculateDamage(float baseDamage, float resistance)
    {
        return baseDamage * (1 - resistance * 0.01f);
    }
}

public class MagicalDamageStrategy : IDamageStrategy
{
    public float CalculateDamage(float baseDamage, float resistance)
    {
        return baseDamage * (1 - resistance * 0.005f);
    }
}

public class DamageCalculator
{
    private Dictionary<DamageType, IDamageStrategy> strategies;
    
    public float CalculateDamage(DamageType type, float baseDamage, float resistance)
    {
        return strategies[type].CalculateDamage(baseDamage, resistance);
    }
}
```

---

## 🎮 Unity-Specific Refactoring Patterns

### 1. Component Composition Refactoring
**Problem:** Monolithic MonoBehaviour classes  
**Solution:** Break into focused components

```csharp
// Before - Everything in one component
public class Player : MonoBehaviour
{
    // 200+ lines mixing health, movement, combat, UI, etc.
}

// After - Composition pattern
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerUI ui;
    
    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        ui = GetComponent<PlayerUI>();
    }
    
    // Delegates to appropriate components
    public void TakeDamage(float damage) => health.TakeDamage(damage);
    public void Move(Vector3 direction) => movement.Move(direction);
}
```

### 2. ScriptableObject Configuration Refactoring
**Problem:** Hard-coded values in MonoBehaviour  
**Solution:** Extract to ScriptableObject assets

```csharp
// Before - Hard-coded in component
public class Fireball : MonoBehaviour
{
    private float damage = 50f;
    private float speed = 10f;
    private float lifetime = 3f;
    private float explosionRadius = 2f;
}

// After - ScriptableObject configuration
[CreateAssetMenu(fileName = "New Spell Config", menuName = "Game/Spell Config")]
public class SpellConfig : ScriptableObject
{
    [Header("Damage")]
    public float damage = 50f;
    
    [Header("Movement")]
    public float speed = 10f;
    public float lifetime = 3f;
    
    [Header("Area of Effect")]
    public float explosionRadius = 2f;
}

public class Fireball : MonoBehaviour
{
    [SerializeField] private SpellConfig config;
    
    // Use config.damage, config.speed, etc.
}
```

### 3. Event System Refactoring
**Problem:** Direct component dependencies and tight coupling  
**Solution:** Event-driven architecture

```csharp
// Before - Tight coupling
public class PlayerHealth : MonoBehaviour
{
    private PlayerUI ui;
    private PlayerAnimator animator;
    private PlayerAudio audio;
    
    private void Awake()
    {
        ui = FindObjectOfType<PlayerUI>();
        animator = GetComponent<PlayerAnimator>();
        audio = GetComponent<PlayerAudio>();
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        ui.UpdateHealthBar(currentHealth);  // Direct dependency
        animator.PlayHitAnimation();         // Direct dependency
        audio.PlayHitSound();               // Direct dependency
    }
}

// After - Event-driven
public class PlayerHealth : MonoBehaviour
{
    public static event Action<float> OnHealthChanged;
    public static event Action OnPlayerHit;
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);  // Loose coupling
        OnPlayerHit?.Invoke();                   // Loose coupling
    }
}

public class PlayerUI : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHealthBar;
    }
    
    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHealthBar;
    }
}
```

---

## 🚀 Performance-Focused Refactoring

### 1. Object Pooling Refactoring
**Problem:** Frequent instantiation/destruction causing GC spikes  
**Solution:** Pool frequently used objects

```csharp
// Before - Creating/destroying projectiles
public class ProjectileLauncher : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    
    public void FireProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab);  // GC allocation
        // Configure projectile
        Destroy(projectile, 5f);  // GC allocation
    }
}

// After - Object pooling
public class ProjectileLauncher : MonoBehaviour
{
    [SerializeField] private ProjectilePool pool;
    
    public void FireProjectile()
    {
        Projectile projectile = pool.Get();  // No allocation
        // Configure projectile
        projectile.FireAndReturnToPool(pool, 5f);
    }
}

public class ProjectilePool : MonoBehaviour
{
    private Queue<Projectile> available = new Queue<Projectile>();
    
    public Projectile Get()
    {
        if (available.Count > 0)
            return available.Dequeue();
        
        return CreateNewProjectile();
    }
    
    public void Return(Projectile projectile)
    {
        projectile.Reset();
        available.Enqueue(projectile);
    }
}
```

### 2. Update Loop Optimization
**Problem:** Expensive operations in Update  
**Solution:** Caching, coroutines, and frame distribution

```csharp
// Before - Expensive Update loop
public class EnemyAI : MonoBehaviour
{
    private void Update()
    {
        // Every frame: expensive!
        GameObject player = GameObject.FindWithTag("Player");
        float distance = Vector3.Distance(transform.position, player.transform.position);
        
        if (distance < attackRange)
        {
            AttackPlayer();
        }
    }
}

// After - Optimized with caching and coroutines
public class EnemyAI : MonoBehaviour
{
    private Transform playerTransform;  // Cached reference
    private Coroutine aiUpdateCoroutine;
    
    private void Start()
    {
        playerTransform = GameObject.FindWithTag("Player").transform;
        aiUpdateCoroutine = StartCoroutine(AIUpdateLoop());
    }
    
    private IEnumerator AIUpdateLoop()
    {
        while (true)
        {
            float distanceSquared = (transform.position - playerTransform.position).sqrMagnitude;
            
            if (distanceSquared < attackRange * attackRange)  // Avoid sqrt
            {
                AttackPlayer();
            }
            
            yield return new WaitForSeconds(0.1f);  // Update 10 times per second instead of 60
        }
    }
}
```

---

## 📋 Refactoring Process

### Safe Refactoring Steps
1. **Write Tests First** - Ensure behavior preservation
2. **Refactor in Small Steps** - One technique at a time
3. **Test After Each Step** - Verify nothing breaks
4. **Commit Frequently** - Easy rollback if needed
5. **Review Before Merge** - Team validation

### Unity-Specific Testing for Refactoring
```csharp
// Example test for refactoring validation
[Test]
public void PlayerHealth_TakeDamage_ReducesHealthCorrectly()
{
    // Arrange
    var playerHealth = new GameObject().AddComponent<PlayerHealth>();
    playerHealth.SetMaxHealth(100f);
    
    // Act
    playerHealth.TakeDamage(25f);
    
    // Assert
    Assert.AreEqual(75f, playerHealth.CurrentHealth);
}

// Test event firing after refactoring to events
[Test]
public void PlayerHealth_TakeDamage_FiresHealthChangedEvent()
{
    // Arrange
    var playerHealth = new GameObject().AddComponent<PlayerHealth>();
    bool eventFired = false;
    PlayerHealth.OnHealthChanged += (health) => eventFired = true;
    
    // Act
    playerHealth.TakeDamage(10f);
    
    // Assert
    Assert.IsTrue(eventFired);
}
```

### Refactoring Checklist
- [ ] All existing tests still pass
- [ ] New tests cover refactored functionality
- [ ] No performance regressions
- [ ] Code is more readable and maintainable
- [ ] External behavior unchanged
- [ ] Dependencies reduced or clarified
- [ ] Documentation updated if needed

---

## 🛡️ Safe Refactoring Guidelines

### When NOT to Refactor
- **Just before a deadline** - Risk too high
- **Without tests** - No safety net
- **Performance critical code** - Profile first
- **Code you don't understand** - Study first
- **Legacy code without tests** - Add tests first

### Refactoring Red Flags
```csharp
// 🚨 DANGER - Complex refactoring without safety net
public void RefactorComplexSystem()
{
    // Changing multiple classes at once
    // No tests
    // Under time pressure
    // Don't fully understand the code
}

// ✅ SAFE - Incremental refactoring with tests
[Test]
public void TestCurrentBehavior() { /* Characterize existing behavior */ }

public void RefactorOneMethodAtATime()
{
    // Extract one method
    // Run tests
    // Commit
    // Repeat
}
```

---

## 📊 Refactoring Metrics

### Code Quality Improvements
- **Cyclomatic Complexity:** Target reduction of 20-50%
- **Method Length:** Target average <20 lines
- **Class Coupling:** Reduce dependencies by 30%
- **Code Duplication:** Eliminate repeated blocks >5 lines

### Performance Improvements
- **Frame Rate:** Maintain 60+ FPS after refactoring
- **Memory Usage:** Reduce allocations in Update loops
- **Load Times:** No regression in scene loading
- **Garbage Collection:** Minimize GC spikes

---

## 🔄 Integration with Development Process

### Daily Refactoring
- **Boy Scout Rule:** Improve code you touch
- **Micro-refactoring:** Small improvements during feature work
- **Code Review:** Identify refactoring opportunities

### Planned Refactoring Sprints
- **Technical Debt Sprints:** Dedicated time for major refactoring
- **Performance Sprints:** Focus on optimization refactoring
- **Architecture Sprints:** Large-scale structural improvements

### Tools and Automation
- **Rider/Visual Studio:** Built-in refactoring tools
- **Unity Test Runner:** Automated testing
- **Git:** Version control for safe refactoring
- **Code Analysis:** Automated smell detection

---

**Document Status:** ACTIVE  
**Next Review:** December 2025  
**Cross-References:** `CLEAN_CODE_STANDARDS.md`, `TECHNICAL.md`, `TESTING.md`  
**Team Training:** Scheduled for October 2025
