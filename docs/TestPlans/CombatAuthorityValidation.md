# Combat Authority Validation Plan

## Host/Client Play Session
1. **Launch two editor instances** (or editor + standalone build).
2. Start host via `ProductionNetworkManager` UI hook.
3. Connect client; verify both players spawn through `SimpleGameManager` and show identical score/time HUDs.
4. Cast abilities from client: mana, cooldown, and damage outcome should follow server timing. Observe health bars updating via `IDamageSnapshotReceiver`.
5. Kill an enemy/remote player and ensure respawn timer only starts once the server approves.

## Latency Scenario
1. Enable Unity Transport simulator (150 ms, 20 ms jitter, 2% loss).
2. Repeat ability and scoring actions; verify UI snaps to server values and no duplicate damage occurs.

## Regression Checklist
- Coin collection adds score only when host picks it up (client attempts ignored).
- Manual `RestartGame` / `StartMatch` calls on clients have no effect; server console logs warning.
- NetworkVariables (`teamScores`, `timeRemaining`, `gameActive`) change only on server.
- Logs show ability failures with descriptive `AbilityFailureCode` when preconditions fail.

Record anomalies in `docs/TestPlans/CombatAuthorityValidation.md` (append “Observations” section) and report back.
