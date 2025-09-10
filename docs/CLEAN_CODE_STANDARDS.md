# Clean Code Standards for MoBA Me Project

**Reference Material:** Based on "Clean Code" by Robert C. Martin (Available: `docs/Books/Clean Code ( PDFDrive.com ).pdf`)  
**Project Context:** Unity 6000.0.56f1 MOBA game with C# and Netcode for GameObjects  
**Last Updated:** September 9, 2025

---

## 🎯 Core Principles

### The Boy Scout Rule
> "Always leave the campground cleaner than you found it."

**Application in MOBA Development:**
- Improve any code you touch, even if you didn't write it
- Fix small issues when encountered during feature development
- Refactor unclear variable names when reading code
- Add missing documentation for public methods

---

## 📝 Naming Conventions

### Classes and Interfaces
```csharp
// ✅ GOOD - Clear, descriptive, PascalCase
public class NetworkPlayerController : NetworkBehaviour
public interface IDamageCalculator
public class AbilityStateMachine

// ❌ BAD - Unclear, abbreviated, poor casing
public class NPC
public interface IDmgCalc
public class asm
```

### Methods and Properties
```csharp
// ✅ GOOD - Verb phrases, clear intent, PascalCase
public void ExecuteAbility(AbilityData ability)
public bool IsPlayerInRange(float distance)
public Vector3 CalculateProjectileTrajectory()

// ❌ BAD - Unclear, abbreviated, misleading
public void DoStuff()
public bool Check(float d)
public Vector3 Calc()
```

### Variables and Fields
```csharp
// ✅ GOOD - Descriptive, camelCase for locals, clear units
private float movementSpeed = 5.0f;
private int currentHealthPoints = 100;
private Vector3 targetPosition;
private readonly List<NetworkPlayer> connectedPlayers;

// ❌ BAD - Unclear, abbreviated, no units
private float spd;
private int hp;
private Vector3 pos;
private List<NetworkPlayer> players;
```

### Constants and Enums
```csharp
// ✅ GOOD - ALL_CAPS for constants, descriptive enum values
public const float MAX_CAST_RANGE_METERS = 10.0f;
public const int DEFAULT_PLAYER_HEALTH = 100;

public enum PlayerState
{
    Idle,
    Moving,
    Casting,
    Stunned,
    Dead
}

// ❌ BAD - Unclear, inconsistent naming
public const float MAX_RANGE = 10.0f;
public enum State { I, M, C, S, D }
```

---

## 🎭 Functions and Methods

### Single Responsibility Principle
Each method should do one thing and do it well.

```csharp
// ✅ GOOD - Single responsibility
public bool IsAbilityOnCooldown(AbilityType ability)
{
    return abilityCooldowns[ability] > Time.time;
}

public void StartAbilityCooldown(AbilityType ability, float duration)
{
    abilityCooldowns[ability] = Time.time + duration;
}

// ❌ BAD - Multiple responsibilities
public bool CheckAndStartCooldown(AbilityType ability, float duration)
{
    bool onCooldown = abilityCooldowns[ability] > Time.time;
    if (!onCooldown)
    {
        abilityCooldowns[ability] = Time.time + duration;
        PlayCooldownEffect();
        UpdateUI();
        LogAbilityUsage(ability);
    }
    return onCooldown;
}
```

### Function Size Guidelines
- **Ideal:** 5-15 lines
- **Maximum:** 30 lines
- **If longer:** Extract smaller functions

```csharp
// ✅ GOOD - Small, focused function
public void TakeDamage(float damage, DamageType type)
{
    float finalDamage = CalculateFinalDamage(damage, type);
    ApplyDamage(finalDamage);
    TriggerDamageEffects(finalDamage, type);
}

// ✅ GOOD - Extracted helper functions
private float CalculateFinalDamage(float baseDamage, DamageType type)
{
    float resistance = GetResistance(type);
    return baseDamage * (1.0f - resistance);
}

private void ApplyDamage(float damage)
{
    currentHealth = Mathf.Max(0, currentHealth - damage);
    if (currentHealth <= 0) HandleDeath();
}
```

### Function Parameters
- **Maximum:** 3 parameters
- **If more needed:** Use parameter objects or configuration classes

```csharp
// ✅ GOOD - Parameter object for complex data
public class AbilityConfig
{
    public float damage;
    public float castTime;
    public float cooldown;
    public float range;
    public AbilityType type;
}

public void CastAbility(AbilityConfig config, Vector3 targetPosition)
{
    // Implementation
}

// ❌ BAD - Too many parameters
public void CastAbility(float damage, float castTime, float cooldown, 
                       float range, AbilityType type, Vector3 target)
{
    // Implementation
}
```

---

## 📝 Comments and Documentation

