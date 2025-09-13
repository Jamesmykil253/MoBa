# Scoring Formulas

This document summarizes the scoring mechanics derived from Unite‑DB and adapted for a generic MOBA.

## Scoring Speed Factor

Scoring speed is modified by additive factors that accelerate the time it takes to score points. The base scoring time is reduced by adding factors to the numerator of a ratio, and then taking the reciprocal. For example:

- If a player has a factor of **1**, the effective time required becomes `(1 + 1) / 1 = 2`; thus, the action takes 50 % of its original duration.
- A factor of **3** results in `(1 + 3) / 1 = 4`; the scoring time is 25 % of the original.
- Factors stack linearly; factors of 1 and 3 together produce `(1 + 1 + 3) / 1 = 5`, yielding 20 % of base scoring time.

### Team Synergy

Having allies nearby further reduces scoring time: one ally grants a 30 % reduction, two allies grant 35 %, three allies grant 40 %, and four allies grant 60 %. These reductions are applied multiplicatively to the base scoring speed.

### Baseline Scoring Times

Different point amounts have different base times to score. Approximate baselines might look like the following table (values are in seconds):

| Points Deposited | Base Time (s) |
|------------------|---------------|
| 1–6             | 0.5           |
| 7–12            | 1.0           |
| 13–18           | 1.5           |
| 19–24           | 2.0           |
| 25–33           | 3.0           |

These baselines can be tuned for your own MOBA to balance scoring risk and reward.

---
