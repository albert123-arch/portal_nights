using System.Collections.Generic;
using UnityEngine;

namespace PortalNights.Visuals
{
    public static class PortalNightsEnemyVisualCatalog
    {
        private const string ResourcesPrefix = "PortalNightsEnemyVisuals/";
        private static readonly Dictionary<PortalNightsEnemyVisualKind, GameObject> PrefabCache = new Dictionary<PortalNightsEnemyVisualKind, GameObject>();
        private static readonly HashSet<PortalNightsEnemyVisualKind> MissingLogged = new HashSet<PortalNightsEnemyVisualKind>();

        public static GameObject LoadPrefab(PortalNightsEnemyVisualKind kind)
        {
            if (kind == PortalNightsEnemyVisualKind.None)
            {
                return null;
            }

            if (PrefabCache.TryGetValue(kind, out GameObject cached))
            {
                return cached;
            }

            GameObject prefab = Resources.Load<GameObject>(ResourcesPrefix + GetPrefabName(kind));
            PrefabCache[kind] = prefab;
            if (prefab == null && MissingLogged.Add(kind))
            {
                Debug.LogWarning("[PortalNights] Missing enemy visual resource for " + kind + ".");
            }

            return prefab;
        }

        public static PortalNightsEnemyVisualKind GetVisualKindForEnemy(int planetIndex, PortalNightsEnemyKind kind, bool enhanced = false, PortalNightsPlanet4EnemyVariant planet4Variant = PortalNightsPlanet4EnemyVariant.Swarmer)
        {
            if (planetIndex == 4)
            {
                return planet4Variant switch
                {
                    PortalNightsPlanet4EnemyVariant.Runner => PortalNightsEnemyVisualKind.Maw,
                    PortalNightsPlanet4EnemyVariant.Brute => PortalNightsEnemyVisualKind.Warrok,
                    _ => enhanced ? PortalNightsEnemyVisualKind.Ch50 : PortalNightsEnemyVisualKind.Parasite
                };
            }

            if (kind == PortalNightsEnemyKind.Brute)
            {
                return enhanced && planetIndex == 3 ? PortalNightsEnemyVisualKind.Ch50 : PortalNightsEnemyVisualKind.Warrok;
            }

            if (enhanced)
            {
                return planetIndex switch
                {
                    2 => PortalNightsEnemyVisualKind.Vampire,
                    3 => PortalNightsEnemyVisualKind.Vampire,
                    5 => PortalNightsEnemyVisualKind.Ch50,
                    _ => PortalNightsEnemyVisualKind.Vampire
                };
            }

            return planetIndex switch
            {
                1 => PortalNightsEnemyVisualKind.Ch40,
                2 => PortalNightsEnemyVisualKind.Parasite,
                3 => PortalNightsEnemyVisualKind.Ch40,
                5 => PortalNightsEnemyVisualKind.Ch50,
                _ => PortalNightsEnemyVisualKind.Parasite
            };
        }

        public static PortalNightsEnemyVisualKind GetVisualKindForBoss(string bossName)
        {
            if (!string.IsNullOrWhiteSpace(bossName) && bossName.ToLowerInvariant().Contains("crimson"))
            {
                return PortalNightsEnemyVisualKind.Pumpkinhulk;
            }

            return PortalNightsEnemyVisualKind.Yaku;
        }

