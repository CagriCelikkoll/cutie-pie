using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G19 fırın ekranının prefab'larını baştan kurar ("Cutie Pie/UI/Fırın Panelini Kur", tekrar çalıştırılabilir).
    /// - Prefabs/UI/Kitchen/StockEntry.prefab: stok şeridindeki tek pasta.
    /// - Prefabs/UI/Kitchen/RecipeOption.prefab: tarif seçicideki tek tarif.
    /// - Prefabs/UI/Kitchen/OvenCard.prefab: tek fırın kartı (Boş / Pişiyor / Hazır / Kilitli).
    /// - Prefabs/UI/Kitchen/OvenPanel.prefab: kendi Canvas'ı olan ekran (stok şeridi, fırınlar, tarif seçici, uçuş katmanı).
    /// Hiçbir sahneye dokunmaz; Kitchen.unity'ye prefab'ı altyapı geliştiricisi ekler, test için "Cutie Pie/Test/Fırın Test Sahnesini Kur".
    /// Ortak renkler RoundEndSettings'ten, fırına özel renkler OvenPanelSettings'ten okunur (yoksa oluşturulur);
    /// buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    public static class OvenPanelUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Fırın Panelini Kur";
        private const string OvenSettingsPath = "Assets/_Project/Data/Salon/OvenPanelSettings.asset";

        private const string StockEntryPath = UIBuilderKit.KitchenPrefabFolder + "/StockEntry.prefab";
        private const string RecipeOptionPath = UIBuilderKit.KitchenPrefabFolder + "/RecipeOption.prefab";
        private const string OvenCardPath = UIBuilderKit.KitchenPrefabFolder + "/OvenCard.prefab";
        private const string PanelPath = UIBuilderKit.KitchenPrefabFolder + "/OvenPanel.prefab";

        // Mutfak ekranı tabanda durur; round sonu (10) ve geliştirme mağazası (20) gibi açılır pencereler üstünde kalır.
        public const int CanvasSortingOrder = 5;

        // --- Panel (ekran merkezine göre)
        private static readonly Vector2 StockBarPosition = new Vector2(0f, 425f);
        private static readonly Vector2 StockBarSize = new Vector2(1780f, 170f);
        private const float StockTitleX = -770f;
        private static readonly Vector2 StockTitleSize = new Vector2(200f, 90f);
        private const float StockTitleFontSize = 44f;
        private const float StockEntriesX = 110f;
        private static readonly Vector2 StockEntriesSize = new Vector2(1500f, 130f);
        private const float StockEntrySpacing = 24f;

        private const float OvensTitleY = 290f;
        private static readonly Vector2 OvensTitleSize = new Vector2(800f, 90f);
        private const float OvensTitleFontSize = 64f;

        // 3 kart tam sığar, fazlası kaydırılır.
        private static readonly Vector2 OvenScrollPosition = new Vector2(0f, -125f);
        private static readonly Vector2 OvenScrollSize = new Vector2(1800f, 730f);
        private const int ListPaddingX = 15;
        private const int ListPaddingY = 10;
        private const float OvenCardSpacing = 40f;

        // --- Tarif seçici (kart merkezine göre)
        private static readonly Vector2 PickerCardSize = new Vector2(1500f, 820f);
        private const float PickerTitleY = 335f;
        private static readonly Vector2 PickerTitleSize = new Vector2(1000f, 90f);
        private const float PickerTitleFontSize = 64f;
        private static readonly Vector2 PickerScrollPosition = new Vector2(0f, -5f);
        private static readonly Vector2 PickerScrollSize = new Vector2(1420f, 520f);
        private const float OptionSpacing = 30f;
        private static readonly Vector2 PickerCloseButtonPosition = new Vector2(0f, -340f);
        private static readonly Vector2 PickerCloseButtonSize = new Vector2(420f, 100f);
        private const float PickerCloseFontSize = 42f;

        // --- Stok kalemi (kalem merkezine göre)
        private static readonly Vector2 StockEntrySize = new Vector2(320f, 110f);
        private const float StockIconX = -105f;
        private const float StockIconSize = 80f;
        private const float StockTextX = 45f;
        private static readonly Vector2 StockTextSize = new Vector2(200f, 48f);
        private const float StockNameY = 22f;
        private const float StockNameFontSize = 28f;
        private const float StockAmountY = -24f;
        private const float StockAmountFontSize = 40f;

        // --- Tarif kartı (kart merkezine göre)
        private static readonly Vector2 OptionSize = new Vector2(360f, 470f);
        private const float OptionIconY = 120f;
        private const float OptionIconSize = 180f;
        private static readonly Vector2 OptionTextSize = new Vector2(320f, 60f);
        private const float OptionNameY = -10f;
        private const float OptionNameFontSize = 38f;
        private const float OptionDurationY = -85f;
        private const float OptionSlicesY = -145f;
        private const float OptionDetailFontSize = 32f;

        // --- Fırın kartı (iç kart merkezine göre)
        private static readonly Vector2 OvenCardSize = new Vector2(480f, 660f);
        private const float CardTitleY = 275f;
        private static readonly Vector2 CardTextSize = new Vector2(440f, 60f);
        private const float CardTitleFontSize = 40f;
        private const float CardIconY = 150f;
        private const float CardIconSize = 170f;
        private const float CardStatusY = 35f;
        private const float CardStatusFontSize = 36f;
        private const float BakingGroupY = -85f;
        private static readonly Vector2 BakingGroupSize = new Vector2(440f, 140f);
        private const float CountdownY = 35f;
        private static readonly Vector2 CountdownSize = new Vector2(400f, 70f);
        private const float CountdownFontSize = 56f;
        private const float ProgressY = -35f;
        private static readonly Vector2 ProgressSize = new Vector2(380f, 34f);
        private static readonly Vector2 CardButtonPosition = new Vector2(0f, -245f);
        private static readonly Vector2 CardButtonSize = new Vector2(400f, 110f);
        private const float CardButtonFontSize = 40f;
        private const float AdButtonFontSize = 32f;
        private const float UnlockButtonFontSize = 34f;
        private static readonly Vector2 GlowSize = new Vector2(440f, 150f);

        private const string KeyStockTitle = "kitchen.stock.title";
        private const string KeyOvensTitle = "kitchen.ovens.title";
        private const string KeyPickerTitle = "kitchen.picker.title";
        private const string KeyClose = "common.close";
        private const string KeyOvenName = "kitchen.oven.name";
        private const string KeyBake = "kitchen.oven.bake";
        private const string KeySpeedUp = "kitchen.oven.speed_up";
        private const string KeyCollect = "kitchen.oven.collect";
        private const string KeyUnlock = "kitchen.oven.unlock";

        [MenuItem(MenuPath)]
        private static void BuildFromMenu()
        {
            GameObject panel = BuildPrefabs();
            Selection.activeObject = panel;
            EditorGUIUtility.PingObject(panel);
            Debug.Log($"[OvenPanelUIBuilder] Fırın ekranı prefab'ları kuruldu: {PanelPath}");
        }

        /// <summary>Tüm prefab'ları baştan kurar; panel prefab asset'ini döndürür.</summary>
        public static GameObject BuildPrefabs()
        {
            RoundEndSettings ui = UIBuilderKit.GetOrCreateSettings();
            OvenPanelSettings oven = GetOrCreateOvenSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();
            UIBuilderKit.EnsureFolder(UIBuilderKit.KitchenPrefabFolder);

            StockEntry entry = BuildStockEntry(ui, oven, rounded).GetComponent<StockEntry>();
            RecipeOption option = BuildRecipeOption(ui, oven, rounded).GetComponent<RecipeOption>();
            OvenCard card = BuildOvenCard(ui, oven, rounded).GetComponent<OvenCard>();
            return BuildPanel(ui, oven, rounded, entry, option, card);
        }

        /// <summary>Data/Salon/OvenPanelSettings.asset; yoksa varsayılan değerlerle oluşturur.</summary>
        public static OvenPanelSettings GetOrCreateOvenSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<OvenPanelSettings>(OvenSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(OvenSettingsPath)?.Replace('\\', '/'));
            settings = ScriptableObject.CreateInstance<OvenPanelSettings>();
            AssetDatabase.CreateAsset(settings, OvenSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        // ------------------------------------------------------------------ Stok kalemi

        private static GameObject BuildStockEntry(RoundEndSettings ui, OvenPanelSettings oven, Sprite rounded)
        {
            var root = new GameObject("StockEntry", typeof(RectTransform));
            try
            {
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = StockEntrySize;
                UIBuilderKit.AddSlicedImage(root, rounded, oven.stockEntryColor).raycastTarget = false;
                StockEntry entry = root.AddComponent<StockEntry>();

                Image icon = CreatePlaceholderIcon(tf, "Icon", new Vector2(StockIconX, 0f), StockIconSize, rounded, oven);

                TMP_Text nameText = UIBuilderKit.CreateText(tf, "Name", string.Empty, new Vector2(StockTextX, StockNameY),
                    StockTextSize, StockNameFontSize, ui.textColor, TextAlignmentOptions.MidlineLeft, bold: false);
                TMP_Text amount = UIBuilderKit.CreateText(tf, "Amount", null, new Vector2(StockTextX, StockAmountY),
                    StockTextSize, StockAmountFontSize, ui.textColor, TextAlignmentOptions.MidlineLeft);
                amount.text = "0";

                var so = new SerializedObject(entry);
                so.FindProperty("settings").objectReferenceValue = oven;
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("nameText").objectReferenceValue = nameText.GetComponent<LocalizedText>();
                so.FindProperty("amountText").objectReferenceValue = amount;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, StockEntryPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ Tarif kartı

        private static GameObject BuildRecipeOption(RoundEndSettings ui, OvenPanelSettings oven, Sprite rounded)
        {
            var root = new GameObject("RecipeOption", typeof(RectTransform));
            try
            {
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = OptionSize;

                // Kartın tamamı butondur; sürükleme üstteki ScrollRect'e geçer.
                Image background = UIBuilderKit.AddSlicedImage(root, rounded, ui.upgradeCardColor);
                Button button = UIBuilderKit.AddButton(root, background);
                RecipeOption option = root.AddComponent<RecipeOption>();

                Image icon = CreatePlaceholderIcon(tf, "Icon", new Vector2(0f, OptionIconY), OptionIconSize, rounded, oven);

                TMP_Text nameText = UIBuilderKit.CreateText(tf, "Name", string.Empty, new Vector2(0f, OptionNameY),
                    OptionTextSize, OptionNameFontSize, ui.textColor, TextAlignmentOptions.Center);
                TMP_Text duration = UIBuilderKit.CreateText(tf, "Duration", string.Empty, new Vector2(0f, OptionDurationY),
                    OptionTextSize, OptionDetailFontSize, ui.textColor, TextAlignmentOptions.Center, bold: false);
                TMP_Text slices = UIBuilderKit.CreateText(tf, "Slices", string.Empty, new Vector2(0f, OptionSlicesY),
                    OptionTextSize, OptionDetailFontSize, ui.textColor, TextAlignmentOptions.Center, bold: false);

                var so = new SerializedObject(option);
                so.FindProperty("settings").objectReferenceValue = oven;
                so.FindProperty("button").objectReferenceValue = button;
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("nameText").objectReferenceValue = nameText.GetComponent<LocalizedText>();
                so.FindProperty("durationText").objectReferenceValue = duration.GetComponent<LocalizedText>();
                so.FindProperty("slicesText").objectReferenceValue = slices.GetComponent<LocalizedText>();
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, RecipeOptionPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ Fırın kartı

        private static GameObject BuildOvenCard(RoundEndSettings ui, OvenPanelSettings oven, Sprite rounded)
        {
            var root = new GameObject("OvenCard", typeof(RectTransform));
            try
            {
                // Kök: kenarlık; içinde rengi duruma göre değişen zemin. Yerleşim düzeni kökün boyutunu kullanır.
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = OvenCardSize + Vector2.one * (UIBuilderKit.CardBorderWidth * 2f);
                UIBuilderKit.AddSlicedImage(root, rounded, ui.cardBorderColor);
                CanvasGroup group = root.AddComponent<CanvasGroup>();
                OvenCard card = root.AddComponent<OvenCard>();

                RectTransform inner = UIBuilderKit.CreateRect(tf, "Inner", Vector2.zero, OvenCardSize);
                Image background = UIBuilderKit.AddSlicedImage(inner.gameObject, rounded, oven.emptyCardColor);

                TMP_Text title = UIBuilderKit.CreateText(inner, "Title", KeyOvenName, new Vector2(0f, CardTitleY),
                    CardTextSize, CardTitleFontSize, ui.textColor, TextAlignmentOptions.Center);
                Image icon = CreatePlaceholderIcon(inner, "Icon", new Vector2(0f, CardIconY), CardIconSize, rounded, oven);
                TMP_Text status = UIBuilderKit.CreateText(inner, "Status", string.Empty, new Vector2(0f, CardStatusY),
                    CardTextSize, CardStatusFontSize, ui.textColor, TextAlignmentOptions.Center);

                // Pişiyor: geri sayım + ilerleme çubuğu.
                RectTransform baking = UIBuilderKit.CreateRect(inner, "Baking", new Vector2(0f, BakingGroupY), BakingGroupSize);
                TMP_Text countdown = UIBuilderKit.CreateText(baking, "Countdown", null, new Vector2(0f, CountdownY),
                    CountdownSize, CountdownFontSize, ui.textColor, TextAlignmentOptions.Center);
                countdown.text = "0:00";
                RectTransform fill = CreateProgressBar(baking, new Vector2(0f, ProgressY), rounded, oven);

                Button bake = UIBuilderKit.CreateButton(inner, "BakeButton", KeyBake, CardButtonPosition, CardButtonSize,
                    CardButtonFontSize, oven.bakeButtonColor, rounded, ui);
                Button ad = UIBuilderKit.CreateButton(inner, "AdButton", KeySpeedUp, CardButtonPosition, CardButtonSize,
                    AdButtonFontSize, ui.adButtonColor, rounded, ui);

                // Parlama Topla'nın arkasında (önce eklenir, altta çizilir).
                RectTransform glowTf = UIBuilderKit.CreateRect(inner, "CollectGlow", CardButtonPosition, GlowSize);
                Image glow = UIBuilderKit.AddSlicedImage(glowTf.gameObject, rounded, oven.glowColor);
                glow.raycastTarget = false;
                Button collect = UIBuilderKit.CreateButton(inner, "CollectButton", KeyCollect, CardButtonPosition,
                    CardButtonSize, CardButtonFontSize, oven.collectButtonColor, rounded, ui);

                Button unlock = UIBuilderKit.CreateButton(inner, "UnlockButton", KeyUnlock, CardButtonPosition,
                    CardButtonSize, UnlockButtonFontSize, oven.unlockButtonColor, rounded, ui);

                // Kayıtta yalnızca Boş görünümü açık; kart Bind/Show ile kendini ayarlar.
                icon.gameObject.SetActive(false);
                baking.gameObject.SetActive(false);
                ad.gameObject.SetActive(false);
                glow.gameObject.SetActive(false);
                collect.gameObject.SetActive(false);
                unlock.gameObject.SetActive(false);

                var so = new SerializedObject(card);
                so.FindProperty("settings").objectReferenceValue = oven;
                so.FindProperty("background").objectReferenceValue = background;
                so.FindProperty("canvasGroup").objectReferenceValue = group;
                so.FindProperty("titleText").objectReferenceValue = title.GetComponent<LocalizedText>();
                so.FindProperty("statusText").objectReferenceValue = status.GetComponent<LocalizedText>();
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("bakingGroup").objectReferenceValue = baking.gameObject;
                so.FindProperty("countdownText").objectReferenceValue = countdown;
                so.FindProperty("progressFill").objectReferenceValue = fill;
                so.FindProperty("bakeButton").objectReferenceValue = bake;
                so.FindProperty("adButton").objectReferenceValue = ad;
                so.FindProperty("adLabel").objectReferenceValue = ad.GetComponentInChildren<LocalizedText>(true);
                so.FindProperty("collectButton").objectReferenceValue = collect;
                so.FindProperty("collectGlow").objectReferenceValue = glow;
                so.FindProperty("unlockButton").objectReferenceValue = unlock;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, OvenCardPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Zemin + soldan dolan dolgu; dolgunun genişliği OvenCard'da anchorMax.x ile (0–1) ayarlanır.
        private static RectTransform CreateProgressBar(Transform parent, Vector2 position, Sprite rounded, OvenPanelSettings oven)
        {
            RectTransform back = UIBuilderKit.CreateRect(parent, "Progress", position, ProgressSize);
            UIBuilderKit.AddSlicedImage(back.gameObject, rounded, oven.progressBackColor).raycastTarget = false;

            RectTransform fill = UIBuilderKit.CreateStretched(back, "Fill");
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UIBuilderKit.AddSlicedImage(fill.gameObject, rounded, oven.progressFillColor).raycastTarget = false;
            return fill;
        }

        // ------------------------------------------------------------------ Panel

        private static GameObject BuildPanel(RoundEndSettings ui, OvenPanelSettings oven, Sprite rounded,
            StockEntry entryPrefab, RecipeOption optionPrefab, OvenCard cardPrefab)
        {
            var root = new GameObject("OvenPanel", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                OvenPanel panel = root.AddComponent<OvenPanel>();

                RectTransform content = UIBuilderKit.CreateStretched(root.transform, "Content");

                // Tam ekran zemin; alttaki sahneye dokunulmaz.
                RectTransform background = UIBuilderKit.CreateStretched(content, "Background");
                background.gameObject.AddComponent<Image>().color = oven.backgroundColor;

                StockSummary stock = CreateStockBar(content, ui, oven, rounded, entryPrefab);

                UIBuilderKit.CreateText(content, "OvensTitle", KeyOvensTitle, new Vector2(0f, OvensTitleY), OvensTitleSize,
                    OvensTitleFontSize, ui.textColor, TextAlignmentOptions.Center);
                (ScrollRect _, RectTransform ovenList) = UIBuilderKit.CreateHorizontalScroll(content, "OvenScroll", "Ovens",
                    OvenScrollPosition, OvenScrollSize, ListPaddingX, ListPaddingY, OvenCardSpacing);

                RecipePicker picker = CreatePicker(content, ui, rounded, optionPrefab);
                SliceFlyAnimator fly = CreateFlyLayer(content, oven, rounded);

                var so = new SerializedObject(panel);
                so.FindProperty("settings").objectReferenceValue = oven;
                so.FindProperty("content").objectReferenceValue = content.gameObject;
                so.FindProperty("ovenContainer").objectReferenceValue = ovenList;
                so.FindProperty("ovenCardPrefab").objectReferenceValue = cardPrefab;
                so.FindProperty("stockSummary").objectReferenceValue = stock;
                so.FindProperty("recipePicker").objectReferenceValue = picker;
                so.FindProperty("flyAnimator").objectReferenceValue = fly;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Üst şerit: solda "Stok" başlığı, sağında pastalar yan yana (ortalı).
        private static StockSummary CreateStockBar(Transform content, RoundEndSettings ui, OvenPanelSettings oven,
            Sprite rounded, StockEntry entryPrefab)
        {
            RectTransform bar = UIBuilderKit.CreateRect(content, "StockBar", StockBarPosition, StockBarSize);
            UIBuilderKit.AddSlicedImage(bar.gameObject, rounded, oven.stockBarColor).raycastTarget = false;
            StockSummary summary = bar.gameObject.AddComponent<StockSummary>();

            UIBuilderKit.CreateText(bar, "Title", KeyStockTitle, new Vector2(StockTitleX, 0f), StockTitleSize,
                StockTitleFontSize, ui.textColor, TextAlignmentOptions.Center);

            RectTransform entries = UIBuilderKit.CreateRect(bar, "Entries", new Vector2(StockEntriesX, 0f), StockEntriesSize);
            HorizontalLayoutGroup layout = entries.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = StockEntrySpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var so = new SerializedObject(summary);
            so.FindProperty("entryContainer").objectReferenceValue = entries;
            so.FindProperty("entryPrefab").objectReferenceValue = entryPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            return summary;
        }

        // Perde + kart: başlık, yatay kaydırılan tarifler, Kapat. Kök hep açık; içerik RecipePicker'ın Awake'inde kapanır.
        private static RecipePicker CreatePicker(Transform content, RoundEndSettings ui, Sprite rounded, RecipeOption optionPrefab)
        {
            RectTransform pickerRoot = UIBuilderKit.CreateStretched(content, "RecipePicker");
            RecipePicker picker = pickerRoot.gameObject.AddComponent<RecipePicker>();

            RectTransform pickerContent = UIBuilderKit.CreateStretched(pickerRoot, "Content");
            UIBuilderKit.CreateDim(pickerContent, ui);
            RectTransform card = UIBuilderKit.CreateCard(pickerContent, "Card", PickerCardSize, rounded, ui);

            UIBuilderKit.CreateText(card, "Title", KeyPickerTitle, new Vector2(0f, PickerTitleY), PickerTitleSize,
                PickerTitleFontSize, ui.textColor, TextAlignmentOptions.Center);
            (ScrollRect scroll, RectTransform list) = UIBuilderKit.CreateHorizontalScroll(card, "RecipeScroll", "Recipes",
                PickerScrollPosition, PickerScrollSize, ListPaddingX, ListPaddingY, OptionSpacing);
            Button close = UIBuilderKit.CreateButton(card, "CloseButton", KeyClose, PickerCloseButtonPosition,
                PickerCloseButtonSize, PickerCloseFontSize, ui.closeButtonColor, rounded, ui);

            var so = new SerializedObject(picker);
            so.FindProperty("content").objectReferenceValue = pickerContent.gameObject;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("scrollRect").objectReferenceValue = scroll;
            so.FindProperty("optionContainer").objectReferenceValue = list;
            so.FindProperty("optionPrefab").objectReferenceValue = optionPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            pickerContent.gameObject.SetActive(false);
            return picker;
        }

        // En üstteki katman: tıklama almaz, uçan dilimler burada çizilir.
        private static SliceFlyAnimator CreateFlyLayer(Transform content, OvenPanelSettings oven, Sprite rounded)
        {
            RectTransform layer = UIBuilderKit.CreateStretched(content, "FlyLayer");
            SliceFlyAnimator fly = layer.gameObject.AddComponent<SliceFlyAnimator>();

            Image template = CreatePlaceholderIcon(layer, "SliceTemplate", Vector2.zero, oven.flyIconSize, rounded, oven);
            template.gameObject.SetActive(false);

            var so = new SerializedObject(fly);
            so.FindProperty("settings").objectReferenceValue = oven;
            so.FindProperty("layer").objectReferenceValue = layer;
            so.FindProperty("iconTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();
            return fly;
        }

        // Simgesi olmayan tarif/ürün için yuvarlak köşeli yer tutucu kutu; tıklama almaz.
        private static Image CreatePlaceholderIcon(Transform parent, string name, Vector2 position, float size, Sprite rounded,
            OvenPanelSettings oven)
        {
            RectTransform tf = UIBuilderKit.CreateRect(parent, name, position, new Vector2(size, size));
            Image image = UIBuilderKit.AddSlicedImage(tf.gameObject, rounded, oven.iconPlaceholderColor);
            image.raycastTarget = false;
            return image;
        }
    }
}
