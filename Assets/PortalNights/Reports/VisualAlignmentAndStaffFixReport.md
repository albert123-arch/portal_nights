# Visual Alignment And Staff Fix Report

## Scope

This stage replaces the previous offset-based grounding with one universal character grounding pipeline:

```text
spawn gameplay root
find walkable floor
move gameplay root so collider bottom == floorY
attach visual
scale visual from renderer bounds
move visual container so renderer bottom == floorY
validate collider and visual bounds
```

No gameplay balance, HP, damage, movement speed, waves, turret stats, coins, score, leaderboard, player movement, shooting, SWAT player visual, map geometry, lazy planet activation, SlimUI, WebGL template, or WebGL build output was intentionally changed.

No scene file was changed in this stage.

## Files Changed In This Stage

- `Assets/PortalNights/Scripts/Visuals/PortalNightsGroundingUtility.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsGroundingValidator.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsVisualBindingUtility.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsEnemyVisualCatalog.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsEnemyVisualBinder.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsStaffVisualBinder.cs`
- `Assets/PortalNights/Scripts/PortalNightsEnemy.cs`
- `Assets/PortalNights/Scripts/PortalNightsPlanet5BossController.cs`
- `Assets/PortalNights/Scripts/PortalNightsGameController.cs`
- `Assets/PortalNights/Scripts/PortalNightsStaffRescue.cs`
- `Assets/PortalNights/Editor/PortalNightsCharacterGroundingSmokeTest.cs`
- `Assets/PortalNights/Reports/VisualAlignmentAndStaffFixReport.md`

## What Changed

Removed the previous fixed visual foot offsets. There is no longer per-monster `GroundYOffset`, staff Y offset, or manual `floor + offset` placement in the visual binding path.

`PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(...)` now:

- uses `Renderer.bounds` from all `SkinnedMeshRenderer` and `MeshRenderer` children
- scales the visual to target world height
- raycasts down from `root.position + Vector3.up * 10`
- accepts only walkable floor-like colliders
- rejects triggers, build pads, turrets, VFX, markers, walls, rails, portal frames/surfaces/rings, pickups, objectives, and gameplay actors
- moves only the visual container by `floorY - rendererBottom`
- validates with tolerance `< 0.05`
- logs `[GROUNDING_FAIL]` with character, planet, floorY, visualBottom, and difference when validation fails

`PortalNightsGameController` now grounds gameplay roots in the server spawn/reset paths before `NetworkObject.Spawn(...)` for Planet 1, Planet 2, Planet 3, Planet 4, and Planet 5 enemies/bosses. This fixes the failed follow-up where visuals could validate locally while the actual networked enemy root stayed at the high portal/spawn transform and appeared floating in Play Mode.

`PortalNightsStaffRescue.ResetCapturedServer(...)` grounds the staff gameplay root before regrounding the Ch32 visual.

`PortalNightsGroundingUtility.TryGetMainColliderBottom(...)` now prefers the collider directly on the gameplay root before considering child colliders, so imported or legacy visual child colliders cannot accidentally replace the gameplay capsule as the grounding source.

Follow-up correction after the failed portal-area result:

- portal frames, portal roots, pylons, arches, and portal surfaces are now rejected through the local parent stack
- the global `PortalNightsArena` root name no longer makes every object below it look like a valid arena floor
- `PortalNightsVisualBindingUtility` now calculates skinned visual height/bottom from baked mesh vertices instead of trusting broad `Renderer.bounds`
- enemy/boss visual assignment regrounds the gameplay root once on the server after the visual kind/planet is known
- repeated enemy visual binds with the same visual kind/height/planet now run `RegroundCurrentVisual()` instead of returning early
- enemy visuals perform several short startup re-align passes so host/client `NetworkTransform` settling cannot leave the model with stale local Y

The floor-name filter was also corrected so real walkable floor names like `LeftEnemyLane_Surface_Floor` and `CrystalMoon_Floor` are not rejected just because they contain words like `Enemy` or `Crystal`.

## Runtime Validator

Added `PortalNightsGroundingValidator`.

When a character visual is bound during play, it logs:

```text
[GROUND CHECK]
Enemy:
Planet:
Collider bottom:
Visual bottom:
Floor:
Difference:
```

It logs `[GROUNDING_FAIL]` if the collider or renderer bottom is outside the `< 0.05` tolerance.

## Target Visual Heights

These are visual-only heights. Gameplay colliders and stats are not scaled by this table.

