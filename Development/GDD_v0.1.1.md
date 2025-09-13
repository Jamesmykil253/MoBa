# GDD_v0.1.md

## Scope (v0.1)
- **1 map**, mirrored lanes; **2 archetypes**; **4 abilities** total (2 per hero).  
- Core loop (farm/fight/score), Locomotion/Ability/Scoring FSMs, Spawn pipeline, simple match loop.  
- Netcode baseline (server auth, client prediction).  
- Tuning targets: 8–12 min matches, average 4 deposits per player, 1–2 ultimates per minute team-wide.

## MVP FSMs
- **Locomotion**: `Grounded ↔ Airborne`, knockback interrupts, global `Disabled`.  
- **Ability**: `Idle → Casting → Executing → Cooldown`.  
- **Scoring**: `Carrying → Channeling → (Deposited|Interrupted)`.

## Balance Defaults (editable in SOs)
- Scoring baselines (seconds): 1–6→0.5, 7–12→1.0, 13–18→1.5, 19–24→2.0, 25–33→3.0. :contentReference[oaicite:10]{index=10}  
- Team synergy reductions per ally (example): 1→0.70, 2→0.65, 3→0.60, 4→0.40 time multiplier. :contentReference[oaicite:11]{index=11}  
- Defense curve constant: 600. :contentReference[oaicite:12]{index=12}

## Out of Scope (v0.1)
- Ranked, cosmetics, advanced objectives, replays.

## Risks
- Netcode reconciliation readability; mitigate via small interpolation windows and generous client aim assist.
