MOBA Game Design Document (GDD) v0.1
Design Pillars

Vertical Movement & Traversal – The core novelty of this MOBA is its emphasis on vertical play. Lanes run north to south with multiple levels connected by jump pads and climbable surfaces. Players must time their jumps, double jumps and knockback recoils to control high ground and drop on enemies. A deterministic state machine governs actions like idling, walking, running and jumping; characters can only move to certain states from their current state
docs.unity3d.com
.

Short, Intense Matches – Matches are intentionally brief to encourage fast‑paced, repeatable play. Modes include a 1‑minute sprint for quick tests, a 5‑minute mid‑length bout and a 10‑minute standard match. There is no overtime; if teams are tied at the end of regulation, the team that deposits the next set of points wins.

Scoring & Deposits – Victory is achieved by collecting score orbs and depositing them at goal zones. Depositing takes time based on baseline channel durations and additive speed factors from the scoring formulas (see the technical design). Successful deposits not only earn points but also build ultimate energy.

Role Diversity & Hero Identity – Five starting hero archetypes (Bruiser, Ranged DPS, Support, Tank and Mage) deliver distinct playstyles. Each hero has different base statistics (health, mana, stamina, strength, agility, intellect, defense), jump heights and ability coefficients. Base stats are stored in ScriptableObject assets for easy tuning; ScriptableObjects exist as assets and act as shared data containers, separate from GameObjects
docs.unity3d.com
.

Fairness & Determinism – The map is symmetrical, and all mechanics run on a fixed simulation step. The server owns authoritative decisions, while clients predict locally and reconcile. Using state machines and ScriptableObjects ensures determinism and reproducible behaviour
docs.unity3d.com
docs.unity3d.com
.

Game Overview

This MOBA prototype pits two teams against each other in a vertically stacked arena. Lanes run north–south and feature raised platforms, jump pads and branching paths. Teams spawn at opposite ends and compete to gather score orbs, defeat neutral creatures and out‑maneuver opponents. The first team to reach the target score or to have the highest score when time expires wins.

Map & Lane Structure

Two Main Lanes – The map contains two long lanes separated by a central jungle. Each lane has multiple tiers connected by ramps and jump pads, allowing players to leap from a lower tier to a higher one or drop down to ambush opponents.

Jungle & Neutral Objectives – Between the lanes lies a jungle zone filled with neutral creatures that drop score orbs and temporary buffs. Controlling the jungle grants access to jump pads leading to the enemy’s scoring area.

Goal Zones – Each team has a deposit area near their base. Depositing score orbs requires channeling; the time to deposit is determined by baseline channel times and speed factors defined in the ScoringDef ScriptableObject. Deposits can be interrupted by enemy attacks.

Hero Roles & Archetypes

Bruiser – Balanced offense and durability. Base stats: 130 health, 60 mana, 70 stamina, 15 strength, 12 agility, 8 intellect, 10 defense. Abilities include a leap that deals area damage and a charge that knocks enemies back.

Ranged DPS – High damage at long range but frail. Base stats: 100 health, 80 mana, 60 stamina, 8 strength, 14 agility, 10 intellect, 5 defense. Abilities include a double jump with rapid‑fire projectiles and a knockback shot.

Support – Focuses on healing and crowd control. Base stats: 110 health, 100 mana, 60 stamina, 6 strength, 10 agility, 14 intellect, 7 defense. Abilities include an area heal, a bubble that reduces incoming damage and a gentle knockback.

Tank – Soaks damage and controls space. Base stats: 160 health, 50 mana, 80 stamina, 10 strength, 8 agility, 6 intellect, 15 defense. Abilities include a ground slam that knocks enemies upward and a shield that absorbs damage.

Mage – Burst damage at medium range. Base stats: 100 health, 120 mana, 60 stamina, 6 strength, 10 agility, 16 intellect, 5 defense. Abilities include a double jump that leaves a lingering damage zone and a telekinetic push.

All heroes share basic movements: walking, running, high jump, double jump and a knockback attack. Ultimate abilities are unique to each archetype and require charging via damage dealt, damage taken and deposit actions.

Abilities & Mechanics

High Jump / Double Jump – Each hero can perform a high jump followed by a mid‑air double jump. The jump physics (initial velocity, gravity, window for second jump) are defined in a JumpPhysicsDef asset. Jumping transitions the character from a grounded state to an airborne state in the state machine
docs.unity3d.com
.

Knockback – A melee or ranged attack that pushes the target backward and interrupts channeling. Knockback force scales with the attacker’s strength and the target’s defense; the exact formula is defined in the combat model (see TDD v0.1). If the knockback sends a player off a ledge, fall damage is applied.

Ultimate Abilities – Each archetype has an ultimate ability triggered once its energy bar is full. Energy accumulates over time, from dealing damage and from scoring. The fill rate and cooldown after use are defined in an UltimateEnergyDef asset.

Damage & Defense – Base damage is derived from a hero’s primary stat (strength for melee, agility for ranged, intellect for magic). Incoming damage is reduced using a defense formula. The technical design document details the RSB damage model and effective HP calculations.

Match Modes & Timers

