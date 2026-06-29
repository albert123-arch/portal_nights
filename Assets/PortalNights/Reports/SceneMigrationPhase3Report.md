# Scene Migration Phase 3 Report

This report documents the Phase 3 editor-only migration pass that generates standalone planet scene copies for Planets 2-5.

## Generated Scene Paths

- `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet2_CrystalMoon.unity`
- `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet3_AshRelayStation.unity`
- `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet4_SwarmExpanse.unity`
- `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet5_CrimsonSingularity.unity`

## Dry Run Results

### Planet2_CrystalMoon

- Root found: yes
- Target scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet2_CrystalMoon.unity`
- Existing `PortalNightsPlanetSceneRoot`: no
- Would add `PortalNightsPlanetSceneRoot`: yes
- Object count: 388
- Renderer count: 316
- Collider count: 32
- Light count: 12
- Particle count: 3
- MonoBehaviour count: 18
- NetworkObject count: 9

### Planet3_AshRelayStation

- Root found: yes
- Target scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet3_AshRelayStation.unity`
- Existing `PortalNightsPlanetSceneRoot`: no
- Would add `PortalNightsPlanetSceneRoot`: yes
- Object count: 182
- Renderer count: 117
- Collider count: 129
- Light count: 22
- Particle count: 0
- MonoBehaviour count: 40
- NetworkObject count: 18

### Planet4_SwarmExpanse

- Root found: yes
- Target scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet4_SwarmExpanse.unity`
- Existing `PortalNightsPlanetSceneRoot`: no
- Would add `PortalNightsPlanetSceneRoot`: yes
- Object count: 434
- Renderer count: 346
- Collider count: 358
- Light count: 24
- Particle count: 0
- MonoBehaviour count: 32
- NetworkObject count: 18

### Planet5_CrimsonSingularity

- Root found: yes
- Target scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet5_CrimsonSingularity.unity`
- Existing `PortalNightsPlanetSceneRoot`: no
- Would add `PortalNightsPlanetSceneRoot`: yes
- Object count: 302
- Renderer count: 235
- Collider count: 239
- Light count: 20
- Particle count: 0
- MonoBehaviour count: 21
- NetworkObject count: 11


## Generation Results

### Planet2_CrystalMoon

- Copied successfully: yes
- Added `PortalNightsPlanetSceneRoot`: yes
- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: yes
- `PortalNightsPlanetSceneRoot` count under copied root: 1
- NetworkObject count: 9
- Object count: 388
- Renderer count: 316
- Collider count: 32
- Light count: 12
- Particle count: 3
- MonoBehaviour count: 19
- NetworkObject count: 9

### Planet3_AshRelayStation

- Copied successfully: yes
- Added `PortalNightsPlanetSceneRoot`: yes
- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: yes
- `PortalNightsPlanetSceneRoot` count under copied root: 1
- NetworkObject count: 18
- Object count: 182
- Renderer count: 117
- Collider count: 129
- Light count: 22
- Particle count: 0
- MonoBehaviour count: 41
- NetworkObject count: 18

### Planet4_SwarmExpanse

- Copied successfully: yes
- Added `PortalNightsPlanetSceneRoot`: yes
- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: yes
- `PortalNightsPlanetSceneRoot` count under copied root: 1
- NetworkObject count: 18
- Object count: 434
- Renderer count: 346
- Collider count: 358
- Light count: 24
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 18

### Planet5_CrimsonSingularity

- Copied successfully: yes
- Added `PortalNightsPlanetSceneRoot`: yes
- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: yes
- `PortalNightsPlanetSceneRoot` count under copied root: 1
- NetworkObject count: 11
- Object count: 302
- Renderer count: 235
- Collider count: 239
- Light count: 20
- Particle count: 0
- MonoBehaviour count: 22
- NetworkObject count: 11


## Validation Results

### Planet2_CrystalMoon

- Validation success: yes
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` exists: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains player controller: no
- Forbidden objects copied: none
- Object count: 388
- Renderer count: 316
- Collider count: 32
- Light count: 12
- Particle count: 3
- MonoBehaviour count: 19
- NetworkObject count: 9

### Planet3_AshRelayStation

- Validation success: yes
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` exists: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains player controller: no
- Forbidden objects copied: none
- Object count: 182
- Renderer count: 117
- Collider count: 129
- Light count: 22
- Particle count: 0
- MonoBehaviour count: 41
- NetworkObject count: 18

### Planet4_SwarmExpanse

- Validation success: yes
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` exists: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains player controller: no
- Forbidden objects copied: none
- Object count: 434
- Renderer count: 346
- Collider count: 358
- Light count: 24
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 18

### Planet5_CrimsonSingularity

- Validation success: yes
- Expected root found: yes
- Expected root count: 1
- Root active: yes
- `PortalNightsPlanetSceneRoot` exists: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Contains player controller: no
- Forbidden objects copied: none
- Object count: 302
- Renderer count: 235
- Collider count: 239
- Light count: 20
- Particle count: 0
- MonoBehaviour count: 22
- NetworkObject count: 11


## Phase Notes

- Planet 1 was intentionally deferred because its current content is mixed with core/global objects inside `PortalNightsArena.unity`.
- The Core/global scene split was intentionally deferred because runtime ownership, HUD, transition flow, and additive Netcode wiring still need a dedicated migration phase.
- Scene-placed `NetworkObject`s were copied as scene data only. Additive Netcode activation is still a later Phase 5 task.
- Addressables are still intentionally deferred because scene contracts and load sequencing are not stable enough yet.

## Safety Confirmations

- Current one-scene gameplay path remains unchanged: yes
- `PortalNightsArena.unity` was not modified: yes
- `EditorBuildSettings.asset` was not modified: yes
- `PortalNightsGameController.cs` runtime wiring remains untouched by this tool: yes
- Generated scenes are not added to Build Settings in this phase: yes
- Post-generation Unity batch compile check passed: yes

## Git Checks

### git status -sb

```text
## codex/portal-nights-full-stage-push...origin/codex/portal-nights-full-stage-push [ahead 2]
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


## Recommended Phase 4 Steps

1. Add a validation utility that compares source root metrics to generated standalone scene metrics and highlights mismatches.
2. Pilot one additive load path with a generated planet scene while keeping the current one-scene fallback intact.
3. Define the future Core/global scene contract before attempting a Planet 1 split.
4. Plan explicit Netcode handling for scene-placed `NetworkObject`s before runtime scene activation is introduced.
