# Portal Nights Endgame Loop

This document describes the planned full run structure after the current Planet 1-3 vertical slice. It is design and architecture only. Gameplay implementation is staged separately.

## Universe Structure

Portal Nights is structured as a repeatable Universe run. A run starts at Universe 1, Planet 1, then moves through five planets. Completing Planet 5 restores the final sphere, submits the run to the local leaderboard, and unlocks the option to enter Universe N+1 with stronger enemies and higher score potential.

### Planet 1: Core Defense

Role:
- Opening defensive arena.
- Players defend the Core from portal waves.
- Build and upgrade required turrets.
- Survive the final wave and open the portal to Planet 2.

Core beats:
- Wave-based defense.
- Central Core health.
- Coins from kills.
- Turret build pads.
- Turret upgrades level 1 to 3.
- Portal opens only after final defense requirements are met.

### Planet 2: Crystal Moon

Role:
- Clear-area encounter followed by sphere defense.
- Introduces larger arena traversal and a defendable sphere objective.

Core beats:
- Arrive on Crystal Moon.
- Clear initial monsters.
- Activate central sphere.
- Defend the sphere through enemy pressure.
- Clear Planet 2 and continue to Planet 3.

### Planet 3: Ash Relay Station

Role:
- Rescue objective plus Relay Sphere defense.
- The objective is the central RelaySphere, not an evacuation portal.

Core beats:
- Arrive at Ash Relay Station.
- Rescue `Staff_01` and `Staff_02`.
- Escort both staff members to the RelaySphere safe zone.
- Activate RelaySphere.
- Build/upgrade defenses during preparation.
- Defend RelaySphere while it charges.
- Planet clears only when RelaySphere charge completes, staff are safe, and enemies are cleared.

### Planet 4: Swarm Expanse

Role:
- Large horde extermination endurance map.
- The player should feel the run is approaching the finale.

Design:
- Large infected alien wasteland.
- Kill `140` enemies.
- Close `4` hive rifts.
- Open the portal to Planet 5.

Main objectives:
- `KILL THE SWARM`
- `ENEMIES DEFEATED: X/140`
- `RIFTS CLOSED: X/4`

Map requirements:
- Large combat footprint.
- Four hive rifts.
- Wide routes and no maze.
- Buildable turret pads with the existing 3-level upgrade logic.
- Exit portal to Planet 5 remains dormant until completion.

Gameplay requirements:
- Horde director with max alive cap.
- Rifts spawn groups over time.
- Rifts become closable after enough kills from that rift.
- Player holds `E` to close rifts.
- Closed rifts stop spawning.

### Planet 5: Crimson Singularity

Role:
- Final boss arena for the current universe.
- Includes two super bosses, two helpers, a corrupted healing sphere, and a mandatory restoration phase.

Design:
- Large final arena.
- Dramatic corrupted red/yellow energy.
- Central corrupted healing sphere.
- Two boss start zones.
- Two helper arrival points.
- Three restoration stabilizers.

Core correction:
- Killing bosses is not the end.
- The final mission is complete only after the corrupted sphere is restored to a normal cyan/white sphere.

Boss phase:
- `Solar Warden`
  - Ranged energy boss.
  - Medium-distance behavior.
  - Projectiles, pulse, and optional summons.
- `Crimson Behemoth`
  - Melee tank boss.
  - Chase, charge, slam, and heavy close-range pressure.

Corrupted Healing Sphere:
- Starts as yellow/red corrupted sphere.
- Heals both bosses while alive.
- Prevents bosses from fully dying while alive.
- Player should destroy the sphere before killing bosses.

Sphere behavior while alive:
- Every `12` seconds, heals both bosses for `8%` max HP.
- If a boss HP falls below `10%`, heals that boss to `25%`.
- If a boss would die while sphere is alive, clamp boss to `1 HP` and trigger a healing pulse.

Helper dialogue:
- If the player ignores the sphere for `18` seconds:
  - `Helper: "Commander, we need to destroy the sphere first!"`
- If the player keeps ignoring the sphere for another `25-30` seconds:
  - `Helper: "It is healing them! Break the sphere or the bosses will not fall!"`
- Dialogue must not spam.
- Dialogue stops after the sphere is destroyed.
- Damaging the sphere resets reminder timing.

After corrupted sphere destruction:
- Boss healing disabled.
- Boss immortality disabled.
- HUD changes to `KILL THE BOSSES`.
- Sphere remains as `Damaged Sphere Core`, not deleted.

After both bosses die:
- Do not complete the universe yet.
- State changes to restoration phase.
- HUD shows `RESTORE THE SPHERE`.

Sphere restoration:
- Three stabilizers become active:
  - `NorthStabilizer`
  - `WestStabilizer`
  - `EastStabilizer`
- Each stabilizer prompt:
  - `HOLD E TO STABILIZE`
- Hold time:
  - `3` seconds per stabilizer.
- Helpers do not activate stabilizers automatically.

After all three stabilizers complete:
- Damaged red sphere becomes restored cyan/white sphere.
- Yellow/red corruption visuals stop.
- Cyan beam appears.
- HUD shows:
  - `SPHERE RESTORED`
  - `UNIVERSE STABILIZED`
- Universe Complete triggers only here.

## Universe Complete

Universe Complete happens after Planet 5 sphere restoration, not after boss death.

Summary should show:
- Universe completed.
- Final score.
- Total time.
- Enemies killed.
- Bosses defeated.
- Staff saved.
- Spheres restored.
- Deaths.
- Turrets built/upgraded.