1‑Minute “Clash” Mode – Two teams of three players face off on a condensed version of the map. Scores are typically low; the first team to deposit 50 points or hold the most at the end wins.

5‑Minute “Skirmish” Mode – Standard quick‑play match for 3v3 or 5v5. Neutral objectives spawn at fixed times. The first team to reach 200 points or lead at time’s end wins.

10‑Minute “Epic” Mode – Full‑length match with 5v5 teams. Neutral objectives include a major boss that grants a team buff when defeated. Victory condition is 500 points or highest score at time’s end. There is no overtime; ties are resolved by sudden‑deposit.

Scoring System

Players collect score orbs dropped by neutral creatures and enemy players. Each orb is worth 1 point. To convert orbs into team score, players must deposit them at their goal zone:

Channeling Time – Depositing requires holding still for a baseline duration. The ScoringDef asset defines a baseline time (e.g., 3 seconds for a 10‑orb deposit) and speed factors that reduce channel time when teammates are nearby or buffs are active. The deposit is cancelled if the player takes damage or moves.

Return Penalty – If a player dies while carrying unbanked orbs, those orbs drop on the ground and can be picked up by anyone. Death also feeds ultimate energy to the killer.

Score Milestones – Reaching certain score thresholds triggers events such as additional neutral spawns or temporary buffs. These milestones are defined in the MatchModeDef asset.

Ultimate Energy System

Ultimate abilities are fueled by an energy bar that fills through multiple actions:

Passive Regeneration – Heroes gain a small amount of energy each second.

Combat Contribution – Dealing damage and assisting in kills grants energy proportional to the damage or contribution.

Objective Play – Collecting and depositing orbs grants bonus energy. Larger deposits yield more energy.

Once the energy bar reaches 100%, the ultimate can be activated. After use, the bar resets and enters a cooldown period defined by the UltimateEnergyDef asset.

Match Flow

Lobby – Players select heroes, load into the map and view match settings.

Spawn – The player spawn state machine instantiates and configures each player. It ensures that base stats are assigned and validated before the match begins.

Match Start – Teams leave their bases, gather orbs, control neutral objectives and engage in combat.

Deposits & Objectives – Players return to deposit orbs, while others contest the enemy’s deposits. Neutral objectives spawn on a schedule.

End Game – When the timer reaches zero or a score cap is reached, the match ends. A sudden‑deposit tiebreaker occurs if scores are tied.

Results & Replay – Post‑match results are displayed, and players can review highlights. The match state machine transitions to results and then returns to lobby or replay.

HUD & Accessibility

Score & Timer Display – The HUD shows team scores, match time remaining and personal orb count. Icons and colors are designed for colour‑blind accessibility.

Ability & Energy Bars – Ability icons display cooldown timers. The ultimate energy bar fills clockwise around the ability icon.

Minimap & Ping System – A simplified minimap shows lane positions, neutral objectives and ping indicators for quick communication.

Audio & Haptics – Sound cues inform players of deposits, ultimate availability and state changes. Haptic feedback (if supported) accentuates knockbacks and ultimate activations.

Tuneable Parameters & Data Assets

The game exposes most numerical values via ScriptableObject assets to enable quick tuning without code changes:

BaseStatsTemplate – Defines base stats and jump parameters for each hero archetype. Because ScriptableObjects are assets independent of GameObjects, they act as shared data stores that avoid data duplication
docs.unity3d.com
.

AbilityDef – Stores metadata for each ability (name, cast time, cooldown, input binding, damage coefficients, knockback strength).

JumpPhysicsDef – Specifies physical parameters for high jump and double jump (initial upward velocity, gravity, time window for second jump). These values are used in deterministic physics calculations.

MatchModeDef – Contains match length, team size and milestone events for each mode.

ScoringDef – Defines baseline deposit times and speed modifiers. Designers can adjust deposit durations and reward scaling here.

UltimateEnergyDef – Stores passive energy regen rate, combat contribution values, deposit bonuses and post‑activation cooldown duration.

Fairness & Balance Considerations

Symmetric Layout – Both teams’ bases and lanes are mirrored to reduce positional advantages. Neutral objectives spawn at equidistant points from both bases.

Server Authority – All critical gameplay decisions (movement resolution, combat damage, deposits) occur on the server. Clients predict movement locally and reconcile based on server state.

Deterministic Simulation – The game runs on a fixed update loop, and physics and random seeds are synchronized. State machines restrict transitions, preventing impossible actions such as jumping directly from idle to a running jump
docs.unity3d.com
.

Clear Counterplay – Every ability has a telegraph and a counter. For example, knockback attacks have wind‑up animations, and double jumps can be baited by holding crowd control spells.

Glossary

Orb – A collectible item dropped by neutral monsters or enemies, worth one score point when deposited.

Channeling – The act of standing still at a goal zone to deposit orbs. Channeling can be interrupted by damage or movement.

Neutral Objective – A non‑player enemy or structure that grants buffs or points when defeated.

State Machine – A system composed of states and transitions where an entity can only be in one state at a time, and transitions are restricted by rules
docs.unity3d.com
.

ScriptableObject – A serializable asset that stores data independent of any GameObject. It is commonly used to hold shared data for multiple objects, reducing duplication
docs.unity3d.com
.