### When to Comment
```csharp
// ✅ GOOD - Explain WHY, not WHAT
// Use Pythagorean theorem because Unity's Distance is expensive in Update loops
private float CalculateDistanceSquared(Vector3 a, Vector3 b)
{
    float dx = a.x - b.x;
    float dy = a.y - b.y;
    float dz = a.z - b.z;
    return dx * dx + dy * dy + dz * dz;
}

// ✅ GOOD - Explain complex business logic
// Damage reduction follows diminishing returns: resistance / (resistance + 100)
// This prevents 100% damage immunity while maintaining meaningful scaling
private float CalculateDamageReduction(float resistance)
{
    return resistance / (resistance + 100.0f);
}

// ❌ BAD - Explains obvious code
// Increment the counter by one
counter++;

// Set the player's health to zero
player.health = 0;
```

### XML Documentation for Public APIs
```csharp
/// <summary>
/// Calculates projectile trajectory accounting for gravity and target movement prediction.
/// </summary>
/// <param name="startPosition">World position where projectile originates</param>
/// <param name="targetPosition">Current target position</param>
/// <param name="targetVelocity">Target's current velocity for prediction</param>
/// <param name="projectileSpeed">Projectile speed in units per second</param>
/// <returns>Launch direction vector (normalized)</returns>
public Vector3 CalculateProjectileTrajectory(Vector3 startPosition, 
                                           Vector3 targetPosition,
                                           Vector3 targetVelocity, 
                                           float projectileSpeed)
{
    // Implementation
}
```

---

## 🔧 Error Handling

### Defensive Programming
```csharp
// ✅ GOOD - Validate inputs and provide clear error messages
public void DealDamage(IDamageable target, float damage)
{
    if (target == null)
    {
        Debug.LogError("DealDamage: target cannot be null");
        return;
    }
    
    if (damage < 0)
    {
        Debug.LogWarning($"DealDamage: negative damage value {damage}, clamping to 0");
        damage = 0;
    }
    
    target.TakeDamage(damage);
}

// ❌ BAD - No validation, unclear failures
public void DealDamage(IDamageable target, float damage)
{
    target.TakeDamage(damage); // NullReferenceException if target is null
}
```

### Exception Handling Guidelines
```csharp
// ✅ GOOD - Specific exception handling with recovery
public bool TryLoadPlayerData(string playerId, out PlayerData data)
{
    data = null;
    
    try
    {
        string filePath = GetPlayerDataPath(playerId);
        string json = File.ReadAllText(filePath);
        data = JsonUtility.FromJson<PlayerData>(json);
        return true;
    }
    catch (FileNotFoundException)
    {
        Debug.Log($"Player data not found for {playerId}, will create new profile");
        return false;
    }
    catch (JsonException ex)
    {
        Debug.LogError($"Corrupted player data for {playerId}: {ex.Message}");
        return false;
    }
}

// ❌ BAD - Generic exception swallowing
public PlayerData LoadPlayerData(string playerId)
{
    try
    {
        // Complex loading logic
    }
    catch
    {
        return null; // What went wrong? How should caller handle this?
    }
}
```

---

## 🏗️ Class Design

### Single Responsibility Principle
Each class should have one reason to change.

```csharp
// ✅ GOOD - Single responsibility: managing player health
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    public event Action<float> OnHealthChanged;
    public event Action OnPlayerDied;
    
    public void TakeDamage(float damage) { /* Implementation */ }
    public void Heal(float amount) { /* Implementation */ }
    public float GetHealthPercentage() { return currentHealth / maxHealth; }
}

// ❌ BAD - Multiple responsibilities mixed together
public class Player : NetworkBehaviour
{
    // Health management
    private float health;
    public void TakeDamage(float damage) { }
    
    // Movement
    private Vector3 velocity;
    public void Move(Vector3 direction) { }
    
    // Inventory
    private List<Item> inventory;
    public void AddItem(Item item) { }
    
    // UI Updates
    public void UpdateHealthBar() { }
    public void UpdateInventoryDisplay() { }
}
```

### Composition Over Inheritance
Prefer composition and interfaces over deep inheritance hierarchies.

```csharp
// ✅ GOOD - Composition with interfaces
public class PlayerCharacter : NetworkBehaviour
{
    private IMovementController movementController;
    private IHealthSystem healthSystem;
    private IAbilitySystem abilitySystem;
    
    private void Awake()
    {
        movementController = GetComponent<IMovementController>();
        healthSystem = GetComponent<IHealthSystem>();
        abilitySystem = GetComponent<IAbilitySystem>();
    }
}

// ❌ BAD - Deep inheritance hierarchy
public class Character { }
public class PlayableCharacter : Character { }
public class HeroCharacter : PlayableCharacter { }
public class MageHero : HeroCharacter { }
public class FireMage : MageHero { } // Too deep, inflexible
```

---

## 🎮 Unity-Specific Standards

### SerializeField Best Practices
```csharp
// ✅ GOOD - Private fields with SerializeField
public class AbilityComponent : MonoBehaviour
{
    [Header("Ability Configuration")]
    [SerializeField] private float damage = 50f;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float castTime = 1f;
    
    [Header("Effects")]
    [SerializeField] private GameObject castEffect;
    [SerializeField] private AudioClip castSound;
    
    // Public properties for controlled access
    public float Damage => damage;
    public float Cooldown => cooldown;
}

// ❌ BAD - Public fields, no organization
public class AbilityComponent : MonoBehaviour
{
    public float damage;
    public float cooldown;
    public float castTime;
    public GameObject castEffect;
    public AudioClip castSound;
}
```

