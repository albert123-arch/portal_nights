using System;

namespace PortalNights
{
    [Serializable]
    public sealed class PortalNightsLeaderboardEntry
    {
        public string playerName = "Commander";
        public int score;
        public int universe;
        public int planet;
        public float totalTime;
        public int enemiesKilled;
        public int bossesKilled;
        public int staffSaved;
        public int spheresRestored;
        public string dateUtc;

        public static PortalNightsLeaderboardEntry FromRunState(PortalNightsRunState runState, string playerName = "Commander")
        {
            if (runState == null)
            {
                return new PortalNightsLeaderboardEntry
                {
                    playerName = string.IsNullOrWhiteSpace(playerName) ? "Commander" : playerName,
                    dateUtc = DateTime.UtcNow.ToString("o")
                };
            }

            runState.UpdateTotalRunTime();
            return new PortalNightsLeaderboardEntry
            {
                playerName = string.IsNullOrWhiteSpace(playerName) ? "Commander" : playerName,
                score = PortalNightsScoreCalculator.CalculateSummaryScore(runState),
                universe = runState.universeIndex,
                planet = runState.currentPlanetIndex,
                totalTime = runState.totalRunTime,
                enemiesKilled = runState.enemiesKilled + runState.enhancedEnemiesKilled,
                bossesKilled = runState.bossesKilled,
                staffSaved = runState.staffSaved,
                spheresRestored = runState.spheresRestored,
                dateUtc = DateTime.UtcNow.ToString("o")
            };
        }
    }
}
