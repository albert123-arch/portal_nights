using System.Runtime.InteropServices;
using UnityEngine;

namespace PortalNights
{
    public static class PortalNightsLanguageBootstrap
    {
        private const string EditorOverrideKey = "PortalNights.Localization.EditorLanguageOverride";
        private static bool initialized;
        private static bool unavailableWarningLogged;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string PortalNights_GetYandexLanguage();

        [DllImport("__Internal")]
        private static extern string PortalNights_GetYandexLanguageSource();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            ApplyStartupLanguage();
        }

        public static void ApplyStartupLanguage()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            string languageCode = "ru";
            string source = "EditorFallback";

#if UNITY_WEBGL && !UNITY_EDITOR
            languageCode = GetWebGLYandexLanguage();
            source = GetWebGLYandexLanguageSource();
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                languageCode = "ru";
                source = string.IsNullOrWhiteSpace(source) ? "YandexUnavailableFallback" : source;
                LogUnavailableWarningOnce();
            }
#elif UNITY_EDITOR
            string editorOverride = GetEditorTestOverride();
            if (!string.IsNullOrWhiteSpace(editorOverride))
            {
                languageCode = editorOverride;
                source = "EditorTestOverride";
            }
#endif

            PortalNightsLocalization.SetLanguageFromCode(languageCode);
            Debug.Log("[PortalNightsLocalization] Language selected: " + GetCurrentLanguageCode() + ", source: " + source);
        }

        public static string GetCurrentLanguageCode()
        {
            return PortalNightsLocalization.CurrentLanguage == PortalNightsLanguage.English ? "en" : "ru";
        }

#if UNITY_EDITOR
        public static void SetEditorTestOverride(string languageCode)
        {
            UnityEditor.EditorPrefs.SetString(EditorOverrideKey, languageCode ?? string.Empty);
            Debug.Log("[PortalNightsLocalization] Editor test language override set to: " + (string.IsNullOrWhiteSpace(languageCode) ? "none" : languageCode));
        }

        public static void ForceRussianForEditorTest()
        {
            SetEditorTestOverride("ru");
        }

        public static void ForceEnglishForEditorTest()
        {
            SetEditorTestOverride("en");
        }

        public static void ClearEditorTestOverride()
        {
            UnityEditor.EditorPrefs.DeleteKey(EditorOverrideKey);
            Debug.Log("[PortalNightsLocalization] Editor test language override cleared.");
        }

        private static string GetEditorTestOverride()
        {
            return UnityEditor.EditorPrefs.GetString(EditorOverrideKey, string.Empty);
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        private static string GetWebGLYandexLanguage()
        {
            try
            {
                return PortalNights_GetYandexLanguage();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[PortalNightsLocalization] Failed to read Yandex SDK language: " + exception.Message);
                return string.Empty;
            }
        }

        private static string GetWebGLYandexLanguageSource()
        {
            try
            {
                string source = PortalNights_GetYandexLanguageSource();
                return string.IsNullOrWhiteSpace(source) ? "YandexSDK" : source;
            }
            catch
            {
                return "YandexUnavailableFallback";
            }
        }
#endif

        private static void LogUnavailableWarningOnce()
        {
            if (unavailableWarningLogged)
            {
                return;
            }

            unavailableWarningLogged = true;
            Debug.LogWarning("[PortalNightsLocalization] Yandex SDK language unavailable at startup. Falling back to Russian.");
        }
    }
}
