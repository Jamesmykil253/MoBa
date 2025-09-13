# Ultimate Energy and Cooldown Mechanics

Special abilities (ultimates) require accumulating energy. This document outlines a generic energy system based on the Unite‑DB model.

## Energy Accumulation

Energy is gained through multiple actions:

- **Passive gain:** A small amount of energy is generated automatically each second.
- **Defeating non‑player enemies:** Grants a lump sum of energy to the player who delivers the final hit.
- **Scoring objectives:** Depositing points into goals awards a significant energy bonus.
- **Being defeated:** Players receive energy when they are KO’d, enabling comeback mechanics.
- **Equipment and emblems:** Items may provide percentage bonuses to energy gains.

### Example Energy Values

| Event                      | Energy Gained |
|----------------------------|---------------|
| Passive gain per second    | 900           |
| Defeating a non‑player unit | 5,000         |
| Scoring a goal             | 12,000        |
| Being KO’d                 | 12,000        |

These numbers can be scaled up or down for your own MOBA.

## Cooldown Calculation

The cooldown for an ultimate ability is determined by dividing the energy requirement by a constant. If the ability requires `EnergyRequirement` points, the cooldown (in seconds) is:

`Cooldown = EnergyRequirement / Constant`

For example, with a constant of 900, an ultimate requiring 90,000 energy would have a cooldown of 100 seconds. The constant and energy values can be tuned to achieve desired gameplay pacing.

---
