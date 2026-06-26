using System;
using UnityEngine;

namespace PortalNights
{
    [Serializable]
    public readonly struct PortalNightsUniverseScaling
    {
        public readonly int universeIndex;
        public readonly float enemyHpMultiplier;
        public readonly float enemyDamageMultiplier;
        public readonly float enemyCountMultiplier;
        public readonly float bossHpMultiplier;
        public readonly float coinMultiplier;
        public readonly int scoreMultiplier;

        private PortalNightsUniverseScaling(int universe)
        {
            universeIndex = Mathf.Max(1, universe);
            int extraUniverses = universeIndex - 1;
            enemyHpMultiplier = 1f + extraUniverses * 0.35f;
            enemyDamageMultiplier = 1f + extraUniverses * 0.20f;
            enemyCountMultiplier = 1f + extraUniverses * 0.10f;
            bossHpMultiplier = 1f + extraUniverses * 0.45f;
            coinMultiplier = 1f + extraUniverses * 0.15f;
            scoreMultiplier = universeIndex;
        }

        public static PortalNightsUniverseScaling ForUniverse(int universeIndex)
        {
            return new PortalNightsUniverseScaling(universeIndex);
        }

        public int ScaleEnemyCount(int baseCount)
        {
            return Mathf.Max(0, Mathf.CeilToInt(baseCount * enemyCountMultiplier));
        }

        public int ScaleCoins(int baseCoins)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseCoins * coinMultiplier));
        }
    }
}
