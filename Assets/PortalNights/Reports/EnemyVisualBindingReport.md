# Portal Nights Enemy Visual Binding Report

## Scope

Connected imported monster visual prefabs to existing gameplay enemies and Planet 5 bosses without changing HP, damage, speed, waves, coins, scoring, turrets, player movement, shooting, maps, or lazy planet activation.

## Runtime Visual System

- `PortalNightsEnemyVisualKind` defines the available imported monster visuals.
- `PortalNightsEnemyVisualCatalog` centralizes planet/role mapping and lazy-loads visual prefabs with `Resources.Load`.
- `PortalNightsEnemyVisualBinder` creates or reuses a child named `VisualRoot`, removes only known legacy visual-only children, instantiates the selected imported visual, strips accidental gameplay physics/network components from the visual instance, aligns feet to ground, scales to an approximate role height, and drives move/attack/hit/death animation hooks where available.
- Selected visual kind is synchronized through a `NetworkVariable<int>` on enemies and Planet 5 bosses. The visual prefab itself is not a `NetworkObject`.

## Lazy Loading

Runtime visual prefabs are available under:

`Assets/PortalNights/Resources/PortalNightsEnemyVisuals`

The catalog caches a prefab only after that specific visual is first requested. No scene object directly references all monster visuals, and future planet roots are not activated by this system.

## Visual Assignment

| Area | Gameplay role | Visual |
| --- | --- | --- |
| Planet 1 | Small/basic enemy | Ch40 |
| Planet 1 | Brute | Warrok |
| Planet 2 | Small/swarmer | Parasite |
| Planet 2 | Brute | Warrok |
| Planet 3 | Small/normal attacker | Ch40 |
| Planet 3 | Enhanced/elite/brute attacker | Ch50 |
| Planet 4 | Swarmer | Parasite |
| Planet 4 | Runner | Maw |
| Planet 4 | Brute | Warrok |
| Planet 5 | Solar Warden ranged boss | Yaku |
| Planet 5 | Crimson Behemoth melee boss | Pumpkinhulk |
| Planet 5 | Summoned elite fallback if used later | Ch50 |

## Animation Hooks

- Movement and running are driven by visual root position delta in `PortalNightsEnemyVisualBinder`.
- Enemy attack animation is triggered from the existing enemy attack VFX RPC.
- Enemy hit animation is triggered from `PortalNightsHealth.HealthChanged` when health decreases.
- Enemy death animation is triggered from the existing death VFX RPC before despawn.
- Planet 5 bosses trigger attack/special/death animation hooks from their existing boss RPCs.

## Mutant Status

Mutant remains imported and available in the catalog, but it is not currently assigned to live spawns because its Humanoid Avatar was previously identified as invalid due to missing `LeftHand`. It can be used later as a visual-only fallback after confirming scale/animation quality.

## Changed Scripts

- `Assets/PortalNights/Scripts/Visuals/PortalNightsEnemyVisualKind.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsEnemyVisualCatalog.cs`
- `Assets/PortalNights/Scripts/Visuals/PortalNightsEnemyVisualBinder.cs`
- `Assets/PortalNights/Scripts/PortalNightsEnemy.cs`
- `Assets/PortalNights/Scripts/PortalNightsGameController.cs`
- `Assets/PortalNights/Scripts/PortalNightsPlanet5BossController.cs`

## Validation Notes

- Unity imported the new visual resources and reloaded assemblies successfully.
- Fresh Editor log tail did not show new C# compiler errors.
- Startup freeze recorder output still showed `roots=P2:off P3:off P4:off P5:off`, confirming future planet roots stayed inactive during the observed Planet 1 startup.
- `Assets/PortalNights/Scripts/PortalNightsStaffRescue.cs` was already dirty before this task and was not modified by this task.
- `Assets/SlimUI` and `Assets/SlimUI.meta` were not touched.
- No scene files were modified.

## Remaining Notes

- A runtime warning remains in the Editor log: `Rpc methods can only be invoked after starting the NetworkManager!` from `MissionObjectiveClientRpc`. This was not introduced by the visual binding work and should be handled separately if it persists during normal Play Mode.
- Full manual smoke coverage for Planet 2 and Planet 5 visuals still needs in-editor gameplay traversal or a dedicated smoke shortcut.
