# Portal Nights Progression Plan

## Core Loop

Planet defense -> clear 10 waves -> build and upgrade required turrets -> portal opens -> enter portal -> Crystal Moon combat area -> kill monsters -> activate sphere -> defend sphere -> planet cleared.

## Planet 1 Rules

- State: `Planet1_Defense`
- The Core must stay alive.
- Waves spawn from the existing two-lane portal layout.
- Wave 10 is the final defense wave for the first progression slice.
- After waves 3, 6, and 10, the host chooses one team reward:
  - `1`: +15% player weapon damage for this run
  - `2`: +15% turret damage for this run
  - `3`: repair Core by 150
- Wave clear coin bonuses:
  - Waves 1-3: +75 coins
  - Waves 4-6: +125 coins
  - Waves 7-10: +175 coins

## Portal Unlock

The first portal opens only when:

- Wave 10 is complete
- No enemies remain
- Core is alive
- Required build pads all have turrets
- Required turrets are all level 3

Current configuration counts all build pads as required. This can be changed later through the build pad `requiredForPortal` flag and controller `countAllBuildPadsAsRequired` setting.

## Turret Upgrade Costs And Stats

- Build level 1: 120 coins
- Upgrade level 2: 180 coins
- Upgrade level 3: 260 coins

Current first pass stats:

- Level 1: 18 damage, 0.32s fire interval, 18 range
- Level 2: 26.1 damage, 0.267s fire interval, 20 range
- Level 3: 39.6 damage, 0.221s fire interval, 22 range

Turret visuals use the current FBX level models and muzzle setup: one barrel at level 1, two barrels at level 2, three barrels at level 3.

## Planet 2: Crystal Moon

- State: `Planet2_ClearArea`
- Runtime area generated under `PortalNightsArena/Planet2_CrystalMoon`.
- Visual identity: larger darker moon arena, segmented neon rim, cyan and purple crystal accents, central objective plate, safe boundaries.
- Clear area group: 6 small enemies and 2 brute enemies.
- After all enemies die, the `SphereObjective` becomes interactable.

## Sphere Defense

- State: `Planet2_SphereReady` until activated.
- Interact prompt: `E ACTIVATE SPHERE`
- Sphere starts at 500 HP.
- Enemies target the sphere during `Planet2_DefendSphere`.
- If sphere HP reaches 0: `Failed`, HUD shows `SPHERE LOST - PRESS R TO RETRY`.

Defense waves:

- Wave A: 6 enemies
- Wave B: 8 enemies
- Wave C: 10 enemies and 1 brute

After all three waves clear:

- State: `Planet2_Cleared`
- HUD shows `PLANET CLEARED`
- Players remain on Crystal Moon.

## Drops

Drop roll:

- Coin: 65%
- Heal: 12%
- Armor: 10%
- WeaponDamageBoost: 8%
- TurretDamageBoost: 5%

Effects:

- Coin: adds coins
- Heal: restores 40 HP
- Armor: reduces incoming player damage by 30% for 30 seconds
- WeaponDamageBoost: player weapon damage +20% for 30 seconds
- TurretDamageBoost: team turret damage +20% for 30 seconds

## Future Planet Ideas

- Ember Foundry: heat vents, fire-resistant monsters, shield generators
- Storm Garden: moving electric hazards, chain-lightning turrets
- Frozen Relay: slippery lanes, slow fields, repair drones
- Void Dock: low gravity islands, elite portal guardians