### Component References
```csharp
// ✅ GOOD - Cached references with null checking
public class PlayerController : MonoBehaviour
{
    private Rigidbody playerRigidbody;
    private Animator playerAnimator;
    
    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        
        // Validate required components
        Debug.Assert(playerRigidbody != null, "PlayerController requires Rigidbody component");
        Debug.Assert(playerAnimator != null, "PlayerController requires Animator component");
    }
}

// ❌ BAD - GetComponent in Update or repeated calls
public class PlayerController : MonoBehaviour
{
    private void Update()
    {
        GetComponent<Rigidbody>().velocity = Vector3.forward; // Expensive!
        GetComponent<Animator>().SetBool("isMoving", true);   // Expensive!
    }
}
```

---

## 🔄 Refactoring Guidelines

### Code Smells to Watch For

#### Long Method
```csharp
// 🚨 CODE SMELL - Method too long (>30 lines)
public void UpdatePlayer()
{
    // 50+ lines of mixed responsibilities
    // Health updates, movement, combat, UI, etc.
}

// ✅ REFACTORED - Extracted responsibilities
public void UpdatePlayer()
{
    UpdateMovement();
    UpdateCombat();
    UpdateAbilities();
    UpdateUI();
}
```

#### Magic Numbers
```csharp
// 🚨 CODE SMELL - Magic numbers
if (distance < 5.0f && angle < 45.0f)
{
    // What do these numbers mean?
}

// ✅ REFACTORED - Named constants
private const float MAX_INTERACTION_DISTANCE = 5.0f;
private const float MAX_INTERACTION_ANGLE = 45.0f;

if (distance < MAX_INTERACTION_DISTANCE && angle < MAX_INTERACTION_ANGLE)
{
    // Clear intent
}
```

#### Duplicate Code
```csharp
// 🚨 CODE SMELL - Duplicate logic
public void CastFireball() 
{
    if (mana >= 20) { mana -= 20; /* cast logic */ }
}

public void CastIceBolt() 
{
    if (mana >= 15) { mana -= 15; /* cast logic */ }
}

// ✅ REFACTORED - Extracted common logic
public bool TryConsumeManaThenCast(int manaCost, Action castAction)
{
    if (mana >= manaCost)
    {
        mana -= manaCost;
        castAction?.Invoke();
        return true;
    }
    return false;
}
```

---

## 📊 Code Quality Metrics

### Acceptable Thresholds
- **Cyclomatic Complexity:** ≤ 10 per method
- **Method Length:** ≤ 30 lines
- **Class Length:** ≤ 300 lines
- **Parameter Count:** ≤ 3 per method
- **Nesting Depth:** ≤ 4 levels

### Quality Gates
```csharp
// ✅ GOOD - Low complexity, clear flow
public PlayerState DetermineNextState(PlayerInput input)
{
    if (input.isJumping && isGrounded) return PlayerState.Jumping;
    if (input.isMoving) return PlayerState.Moving;
    return PlayerState.Idle;
}

// ❌ BAD - High complexity, nested conditions
public PlayerState DetermineNextState(PlayerInput input)
{
    if (input.isJumping)
    {
        if (isGrounded)
        {
            if (hasDoubleJump)
            {
                if (input.isRunning)
                {
                    // Deep nesting continues...
                }
            }
        }
    }
    // More complex logic...
}
```

---

## 🛠️ Tools and Automation

### Recommended Tools
- **Rider/Visual Studio:** Built-in code analysis
- **SonarLint:** Real-time code quality feedback
- **Unity Code Analysis:** Unity-specific rules
- **Custom Analyzers:** Project-specific standards

### Pre-commit Checklist
- [ ] All public methods have XML documentation
- [ ] No magic numbers (use named constants)
- [ ] Methods under 30 lines
- [ ] No duplicate code blocks
- [ ] Meaningful variable names
- [ ] Proper error handling
- [ ] Unit tests for new functionality

---

## 🎓 Team Guidelines

### Code Review Standards
1. **Readability First:** Code should tell a story
2. **No Clever Code:** Simple and obvious wins
3. **Consistent Style:** Follow project conventions
4. **Performance Awareness:** Profile before optimizing
5. **Documentation:** Public APIs must be documented

### Learning Resources
- **Primary:** "Clean Code" by Robert C. Martin (`docs/Books/Clean Code ( PDFDrive.com ).pdf`)
- **Secondary:** "Code Complete" by Steve McConnell (`docs/Books/code complete.pdf`)
- **Practice:** Regular code review sessions
- **Validation:** Automated code analysis tools

---

**Document Status:** ACTIVE  
**Next Review:** December 2025  
**Compliance:** Monitored through code review process  
**Integration:** Cross-referenced with `REFACTORING_GUIDELINES.md` and `DEVELOPMENT.md`
