using UnityEngine;

namespace PortalNights
{
    public static class PortalNightsScoreCalculator
    {
        public const int Planet1Cleared = 10000;
        public const int Planet2Cleared = 15000;
        public const int Planet3Cleared = 25000;
        public const int Planet4Cleared = 35000;
        public const int Planet5Cleared = 75000;
        public const int NormalEnemyKilled = 50;
        public const int EnhancedEnemyKilled = 200;
        public const int BossKilled = 15000;
        public const int StaffSaved = 5000;
        public const int SphereRestored = 25000;
        public const int PlayerDeathPenalty = -5000;
        public const int MaxObjectiveHealthBonus = 20000;

        public static int GetPlanetClearScore(int planetIndex)
        {
            switch (planetIndex)
            {
                case 1:
                    return Planet1Cleared;
                case 2:
                    return Planet2Cleared;
                case 3:
                    return Planet3Cleared;
                case 4:
                    return Planet4Cleared;
                case 5:
                    return Planet5Cleared;
                default:
                    return 0;
            }
        }

        public static int GetUniverseCompleteScore(int universeIndex)
        {
            return 100000 * Mathf.Max(1, universeIndex);
        }

        public static int GetObjectiveHealthBonus(float normalizedHealth)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(normalizedHealth) * MaxObjectiveHealthBonus);
        }

        public static int ApplyUniverseMultiplier(int baseScore, int universeIndex)
        {
            PortalNightsUniverseScaling scaling = PortalNightsUniverseScaling.ForUniverse(universeIndex);
            return baseScore * scaling.scoreMultiplier;
        }

        public static int CalculateSummaryScore(PortalNightsRunState runState, float objectiveHealthNormalized = 0f)
        {
            if (runState == null)
            {
                return 0;
            }

            int baseScore = runState.score;
            baseScore += runState.enemiesKilled * NormalEnemyKilled;
            baseScore += runState.enhancedEnemiesKilled * EnhancedEnemyKilled;
            baseScore += runState.bossesKilled * BossKilled;
            baseScore += runState.staffSaved * StaffSaved;
            baseScore += runState.spheresRestored * SphereRestored;
            baseScore += runState.playerDeaths * PlayerDeathPenalty;
            baseScore += GetObjectiveHealthBonus(objectiveHealthNormalized);

            for (int planet = 1; planet <= runState.planetsCleared; planet++)
            {
                baseScore += GetPlanetClearScore(planet);
            }

            if (runState.universeCompleted)
            {
                baseScore += GetUniverseCompleteScore(runState.universeIndex);
            }

            return Mathf.Max(0, ApplyUniverseMultiplier(baseScore, runState.universeIndex));
        }
    }
}
