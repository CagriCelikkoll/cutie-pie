using System.IO;
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
    /// Yalnızca editörde: G6 round sonu ekranını kurar. "Cutie Pie/UI/Round Sonu Panelini Kur" tekrar çalıştırılabilir:
    /// yer tutucu sprite'ları (yuvarlak köşeli kutu, yıldız) ve RoundEndSettings asset'ini yoksa üretir,
    /// RoundEndPanel.prefab'ı baştan kurar, prefab'ı Salon sahnesine ekler (yoksa) ve sahnedeki RoundManager'ı bağlar.
    /// Renkler RoundEndSettings'ten okunur; buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    public static class RoundEndUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Round Sonu Panelini Kur";
        private const string SalonSceneName = "Salon";

        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/RoundEndPanel.prefab";
        private const string SettingsPath = "Assets/_Project/Data/Salon/RoundEndSettings.asset";
        private const string PlaceholderFolder = "Assets/_Project/Art/Salon/Placeholders";
        private const string RoundedSpritePath = PlaceholderFolder + "/RoundedRect.png";
        private const string StarSpritePath = PlaceholderFolder + "/Star.png";

        // Yuvarlak köşeli kutu dokusu: 9-slice kenarı köşe yarıçapından biraz geniş tutulur.
        private const int RoundedTextureSize = 64;
        private const int RoundedCornerRadius = 26;
        private const int RoundedSliceBorder = 28;
        private const int StarTextureSize = 128;
        private const int StarPoints = 5;
        private const float StarInnerRatio = 0.48f;
        private const int Supersample = 4;
        private const float SpritePixelsPerUnit = 100f;

        // Canvas: TimerCanvas'ın (0) üstünde.
        private const int CanvasSortingOrder = 10;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float MatchWidthOrHeight = 0.5f;

        // Kart yerleşimi (kart merkezine göre, referans piksel).
        private static readonly Vector2 CardSize = new Vector2(1120f, 920f);
        private const float CardBorderWidth = 14f;
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
        private const float MinAutoFontSize = 24f;

        // Geliştirmeler paneli.
        private static readonly Vector2 UpgradesCardSize = new Vector2(900f, 600f);
        private const float UpgradesTitleY = 215f;
        private const float UpgradesBodyY = 20f;
        private const float UpgradesBodyFontSize = 44f;
        private const float UpgradesCloseY = -195f;

        // Buton tonları (Image rengi butonun kendi rengidir; bunlar onun üstüne çarpılır).
        private static readonly Color ButtonHighlighted = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ButtonPressed = new Color(0.82f, 0.82f, 0.82f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.7f, 0.7f, 0.7f, 0.5f);

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
        private const string KeyUpgradesTitle = "upgrades.title";
        private const string KeyComingSoon = "common.coming_soon";
        private const string KeyClose = "common.close";

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorSceneManager.GetActiveScene().name != SalonSceneName)
            {
                EditorUtility.DisplayDialog("Round Sonu Paneli", "Önce Salon.unity sahnesini açın.", "Tamam");
                return;
            }

            RoundEndSettings settings = GetOrCreateSettings();
            Sprite rounded = GetOrCreateRoundedSprite();
            Sprite star = GetOrCreateStarSprite();

            GameObject prefab = BuildPrefab(settings, rounded, star);
            RoundEndPanel panel = PlaceInScene(prefab);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("[RoundEndUIBuilder] Round sonu paneli kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        // ------------------------------------------------------------------ Prefab

        private static GameObject BuildPrefab(RoundEndSettings settings, Sprite rounded, Sprite star)
        {
            EnsureFolder(PrefabFolder);

            var root = new GameObject("RoundEndPanel", typeof(RectTransform));
            try
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = CanvasSortingOrder;

                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.matchWidthOrHeight = MatchWidthOrHeight;
                root.AddComponent<GraphicRaycaster>();

                RoundEndPanel panel = root.AddComponent<RoundEndPanel>();

                // İçerik: perde + kart.
                RectTransform content = CreateStretched(root.transform, "Content");
                CreateDim(content, settings);
                RectTransform card = CreateCard(content, "Card", CardSize, rounded, settings);

                LocalizedText title = CreateText(card, "Title", KeyTitle, new Vector2(0f, TitleY), TitleSize,
                    TitleFontSize, settings.textColor, TextAlignmentOptions.Center).GetComponent<LocalizedText>();

                RectTransform earlyRow = CreateRect(card, "EarlyEndRow", new Vector2(0f, EarlyEndY), EarlyEndSize);
                LocalizedText earlyText = CreateText(earlyRow, "Text", KeyEarlyReason, Vector2.zero, EarlyEndSize,
                    EarlyEndFontSize, settings.earlyEndColor, TextAlignmentOptions.Center).GetComponent<LocalizedText>();
                earlyRow.gameObject.SetActive(false);

                StarRow starRow = CreateStars(card, star, settings);

                TMP_Text goldValue = CreateStatRow(card, "GoldRow", KeyGold, StatRowY[0], rounded, settings, null);
                TMP_Text tipsValue = CreateStatRow(card, "TipsRow", KeyTips, StatRowY[1], rounded, settings, null);
                TMP_Text comboValue = CreateStatRow(card, "ComboRow", KeyBestCombo, StatRowY[2], rounded, settings, KeyComboValue);

                Button playAgain = CreateButton(card, "PlayAgainButton", KeyPlayAgain, new Vector2(-ButtonColumnX, ButtonRowY[0]),
                    PrimaryButtonSize, PrimaryButtonFontSize, settings.playAgainColor, rounded, settings);
                Button adButton = CreateButton(card, "AdButton", KeyWatchAd, new Vector2(ButtonColumnX, ButtonRowY[0]),
                    PrimaryButtonSize, PrimaryButtonFontSize, settings.adButtonColor, rounded, settings);
                Button shop = CreateButton(card, "ShopButton", KeyBackToShop, new Vector2(-ButtonColumnX, ButtonRowY[1]),
                    SecondaryButtonSize, SecondaryButtonFontSize, settings.shopButtonColor, rounded, settings);
                Button upgrades = CreateButton(card, "UpgradesButton", KeyUpgrades, new Vector2(ButtonColumnX, ButtonRowY[1]),
                    SecondaryButtonSize, SecondaryButtonFontSize, settings.upgradesButtonColor, rounded, settings);

                UpgradesPanel upgradesPanel = CreateUpgradesPanel(root.transform, rounded, settings);

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

        private static void CreateDim(Transform parent, RoundEndSettings settings)
        {
            RectTransform dim = CreateStretched(parent, "Dim");
            Image image = dim.gameObject.AddComponent<Image>();
            image.color = settings.dimColor;

            // Perde tıklamaları yutar; alttaki salona dokunulmaz.
            image.raycastTarget = true;
        }

        /// <summary>Kenarlıklı, yuvarlak köşeli kart; içindeki parçalar dönen RectTransform'a eklenir.</summary>
        private static RectTransform CreateCard(Transform parent, string name, Vector2 size, Sprite rounded, RoundEndSettings settings)
        {
            RectTransform border = CreateRect(parent, name, Vector2.zero, size + Vector2.one * (CardBorderWidth * 2f));
            AddSlicedImage(border.gameObject, rounded, settings.cardBorderColor);

            RectTransform card = CreateRect(border, "Inner", Vector2.zero, size);
            AddSlicedImage(card.gameObject, rounded, settings.cardColor);
            return card;
        }

        private static StarRow CreateStars(Transform card, Sprite star, RoundEndSettings settings)
        {
            float width = StarSize * 3f + StarSpacing * 2f;
            RectTransform row = CreateRect(card, "Stars", new Vector2(0f, StarsY), new Vector2(width, StarSize));
            StarRow starRow = row.gameObject.AddComponent<StarRow>();

            var images = new Object[3];
            for (int i = 0; i < images.Length; i++)
            {
                float x = (i - 1) * (StarSize + StarSpacing);
                RectTransform tf = CreateRect(row, $"Star_{i + 1}", new Vector2(x, 0f), new Vector2(StarSize, StarSize));
                Image image = tf.gameObject.AddComponent<Image>();
                image.sprite = star;
                image.color = settings.starEmptyColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
                images[i] = image;
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
            RectTransform row = CreateRect(card, name, new Vector2(0f, y), StatRowSize);
            AddSlicedImage(row.gameObject, rounded, settings.statRowColor).raycastTarget = false;

            var textSize = new Vector2(StatRowSize.x * 0.5f - StatPaddingX, StatRowSize.y);
            float textX = StatRowSize.x * 0.25f - StatPaddingX * 0.5f;
            CreateText(row, "Label", labelKey, new Vector2(-textX, 0f), textSize, StatFontSize, settings.textColor,
                TextAlignmentOptions.MidlineLeft);
            TMP_Text value = CreateText(row, "Value", valueKey, new Vector2(textX, 0f), textSize, StatFontSize,
                settings.textColor, TextAlignmentOptions.MidlineRight);
            if (valueKey == null)
            {
                value.text = "0";
            }

            return value;
        }

        private static Button CreateButton(Transform parent, string name, string key, Vector2 position, Vector2 size,
            float fontSize, Color color, Sprite rounded, RoundEndSettings settings)
        {
            RectTransform tf = CreateRect(parent, name, position, size);
            Image image = AddSlicedImage(tf.gameObject, rounded, color);

            Button button = tf.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = ButtonHighlighted;
            colors.selectedColor = Color.white;
            colors.pressedColor = ButtonPressed;
            colors.disabledColor = ButtonDisabled;
            button.colors = colors;

            // Dokunmatikte seçili kalma görünümü olmasın.
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            CreateText(tf, "Label", key, Vector2.zero, size, fontSize, settings.buttonTextColor, TextAlignmentOptions.Center);
            return button;
        }

        private static UpgradesPanel CreateUpgradesPanel(Transform root, Sprite rounded, RoundEndSettings settings)
        {
            RectTransform panelTf = CreateStretched(root, "UpgradesPanel");
            UpgradesPanel panel = panelTf.gameObject.AddComponent<UpgradesPanel>();

            RectTransform content = CreateStretched(panelTf, "Content");
            CreateDim(content, settings);
            RectTransform card = CreateCard(content, "Card", UpgradesCardSize, rounded, settings);

            CreateText(card, "Title", KeyUpgradesTitle, new Vector2(0f, UpgradesTitleY), TitleSize, TitleFontSize,
                settings.textColor, TextAlignmentOptions.Center);
            CreateText(card, "Body", KeyComingSoon, new Vector2(0f, UpgradesBodyY), TitleSize, UpgradesBodyFontSize,
                settings.textColor, TextAlignmentOptions.Center);
            Button close = CreateButton(card, "CloseButton", KeyClose, new Vector2(0f, UpgradesCloseY), SecondaryButtonSize,
                SecondaryButtonFontSize, settings.closeButtonColor, rounded, settings);

            var so = new SerializedObject(panel);
            so.FindProperty("content").objectReferenceValue = content.gameObject;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            content.gameObject.SetActive(false);
            return panel;
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

        // ------------------------------------------------------------------ UI yardımcıları

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var tf = (RectTransform)go.transform;
            tf.SetParent(parent, false);
            tf.anchorMin = tf.anchorMax = tf.pivot = new Vector2(0.5f, 0.5f);
            tf.anchoredPosition = position;
            tf.sizeDelta = size;
            return tf;
        }

        private static RectTransform CreateStretched(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var tf = (RectTransform)go.transform;
            tf.SetParent(parent, false);
            tf.anchorMin = Vector2.zero;
            tf.anchorMax = Vector2.one;
            tf.offsetMin = tf.offsetMax = Vector2.zero;
            return tf;
        }

        private static Image AddSlicedImage(GameObject go, Sprite sprite, Color color)
        {
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        /// <summary>TMP metni; key verilirse LocalizedText eklenir ve metin çalışma zamanında yerelleştirme dosyasından gelir.</summary>
        private static TMP_Text CreateText(Transform parent, string name, string key, Vector2 position, Vector2 size,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform tf = CreateRect(parent, name, position, size);
            TextMeshProUGUI text = tf.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = MinAutoFontSize;
            text.fontSizeMax = fontSize;
            text.fontSize = fontSize;
            text.raycastTarget = false;

            if (!string.IsNullOrEmpty(key))
            {
                // Editörde anahtar görünür; oyunda LocalizedText gerçek metni yazar.
                text.text = key;
                LocalizedText localized = tf.gameObject.AddComponent<LocalizedText>();
                var so = new SerializedObject(localized);
                so.FindProperty("key").stringValue = key;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return text;
        }

        // ------------------------------------------------------------------ Asset'ler

        private static RoundEndSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<RoundEndSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder(Path.GetDirectoryName(SettingsPath)?.Replace('\\', '/'));
            settings = ScriptableObject.CreateInstance<RoundEndSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static Sprite GetOrCreateRoundedSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (existing != null)
            {
                return existing;
            }

            int size = RoundedTextureSize;
            float r = RoundedCornerRadius;
            return WriteSprite(RoundedSpritePath, size, (x, y) =>
            {
                // Köşe dairelerinin merkezine olan uzaklık; kenar ortalarında köşe yok.
                float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (size - r)));
                float dy = Mathf.Max(0f, Mathf.Max(r - y, y - (size - r)));
                return dx * dx + dy * dy <= r * r;
            }, new Vector4(RoundedSliceBorder, RoundedSliceBorder, RoundedSliceBorder, RoundedSliceBorder));
        }

        private static Sprite GetOrCreateStarSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);
            if (existing != null)
            {
                return existing;
            }

            int size = StarTextureSize;
            float center = size * 0.5f;
            float outer = center * 0.96f;
            float inner = outer * StarInnerRatio;

            // Uçları ve iç köşeleri sırayla dizilen çokgen; ilk uç yukarı bakar.
            var polygon = new Vector2[StarPoints * 2];
            for (int i = 0; i < polygon.Length; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / StarPoints;
                float radius = i % 2 == 0 ? outer : inner;
                polygon[i] = new Vector2(center + Mathf.Cos(angle) * radius, center + Mathf.Sin(angle) * radius);
            }

            return WriteSprite(StarSpritePath, size, (x, y) => InsidePolygon(polygon, new Vector2(x, y)), Vector4.zero);
        }

        // Beyaz şekil dokusu yazar (kenarlar Supersample x Supersample örnekle yumuşatılır) ve Sprite olarak içe aktarır.
        private static Sprite WriteSprite(string path, int size, System.Func<float, float, bool> inside, Vector4 border)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float step = 1f / Supersample;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            if (inside(x + (sx + 0.5f) * step, y + (sy + 0.5f) * step))
                            {
                                hits++;
                            }
                        }
                    }

                    byte alpha = (byte)(255 * hits / (Supersample * Supersample));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool InsidePolygon(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
