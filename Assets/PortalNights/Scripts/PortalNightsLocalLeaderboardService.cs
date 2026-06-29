using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsLocalLeaderboardService : PortalNightsLeaderboardService
    {
        private const int MaxEntries = 20;
        private const string FileName = "PortalNightsLeaderboard.json";

        public string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public void SubmitScore(PortalNightsLeaderboardEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            PortalNightsLeaderboardStore store = LoadStore();
            store.entries.Add(entry);
            SortAndTrim(store.entries);
            SaveStore(store);
        }

        public IReadOnlyList<PortalNightsLeaderboardEntry> GetTopEntries(int limit)
        {
            PortalNightsLeaderboardStore store = LoadStore();
            SortAndTrim(store.entries);
            int count = Mathf.Clamp(limit, 0, store.entries.Count);
            return store.entries.GetRange(0, count);
        }

        public void ClearLocalEntries()
        {
            SaveStore(new PortalNightsLeaderboardStore());
        }

        private PortalNightsLeaderboardStore LoadStore()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return new PortalNightsLeaderboardStore();
                }

                string json = File.ReadAllText(SavePath);
                PortalNightsLeaderboardStore store = JsonUtility.FromJson<PortalNightsLeaderboardStore>(json);
                return store ?? new PortalNightsLeaderboardStore();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[PortalNights] Failed to load local leaderboard: " + exception.Message);
                return new PortalNightsLeaderboardStore();
            }
        }

        private void SaveStore(PortalNightsLeaderboardStore store)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? Application.persistentDataPath);
                string json = JsonUtility.ToJson(store, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[PortalNights] Failed to save local leaderboard: " + exception.Message);
            }
        }

        private static void SortAndTrim(List<PortalNightsLeaderboardEntry> entries)
        {
            entries.RemoveAll(entry => entry == null);
            entries.Sort(CompareEntries);
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            }
        }

        private static int CompareEntries(PortalNightsLeaderboardEntry a, PortalNightsLeaderboardEntry b)
        {
            int scoreCompare = b.score.CompareTo(a.score);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int universeCompare = b.universe.CompareTo(a.universe);
            if (universeCompare != 0)
            {
                return universeCompare;
            }

            return a.totalTime.CompareTo(b.totalTime);
        }

        [Serializable]
        private sealed class PortalNightsLeaderboardStore
        {
            public List<PortalNightsLeaderboardEntry> entries = new List<PortalNightsLeaderboardEntry>();
        }
    }
}
