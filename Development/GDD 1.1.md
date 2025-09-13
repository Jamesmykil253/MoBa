# GDD.md

## Vision & Pillars
A fast, readable, team-fight-first MOBA where decisions are clear, actions are crisp, and victories are earned through coordination and timing.

**Pillars**
1) **Clarity over chaos** – readable telegraphs, short TTK bursts with counterplay.  
2) **Deterministic feel** – fixed-tick simulation; explicit state machines avoid “impossible” actions.  
3) **Data-driven** – designers iterate via ScriptableObjects (SO) without code changes.  
4) **Low friction teamplay** – ally proximity matters (scoring synergy, auras), generous comms.

## Core Loop
Farm → Fight → Score → Regroup. Risk escalates as carried orbs increase channel risk; ally presence accelerates scoring.

## Data Model (ScriptableObjects)
- **BaseStatsTemplate** – base stats & jump params per archetype (shared SO minimizes duplication).  
- **AbilityDef** – cast time, cooldown, inputs, RSB coefficients, knockback.  
- **JumpPhysicsDef** – initial velocity, gravity, coyote/double-jump windows for deterministic kinematics.  
- **MatchModeDef** – match length, team sizes, milestone events.  
- **ScoringDef** – baselines for deposit times and additive speed factors.  
- **UltimateEnergyDef** – passive regen, event gains, deposit bonuses, post-ultimate cooldown.

> SOs are the single source of balance truth referenced by runtime systems (no hidden constants).

## Network Model
- **Server authoritative** for movement, damage, deposits; clients predict and reconcile.  
- **Tick**: server FixedUpdate at 50 Hz (configurable), client interpolates/extrapolates small windows.  
- **Idempotent RPCs**, sequence numbers for input; snapshots carry state + FSM tags where helpful.

## Core State Machines (Overview)
We use explicit FSMs across gameplay:

- **Locomotion**: `Grounded ↔ Airborne`, interrupts to `Knockback`, global `Disabled`.  
- **Ability**: `Idle → Casting → Executing → Cooldown → Idle`; damage/movement can cancel casting.  
- **Scoring/Deposit**: `Carrying → Channeling → (Deposited | Interrupted)` with ally synergy.  
- **Spawn**: `Idle → InitialSetup → AssignBaseStats → ValidateStats → Finalize → (Spawned | Error)`.  
- **Match Loop**: `Lobby → Loading → MatchStart → InMatch → MatchEnd → Results`.

Determinism goal: transitions are the only way behavior changes; no side-effects outside `Enter/Update/Exit`.

## Combat System (RSB + Defense)
Damage uses RSB:  
`rawDamage = floor(R*Attack + S*(Level-1) + B)` then defense mitigation:  
`taken = floor(rawDamage * 600 / (600 + Defense))`  
eHP link: `EffectiveHP = MaxHP * (1 + Defense/600)`.  
Balance levers: adjust S (level pressure), R (scaling), B (floor), defense curve (600) for global pacing.

## Scoring System
**Speed factors are additive on speed**, not time.  
`speedMultiplier = 1 + Σ(additiveFactors)`  
`channelTime = baseTime / speedMultiplier × teamSynergyMultiplier`  
Team synergy: ally proximity reduces time via a multiplicative factor per ally (configurable in ScoringDef).

## Ultimate Energy
Sources: passive regen, combat damage/assists, scoring deposits, comeback on KO. Post-ultimate cooldown is computed from energy requirement and a global constant in **UltimateEnergyDef**.

## Content & UX
- **Readability**: consistent VFX language, colorblind-safe palettes, short pre-casts.  
- **Counterplay**: every “power moment” has a tell and at least one systemic counter.  
- **Onboarding**: tooltips from SOs, practice range with live numbers.

## Glossary
Orb – 1 point toward scoring. Channeling – stationary deposit action, interruptible by damage/move. Neutral Objective – grants buffs/points when defeated. State Machine – only one active state, explicit transitions. ScriptableObject – shared balance data asset.

## References & Design Notes
- Scoring baselines/synergy live in ScoringDef. :contentReference[oaicite:8]{index=8}  
- Deterministic simulation + FSMs for safe transitions. :contentReference[oaicite:9]{index=9}
