# Core Gameplay Mechanics

## Core Mechanics Overview

### Movement System
**Base Movement**: Standard 3D MOBA movement with WASD/virtual joystick
- **Walk Speed**: 350 units/second base, varies by character archetype
- **Phase-Through Players**: Players pass through each other, only abilities create knockback
- **Collision**: 3D environmental obstacles and walls provide strategic positioning
- **Camera-Relative Movement**: Movement adjusts based on camera orientation for 3D spatial awareness

**3D Platformer Elements**:
- **Jump**: Single jump with 150ms input buffer, 8 units vertical velocity
- **Double Jump**: Available 0.2s after first jump, 6 units vertical velocity
- **Coyote Time**: 100ms grace period for jumping after leaving platforms
- **Wall Sliding**: Characters can slide down walls at 50% normal fall speed
- **Ledge Grabbing**: Optional mechanic for advanced 3D movement options
- **Air Control**: Directional control during jumps and falls for 3D positioning
- **Momentum Preservation**: Physics-based movement with realistic momentum

### RSB Combat Formula Implementation

**Cross-Reference:** For technical implementation of damage strategies, see [TECHNICAL.md - Strategy Pattern for Combat Formulas](TECHNICAL.md#strategy-pattern-for-combat-formulas).

**Damage Calculation** (adapted from advanced MOBA mechanics):
```
rawDamage = floor(R × AttackStat + S × (Level - 1) + B)
damageTaken = floor(rawDamage × 600 / (600 + Defense))
manualAimBonus = isManualAim ? 1.2 : 1.0
finalDamage = damageTaken × manualAimBonus
```

**RSB Coefficient Guidelines**:
| Archetype | Ratio (R) | Slider (S) | Base (B) |
|-----------|-----------|------------|----------|
| Ranged DPS | 0.8-1.2 | 20-40 | 60-120 |
| Melee Brawler | 0.6-1.0 | 15-30 | 80-150 |
| Tank/Support | 0.4-0.8 | 10-25 | 40-100 |
| Ultimate Abilities | 1.5-2.5 | 50-100 | 150-300 |

**Design Principles**:
- **Ratio**: Higher values reward building attack stat
- **Slider**: Higher values increase pressure to level up
- **Base**: Higher values ensure ability remains useful at low stats
- **Balance Target**: 2-4 second time-to-kill for equally-leveled opponents

**Targeting System Integration**:
- **Auto-Target Scope**: Basic attacks and homing projectiles ONLY
- **Manual Aim Requirement**: Ability1, Ability2, Ultimate require manual targeting
- **MOBA Lock-On**: Hold ability button shows targeting reticle, release to cast
- **Target Acquisition Logic**: Uses same priority system across all abilities
- **Skill Expression Bonus**: 20% damage bonus for manual aim accuracy

### Crypto Coin Economy + Left Alt Interactions

**Scoring Mechanics**:
- **Left Alt (Interact)**: Primary scoring button for crypto coin deposits
- **Contextual Scoring**: Hold Left Alt near scoring zones to deposit coins
- **Team Synergy**: Multiple players can assist scoring simultaneously (bonus speed)
- **Risk Assessment**: More carried coins = longer scoring time = higher vulnerability

**Coin Generation**:
- **NPC Kills**: 5-10 coins per neutral creep
- **Player Kills**: 25-50 coins per elimination (scales with carried coins)
- **Assist Bonus**: 15 coins for damage/heal assists within 3 seconds of kill
- **Environmental**: Map-specific coin spawns every 30 seconds

**Coin Mechanics**:
- **Pickup Range**: 2 unit radius, automatic collection on contact
- **Spin Animation**: Visual feedback with satisfying audio cue
- **Carry Capacity**: No limit, but death drops 50% of carried coins
- **Scoring**: Deposit coins at team scoring zones for permanent points
- **Risk/Reward**: More carried coins = bigger target, longer scoring time

**Scoring Formula**:
```
baseScoreTime = 0.5 + (carriedCoins × 0.05) seconds
teamSynergyMultiplier = 1.0 - (alliesInRange × 0.15)
finalScoreTime = baseScoreTime × teamSynergyMultiplier
```

## Match Formats & Victory Conditions

### 1v1 Duels (60s-3min)
- **Victory**: First to 50 points OR most points at time limit
- **Respawn**: 3 second respawn timer
- **Map**: Small arena with vertical platforms
- **Special**: Sudden death mode if tied at time limit

### 3v3 Skirmish (3min-5min)
- **Victory**: First to 200 points OR most points at time limit
- **Respawn**: 5 second respawn timer
- **Map**: Medium map with multiple scoring zones
- **Special**: Final 60 seconds = 2x scoring multiplier

### 5v5 Strategic (5min-10min)
- **Victory**: First to 500 points OR most points at time limit
- **Respawn**: 8 second respawn timer
- **Map**: Large map with contested central objective
- **Special**: Mega objective spawns at 50% time remaining

## Character Archetypes

### Elom Nusk - Ranged Technomancer

**Fantasy**: Satirical tech mogul wielding "not a flamethrower" and Tesla coils

**Abilities**:
- **Basic Attack**: Auto-targeting ranged/melee attacks with cooldown ✅ AUTO-TARGET
- **Ability 1: Tesla Coil** - Deploy stationary turret, manual aim for placement (8s duration, 12s cooldown) ❌ MANUAL AIM
- **Ability 2: Rocket Boost** - Dash with brief flight, manual directional input (18s cooldown) ❌ MANUAL AIM
- **Ultimate: Satellite Strike** - Global range targeted laser, manual cursor targeting (60 energy requirement) ❌ MANUAL AIM

**RSB Coefficients**:
```json
{
  "basicAttack": { "R": 0.8, "S": 25, "B": 80 },
  "teslCoil": { "R": 1.2, "S": 40, "B": 120 },
  "rocketBoost": { "R": 0.6, "S": 20, "B": 60 },
  "satelliteStrike": { "R": 2.5, "S": 80, "B": 200 }
}
```

### DOGE - Evolving Brawler

**Fantasy**: Cute puppy that evolves into buff meme dog at level 5 (mid-match)

**Evolution Mechanics**:
- **Form 1 (Puppy)**: High mobility, low damage, crowd control focus
- **Form 2 (Buff Doge)**: Increased stats, new ability animations, intimidation factor

**Abilities**:
- **Basic Attack**: Bite/Claw - Auto-targeting melee range ✅ AUTO-TARGET
- **Ability 1: Pounce** - Gap closer with stun (Puppy) / knockback (Evolved), manual direction targeting ❌ MANUAL AIM
- **Ability 2: Bark** - AoE fear effect, manual cursor placement, larger radius when evolved ❌ MANUAL AIM
- **Ultimate: Very Wow** - Temporary invulnerability + damage boost, manual activation timing ❌ MANUAL AIM

**Evolution Stats Change**:
```json
{
  "level1to4": {
    "hp": 1000, "attack": 100, "defense": 100, "moveSpeed": 360
  },
  "level5plus": {
    "hp": 1200, "attack": 140, "defense": 120, "moveSpeed": 340
  }
}
```

## Hierarchical State Machine Architecture

**Implementation Details:** See [TECHNICAL.md - Finite State Machine (FSM) Pattern](TECHNICAL.md#finite-state-machine-fsm-pattern) for complete technical implementation including interfaces, state management classes, and performance optimization details.

**Cross-Reference:** This section focuses on gameplay state behavior. For input handling integration, see [CONTROLS.md - State Pattern Integration](CONTROLS.md#input-system-architecture).

### Core State Hierarchy (Based on State Pattern from Game Programming Patterns)

```mermaid
stateDiagram-v2
    [*] --> Grounded
    [*] --> Airborne
    [*] --> Stunned
    [*] --> Dead

    Grounded --> Airborne : Jump/Double Jump
    Airborne --> Grounded : Land
    Grounded --> Stunned : Take Damage
    Airborne --> Stunned : Take Damage
    Stunned --> Grounded : Stun End
    Stunned --> Airborne : Stun End + Airborne
    Grounded --> Dead : Health <= 0
    Airborne --> Dead : Health <= 0
    Stunned --> Dead : Health <= 0

    state Grounded as "Grounded Super-State" as G
    state Airborne as "Airborne Super-State" as A
    state Stunned as "Stunned Super-State" as S

    state G as Grounded {
        [*] --> Idle
        Idle --> BasicAttacking : Auto-Target Basic
        Idle --> ManualAiming : Hold Ability (Q/E/R)
        Idle --> Scoring : Left Alt + Near Zone
        Idle --> Moving : WASD Movement

        BasicAttacking --> Idle : Release Attack
        ManualAiming --> AbilityCasting : Release Ability
        ManualAiming --> Idle : Cancel/Timeout
        AbilityCasting --> Idle : Cast Complete
        Scoring --> Idle : Score Complete/Interrupted
        Moving --> Idle : Stop Movement

        BasicAttacking --> Moving : WASD During Attack
        Moving --> BasicAttacking : Attack During Movement
    }

    state A as Airborne {
        [*] --> Jumping
        Jumping --> DoubleJumping : Double Jump
        DoubleJumping --> Falling : Double Jump End
        Jumping --> Falling : Jump Peak
        Falling --> Landing : Ground Contact

        Jumping --> BasicAttacking : Auto-Target Basic
        DoubleJumping --> BasicAttacking : Auto-Target Basic
        Falling --> BasicAttacking : Auto-Target Basic

        BasicAttacking --> Falling : Attack End
    }

    state S as Stunned {
        [*] --> StunActive
        StunActive --> StunRecovery : Stun Duration End
        StunRecovery --> [*] : Recovery Complete

        StunActive --> Knockback : Knockback Force
        Knockback --> StunRecovery : Knockback End
    }
```

### Hierarchical State Machine Implementation

**Technical Implementation:** See [TECHNICAL.md - FSM Pattern](TECHNICAL.md#finite-state-machine-fsm-pattern) for complete code examples, interfaces, and architectural details.

**Gameplay Benefits:**
- **Hierarchical Organization**: Super-states manage common behavior, sub-states handle specific actions
- **State Composition**: States can contain other states for complex behaviors  
- **Transition Management**: Clear entry/exit points prevent state corruption
- **Memory Efficiency**: Shared state objects reduce allocation overhead
- **Debugging**: Hierarchical structure makes state tracking easier during development

## Key Performance Indicators (KPIs)

### Gameplay Balance KPIs
- **Match Duration Accuracy**: 95% of matches complete within target timeframes
- **Character Win Rates**: All characters maintain 45-55% win rate across skill levels
- **Ability Usage**: Each ability used at least once per match on average
- **Score Distribution**: 80% of matches decided by <20% score margin

### Engagement KPIs
- **Match Completion**: >90% of started matches finish (low quit rate)
- **Movement Utilization**: Jump mechanics used 5+ times per minute average
- **Combat Frequency**: Player vs player engagements every 30 seconds average
- **Strategic Depth**: 60% of victories involve team coordination

### Technical Performance KPIs
- **Input Responsiveness**: <16ms input lag on all movement and combat actions
- **Server Reconciliation**: <1% visible prediction correction events
- **Deterministic Replay**: 100% frame-perfect replay accuracy
- **Cross-Platform Parity**: <5% performance difference between mobile/PC

## Acceptance Criteria

### MOBA-Style Targeting System
**Scenario**: Player executes manual aim ability
- Given a player holds Q (Ability1) button
- When targeting reticle appears with auto-lock starting position
- And player adjusts aim direction with mouse/stick
- And player releases Q button
- Then ability casts at targeted location with 20% damage bonus
- And targeting state returns to idle
- And manual aim accuracy is recorded for skill expression

### Left Alt Scoring Interaction
**Scenario**: Player deposits crypto coins with Left Alt
- Given a player has accumulated crypto coins
- And player is within scoring zone proximity
- When player holds Left Alt (Interact) button
- Then scoring progress bar appears with time scaling
- And team synergy bonuses apply if allies assist
- And coins transfer to team score upon completion
- And death during scoring drops 50% of carried coins

## Common Anti-Patterns and Best Practices

### Combat System Anti-Patterns to Avoid
- **Magic Numbers in Damage Calculation**: Always use named constants or ScriptableObjects for balance values
- **Inconsistent Targeting Logic**: Ensure auto-target and manual aim use the same priority system
- **State-Dependent Damage**: Avoid damage modifiers that depend on arbitrary state combinations
- **Floating Point Precision Issues**: Use integer arithmetic for damage calculations where possible

### Movement System Best Practices
- **Coyote Time Implementation**: Always include grace periods for better player experience
- **Input Buffering**: Buffer jump inputs for 100-200ms to handle fast presses
- **Collision Layer Management**: Use separate layers for terrain, players, and projectiles
- **Velocity Clamping**: Prevent excessive speeds from accumulation of forces

### Performance Anti-Patterns
- **Update Method Overuse**: Move non-critical updates to FixedUpdate or coroutines
- **Raycast Abuse**: Cache raycast results and use layer masks effectively
- **String Operations in Hot Path**: Avoid string concatenation in Update loops
- **Unnecessary Allocations**: Use object pooling for particles and projectiles

### Balance Design Principles
- **Risk/Reward Clarity**: Make high-risk plays obviously beneficial
- **Counter-Play Visibility**: Ensure players can see and react to threats
- **Progression Transparency**: Show clear paths for character improvement
- **Fairness in Asymmetry**: Balance different archetypes through distinct strengths/weaknesses

### Testing Anti-Patterns
- **Manual Testing Only**: Always include automated tests for core mechanics
- **Single-Player Testing**: Test multiplayer scenarios with bots or multiple instances
- **Optimal Conditions Only**: Test under various network conditions and device specs
- **Feature-Complete Testing**: Test partial implementations can lead to false confidence

## Dependencies
- Unity Physics for 3D platformer mechanics and collision detection
- Netcode for GameObjects synchronization of 3D movement and combat
- ScriptableObject system for RSB coefficient data management
- Animation system supporting character evolution transitions
- Audio system for satisfying crypto coin collection feedback
- State Machine Pattern implementation for character behavior management
- Strategy Pattern for flexible damage calculation algorithms
- Observer Pattern for event-driven system communication
- Command Pattern for input handling with undo capabilities
- Flyweight Pattern for memory-efficient projectile management
- Object Pool Pattern for performance optimization of projectile systems