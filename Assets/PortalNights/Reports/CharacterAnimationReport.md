# Portal Nights Character Animation Report

Scope: visual/animation setup only. No scene, spawning, map, movement, shooting, staff rescue, or lazy activation changes.

## Imported Animation FBX

| Key | Source | Target | Rig | Clips | Root motion disabled | Warnings |
|---|---|---|---|---|---:|---|
| GenericIdle | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ActionAdventure\idle.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/ActionAdventure/idle.fbx | Humanoid | PN_Generic_Idle | yes | none |
| GenericWalk | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ActionAdventure\walking.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/ActionAdventure/walking.fbx | Humanoid | PN_Generic_Walk | yes | none |
| GenericRun | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ActionAdventure\running.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/ActionAdventure/running.fbx | Humanoid | PN_Generic_Run | yes | none |
| Hit | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing react small from front.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing react small from front.fbx | Humanoid | PN_Generic_Hit | yes | none |
| DeathBackward | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing death backward 01.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing death backward 01.fbx | Humanoid | PN_Generic_Death_Backward | yes | none |
| DeathForward | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing death forward 01.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing death forward 01.fbx | Humanoid | PN_Generic_Death_Forward | yes | none |
| MeleePunch | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing melee punch.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing melee punch.fbx | Humanoid | PN_Generic_Attack_Punch | yes | none |
| MeleeKick | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing melee kick.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing melee kick.fbx | Humanoid | PN_Generic_Attack_Kick | yes | none |
| MutantPunch | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\RoleSpecific\Mutant@Mutant Punch.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Mutant/Mutant@Mutant Punch.fbx | Generic | PN_Mutant_Attack_Punch | yes | none |
| MutantWalk | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\RoleSpecific\Mutant@Mutant Walking.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Mutant/Mutant@Mutant Walking.fbx | Generic | PN_Mutant_Walk | yes | none |
| StaffIdle | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing idle 01.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/standing idle 01.fbx | Humanoid | PN_Staff_Idle | yes | none |
| StaffWalk | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing walk forward.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/standing walk forward.fbx | Humanoid | PN_Staff_Walk | yes | none |
| StaffRun | D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing run forward.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/standing run forward.fbx | Humanoid | PN_Staff_Run | yes | none |
| StaffPanicIdle | D:\www\Neon Fighter\Assets\External\Mixamo\NPCs\Animations\Peasant Girl@Standing Idle.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/Peasant Girl@Standing Idle.fbx | Humanoid | PN_Staff_PanicIdle | yes | none |
| StaffPanicArguing | D:\www\Neon Fighter\Assets\External\Mixamo\NPCs\Animations\Peasant Girl@Standing Arguing.fbx | Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/Peasant Girl@Standing Arguing.fbx | Humanoid | PN_Staff_Panic_Arguing | yes | none |

## Visual Prefab Assignments

| Prefab | Avatar valid | AnimatorController | Parameters | Clips | Root motion disabled | Preview status | Warnings |
|---|---:|---|---|---|---:|---|---|
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Ch40.prefab | yes | Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Run, PN_Generic_Attack_Punch, PN_Generic_Hit, PN_Generic_Death_Backward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Parasite.prefab | yes | Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Run, PN_Generic_Attack_Punch, PN_Generic_Hit, PN_Generic_Death_Backward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Vampire.prefab | yes | Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Run, PN_Generic_Attack_Punch, PN_Generic_Hit, PN_Generic_Death_Backward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Ch50.prefab | yes | Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Run, PN_Generic_Attack_Punch, PN_Generic_Hit, PN_Generic_Death_Backward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Yaku.prefab | yes | Assets/PortalNights/Animations/Bosses/PN_Boss_Yaku.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Attack_Kick, PN_Generic_Attack_Punch, PN_Generic_Hit, PN_Generic_Death_Backward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Pumpkinhulk.prefab | yes | Assets/PortalNights/Animations/Bosses/PN_Boss_Pumpkinhulk.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Attack_Punch, PN_Generic_Attack_Kick, PN_Generic_Hit, PN_Generic_Death_Forward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Maw.prefab | yes | Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Run, PN_Generic_Attack_Kick, PN_Generic_Hit, PN_Generic_Death_Forward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Warrok.prefab | yes | Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Generic_Idle, PN_Generic_Walk, PN_Generic_Run, PN_Generic_Attack_Kick, PN_Generic_Hit, PN_Generic_Death_Forward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |
| Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Mutant.prefab | no | none | none | none | yes | Not validated | No controller assigned. Mutant Humanoid Avatar is invalid, so broken retargeting was intentionally avoided. |
| Assets/PortalNights/Prefabs/Characters/Staff/PN_Visual_Staff_Ch32.prefab | yes | Assets/PortalNights/Animations/Staff/PN_Staff.controller | Bool Moving, Bool Running, Float MoveSpeed, Trigger Attack, Trigger Hit, Trigger Death, Trigger Special | PN_Staff_Idle, PN_Staff_Walk, PN_Staff_Run, PN_Staff_PanicIdle, PN_Generic_Death_Forward | yes | Asset-level OK: controller states have clips; no scene preview was created. | none |

## Controller States

- `Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller` for `Ch40`: Idle, Move, Run, Attack, Hit, Death
- `Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller` for `Parasite`: Idle, Move, Run, Attack, Hit, Death
- `Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller` for `Vampire`: Idle, Move, Run, Attack, Hit, Death
- `Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller` for `Ch50`: Idle, Move, Run, Attack, Hit, Death
- `Assets/PortalNights/Animations/Bosses/PN_Boss_Yaku.controller` for `Yaku`: Idle, Move, Cast, Attack, Hit, Death
- `Assets/PortalNights/Animations/Bosses/PN_Boss_Pumpkinhulk.controller` for `Pumpkinhulk`: Idle, Move, Attack, Slam, Hit, Death
- `Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller` for `Maw`: Idle, Move, Run, Attack, Hit, Death
- `Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller` for `Warrok`: Idle, Move, Run, Attack, Hit, Death
- `Assets/PortalNights/Animations/Staff/PN_Staff.controller` for `Staff_Ch32`: Idle, Walk, Run, PanicIdle, Downed, Revive, SafeIdle

## Notes

- `Mutant` intentionally has no Humanoid controller because its imported Avatar is invalid (`LeftHand` missing according to Unity).
- Preview status is asset-level validation only; no models were placed into `PortalNightsArena`.
