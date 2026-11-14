using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G18 Ayarlar panelinin prefab'ını baştan kurar ("Cutie Pie/UI/Ayarlar Panelini Kur", tekrar çalıştırılabilir).
    /// Prefabs/UI/SettingsPanel.prefab: kendi Canvas'ı olan panel (başlık, "Dil" ve iki büyük dil butonu, Kapat, köşede sürüm).
    /// Sahneye eklemez; kullanan sahne prefab'ı bırakıp bir butonu SettingsPanel.Open'a bağlar.
    /// Ses satırı için yer bırakılır, satır kurulmaz (AudioManager henüz yok).
    /// Renkler RoundEndSettings'ten okunur; buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    internal static class SettingsUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Ayarlar Panelini Kur";
        private const string PrefabPath = UIBuilderKit.PrefabFolder + "/SettingsPanel.prefab";

        // Geliştirme mağazasıyla aynı katman; ikisi aynı anda açık olmaz (perde alttaki butonları yutar).
        private const int CanvasSortingOrder = UpgradesUIBuilder.CanvasSortingOrder;

        // Kart yerleşimi (kart merkezine göre, referans piksel).
        private static readonly Vector2 CardSize = new Vector2(1100f, 760f);
        private const float TitleY = 290f;
        private static readonly Vector2 TitleSize = new Vector2(800f, 100f);
        private const float TitleFontSize = 76f;

        private const float LanguageLabelY = 165f;
        private static readonly Vector2 LanguageLabelSize = new Vector2(600f, 70f);
        private const float LanguageLabelFontSize = 44f;

        // İki büyük dil butonu yan yana; seçili olanın arkasında çerçeve görünür.
        private const float LanguageButtonY = 30f;
        private const float LanguageButtonX = 240f;
        private static readonly Vector2 LanguageButtonSize = new Vector2(420f, 150f);
        private const float LanguageFontSize = 56f;
        private const float SelectedFrameWidth = 12f;

        // LanguageButtonY ile Kapat arasındaki boşluk (yaklaşık y = -140) ses satırına ayrılmıştır.

        private static readonly Vector2 CloseButtonPosition = new Vector2(0f, -290f);
        private static readonly Vector2 CloseButtonSize = new Vector2(480f, 104f);
        private const float CloseFontSize = 42f;

        // Sürüm yazısı kartın sağ alt köşesine yaslanır (Kapat butonuyla çakışmayacak genişlikte).
        private static readonly Vector2 VersionOffset = new Vector2(-30f, 18f);
        private static readonly Vector2 VersionSize = new Vector2(260f, 50f);
        private const float VersionFontSize = 28f;

        private const string KeyTitle = "settings.title";
        private const string KeyLanguage = "settings.language";
        private const string KeyVersion = "settings.version";
        private const string KeyClose = "common.close";

        // Dil butonları soldan sağa; kodlar GameConfig.languages'teki kodlarla aynı olmalı.
        private static readonly (string code, string labelKey)[] Languages =
        {
            ("tr", "settings.lang_tr"),
            ("en", "settings.lang_en"),
        };

        [MenuItem(MenuPath)]
        private static void BuildFromMenu()
        {
            GameObject prefab = BuildPrefab();
            Selection.activeObject = prefab;
            Debug.Log($"[SettingsUIBuilder] Ayarlar paneli kuruldu: {PrefabPath}. Sahneye bırakıp bir butonu SettingsPanel.Open'a bağlayın.");
        }

        /// <summary>Prefab'ı kurar ve asset'ini döndürür (test sahnesi kurucusu da kullanır).</summary>
        public static GameObject BuildPrefab()
        {
            UIBuilderKit.EnsureFolder(UIBuilderKit.PrefabFolder);
            RoundEndSettings settings = UIBuilderKit.GetOrCreateSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();

            var root = new GameObject("SettingsPanel", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                SettingsPanel panel = root.AddComponent<SettingsPanel>();

                RectTransform content = UIBuilderKit.CreateStretched(root.transform, "Content");
                UIBuilderKit.CreateDim(content, settings);
                RectTransform card = UIBuilderKit.CreateCard(content, "Card", CardSize, rounded, settings);

                UIBuilderKit.CreateText(card, "Title", KeyTitle, new Vector2(0f, TitleY), TitleSize, TitleFontSize,
                    settings.textColor, TextAlignmentOptions.Center);
                UIBuilderKit.CreateText(card, "LanguageLabel", KeyLanguage, new Vector2(0f, LanguageLabelY),
                    LanguageLabelSize, LanguageLabelFontSize, settings.textColor, TextAlignmentOptions.Center);

                Button close = UIBuilderKit.CreateButton(card, "CloseButton", KeyClose, CloseButtonPosition, CloseButtonSize,
                    CloseFontSize, settings.closeButtonColor, rounded, settings);
                TMP_Text version = CreateVersionText(card, settings);

                var so = new SerializedObject(panel);
                so.FindProperty("settings").objectReferenceValue = settings;
                so.FindProperty("content").objectReferenceValue = content.gameObject;
                so.FindProperty("closeButton").objectReferenceValue = close;
                so.FindProperty("versionText").objectReferenceValue = version.GetComponent<LocalizedText>();

                SerializedProperty options = so.FindProperty("languages");
                options.arraySize = Languages.Length;
                for (int i = 0; i < Languages.Length; i++)
                {
                    float x = Languages.Length == 1 ? 0f : Mathf.Lerp(-LanguageButtonX, LanguageButtonX, i / (Languages.Length - 1f));
                    CreateLanguageOption(card, Languages[i].code, Languages[i].labelKey, new Vector2(x, LanguageButtonY),
                        rounded, settings, options.GetArrayElementAtIndex(i));
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                // Prefab içerik kapalı kaydedilir; panel Awake'te de kendini kapatır.
                content.gameObject.SetActive(false);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Grup: arkada çerçeve (yalnızca seçiliyken açık), önde buton. Kardeş sırası çizim sırasıdır.
        private static void CreateLanguageOption(Transform card, string code, string labelKey, Vector2 position,
            Sprite rounded, RoundEndSettings settings, SerializedProperty option)
        {
            Vector2 frameSize = LanguageButtonSize + Vector2.one * (SelectedFrameWidth * 2f);
            RectTransform group = UIBuilderKit.CreateRect(card, $"Language_{code}", position, frameSize);

            RectTransform frame = UIBuilderKit.CreateRect(group, "SelectedFrame", Vector2.zero, frameSize);
            UIBuilderKit.AddSlicedImage(frame.gameObject, rounded, settings.cardBorderColor).raycastTarget = false;
            frame.gameObject.SetActive(false);

            // Zemin rengini panel seçime göre değiştirir; prefab'da seçili değil rengiyle kaydedilir.
            Button button = UIBuilderKit.CreateButton(group, "Button", labelKey, Vector2.zero, LanguageButtonSize,
                LanguageFontSize, settings.languageIdleColor, rounded, settings);

            option.FindPropertyRelative("code").stringValue = code;
            option.FindPropertyRelative("button").objectReferenceValue = button;
            option.FindPropertyRelative("background").objectReferenceValue = button.targetGraphic;
            option.FindPropertyRelative("selectedFrame").objectReferenceValue = frame.gameObject;
        }

        private static TMP_Text CreateVersionText(RectTransform card, RoundEndSettings settings)
        {
            TMP_Text text = UIBuilderKit.CreateText(card, "Version", KeyVersion, Vector2.zero, VersionSize, VersionFontSize,
                settings.versionTextColor, TextAlignmentOptions.BottomRight, bold: false);

            var tf = (RectTransform)text.transform;
            tf.anchorMin = tf.anchorMax = tf.pivot = new Vector2(1f, 0f);
            tf.anchoredPosition = VersionOffset;
            return text;
        }
    }
}