After Universe Complete:
- Submit run to local leaderboard.
- Show top 5 local leaderboard entries.
- Open `ENTER UNIVERSE N+1` portal/button.
- Do not enter Universe N+1 automatically.

## Universe N Scaling

Universe N+1 restarts at Planet 1 with stronger enemies and higher scoring.

Given `universeIndex`, multipliers are:

- `enemyHpMultiplier = 1 + (universeIndex - 1) * 0.35`
- `enemyDamageMultiplier = 1 + (universeIndex - 1) * 0.20`
- `enemyCountMultiplier = 1 + (universeIndex - 1) * 0.10`
- `bossHpMultiplier = 1 + (universeIndex - 1) * 0.45`
- `coinMultiplier = 1 + (universeIndex - 1) * 0.15`
- `scoreMultiplier = universeIndex`

Scaling application points:
- Enemy HP on spawn.
- Enemy attack damage on spawn.
- Enemy count where wave or horde definitions allow.
- Boss HP on Planet 5 spawn.
- Coin rewards.
- Final score additions.

## Leaderboard

Leaderboard should be local-first and online-ready later.

Initial implementation:
- JSON storage under `Application.persistentDataPath`.
- Keep top `20` entries.
- Display top `5` after Universe Complete.
- No network calls.
- Save failures should not block gameplay.

Submit score:
- After Game Over.
- After Planet 5 Universe Complete.
- When exiting a run, if a run exit flow is added later.

### Leaderboard Entry Fields

- `playerName`
- `score`
- `highestUniverse`
- `highestPlanet`
- `totalTime`
- `enemiesKilled`
- `enhancedEnemiesKilled`
- `bossesKilled`
- `staffSaved`
- `spheresRestored`
- `spheresDefended`
- `coresDefended`
- `playerDeaths`
- `alliesDowned`
- `turretsBuilt`
- `turretsUpgraded`
- `damageDealt`
- `damageTaken`
- `dateUtc`

### Sort Order

1. Score descending.
2. Universe descending.
3. Total time ascending.

## Score Formula

Base score events:
- Planet 1 cleared: `10000`
- Planet 2 cleared: `15000`
- Planet 3 cleared: `25000`
- Planet 4 cleared: `35000`
- Planet 5 cleared: `75000`
- Universe completed: `100000 * universeIndex`
- Normal enemy killed: `50`
- Enhanced enemy killed: `200`
- Boss killed: `15000`
- Staff saved: `5000`
- Sphere restored: `25000`
- Player death penalty: `-5000`
- Objective health bonus: up to `20000`

Final score events should be multiplied by `scoreMultiplier` where appropriate.

## Proposed Classes

### `PortalNightsRunState`

Purpose:
- Track the current run across all planets and universes.
- Own aggregate stats used by score and leaderboard.

Initial fields:
- `universeIndex`
- `currentPlanetIndex`
- `runStartTime`
- `totalRunTime`
- `score`
- `enemiesKilled`
- `enhancedEnemiesKilled`
- `bossesKilled`
- `playerDeaths`
- `alliesDowned`
- `staffSaved`
- `spheresDefended`
- `spheresRestored`
- `coresDefended`
- `turretsBuilt`
- `turretsUpgraded`
- `damageDealt`
- `damageTaken`
- `planetsCleared`
- `universeCompleted`

### `PortalNightsUniverseScaling`

Purpose:
- Provide deterministic scaling multipliers by universe index.
- Keep scaling formulas centralized.

### `PortalNightsScoreCalculator`

Purpose:
- Convert run events and run summary into score.
- Keep score rules out of planet-specific gameplay code.

### `PortalNightsLeaderboardEntry`

Purpose:
- Serializable leaderboard data row.
- Local JSON storage should use this type.

### `PortalNightsLeaderboardService`

Purpose:
- Interface for leaderboard operations.
- Allows local JSON implementation now and online implementation later.

Required methods:
- `SubmitScore(entry)`
- `GetTopEntries(limit)`
- `ClearLocalEntries()`

### `PortalNightsLocalLeaderboardService`

Purpose:
- JSON-backed local leaderboard.
- Saves under `Application.persistentDataPath`.
- Keeps top 20 entries.

### `Planet4HordeDirector`

Purpose:
- Own Planet 4 horde pacing.
- Keeps max alive cap.
- Chooses active rift spawn groups.
- Tracks kill requirements and completion.

### `Planet4HiveRift`

Purpose:
- Own one hive rift state.
- Tracks kills from that rift.
- Handles closable/closing/closed visual state.
- Provides spawn point for horde director.

### `Planet5BossController`

Purpose:
- Own one boss character.
- Handles boss stats, HP scaling, attacks, and death protection.
- Communicates with `Planet5HealingSphere`.

### `Planet5HealingSphere`

Purpose:
- Own corrupted sphere HP and healing behavior.
- Tracks whether boss healing/death protection is active.
- Emits damaged-core state after destruction.

### `Planet5SphereRestoration`

Purpose:
- Own the three stabilizers.
- Tracks stabilization progress.
- Converts damaged sphere into restored cyan/white sphere.
- Triggers Universe Complete after restoration.

### `PortalNightsHelperDialogue`

Purpose:
- Manages helper hint timing and subtitles.
- Prevents repeated spam.
- Resets timers when the player damages the corrupted sphere.

## Staging Notes

- Planet 4 map should be built before horde gameplay.
- Planet 5 map should be built before boss gameplay.
- Planet 5 boss death must not submit leaderboard or complete universe.
- Universe Complete should be triggered only by restored sphere.
- Existing player movement, shooting, SWAT visual, camera, turret upgrades, and existing planets must remain stable across all stages.

