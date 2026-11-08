using UnityEditor;

namespace CutiePie.Core.Editor
{
    /// <summary>Yalnızca editörde ve Play sırasında: dili test için değiştirir. Seçim kaydedilmez.</summary>
    public static class LanguageMenu
    {
        // Dil kodları GameConfig.languages'taki kodlarla aynı olmalı.
        private const string TurkishCode = "tr";
        private const string EnglishCode = "en";

        private const string TurkishMenu = "Cutie Pie/Dil: Türkçe";
        private const string EnglishMenu = "Cutie Pie/Dil: English";

        [MenuItem(TurkishMenu)]
        private static void SetTurkish() => GameManager.Instance.Localization.SetLanguage(TurkishCode);

        [MenuItem(EnglishMenu)]
        private static void SetEnglish() => GameManager.Instance.Localization.SetLanguage(EnglishCode);

        [MenuItem(TurkishMenu, true)]
        [MenuItem(EnglishMenu, true)]
        private static bool Validate() =>
            EditorApplication.isPlaying && GameManager.Instance != null && GameManager.Instance.Localization != null;
    }
}
