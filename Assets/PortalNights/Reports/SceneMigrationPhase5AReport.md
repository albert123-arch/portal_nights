# Scene Migration Phase 5A Report

This report documents Phase 5A: Build Settings registration for generated scenes and editor-only additive validation, without wiring runtime gameplay to the new scene flow yet.

## Build Settings Final Scene List

- `PortalNightsArena.unity` first enabled scene: yes
- Core scene registered: yes
- Planet scenes registered: yes
- Duplicate scene paths: none
- Missing scene assets: none
- Missing Build Settings registrations: none

1. `Assets/PortalNights/Scenes/PortalNightsArena.unity` (enabled)
2. `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity` (enabled)
3. `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity` (enabled)
4. `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet2_CrystalMoon.unity` (enabled)
5. `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet3_AshRelayStation.unity` (enabled)
6. `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet4_SwarmExpanse.unity` (enabled)
7. `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet5_CrimsonSingularity.unity` (enabled)
8. `Assets/Core/TestScenes/[BB] Core.unity` (enabled)
9. `Assets/Core/TestScenes/[BB] Core MultiplayerSession.unity` (enabled)
10. `Assets/Platformer/TestScenes/[BB] Platformer.unity` (enabled)
11. `Assets/Platformer/TestScenes/[BB] Platformer MultiplayerSession.unity` (enabled)
12. `Assets/Shooter/TestScenes/[BB] Shooter.unity` (enabled)
13. `Assets/Shooter/TestScenes/[BB] Shooter MultiplayerSession.unity` (enabled)

## Additive Editor Validation

- Overall validation success: yes
- Source scene opened for validation: yes
- Source scene reopened at end: yes

### Core Scene

- Scene path: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Validation success: yes
- Root count: 7
- Contains NetworkManager: yes
- Contains PN_GameController: yes
- Contains PN_HUD_Canvas: yes
- Contains EventSystem: yes
- Contains Main Camera: yes
- Contains PortalNightsArena root: no
- Contains PN_Central_Core: no
- Contains PN_BuildPads: no
- PN_PlayerSpawn_* count: 0
- Forbidden future roots: none
- Forbidden Planet 1 objects: none
- Object count: 23
- Renderer count: 0
- Collider count: 0
- Light count: 1
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 1

### Planet Scenes

#### Planet 1 — `PortalNightsPlanet1_Defense`

- Scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity`
- Validation success: yes
- Root count: 1
- Expected root name: `Planet1_Defense`
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- Has `PortalNightsPlanetSceneRoot` on expected root: yes
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains `PortalNightsArena` root: no
- Contains player controller: no
- Forbidden global objects: none
- Object count: 272
- Renderer count: 226
- Collider count: 43
- Light count: 10
- Particle count: 7
- MonoBehaviour count: 21
- NetworkObject count: 9

#### Planet 2 — `PortalNightsPlanet2_CrystalMoon`

- Scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet2_CrystalMoon.unity`
- Validation success: yes
- Root count: 1
- Expected root name: `Planet2_CrystalMoon`
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- Has `PortalNightsPlanetSceneRoot` on expected root: yes
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains `PortalNightsArena` root: no
- Contains player controller: no
- Forbidden global objects: none
- Object count: 388
- Renderer count: 316
- Collider count: 32
- Light count: 12
- Particle count: 3
- MonoBehaviour count: 19
- NetworkObject count: 9

#### Planet 3 — `PortalNightsPlanet3_AshRelayStation`

- Scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet3_AshRelayStation.unity`
- Validation success: yes
- Root count: 1
- Expected root name: `Planet3_AshRelayStation`
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- Has `PortalNightsPlanetSceneRoot` on expected root: yes
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains `PortalNightsArena` root: no
- Contains player controller: no
- Forbidden global objects: none
- Object count: 182
- Renderer count: 117
- Collider count: 129
- Light count: 22
- Particle count: 0
- MonoBehaviour count: 41
- NetworkObject count: 18

#### Planet 4 — `PortalNightsPlanet4_SwarmExpanse`

- Scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet4_SwarmExpanse.unity`
- Validation success: yes
- Root count: 1
- Expected root name: `Planet4_SwarmExpanse`
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- Has `PortalNightsPlanetSceneRoot` on expected root: yes
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains `PortalNightsArena` root: no
- Contains player controller: no
- Forbidden global objects: none
- Object count: 434
- Renderer count: 346
- Collider count: 358
- Light count: 24
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 18

#### Planet 5 — `PortalNightsPlanet5_CrimsonSingularity`

- Scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet5_CrimsonSingularity.unity`
- Validation success: yes
- Root count: 1
- Expected root name: `Planet5_CrimsonSingularity`
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- Has `PortalNightsPlanetSceneRoot` on expected root: yes
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains `PortalNightsArena` root: no
- Contains player controller: no
- Forbidden global objects: none
- Object count: 302
- Renderer count: 235
- Collider count: 239
- Light count: 20
- Particle count: 0
- MonoBehaviour count: 22
- NetworkObject count: 11


## Safety Confirmations

- `PortalNightsArena.unity` remains first enabled scene in Build Settings: yes
- Core and Planet1-P5 generated scenes are registered: yes
- `PortalNightsGameController.cs` was not modified: yes
- `PortalNightsArena.unity` was not modified: yes
- Current one-scene gameplay path remains unchanged: yes
- Gameplay is still legacy one-scene: yes
- No WebGL build was run: yes
- No archives/zip/backups were created: yes

## Why Gameplay Is Still Legacy One-Scene

Phase 5A only registers scenes and validates additive editor loading safety. It intentionally does not replace the current `PortalNightsArena.unity` runtime path, does not change startup flow, and does not call gameplay code from the transition manager yet.

## Recommended Phase 5B

Experimental scene-mode bootstrap for Core + Planet1 only, behind a disabled-by-default toggle.

## Git Checks

### git status -sb

```text
## codex/portal-nights-full-stage-push...origin/codex/portal-nights-full-stage-push [ahead 4]
```

### git diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity

```text
(no output)
```

### git diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs

```text
(no output)
```

### git diff --name-only -- ProjectSettings/EditorBuildSettings.asset

```text
(no output)
```

