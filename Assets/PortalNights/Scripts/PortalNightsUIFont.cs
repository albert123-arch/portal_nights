using System;
using UnityEngine;

namespace PortalNights
{
    public static class PortalNightsUIFont
    {
        private const string DefaultFontPath = "LegacyRuntime.ttf";

        private static Font cachedFont;
        private static bool lookupAttempted;
        private static bool warningLogged;

        public static Font GetDefaultFont()
        {
            if (lookupAttempted)
            {
                return cachedFont;
            }

            lookupAttempted = true;
            try
            {
                cachedFont = Resources.GetBuiltinResource<Font>(DefaultFontPath);
            }
            catch (Exception exception)
            {
                LogMissingFontWarning(exception.Message);
                return null;
            }

            if (cachedFont == null)
            {
                LogMissingFontWarning("font asset was not returned");
            }

            return cachedFont;
        }

        private static void LogMissingFontWarning(string detail)
        {
            if (warningLogged)
            {
                return;
            }

            warningLogged = true;
            Debug.LogWarning("[PortalNights] Built-in UI font '" + DefaultFontPath + "' could not be loaded: " + detail + ".");
        }
    }
}
