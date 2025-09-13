# Combat Formulas

This document outlines the generalized combat formulas derived from Unite‑DB and UniteAPI, adapted for a legally distinct multiplayer online battle arena (MOBA).

## Damage Calculation (RSB Model)

The game uses a Ratio–Slider–Base (RSB) model for calculating damage for both physical and magical abilities. The model is defined as follows:

`damage = floor(R × AttackStat + S × (Level − 1) + B)`

- **R (Ratio):** Multiplier for scaling damage with the unit’s attack stat.
- **S (Slider):** Linear per-level addition, increasing damage as the unit’s level rises.
- **B (Base):** Flat damage always applied, regardless of level.

This formula applies to both physical (using Attack stat) and magical (using Special Attack stat) abilities.

### Example RSB Values

A basic attack might have `R = 1.0`, `S = 0`, and `B = 0`, meaning it scales directly with the attack stat and does not add extra damage per level or flat base damage.

### Variable Definitions

| Symbol | Meaning                    | Notes                           |
|-------|---------------------------|--------------------------------|
| R     | Ratio multiplier          | Larger values scale damage more strongly |
| S     | Per-level slider          | Adds fixed damage per level       |
| B     | Base damage               | Flat damage applied always       |
| AttackStat | Physical or magical attack stat | Determines scaling for physical or magical abilities |

## Defense and Damage Reduction

Incoming damage is mitigated using a defense value. The effective damage taken after defense is computed as:

`DamageTaken = floor( DamageOutput × 600 / (600 + Defense) )`

Where `DamageOutput` is the raw damage calculated via the RSB model. Higher defense reduces more damage, but with diminishing returns due to the constant 600 in the denominator.

### Flat Damage Reduction

Some characters or effects apply a flat percentage reduction after defense is applied. The formula becomes:

`DamageTaken = floor( floor( DamageOutput × 600 / (600 + Defense) ) × (1 − flat_reduction) )`

Multiple sources of flat reduction add together. For instance, two separate 35 % reductions result in a combined 70 % reduction.

---
