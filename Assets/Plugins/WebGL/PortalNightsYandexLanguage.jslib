mergeInto(LibraryManager.library, {
  PortalNights_GetYandexLanguage: function () {
    var lang = "";

    if (typeof window !== "undefined") {
      if (typeof window.PortalNightsLanguage === "string" && window.PortalNightsLanguage.length > 0) {
        lang = window.PortalNightsLanguage;
      } else if (
        window.PortalNightsYsdk &&
        window.PortalNightsYsdk.environment &&
        window.PortalNightsYsdk.environment.i18n &&
        typeof window.PortalNightsYsdk.environment.i18n.lang === "string"
      ) {
        lang = window.PortalNightsYsdk.environment.i18n.lang;
      } else if (
        window.ysdk &&
        window.ysdk.environment &&
        window.ysdk.environment.i18n &&
        typeof window.ysdk.environment.i18n.lang === "string"
      ) {
        lang = window.ysdk.environment.i18n.lang;
      }
    }

    var size = lengthBytesUTF8(lang) + 1;
    var buffer = _malloc(size);
    stringToUTF8(lang, buffer, size);
    return buffer;
  },

  PortalNights_GetYandexLanguageSource: function () {
    var source = "";

    if (typeof window !== "undefined" && typeof window.PortalNightsLanguageSource === "string") {
      source = window.PortalNightsLanguageSource;
    }

    var size = lengthBytesUTF8(source) + 1;
    var buffer = _malloc(size);
    stringToUTF8(source, buffer, size);
    return buffer;
  }
});
