using CutiePie.Core;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G6 round sonu ekranını ve G8 geliştirme mağazasını kurar.
    /// "Cutie Pie/UI/Round Sonu Panelini Kur" tekrar çalıştırılabilir: yer tutucu sprite'ları ve RoundEndSettings asset'ini
    /// yoksa üretir, UpgradeCard / UpgradesPanel / RoundEndPanel prefab'larını baştan kurar (UpgradesPanel, RoundEndPanel'in
    /// içine iç içe prefab olarak girer), RoundEndPanel'i Salon sahnesine ekler (yoksa) ve sahnedeki RoundManager'ı bağlar.
    /// Renkler RoundEndSettings'ten okunur; buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    public static class RoundEndUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Round Sonu Panelini Kur";
        private const string SalonSceneName = "Salon";
        private const string PrefabPath = UIBuilderKit.PrefabFolder + "/RoundEndPanel.prefab";

        // Canvas: TimerCanvas'ın (0) üstünde; geliştirme paneli (20) bunun da üstündedir.
        private const int CanvasSortingOrder = 10;

        // Kart yerleşimi (kart merkezine göre, referans piksel).
        private static readonly Vector2 CardSize = new Vector2(1120f, 920f);
        private const float TitleY = 375f;
        private static readonly Vector2 TitleSize = new Vector2(1000f, 100f);
        private const float TitleFontSize = 76f;
        private const float EarlyEndY = 295f;
        private static readonly Vector2 EarlyEndSize = new Vector2(900f, 60f);
        private const float EarlyEndFontSize = 34f;
        private const float StarsY = 190f;
        private const float StarSize = 130f;
        private const float StarSpacing = 40f;
        private static readonly float[] StatRowY = { 60f, -30f, -120f };
        private static readonly Vector2 StatRowSize = new Vector2(800f, 78f);
        private const float StatPaddingX = 36f;
        private const float StatFontSize = 40f;
        private static readonly float[] ButtonRowY = { -250f, -375f };
        private const float ButtonColumnX = 255f;
        private static readonly Vector2 PrimaryButtonSize = new Vector2(480f, 124f);
        private static readonly Vector2 SecondaryButtonSize = new Vector2(480f, 104f);
        private const float PrimaryButtonFontSize = 48f;
        private const float SecondaryButtonFontSize = 42f;

        private const string KeyGold = "round_end.gold";
        private const string KeyTips = "round_end.tips";
        private const string KeyBestCombo = "round_end.best_combo";
        private const string KeyComboValue = "round_end.combo_value";
        private const string KeyTitle = "round_end.title_completed";
        private const string KeyEarlyReason = "round_end.early_reason";
        private const string KeyWatchAd = "round_end.watch_ad";
        private const string KeyPlayAgain = "round_end.play_again";
        private const string KeyBackToShop = "round_end.back_to_shop";
        private const string KeyUpgrades = "round_end.upgrades";

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorSceneManager.GetActiveScene().name != SalonSceneName)
            {
                EditorUtility.DisplayDialog("Round Sonu Paneli", "Önce Salon.unity sahnesini açın.", "Tamam");
                return;
            }

            RoundEndSettings settings = UIBuilderKit.GetOrCreateSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();
            Sprite star = UIBuilderKit.GetOrCreateStarSprite();

            GameObject upgradesPrefab = UpgradesUIBuilder.BuildPrefabs(settings, rounded);
            GameObject prefab = BuildPrefab(settings, rounded, star, upgradesPrefab);
            RoundEndPanel panel = PlaceInScene(prefab);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("[RoundEndUIBuilder] Round sonu paneli ve geliştirme mağazası kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        // ------------------------------------------------------------------ Prefab

        private static GameObject BuildPrefab(RoundEndSettings settings, Sprite rounded, Sprite star, GameObject upgradesPrefab)
        {
            var root = new GameObject("RoundEndPanel", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                RoundEndPanel panel = root.AddComponent<RoundEndPanel>();

                // İçerik: perde + kart.
                RectTransform content = UIBuilderKit.CreateStretched(root.transform, "Content");
                UIBuilderKit.CreateDim(content, settings);
                RectTransform card = UIBuilderKit.CreateCard(content, "Card", CardSize, rounded, settings);

                LocalizedText title = UIBuilderKit.CreateText(card, "Title", KeyTitle, new Vector2(0f, TitleY), TitleSize,
                    TitleFontSize, settings.textColor, TextAlignmentOptions.Center).GetComponent<LocalizedText>();

                RectTransform earlyRow = UIBuilderKit.CreateRect(card, "EarlyEndRow", new Vector2(0f, EarlyEndY), EarlyEndSize);
                LocalizedText earlyText = UIBuilderKit.CreateText(earlyRow, "Text", KeyEarlyReason, Vector2.zero, EarlyEndSize,
                    EarlyEndFontSize, settings.earlyEndColor, TextAlignmentOptions.Center).GetComponent<LocalizedText>();
                earlyRow.gameObject.SetActive(false);

                StarRow starRow = CreateStars(card, star, settings);

                TMP_Text goldValue = CreateStatRow(card, "GoldRow", KeyGold, StatRowY[0], rounded, settings, null);
                TMP_Text tipsValue = CreateStatRow(card, "TipsRow", KeyTips, StatRowY[1], rounded, settings, null);
                TMP_Text comboValue = CreateStatRow(card, "ComboRow", KeyBestCombo, StatRowY[2], rounded, settings, KeyComboValue);

                Button playAgain = UIBuilderKit.CreateButton(card, "PlayAgainButton", KeyPlayAgain,
                    new Vector2(-ButtonColumnX, ButtonRowY[0]), PrimaryButtonSize, PrimaryButtonFontSize,
                    settings.playAgainColor, rounded, settings);
                Button adButton = UIBuilderKit.CreateButton(card, "AdButton", KeyWatchAd,
                    new Vector2(ButtonColumnX, ButtonRowY[0]), PrimaryButtonSize, PrimaryButtonFontSize,
                    settings.adButtonColor, rounded, settings);
                Button shop = UIBuilderKit.CreateButton(card, "ShopButton", KeyBackToShop,
                    new Vector2(-ButtonColumnX, ButtonRowY[1]), SecondaryButtonSize, SecondaryButtonFontSize,
                    settings.shopButtonColor, rounded, settings);
                Button upgrades = UIBuilderKit.CreateButton(card, "UpgradesButton", KeyUpgrades,
                    new Vector2(ButtonColumnX, ButtonRowY[1]), SecondaryButtonSize, SecondaryButtonFontSize,
                    settings.upgradesButtonColor, rounded, settings);

                // Geliştirme mağazası ayrı prefab; burada iç içe örneği durur (alt Canvas olarak ölçeği bu Canvas'tan alır).
                var upgradesInstance = (GameObject)PrefabUtility.InstantiatePrefab(upgradesPrefab, root.transform);
                UIBuilderKit.PrepareNestedCanvas(upgradesInstance, UpgradesUIBuilder.CanvasSortingOrder);
                UpgradesPanel upgradesPanel = upgradesInstance.GetComponent<UpgradesPanel>();

                var so = new SerializedObject(panel);
                so.FindProperty("settings").objectReferenceValue = settings;
                so.FindProperty("content").objectReferenceValue = content.gameObject;
                so.FindProperty("title").objectReferenceValue = title;
                so.FindProperty("earlyEndRow").objectReferenceValue = earlyRow.gameObject;
                so.FindProperty("earlyEndText").objectReferenceValue = earlyText;
                so.FindProperty("goldValue").objectReferenceValue = goldValue;
                so.FindProperty("tipsValue").objectReferenceValue = tipsValue;
                so.FindProperty("comboValue").objectReferenceValue = comboValue.GetComponent<LocalizedText>();
                so.FindProperty("starRow").objectReferenceValue = starRow;
                so.FindProperty("adButton").objectReferenceValue = adButton;
                so.FindProperty("adButtonLabel").objectReferenceValue = adButton.GetComponentInChildren<LocalizedText>(true);
                so.FindProperty("playAgainButton").objectReferenceValue = playAgain;
                so.FindProperty("shopButton").objectReferenceValue = shop;
                so.FindProperty("upgradesButton").objectReferenceValue = upgrades;
                so.FindProperty("upgradesPanel").objectReferenceValue = upgradesPanel;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Prefab içerik kapalı kaydedilir; panel Awake'te de kendini gizler.
                content.gameObject.SetActive(false);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static StarRow CreateStars(Transform card, Sprite star, RoundEndSettings settings)
        {
            float width = StarSize * 3f + StarSpacing * 2f;
            RectTransform row = UIBuilderKit.CreateRect(card, "Stars", new Vector2(0f, StarsY), new Vector2(width, StarSize));
            StarRow starRow = row.gameObject.AddComponent<StarRow>();

            var images = new Object[3];
            for (int i = 0; i < images.Length; i++)
            {
                float x = (i - 1) * (StarSize + StarSpacing);
                images[i] = UIBuilderKit.CreateIcon(row, $"Star_{i + 1}", new Vector2(x, 0f), StarSize, star,
                    settings.starEmptyColor);
            }

            var so = new SerializedObject(starRow);
            SerializedProperty list = so.FindProperty("stars");
            list.arraySize = images.Length;
            for (int i = 0; i < images.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return starRow;
        }

        /// <summary>Solda etiket, sağda değer olan satır. valueKey verilirse değer de yerelleştirilir (ör. "{0} art arda").</summary>
        private static TMP_Text CreateStatRow(Transform card, string name, string labelKey, float y, Sprite rounded,
            RoundEndSettings settings, string valueKey)
        {
            RectTransform row = UIBuilderKit.CreateRect(card, name, new Vector2(0f, y), StatRowSize);
            UIBuilderKit.AddSlicedImage(row.gameObject, rounded, settings.statRowColor).raycastTarget = false;

            var textSize = new Vector2(StatRowSize.x * 0.5f - StatPaddingX, StatRowSize.y);
            float textX = StatRowSize.x * 0.25f - StatPaddingX * 0.5f;
            UIBuilderKit.CreateText(row, "Label", labelKey, new Vector2(-textX, 0f), textSize, StatFontSize, settings.textColor,
                TextAlignmentOptions.MidlineLeft);
            TMP_Text value = UIBuilderKit.CreateText(row, "Value", valueKey, new Vector2(textX, 0f), textSize, StatFontSize,
                settings.textColor, TextAlignmentOptions.MidlineRight);
            if (valueKey == null)
            {
                value.text = "0";
            }

            return value;
        }

        // ------------------------------------------------------------------ Sahne

        private static RoundEndPanel PlaceInScene(GameObject prefab)
        {
            RoundEndPanel panel = Object.FindObjectOfType<RoundEndPanel>(true);
            if (panel == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, EditorSceneManager.GetActiveScene());
                Undo.RegisterCreatedObjectUndo(instance, "Round Sonu Paneli");
                panel = instance.GetComponent<RoundEndPanel>();
            }

            // RoundManager sahnede olduğu için prefab'a değil, sahnedeki örneğe bağlanır.
            RoundManager roundManager = Object.FindObjectOfType<RoundManager>(true);
            if (roundManager == null)
            {
                Debug.LogWarning("[RoundEndUIBuilder] Sahnede RoundManager yok; önce 'Cutie Pie/Salon/7 - Round ve Kirli Masayı Kur' çalıştırın, sonra bu menüyü tekrar.");
            }

            var so = new SerializedObject(panel);
            so.FindProperty("roundManager").objectReferenceValue = roundManager;
            so.ApplyModifiedProperties();
            return panel;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>(true) != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
        }
    }
}
