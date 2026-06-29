# Scene Migration Phase 4 Report

This report documents the Phase 4 editor-only migration pass that generates a safe Core scene copy and a safe Planet 1 scene copy without wiring them into gameplay yet.

## Scene Paths

- Core scene: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Planet 1 scene: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity`

## Dry Run

- Dry run clean: yes
- Core scene path: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Planet 1 scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity`
- Core objects that would be copied: NetworkManager, PN_GameController, PN_HUD_Canvas, EventSystem, Main Camera, PN_GlobalVolume_Bloom, PN_Sun_KeyLight
- Planet 1 objects that would be copied: Environment, PortalArea, EntranceBridge, LaneFork, LeftLane, RightLane, CoreArena, Background, VFX, PN_Central_Core, PN_BuildPads, PN_PlayerSpawn_1, PN_PlayerSpawn_2, PN_PlayerSpawn_3, PN_PlayerSpawn_4, PN_PlayerSpawn_5, PN_PlayerSpawn_6
- Excluded future planet roots: Planet2_CrystalMoon, Planet3_AshRelayStation, Planet4_SwarmExpanse, Planet5_CrimsonSingularity
- Core metrics:
- Object count: 23
- Renderer count: 0
- Collider count: 0
- Light count: 1
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 1
- Planet 1 metrics:
- Object count: 271
- Renderer count: 226
- Collider count: 43
- Light count: 10
- Particle count: 7
- MonoBehaviour count: 20
- NetworkObject count: 9

## Generation Results

### Core Scene

- Scene path: `Assets/PortalNights/Scenes/Core/PortalNightsCore.unity`
- Copied successfully: yes
- Copied objects: NetworkManager, PN_GameController, PN_HUD_Canvas, EventSystem, Main Camera, PN_GlobalVolume_Bloom, PN_Sun_KeyLight
- Intentionally excluded objects: PortalNightsArena, Planet2_CrystalMoon, Planet3_AshRelayStation, Planet4_SwarmExpanse, Planet5_CrimsonSingularity, PN_Central_Core, PN_BuildPads, PN_PlayerSpawn_1, PN_PlayerSpawn_2, PN_PlayerSpawn_3, PN_PlayerSpawn_4, PN_PlayerSpawn_5, PN_PlayerSpawn_6
- NetworkObject count: 1
- Object count: 23
- Renderer count: 0
- Collider count: 0
- Light count: 1
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 1

### Planet 1 Scene

- Scene path: `Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity`
- Copied successfully: yes
- Copied objects: Environment, PortalArea, EntranceBridge, LaneFork, LeftLane, RightLane, CoreArena, Background, VFX, PN_Central_Core, PN_BuildPads, PN_PlayerSpawn_1, PN_PlayerSpawn_2, PN_PlayerSpawn_3, PN_PlayerSpawn_4, PN_PlayerSpawn_5, PN_PlayerSpawn_6
- Intentionally excluded objects: Planet2_CrystalMoon, Planet3_AshRelayStation, Planet4_SwarmExpanse, Planet5_CrimsonSingularity, NetworkManager, PN_GameController, PN_HUD_Canvas, EventSystem, Main Camera, PN_GlobalVolume_Bloom, PN_Sun_KeyLight, PortalNightsArena
- Added `PortalNightsPlanetSceneRoot`: yes
- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: yes
- `PortalNightsPlanetSceneRoot` count under root: 1
- PN_PlayerSpawn_* count: 6
- NetworkObject count: 9
- Object count: 272
- Renderer count: 226
- Collider count: 43
- Light count: 10
- Particle count: 7
- MonoBehaviour count: 21
- NetworkObject count: 9


## Validation Results

### Core Scene Validation

- Validation success: yes
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
- NetworkObject count: 1
- Object count: 23
- Renderer count: 0
- Collider count: 0
- Light count: 1
- Particle count: 0
- MonoBehaviour count: 33
- NetworkObject count: 1

### Planet 1 Scene Validation

- Validation success: yes
- Has expected root: yes
- Total root count: 1
- Root active: yes
- Has `PortalNightsPlanetSceneRoot`: yes
- `PortalNightsPlanetSceneRoot` count in scene: 1
- planetIndex matches: yes
- `ValidateSetup(true)` passed: yes
- Has PN_Central_Core: yes
- Has PN_BuildPads: yes
- PN_PlayerSpawn_* count: 6
- Forbidden core/global objects: none
- Forbidden future planet roots: none
- NetworkObject count: 9
- Object count: 272
- Renderer count: 226
- Collider count: 43
- Light count: 10
- Particle count: 7
- MonoBehaviour count: 21
- NetworkObject count: 9


## Safety Confirmations

- `PortalNightsArena.unity` was not modified: yes
- `EditorBuildSettings.asset` was not modified: yes
- `PortalNightsGameController.cs` was not modified: yes
- Current one-scene gameplay path remains unchanged: yes
- New scenes are not wired into gameplay yet: yes

## Risks

- `PN_GameController` in the copied Core scene may retain missing or source-scene serialized references. This is acceptable for Phase 4 because Phase 5 will replace those assumptions with scene-root driven references.
- Scene-placed `NetworkObject`s remain copied as scene data only. Additive Netcode scene activation remains a dedicated later task.
- Addressables and Build Settings integration are intentionally deferred until the scene-loading contract is stabilized.

## Why Scenes Are Not Wired Yet

These new scenes are migration artifacts only. Runtime loading, object ownership, transition sequencing, and cross-scene references still need a later phase to avoid breaking the current working one-scene game path.

## Git Checks

### git status -sb

```text
## codex/portal-nights-full-stage-push...origin/codex/portal-nights-full-stage-push [ahead 2]
 M Assets/PortalNights/Editor/PortalNightsPlanetSceneMigrationTool.cs
 M Assets/PortalNights/Scripts/Scenes/PortalNightsPlanetSceneRoot.cs
?? Assets/PortalNights/Reports/SceneMigrationPhase4Report.md
?? Assets/PortalNights/Reports/SceneMigrationPhase4Report.md.meta
?? Assets/PortalNights/Scenes/Core.meta
?? Assets/PortalNights/Scenes/Core/
?? Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity
?? Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity.meta
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


## Recommended Phase 5 Steps

1. Introduce a controlled additive bootstrap flow that loads Core first and then a selected planet scene.
2. Replace `PortalNightsGameController` scene assumptions with `PortalNightsPlanetSceneRoot` references.
3. Define explicit Netcode handling for scene-placed `NetworkObject`s across Core and planet scenes.
4. Validate transition/UI ownership and shared persistent systems before enabling runtime scene switching.
