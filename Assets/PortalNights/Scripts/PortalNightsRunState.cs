using System;
using UnityEngine;

namespace PortalNights
{
    [Serializable]
    public sealed class PortalNightsRunState
    {
        public int universeIndex = 1;
        public int currentPlanetIndex = 1;
        public double runStartTime;
        public float totalRunTime;
        public int score;
        public int enemiesKilled;
        public int enhancedEnemiesKilled;
        public int bossesKilled;
        public int playerDeaths;
        public int alliesDowned;
        public int staffSaved;
        public int spheresDefended;
        public int spheresRestored;
        public int coresDefended;
        public int turretsBuilt;
        public int turretsUpgraded;
        public float damageDealt;
        public float damageTaken;
        public int planetsCleared;
        public bool universeCompleted;

        public void StartRun(int startUniverse = 1, int startPlanet = 1)
        {
            universeIndex = Mathf.Max(1, startUniverse);
            currentPlanetIndex = Mathf.Max(1, startPlanet);
            runStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            totalRunTime = 0f;
            score = 0;
            enemiesKilled = 0;
            enhancedEnemiesKilled = 0;
            bossesKilled = 0;
            playerDeaths = 0;
            alliesDowned = 0;
            staffSaved = 0;
            spheresDefended = 0;
            spheresRestored = 0;
            coresDefended = 0;
            turretsBuilt = 0;
            turretsUpgraded = 0;
            damageDealt = 0f;
            damageTaken = 0f;
            planetsCleared = 0;
            universeCompleted = false;
        }

        public void UpdateTotalRunTime()
        {
            if (runStartTime <= 0)
            {
                runStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            totalRunTime = Mathf.Max(0f, (float)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - runStartTime));
        }

        public void MarkPlanetCleared(int planetIndex)
        {
            currentPlanetIndex = Mathf.Max(currentPlanetIndex, planetIndex);
            planetsCleared = Mathf.Max(planetsCleared, planetIndex);
        }

        public void EnterNextUniverse()
        {
            universeIndex = Mathf.Max(1, universeIndex + 1);
            currentPlanetIndex = 1;
            universeCompleted = false;
        }
    }
}
