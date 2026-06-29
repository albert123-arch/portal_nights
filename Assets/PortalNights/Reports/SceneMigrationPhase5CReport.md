# Scene Migration Phase 5C Report

This report documents the experimental Core + Planet1 binding pass. The legacy `PortalNightsArena` workflow remains the default path, while `PortalNightsCore.unity` now carries the minimal saved flags needed for a future additive Planet1 bootstrap.

## Files Changed

- `Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRoot.cs`
- `Assets/PortalNights/Scripts/Scenes/PortalNightsSceneModeBootstrap.cs`
- `Assets/PortalNights/Scripts/PortalNightsGameController.cs`
- `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- `Assets/PortalNights/Editor/PortalNightsPlanetSceneMigrationTool.cs`
- `Assets/PortalNights/Reports/SceneMigrationPhase5CReport.md`

## Dependency Chain Note

`PortalNightsGameController.Awake()` caches scene references early, `Start()` binds HUD/player-friendly colliders, `OnNetworkSpawn()` starts `InitializeServerStateAfterSpawn()`, and `InitializeServerState()` then resets Planet1 state before waves use `portalSpawn`, `leftLanePath`, `rightLanePath`, `playerSpawnPoints`, and `buildPoints`. `GetPlayerSpawnPosition()` and `MakeDefenseObjectsPlayerFriendly()` also need those references to exist before the first real gameplay tick.

The experimental Phase 5C binding now waits for a registered `PortalNightsPlanetSceneRoot` when `experimentalSceneMode` and `waitForExperimentalSceneBootstrap` are both enabled, so the additive Planet1 scene can provide `coreHealth`, spawn points, lane paths, and build pads before server initialization finishes.

## GameController Changes

- Setup success: yes
- Core scene path: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Bootstrap exists in Core: yes
- GameController exists in Core: yes
- Saved bootstrap `enableSceneMode` is false: yes
- Saved bootstrap `initialPlanetIndex`: 1
- Saved `experimentalSceneMode` is true: yes
- Saved `waitForExperimentalSceneBootstrap` is true: yes
- Gating proof: legacy behavior changes only when both new GameController flags are enabled, and those flags are saved only in `PortalNightsCore.unity` for this phase.

## Planet1 Root Binding Validation

- Validation success: yes
- Core scene path: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Planet1 scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity`
- Bootstrap exists in Core: yes
- GameController exists in Core: yes
- Saved bootstrap `enableSceneMode` is false: yes
- Saved bootstrap `initialPlanetIndex`: 1
- Saved `experimentalSceneMode` is true: yes
- Saved `waitForExperimentalSceneBootstrap` is true: yes
- Planet1 expected root found: yes
- Planet1 scene root exists: yes
- Planet1 `PortalNightsPlanetSceneRoot` count: 1
- Planet1 `PlanetIndex == 1`: yes
- Planet1 `ValidateSetup(true)` passed: yes
- Player spawn points discovered: 6
- Build points discovered: 8
- Core health discovered: yes
- Left lane path discovered: yes
- Right lane path discovered: yes
- Portal spawn discovered: no (warning-only if false)
- `PortalNightsArena.unity` still first enabled Build Settings scene: yes

## Core Scene Saved Settings

- `PortalNightsSceneModeBootstrap.enableSceneMode`: false
- `PortalNightsSceneModeBootstrap.initialPlanetIndex`: 1
- `PN_GameController.experimentalSceneMode`: yes
- `PN_GameController.waitForExperimentalSceneBootstrap`: yes

## Manual Play Mode Test

- Was a manual Play Mode test run: no
- Blocker: there is no safe automated Play Mode harness in this shell workflow, and enabling the experimental additive Netcode path interactively would require a manual editor session. The code path was left compiled and disabled-by-bootstrap in committed state instead of guessing at runtime fixes.

## Safety Confirmations

- `PortalNightsArena.unity` was not modified: yes
- `PortalNightsArena.unity` remains first Build Settings scene: yes
- Bootstrap `enableSceneMode` is false in the committed Core scene: yes
- Legacy one-scene workflow remains unchanged: yes
- P2-P5 gameplay was not wired in this phase: yes
- No archives/zip/backups were created: yes
- No WebGL build was run: yes

## Remaining Netcode Risks

- Experimental Core + Planet1 startup has editor validation only in this phase.
- Scene-based player teleporting, one-wave verification, and additive gameplay ownership still need a controlled Play Mode pass.
- Future planets are intentionally still handled by the legacy scene path.

## Recommended Phase 5D

Make the experimental Core + Planet1 mode playable for one wave while still keeping the legacy `PortalNightsArena` startup path untouched.

## Git Checks

### git status -sb

```text
## codex/portal-nights-full-stage-push...origin/codex/portal-nights-full-stage-push [ahead 6]
```

### git diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity

```text
(no output)
```

### git diff --name-only -- ProjectSettings/EditorBuildSettings.asset

```text
(no output)
```

### git diff --name-only -- Assets/SlimUI

```text
(no output)
```