        public static float GetTargetHeight(PortalNightsEnemyVisualKind kind, PortalNightsEnemyKind enemyKind = PortalNightsEnemyKind.Small)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Pumpkinhulk => 3.5f,
                PortalNightsEnemyVisualKind.Yaku => 2.4f,
                PortalNightsEnemyVisualKind.Warrok => 3.5f,
                PortalNightsEnemyVisualKind.Ch50 => 2.5f,
                PortalNightsEnemyVisualKind.Vampire => 2.5f,
                PortalNightsEnemyVisualKind.Mutant => 2.6f,
                PortalNightsEnemyVisualKind.Maw => 3f,
                PortalNightsEnemyVisualKind.Parasite => 2.2f,
                PortalNightsEnemyVisualKind.Ch40 => 2.2f,
                _ => enemyKind == PortalNightsEnemyKind.Brute ? 3.5f : 2.2f
            };
        }

        public static float GetBossTargetHeight(PortalNightsEnemyVisualKind kind)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Yaku => 4f,
                PortalNightsEnemyVisualKind.Pumpkinhulk => 4.6f,
                PortalNightsEnemyVisualKind.Maw => 4.2f,
                PortalNightsEnemyVisualKind.Warrok => 4.3f,
                _ => GetTargetHeight(kind, PortalNightsEnemyKind.Brute)
            };
        }

        public static float GetTargetHeightForPlanet(
            int planetIndex,
            PortalNightsEnemyVisualKind kind,
            PortalNightsEnemyKind enemyKind = PortalNightsEnemyKind.Small,
            bool enhanced = false,
            PortalNightsPlanet4EnemyVariant planet4Variant = PortalNightsPlanet4EnemyVariant.Swarmer)
        {
            if (kind == PortalNightsEnemyVisualKind.None)
            {
                return GetTargetHeight(kind, enemyKind);
            }

            return planetIndex switch
            {
                1 => GetPlanet1TargetHeight(kind, enemyKind),
                2 => GetPlanet2TargetHeight(kind, enemyKind, enhanced),
                3 => GetPlanet3TargetHeight(kind, enemyKind, enhanced),
                4 => GetPlanet4TargetHeight(kind, enemyKind, enhanced, planet4Variant),
                5 => GetPlanet5TargetHeight(kind, enemyKind, enhanced),
                _ => GetTargetHeight(kind, enemyKind)
            };
        }

        private static string GetPrefabName(PortalNightsEnemyVisualKind kind)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Ch40 => "PN_Visual_Ch40",
                PortalNightsEnemyVisualKind.Parasite => "PN_Visual_Parasite",
                PortalNightsEnemyVisualKind.Pumpkinhulk => "PN_Visual_Pumpkinhulk",
                PortalNightsEnemyVisualKind.Maw => "PN_Visual_Maw",
                PortalNightsEnemyVisualKind.Vampire => "PN_Visual_Vampire",
                PortalNightsEnemyVisualKind.Mutant => "PN_Visual_Mutant",
                PortalNightsEnemyVisualKind.Warrok => "PN_Visual_Warrok",
                PortalNightsEnemyVisualKind.Ch50 => "PN_Visual_Ch50",
                PortalNightsEnemyVisualKind.Yaku => "PN_Visual_Yaku",
                _ => string.Empty
            };
        }

        private static float GetPlanet1TargetHeight(PortalNightsEnemyVisualKind kind, PortalNightsEnemyKind enemyKind)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Ch40 => 2.2f,
                PortalNightsEnemyVisualKind.Parasite => 2.2f,
                PortalNightsEnemyVisualKind.Warrok => 3.5f,
                _ => GetTargetHeight(kind, enemyKind)
            };
        }

        private static float GetPlanet2TargetHeight(PortalNightsEnemyVisualKind kind, PortalNightsEnemyKind enemyKind, bool enhanced)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Parasite => 2.2f,
                PortalNightsEnemyVisualKind.Maw => 3f,
                PortalNightsEnemyVisualKind.Vampire => 2.5f,
                PortalNightsEnemyVisualKind.Warrok => 3.5f,
                _ => GetTargetHeight(kind, enemyKind)
            };
        }

        private static float GetPlanet3TargetHeight(PortalNightsEnemyVisualKind kind, PortalNightsEnemyKind enemyKind, bool enhanced)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Ch40 => 2.2f,
                PortalNightsEnemyVisualKind.Mutant => 2.6f,
                PortalNightsEnemyVisualKind.Vampire => 2.5f,
                PortalNightsEnemyVisualKind.Ch50 => 2.5f,
                PortalNightsEnemyVisualKind.Warrok => 3.5f,
                _ => GetTargetHeight(kind, enemyKind)
            };
        }

        private static float GetPlanet4TargetHeight(
            PortalNightsEnemyVisualKind kind,
            PortalNightsEnemyKind enemyKind,
            bool enhanced,
            PortalNightsPlanet4EnemyVariant planet4Variant)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Parasite => 2.2f,
                PortalNightsEnemyVisualKind.Maw => 3f,
                PortalNightsEnemyVisualKind.Mutant => 2.6f,
                PortalNightsEnemyVisualKind.Vampire => 2.5f,
                PortalNightsEnemyVisualKind.Ch50 => 2.5f,
                PortalNightsEnemyVisualKind.Warrok => 3.5f,
                _ => GetTargetHeight(kind, enemyKind)
            };
        }

        private static float GetPlanet5TargetHeight(PortalNightsEnemyVisualKind kind, PortalNightsEnemyKind enemyKind, bool enhanced)
        {
            return kind switch
            {
                PortalNightsEnemyVisualKind.Yaku => 2.4f,
                PortalNightsEnemyVisualKind.Pumpkinhulk => 3.5f,
                PortalNightsEnemyVisualKind.Ch50 => 2.5f,
                PortalNightsEnemyVisualKind.Vampire => 2.5f,
                _ => GetTargetHeight(kind, enemyKind)
            };
        }
    }
}