| Visual role | Target height |
| --- | ---: |
| Parasite | 2.20 |
| Yaku normal enemy | 2.40 |
| Ch40 | 2.20 |
| Ch50 | 2.50 |
| Mutant | 2.60 |
| Vampire | 2.50 |
| Maw | 3.00 |
| Warrok | 3.50 |
| Pumpkinhulk normal brute | 3.50 |
| Yaku boss | 4.00 |
| Pumpkinhulk boss | 4.60 |
| Maw boss | 4.20 |
| Warrok boss | 4.30 |
| Staff Ch32 | 1.77 |

## Runtime Play Mode Measurements

Validation was run in Play Mode in a synced temporary Unity project copy:

```text
D:\www\Noch\PortalNights_VisualSizeCompileTmp
```

Command target:

```text
PortalNights.EditorTools.PortalNightsCharacterGroundingSmokeTest.RunBatchPlayMode
```

Results:

- `[GROUND CHECK]` entries: 16
- `[GROUNDING_FAIL]` entries: 0
- All tested characters: `difference < 0.05`

| Planet | Visual | Target height | Root Y | Floor Y | Collider bottom | Visual bottom | Visual error | Result |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | P1_Ch40 | 2.200 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 1 | P1_Warrok | 3.500 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 2 | P2_Parasite | 2.200 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 2 | P2_Maw | 3.000 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 2 | P2_Vampire | 2.500 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 3 | P3_Mutant | 2.600 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 3 | P3_Ch50 | 2.500 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 3 | P3_Staff_Ch32 | 1.770 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 4 | P4_Parasite | 2.200 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 4 | P4_Warrok | 3.500 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 4 | P4_PumpkinhulkNormal | 3.500 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 5 | P5_YakuNormalReference | 2.400 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 5 | P5_YakuBoss | 4.000 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 5 | P5_PumpkinhulkBoss | 4.600 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 5 | P5_MawBossReference | 4.200 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |
| 5 | P5_WarrokBossReference | 4.300 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 | PASS |

The Play Mode smoke includes invalid portal-frame geometry above the floor to confirm floor detection rejects portal/frame colliders and still lands on the real walkable floor.

Additional real-scene portal bridge probe:

```text
PortalNights.EditorTools.PortalNightsCharacterGroundingSmokeTest.RunBatchArenaPortalBridge
```

This opened `Assets/PortalNights/Scenes/PortalNightsArena.unity` in a temporary project and probed four positions around the Planet 1 portal/entrance bridge.
Probes 1 and 3 intentionally bind the visual before grounding the gameplay root, then repeat the same bind. This catches the host/network race where the previous binder returned early and left the visual half buried after root grounding.

| Probe | Source | Root Y | Floor Y | Collider bottom | Visual bottom | Visual error | Result |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | floor:EntranceBridge_Floor_0 | 0.870 | 0.870 | 0.870 | 0.870 | 0.000 | PASS |
| 2 | floor:EntranceBridge_Floor_0 | 0.870 | 0.870 | 0.870 | 0.870 | 0.000 | PASS |
| 3 | floor:EntranceBridge_Floor_0 | 0.870 | 0.870 | 0.870 | 0.870 | 0.000 | PASS |
| 4 | floor:EntranceBridge_Floor_0 | 0.870 | 0.870 | 0.870 | 0.870 | 0.000 | PASS |

## Root Motion / Hit Sliding

Root motion is disabled centrally:

- `PortalNightsVisualBindingUtility.DisableRootMotion(...)`
- `PortalNightsCharacterVisualAnimator.Awake`
- `PortalNightsCharacterVisualAnimator.OnEnable`
- `PortalNightsCharacterVisualAnimator.SetAnimator`

Enemy and staff binders cache the aligned visual container transform and restore that container in `LateUpdate`. Animations can move bones, but the visual container keeps its local position, rotation, and scale.

## Lazy Activation / Scene Check

`Assets/PortalNights/Scenes/PortalNightsArena.unity` was inspected only and was not modified.

Scene root active flags remain:

```text
Planet2_CrystalMoon m_IsActive: 0
Planet3_AshRelayStation m_IsActive: 0
Planet4_SwarmExpanse m_IsActive: 0
Planet5_CrimsonSingularity m_IsActive: 0
```

## Compile Result

Unity 6000.3.11f1 batch compile and Play Mode smoke completed successfully in the synced temp copy.

No C# compiler errors were present in the final Play Mode smoke run. Existing unrelated warnings from `PortalNightsPlayerController` obsolete ServerRpc ownership and `Assets/SlimUI` unused fields may appear in compile logs; SlimUI was not touched.

No WebGL build was run.
