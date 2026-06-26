using System.Collections.Generic;

namespace PortalNights
{
    public interface PortalNightsLeaderboardService
    {
        void SubmitScore(PortalNightsLeaderboardEntry entry);
        IReadOnlyList<PortalNightsLeaderboardEntry> GetTopEntries(int limit);
        void ClearLocalEntries();
    }
}
