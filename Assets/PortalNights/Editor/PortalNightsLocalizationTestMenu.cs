#if UNITY_EDITOR
using UnityEditor;

namespace PortalNights.EditorTools
{
    public static class PortalNightsLocalizationTestMenu
    {
        [MenuItem("Portal Nights/Localization/Test Override/Russian")]
        private static void ForceRussian()
        {
            PortalNightsLanguageBootstrap.ForceRussianForEditorTest();
        }

        [MenuItem("Portal Nights/Localization/Test Override/English")]
        private static void ForceEnglish()
        {
            PortalNightsLanguageBootstrap.ForceEnglishForEditorTest();
        }

        [MenuItem("Portal Nights/Localization/Test Override/Clear")]
        private static void Clear()
        {
            PortalNightsLanguageBootstrap.ClearEditorTestOverride();
        }
    }
}
#endif
