# MOBA Combat Networking Design

## Goals
- Establish a server-authoritative pipeline for ability casts, damage resolution, and scoring events.
- Minimise bandwidth while providing deterministic outcomes aligned with competitive MOBA requirements.
- Support future latency mitigation strategies (client-side prediction, reconciliation) without rewriting the core contract.

## Authority Model
1. **Client Input:** Player initiates an ability via `EnhancedAbilitySystem.TryCastAbility`. Client immediately queues the request and performs optimistic UI feedback (cooldown greying, cast animation start).
2. **Server Validation:** Server receives the request (via `ServerRpc`) and validates:
   - ability readiness (cooldown, mana, global cooldown),
   - actor state (alive, owned by sender, not stunned, etc.),
   - optional targeting payload.
3. **Damage Application:** Server calculates hit targets using authoritative world state (e.g., query colliders, track projectiles). Damage and combat events are applied to `IDamageable` recipients server-side.
4. **Client Broadcast:** Server propagates results through a compact `ClientRpc` (hit list, damage values, status flags). Clients update health bars, VFX, and UI accordingly.
5. **Score/Timer Sync:** Server updates match-scoped NetworkVariables (score, timer, victory state). Clients rely on server snapshots for HUD display.

## Messaging Choices
- **AbilityCastRequest** – `ServerRpc` with payload: ability index, optional target position/direction, local timestamp for analytics.
- **AbilityCastResult** – `ClientRpc` multicast: cast outcome (success/fail reason), final cooldown start time, mana delta, list of hit targets + damage, optional VFX metadata.
- **DamageNotification** – For repeated hits (DoT, AOE tick), we can reuse `AbilityCastResult` or introduce a lightweight `DamageTickMessage` depending on frequency.
- **ScoreUpdate / TimerUpdate** – NetworkVariables in `SimpleGameManager` to avoid redundant RPC spam. Server writes authoritative values; clients observe.

## Data Structures
```csharp
struct AbilityCastRequest
{
    public ushort AbilityIndex;
    public Vector3 TargetPosition;
    public Vector3 TargetDirection;
    public float ClientTime;
}

struct AbilityCastResult
{
    public ushort AbilityIndex;
    public bool Approved;
    public AbilityFailureCode FailureCode;
    public float CooldownStartServerTime;
    public float CurrentMana;
    public FixedList128Bytes<TargetHit> Hits;
}

struct TargetHit
{
    public NetworkObjectReference Target;
    public float Damage;
    public bool Killed;
}
```
(Exact container types may change depending on final payload size.)

## Integration Points
- `ProductionNetworkManager`: new API surface to submit cast requests and broadcast results. Provides authority checks and exposes events for analytics/UI layers.
- `EnhancedAbilitySystem`: refactored to split client prediction from server truth. Client uses local queue + optimistic UI; server logic re-uses existing mana/cooldown code.
- `SimpleGameManager`: convert score/time to NetworkVariables and raise events when values change. Ensure respawn/coin collection triggers go through server path.
- `EnemyController`: subscribe to damage notifications so AI reacts to being hit, ensuring consistent behaviour across host/clients.

## Testing Strategy
- **Unit/PlayMode:** simulate host + client in editor, issue ability cast, ensure only server applies damage and clients reflect results.
- **Latency Scenarios:** use Unity Transport simulator to inject latency/jitter; confirm UI stays responsive and reconciles with server messages.
- **Regression:** confirm that local-only scenarios (single-player tests) still operate with server path stubbed out (host acts as both client/server).

## Future Extensions
- Client-side prediction for quick projectile abilities (optional).
- Snapshot interpolation for damage feedback on high-ping clients.
- Cheat telemetry (ability spam, suspicious targeting) feeding `ErrorHandler` thresholds.
