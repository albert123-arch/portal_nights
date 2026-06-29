# Scene Migration Phase 2 Report

This report documents the Phase 2 infrastructure-only scene migration pass.

## Files Created

- `Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRoot.cs`
- `Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRegistry.cs`
- `Assets/PortalNights/Scripts/Scenes/PortalNightsSceneTransitionManager.cs`
- `Assets/PortalNights/Reports/SceneMigrationPhase2Report.md`

## Class Summary

### `PortalNightsPlanetSceneRoot`

Purpose:
- Holds future per-planet scene references on a single root component.
- Supports editor-safe/manual reference discovery under its own root only.
- Exposes spawn, portal, staff, rift, stabilizer, build point, and bounds accessors.

Key limits:
- No global object search.
- No per-frame work.
- No gameplay wiring.

### `PortalNightsPlanetSceneRegistry`

Purpose:
- Provides a centralized mapping from planet index to future scene metadata.
- Supplies code-defined defaults if no serialized definitions are configured yet.

Key limits:
- No scene loading.
- No dependency on `PortalNightsGameController`.

### `PortalNightsSceneTransitionManager`

Purpose:
- Provides a dormant additive scene-loading skeleton for future phases.
- Tracks the currently loaded additive planet scene and its `PortalNightsPlanetSceneRoot`.

Key limits:
- Not wired into gameplay yet.
- Does not teleport players.
- Does not call `PortalNightsGameController`.
- Does not spawn or despawn `NetworkObject`s.

## Current One-Scene Workflow

The current one-scene gameplay path remains unchanged:
- `Assets/PortalNights/Scenes/PortalNightsArena.unity` is still the active gameplay scene.
- Existing inactive planet roots inside `PortalNightsArena` remain the current source of future-planet content.
- `PortalNightsGameController` was not modified in this phase.

## Scene / Settings Safety

- No scenes were split in this phase.
- `PortalNightsArena.unity` was not modified.
- `EditorBuildSettings.asset` was not modified.
- `ProjectSettings` were not modified.

## Why Addressables Are Not Added Yet

Addressables are intentionally deferred because this phase only establishes compile-safe structure for future additive loading. Introducing Addressables now would expand the migration scope into asset-group configuration, asynchronous dependency policy, and build pipeline changes before the additive scene contract is stabilized.

## Risks: Scene-Placed Netcode Objects In Additive Scenes

The audited future planet roots already contain scene-placed `NetworkObject`s. Those objects are the main risk for later scene splitting because the current one-scene fallback uses root activation plus local `SpawnUnspawnedNetworkObjectsForPlanetRoot(...)` behavior. Once planets become true additive scenes, those objects will need deliberate Netcode scene-management handling instead of ad hoc spawning.

Phase 2 intentionally does **not** solve that yet.

## Recommended Phase 3 Steps

1. Create the first real planet scene asset while keeping the current one-scene fallback intact.
2. Move only one future planet root into its own scene as a controlled migration pilot.
3. Place `PortalNightsPlanetSceneRoot` on that migrated scene root and validate autodiscovery.
4. Add safe editor tooling to compare old root references vs new scene-root references.
5. Keep runtime gameplay still driven by the current path until additive load/unload is validated.

## Validation

### Compile Check

- Validation method: Unity Editor batch compile/import refresh
- Editor version: `6000.3.11f1`
- Result: success
- New Phase 2 scripts compiled successfully

Observed warnings during compile:
- Existing obsolete RPC warning in `Assets/PortalNights/Scripts/PortalNightsPlayerController.cs`
- Existing unused-field warnings under `Assets/SlimUI/...`

No new compile errors were introduced by the Phase 2 infrastructure files.

## git status -sb

```text
## codex/portal-nights-full-stage-push...origin/codex/portal-nights-full-stage-push
?? Assets/PortalNights/Reports/SceneMigrationPhase2Report.md
?? Assets/PortalNights/Reports/SceneMigrationPhase2Report.md.meta
?? Assets/PortalNights/Scripts/Scenes.meta
?? Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRegistry.cs
?? Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRegistry.cs.meta
?? Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRoot.cs
?? Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRoot.cs.meta
?? Assets/PortalNights/Scripts/Scenes/PortalNightsSceneTransitionManager.cs
?? Assets/PortalNights/Scripts/Scenes/PortalNightsSceneTransitionManager.cs.meta
```
