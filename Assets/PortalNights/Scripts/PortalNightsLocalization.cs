using System.Collections.Generic;

namespace PortalNights
{
    public static class PortalNightsLocalization
    {
        private static readonly Dictionary<string, string> TextByKey = new Dictionary<string, string>
        {
            { "hud.wave", "WAVE" },
            { "hud.enemies", "ENEMIES" },
            { "hud.core", "CORE" },
            { "hud.player", "HERO" },
            { "hud.coins", "COINS" },
            { "hud.buildReady", "E  BUILD TURRET" },
            { "hud.upgradeReady", "E  UPGRADE TURRET  {0}>{1}" },
            { "hud.buildCost", "COST" },
            { "hud.noBuild", "FIND A BUILD PAD" },
            { "hud.wait", "NEXT WAVE IN" },
            { "hud.gameOver", "CORE LOST - PRESS R" },
            { "toast.notEnoughCoins", "Not enough coins" },
            { "toast.turretBuilt", "Turret online" },
            { "toast.turretUpgraded", "Turret upgraded" },
            { "toast.turretMaxed", "Turret is already max level" },
            { "toast.turretMissing", "Turret prefab missing" },
            { "toast.noPad", "Move closer to a build pad" },
            { "toast.coreLost", "The Core has fallen" },
            { "toast.waveClear", "Wave cleared" }
        };

        public static string Text(string key)
        {
            return TextByKey.TryGetValue(key, out string value) ? value : key;
        }
    }
}
