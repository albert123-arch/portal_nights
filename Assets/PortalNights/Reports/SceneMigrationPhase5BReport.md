# Scene Migration Phase 5B Report

This report documents Phase 5B: an experimental Core + Planet1 bootstrap path that stays disabled by default so the current PortalNightsArena workflow is untouched.

## Files Changed

- `Assets/PortalNights/Scripts/Scenes/PortalNightsSceneModeBootstrap.cs`
- `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- `Assets/PortalNights/Editor/PortalNightsPlanetSceneMigrationTool.cs`
- `Assets/PortalNights/Reports/SceneMigrationPhase5BReport.md`
- `Assets/PortalNights/Scripts/PortalNightsGameController.cs`: no change

## Components Added

No Core bootstrap setup has been recorded yet.

## Validation Results

- Validation success: yes
- Core scene path: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Planet1 scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity`
- Core scene root count: 8
- Planet1 scene root count: 1
- Registry exists in Core: yes
- TransitionManager exists in Core: yes
- Bootstrap exists in Core: yes
- GameController exists in Core: yes
- HUD exists in Core: yes
- NetworkManager exists in Core: yes
- Bootstrap `enableSceneMode` is false: yes
- Bootstrap `initialPlanetIndex`: 1
- Bootstrap.transitionManager reference valid: yes
- Bootstrap.registry reference valid: yes
- Bootstrap.gameController reference valid: yes
- Planet1 `PortalNightsPlanetSceneRoot` count: 1
- Planet1 expected root found: yes
- Planet1 expected root active: yes
- Planet1 scene root exists: yes
- Planet1 planetIndex matches: yes
- Planet1 `ValidateSetup(true)` passed: yes
- Core + Planet1 coexist additively in editor: yes
- `PortalNightsArena.unity` still first enabled Build Settings scene: yes
- Core metrics:
- Object count: 24
- Renderer count: 0
- Collider count: 0
- Light count: 1
- Particle count: 0
- MonoBehaviour count: 36
- NetworkObject count: 1
- Planet1 metrics:
- Object count: 272
- Renderer count: 226
- Collider count: 43
- Light count: 10
- Particle count: 7
- MonoBehaviour count: 21
- NetworkObject count: 9

## Manual Experimental Toggle Notes

1. Open `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`.
2. Select `PN_SceneModeServices` and temporarily enable `PortalNightsSceneModeBootstrap.enableSceneMode`.
3. Enter Play Mode and verify Planet1 loads additively.
4. Revert `enableSceneMode` back to `false` before saving or committing.

## Safety Confirmations

- `PortalNightsArena.unity` was not modified: yes
- `PortalNightsArena.unity` remains first Build Settings scene: yes
- Bootstrap `enableSceneMode` is false in the committed Core scene: yes
- Current one-scene workflow remains unchanged: yes
- Current gameplay still uses `PortalNightsArena`: yes
- `PortalNightsGameController.cs` was modified: no
- No archives/zip/backups were created: yes
- No WebGL build was run: yes

## Remaining Risks

- Full Netcode additive scene integration is still not implemented.
- `PortalNightsGameController` still follows legacy one-scene assumptions and is not yet bound to `PortalNightsPlanetSceneRoot` references.
- P2-P5 are intentionally not wired into the experimental bootstrap yet.

## Why P2-P5 Are Not Wired Yet

Phase 5B is limited to an experimental Core + Planet1 startup path. Future planets still need scene-root driven gameplay binding and explicit additive Netcode handling before they can safely join the runtime flow.

## Recommended Phase 5C

Enable the experimental Core + Planet1 Play Mode test locally, then bind `PortalNightsGameController` references from `PortalNightsPlanetSceneRoot` without replacing the legacy one-scene path.

## Git Checks

### git status -sb

```text
## codex/portal-nights-full-stage-push...origin/codex/portal-nights-full-stage-push [ahead 5]
```

### git diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity

```text
(no output)
```

### git diff --name-only -- ProjectSettings/EditorBuildSettings.asset

```text
(no output)
```

### git diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs

```text
(no output)
```

