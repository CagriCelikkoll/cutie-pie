using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G20 özel sipariş ekranının prefab'larını baştan kurar ("Cutie Pie/UI/Sipariş Ekranını Kur", tekrar çalıştırılabilir).
    /// - Prefabs/UI/Kitchen/CakeView.prefab: CakeBuild'i üstten çizen pasta (kutu, taban, krema, süs yüzeyi).
    /// - Prefabs/UI/Kitchen/OrderCard.prefab: panodaki tek yer (fiş ya da boş yer); içinde CakeView iç içe prefab'dır.
    /// - Prefabs/UI/Kitchen/PartOption.prefab: yapım ekranının altındaki tek seçenek (PartOption + DecorationDragSource).
    /// - Prefabs/UI/Kitchen/CakeBuilder.prefab: kendi Canvas'ı olan tam ekran pasta yapım ekranı (fiş, pasta, seçenekler, sonuç).
    /// - Prefabs/UI/Kitchen/OrderBoard.prefab: kendi Canvas'ı olan pano (sıralama 5), ekranın sağ sütunu;
    ///   içinde CakeBuilder iç içe prefab'dır (alt Canvas, sıralama 6).
    /// Hiçbir sahneye dokunmaz; Kitchen.unity'ye prefab'ı altyapı geliştiricisi ekler, test için "Cutie Pie/Test/Sipariş Test Sahnesini Kur".
    /// Ortak renkler RoundEndSettings'ten, siparişe özel renk/oranlar OrderUISettings'ten okunur (yoksa oluşturulur);
    /// buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    public static class OrderUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Sipariş Ekranını Kur";
        private const string OrderSettingsPath = "Assets/_Project/Data/Salon/OrderUISettings.asset";

        private const string CakeViewPath = UIBuilderKit.KitchenPrefabFolder + "/CakeView.prefab";
        private const string OrderCardPath = UIBuilderKit.KitchenPrefabFolder + "/OrderCard.prefab";
        private const string BoardPath = UIBuilderKit.KitchenPrefabFolder + "/OrderBoard.prefab";
        private const string PartOptionPath = UIBuilderKit.KitchenPrefabFolder + "/PartOption.prefab";
        private const string CakeBuilderPath = UIBuilderKit.KitchenPrefabFolder + "/CakeBuilder.prefab";

        /// <summary>Pano Mutfak ekranının tabanında durur (fırın ekranıyla aynı sıra).</summary>
        public const int BoardSortingOrder = 5;

        /// <summary>Yapım ekranı açılınca panonun ve fırın ekranının üstünü kaplar.</summary>
        public const int CakeBuilderSortingOrder = 6;

        // --- Yapım ekranı (ekran merkezine göre). Hepsi 20:9 (2147x966) ve 4:3 (1663x1247) güvenli kutusuna (1663x966) sığar.
        private const float StepCounterY = 445f;
        private static readonly Vector2 StepCounterSize = new Vector2(700f, 50f);
        private const float StepCounterFontSize = 34f;
        private const float StepTitleY = 385f;
        private static readonly Vector2 StepTitleSize = new Vector2(1100f, 70f);
        private const float StepTitleFontSize = 56f;

        private static readonly Vector2 TicketPosition = new Vector2(-620f, 30f);
        private static readonly Vector2 TicketSize = new Vector2(360f, 540f);
        private const float TicketTitleY = 225f;
        private static readonly Vector2 TicketTitleSize = new Vector2(320f, 60f);
        private const float TicketTitleFontSize = 40f;
        private const float TicketCakeY = 25f;
        private const float TicketCakeSize = 300f;
        private const float TicketRewardY = -195f;
        private const float TicketGoldIconX = -40f;
        private const float TicketRewardTextX = 50f;
        private static readonly Vector2 TicketRewardTextSize = new Vector2(140f, 64f);
        private const float TicketRewardFontSize = 50f;

        private static readonly Vector2 BuildCakePosition = new Vector2(0f, 40f);
        private const float BuildCakeSize = 560f;

        private const float DecorationInfoX = 590f;
        private const float DecorationCountY = 120f;
        private static readonly Vector2 DecorationCountSize = new Vector2(440f, 70f);
        private const float DecorationCountFontSize = 48f;
        private const float DecorationHintY = 40f;
        private static readonly Vector2 DecorationHintSize = new Vector2(440f, 100f);
        private const float DecorationHintFontSize = 32f;

        private static readonly Vector2 OptionBarPosition = new Vector2(0f, -360f);
        private static readonly Vector2 OptionBarSize = new Vector2(1040f, 200f);
        private const float OptionSpacing = 24f;

        private static readonly Vector2 BackButtonPosition = new Vector2(-690f, -360f);
        private static readonly Vector2 NextButtonPosition = new Vector2(690f, -360f);
        private static readonly Vector2 StepButtonSize = new Vector2(240f, 110f);
        private const float StepButtonFontSize = 44f;

        // --- Seçenek kartı (kart merkezine göre)
        private static readonly Vector2 OptionSize = new Vector2(220f, 180f);
        private const float OptionFrameWidth = 10f;
        private const float SwatchY = 22f;
        private const float SwatchSize = 100f;
        private const float OptionNameY = -55f;
        private static readonly Vector2 OptionNameSize = new Vector2(190f, 44f);
        private const float OptionNameFontSize = 26f;

        // --- Sonuç kartı (kart merkezine göre)
        private static readonly Vector2 ResultCardSize = new Vector2(900f, 760f);
        private const float ResultTitleY = 300f;
        private static readonly Vector2 ResultTitleSize = new Vector2(820f, 80f);
        private const float ResultTitleFontSize = 60f;
        private const float SimilarityLabelY = 205f;
        private static readonly Vector2 SimilarityLabelSize = new Vector2(600f, 56f);
        private const float SimilarityLabelFontSize = 40f;
        private const float PercentY = 120f;
        private static readonly Vector2 PercentSize = new Vector2(600f, 110f);
        private const float PercentFontSize = 100f;
        private const float StarsY = -10f;
        private const float StarSize = 120f;
        private const float StarSpacing = 150f;
        private const int StarCount = 3;
        private const float GoldRowY = -140f;
        private const float TipRowY = -215f;
        private const float RowIconX = -200f;
        private const float RowIconSize = 56f;
        private const float RowLabelX = -60f;
        private static readonly Vector2 RowLabelSize = new Vector2(220f, 60f);
        private const float RowLabelFontSize = 40f;
        private const float RowValueX = 170f;
        private static readonly Vector2 RowValueSize = new Vector2(200f, 60f);
        private const float RowValueFontSize = 48f;
        private static readonly Vector2 ContinuePosition = new Vector2(0f, -315f);
        private static readonly Vector2 ContinueSize = new Vector2(420f, 100f);
        private const float ContinueFontSize = 42f;

        private const string KeyStepCounter = "order.builder.step_counter";
        private const string KeyTicket = "order.builder.ticket";
        private const string KeyBack = "order.builder.back";
        private const string KeyNext = "order.builder.next";
        private const string KeyDecorationHint = "order.decor.hint";
        private const string KeyResultTitle = "order.result.title";
        private const string KeySimilarity = "order.result.similarity";
        private const string KeyGold = "order.result.gold";
        private const string KeyTip = "order.result.tip";
        private const string KeyContinue = "order.result.continue";

        // --- Pasta çizimi (prefab kökü; örnekler kendi boyutunu verir)
        private const float CakeViewDefaultSize = 200f;

        // --- Alan: Kitchen.unity'deki OrderBoardSlot ile aynı anchor'lar (Canvas'ın 0–1 oranı; 1920x1080'de 653x886).
        // Soldaki fırın ekranı (OvenPanelUIBuilder.AreaAnchorMin/Max, OvenPanelSlot) ve üst şerit (TopBar, y 0.88–1) boş kalır.
        public static readonly Vector2 AreaAnchorMin = new Vector2(0.64f, 0.03f);
        public static readonly Vector2 AreaAnchorMax = new Vector2(0.98f, 0.85f);
        private const float BoardTitleTop = 16f;
        private const float BoardTitleHeight = 70f;
        private const float BoardTitleFontSize = 52f;
        private const float BoardPadding = 16f;
        private const float CardListTop = 100f;
        private const float CardSpacing = 12f;

        // --- Fiş kartı (iç kart merkezine göre). 3 kart geniş (20:9) ve dar (4:3) ekranlarda da alana sığar.
        private static readonly Vector2 CardSize = new Vector2(520f, 182f);
        private const float CardCakeX = -160f;
        private const float CardCakeSize = 160f;
        private const float RewardIconX = 0f;
        private const float RewardIconSize = 56f;
        private const float RewardTextX = 120f;
        private static readonly Vector2 RewardTextSize = new Vector2(180f, 70f);
        private const float RewardFontSize = 56f;
        private const float EmptyLabelY = 32f;
        private static readonly Vector2 EmptyLabelSize = new Vector2(480f, 56f);
        private const float EmptyLabelFontSize = 38f;
        private const float CountdownY = -30f;
        private static readonly Vector2 CountdownSize = new Vector2(480f, 70f);
        private const float CountdownFontSize = 56f;

        private const string KeyBoardTitle = "order.board.title";

        [MenuItem(MenuPath)]
        private static void BuildFromMenu()
        {
            GameObject board = BuildPrefabs();
            Selection.activeObject = board;
            EditorGUIUtility.PingObject(board);
            Debug.Log($"[OrderUIBuilder] Sipariş ekranı prefab'ları kuruldu: {BoardPath}");
        }

        /// <summary>Tüm prefab'ları baştan kurar; pano prefab asset'ini döndürür.</summary>
        public static GameObject BuildPrefabs()
        {
            RoundEndSettings ui = UIBuilderKit.GetOrCreateSettings();
            OrderUISettings order = GetOrCreateOrderSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();
            Sprite circle = UIBuilderKit.GetOrCreateCircleSprite();
            UIBuilderKit.EnsureFolder(UIBuilderKit.KitchenPrefabFolder);

            Sprite star = UIBuilderKit.GetOrCreateStarSprite();

            GameObject cakeView = BuildCakeView(order, rounded, circle);
            OrderCard card = BuildOrderCard(ui, order, rounded, circle, cakeView).GetComponent<OrderCard>();
            PartOption option = BuildPartOption(ui, order, rounded, circle).GetComponent<PartOption>();
            GameObject builder = BuildCakeBuilder(ui, order, rounded, circle, star, cakeView, option);
            return BuildBoard(ui, order, rounded, card, builder);
        }

        /// <summary>Data/Salon/OrderUISettings.asset; yoksa varsayılan değerlerle oluşturur.</summary>
        public static OrderUISettings GetOrCreateOrderSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<OrderUISettings>(OrderSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(OrderSettingsPath)?.Replace('\\', '/'));
            settings = ScriptableObject.CreateInstance<OrderUISettings>();
            AssetDatabase.CreateAsset(settings, OrderSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        // ------------------------------------------------------------------ Pasta çizimi

        private static GameObject BuildCakeView(OrderUISettings order, Sprite rounded, Sprite circle)
        {
            var root = new GameObject("CakeView", typeof(RectTransform));
            try
            {
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = new Vector2(CakeViewDefaultSize, CakeViewDefaultSize);
                CakeView view = root.AddComponent<CakeView>();

                // Kutu tam kare (yuvarlak köşeli, dilimli); taban ve krema daire; oranlar CakeView'da ayarlardan uygulanır.
                Image box = CreateLayer(tf, "Box", 1f, rounded, Image.Type.Sliced, order.missingPartColor);
                Image cakeBase = CreateLayer(tf, "Base", order.baseSizeRatio, circle, Image.Type.Simple, order.missingPartColor);
                Image cream = CreateLayer(tf, "Cream", order.creamSizeRatio, circle, Image.Type.Simple, order.missingPartColor);

                RectTransform surface = UIBuilderKit.CreateStretched(tf, "Surface");
                SetCentered(surface, order.creamSizeRatio);

                RectTransform decoTf = UIBuilderKit.CreateStretched(surface, "DecorationTemplate");
                Image deco = decoTf.gameObject.AddComponent<Image>();
                deco.sprite = circle;
                deco.type = Image.Type.Simple;
                deco.raycastTarget = false;

                // Düzenlenebilir modda (yapım ekranı, süs adımı) konulmuş süse dokununca geri alınır.
                decoTf.gameObject.AddComponent<CakeDecorationHandle>();
                decoTf.gameObject.SetActive(false);

                // Kayıtta kutu kapalı; Render seçilen kutuyu açar.
                box.gameObject.SetActive(false);

                var so = new SerializedObject(view);
                so.FindProperty("settings").objectReferenceValue = order;
                so.FindProperty("boxImage").objectReferenceValue = box;
                so.FindProperty("baseImage").objectReferenceValue = cakeBase;
                so.FindProperty("creamImage").objectReferenceValue = cream;
                so.FindProperty("surface").objectReferenceValue = surface;
                so.FindProperty("decorationTemplate").objectReferenceValue = deco;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, CakeViewPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Image CreateLayer(Transform parent, string name, float ratio, Sprite sprite, Image.Type type, Color color)
        {
            RectTransform tf = UIBuilderKit.CreateStretched(parent, name);
            SetCentered(tf, ratio);
            Image image = tf.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetCentered(RectTransform tf, float ratio)
        {
            float min = (1f - ratio) * 0.5f;
            tf.anchorMin = new Vector2(min, min);
            tf.anchorMax = new Vector2(1f - min, 1f - min);
            tf.offsetMin = tf.offsetMax = Vector2.zero;
        }

        /// <summary>CakeView prefab'ının iç içe örneğini verilen konum ve kare boyutla ekler.</summary>
        private static CakeView InstantiateCakeView(GameObject prefab, Transform parent, Vector2 position, float size)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var tf = (RectTransform)instance.transform;
            tf.SetParent(parent, false);
            tf.anchorMin = tf.anchorMax = tf.pivot = new Vector2(0.5f, 0.5f);
            tf.anchoredPosition = position;
            tf.sizeDelta = new Vector2(size, size);
            return instance.GetComponent<CakeView>();
        }

        // ------------------------------------------------------------------ Fiş kartı

        private static GameObject BuildOrderCard(RoundEndSettings ui, OrderUISettings order, Sprite rounded, Sprite circle,
            GameObject cakeViewPrefab)
        {
            var root = new GameObject("OrderCard", typeof(RectTransform));
            try
            {
                // Kök: kenarlık; içinde rengi duruma göre değişen zemin. Kartın tamamı butondur.
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = CardSize + Vector2.one * (UIBuilderKit.CardBorderWidth * 2f);
                UIBuilderKit.AddSlicedImage(root, rounded, ui.cardBorderColor);

                RectTransform inner = UIBuilderKit.CreateRect(tf, "Inner", Vector2.zero, CardSize);
                Image background = UIBuilderKit.AddSlicedImage(inner.gameObject, rounded, order.ticketColor);
                background.raycastTarget = false;
                Button button = UIBuilderKit.AddButton(root, background);
                OrderCard card = root.AddComponent<OrderCard>();

                // Fiş: solda hedef pasta, sağda altın simgesi + ödül.
                RectTransform ticket = UIBuilderKit.CreateStretched(inner, "Ticket");
                CakeView cake = InstantiateCakeView(cakeViewPrefab, ticket, new Vector2(CardCakeX, 0f), CardCakeSize);
                UIBuilderKit.CreateIcon(ticket, "GoldIcon", new Vector2(RewardIconX, 0f), RewardIconSize, circle, ui.goldIconColor);
                TMP_Text reward = UIBuilderKit.CreateText(ticket, "Reward", null, new Vector2(RewardTextX, 0f), RewardTextSize,
                    RewardFontSize, ui.textColor, TextAlignmentOptions.MidlineLeft);
                reward.text = "0";

                // Boş yer: "Yeni sipariş" + mm:ss ya da "Sırada".
                RectTransform empty = UIBuilderKit.CreateStretched(inner, "Empty");
                TMP_Text label = UIBuilderKit.CreateText(empty, "Label", string.Empty, new Vector2(0f, EmptyLabelY),
                    EmptyLabelSize, EmptyLabelFontSize, ui.textColor, TextAlignmentOptions.Center);
                TMP_Text countdown = UIBuilderKit.CreateText(empty, "Countdown", null, new Vector2(0f, CountdownY),
                    CountdownSize, CountdownFontSize, ui.textColor, TextAlignmentOptions.Center);
                countdown.text = "00:00";
                empty.gameObject.SetActive(false);

                var so = new SerializedObject(card);
                so.FindProperty("settings").objectReferenceValue = order;
                so.FindProperty("background").objectReferenceValue = background;
                so.FindProperty("button").objectReferenceValue = button;
                so.FindProperty("ticketGroup").objectReferenceValue = ticket.gameObject;
                so.FindProperty("cakeView").objectReferenceValue = cake;
                so.FindProperty("rewardText").objectReferenceValue = reward;
                so.FindProperty("emptyGroup").objectReferenceValue = empty.gameObject;
                so.FindProperty("emptyLabel").objectReferenceValue = label.GetComponent<LocalizedText>();
                so.FindProperty("countdownText").objectReferenceValue = countdown;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, OrderCardPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ Seçenek kartı

        private static GameObject BuildPartOption(RoundEndSettings ui, OrderUISettings order, Sprite rounded, Sprite circle)
        {
            var root = new GameObject("PartOption", typeof(RectTransform));
            try
            {
                // Kök: seçilince rengi değişen çerçeve (butonun hedefi); içinde zemin, görsel ve ad.
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = OptionSize;
                Image frame = UIBuilderKit.AddSlicedImage(root, rounded, order.optionFrameColor);
                Button button = UIBuilderKit.AddButton(root, frame);
                PartOption option = root.AddComponent<PartOption>();
                root.AddComponent<DecorationDragSource>();

                RectTransform inner = UIBuilderKit.CreateRect(tf, "Inner", Vector2.zero,
                    OptionSize - Vector2.one * (OptionFrameWidth * 2f));
                UIBuilderKit.AddSlicedImage(inner.gameObject, rounded, order.optionColor).raycastTarget = false;

                // Daire yer tutucu (taban, krema, süs); kutular için kare yer tutucu PartOption'a ayrıca verilir.
                Image swatch = UIBuilderKit.CreateIcon(inner, "Swatch", new Vector2(0f, SwatchY), SwatchSize, circle, Color.white);
                swatch.preserveAspect = false;

                TMP_Text nameText = UIBuilderKit.CreateText(inner, "Name", string.Empty, new Vector2(0f, OptionNameY),
                    OptionNameSize, OptionNameFontSize, ui.textColor, TextAlignmentOptions.Center);

                var so = new SerializedObject(option);
                so.FindProperty("settings").objectReferenceValue = order;
                so.FindProperty("button").objectReferenceValue = button;
                so.FindProperty("frame").objectReferenceValue = frame;
                so.FindProperty("swatch").objectReferenceValue = swatch;
                so.FindProperty("boxPlaceholder").objectReferenceValue = rounded;
                so.FindProperty("nameText").objectReferenceValue = nameText.GetComponent<LocalizedText>();
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, PartOptionPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ Yapım ekranı

        private static GameObject BuildCakeBuilder(RoundEndSettings ui, OrderUISettings order, Sprite rounded, Sprite circle,
            Sprite star, GameObject cakeViewPrefab, PartOption optionPrefab)
        {
            var root = new GameObject("CakeBuilder", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, CakeBuilderSortingOrder);
                CakeBuilder builder = root.AddComponent<CakeBuilder>();

                // Tam ekran zemin: açıkken alttaki pano, fırın ekranı ve üst şerit tıklama almaz.
                RectTransform content = UIBuilderKit.CreateStretched(root.transform, "Content");
                content.gameObject.AddComponent<Image>().color = order.builderBackgroundColor;

                // Başlık: adım sayacı + adımın ne istediği.
                TMP_Text counter = UIBuilderKit.CreateText(content, "StepCounter", string.Empty, new Vector2(0f, StepCounterY),
                    StepCounterSize, StepCounterFontSize, ui.textColor, TextAlignmentOptions.Center, bold: false);
                TMP_Text title = UIBuilderKit.CreateText(content, "StepTitle", string.Empty, new Vector2(0f, StepTitleY),
                    StepTitleSize, StepTitleFontSize, ui.textColor, TextAlignmentOptions.Center);

                // Solda fiş: hedef pasta + ödül.
                RectTransform ticket = UIBuilderKit.CreateCard(content, "Ticket", TicketSize, rounded, ui);
                ticket.parent.GetComponent<RectTransform>().anchoredPosition = TicketPosition;
                UIBuilderKit.CreateText(ticket, "Title", KeyTicket, new Vector2(0f, TicketTitleY), TicketTitleSize,
                    TicketTitleFontSize, ui.textColor, TextAlignmentOptions.Center);
                CakeView ticketCake = InstantiateCakeView(cakeViewPrefab, ticket, new Vector2(0f, TicketCakeY), TicketCakeSize);
                UIBuilderKit.CreateIcon(ticket, "GoldIcon", new Vector2(TicketGoldIconX, TicketRewardY), RewardIconSize, circle,
                    ui.goldIconColor);
                TMP_Text ticketReward = UIBuilderKit.CreateText(ticket, "Reward", null,
                    new Vector2(TicketRewardTextX, TicketRewardY), TicketRewardTextSize, TicketRewardFontSize, ui.textColor,
                    TextAlignmentOptions.MidlineLeft);
                ticketReward.text = "0";

                // Ortada oyuncunun pastası.
                CakeView buildCake = InstantiateCakeView(cakeViewPrefab, content, BuildCakePosition, BuildCakeSize);
                buildCake.name = "BuildCake";

                // Sağda süs adımı bilgisi.
                RectTransform decorationInfo = UIBuilderKit.CreateRect(content, "DecorationInfo",
                    new Vector2(DecorationInfoX, 0f), new Vector2(DecorationCountSize.x, 0f));
                TMP_Text decorationCount = UIBuilderKit.CreateText(decorationInfo, "Count", string.Empty,
                    new Vector2(0f, DecorationCountY), DecorationCountSize, DecorationCountFontSize, ui.textColor,
                    TextAlignmentOptions.Center);
                UIBuilderKit.CreateText(decorationInfo, "Hint", KeyDecorationHint, new Vector2(0f, DecorationHintY),
                    DecorationHintSize, DecorationHintFontSize, ui.textColor, TextAlignmentOptions.Center, bold: false);
                decorationInfo.gameObject.SetActive(false);

                // Altta seçenek şeridi; iki yanında Geri / İleri.
                RectTransform bar = UIBuilderKit.CreateRect(content, "OptionBar", OptionBarPosition, OptionBarSize);
                UIBuilderKit.AddSlicedImage(bar.gameObject, rounded, order.optionBarColor).raycastTarget = false;
                RectTransform optionList = UIBuilderKit.CreateStretched(bar, "Options");
                HorizontalLayoutGroup layout = optionList.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = OptionSpacing;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                Button back = UIBuilderKit.CreateButton(content, "BackButton", KeyBack, BackButtonPosition, StepButtonSize,
                    StepButtonFontSize, order.backButtonColor, rounded, ui);
                Button next = UIBuilderKit.CreateButton(content, "NextButton", KeyNext, NextButtonPosition, StepButtonSize,
                    StepButtonFontSize, order.nextButtonColor, rounded, ui);

                // Sürükleme katmanı: sonuçtan önce, her şeyin üstünde; tıklama almaz.
                RectTransform dragLayer = UIBuilderKit.CreateStretched(content, "DragLayer");
                Image ghost = UIBuilderKit.CreateIcon(dragLayer, "DragGhost", Vector2.zero, RowIconSize, circle, Color.white);
                ghost.preserveAspect = false;
                ghost.gameObject.SetActive(false);

                OrderResultView result = CreateResult(content, ui, order, rounded, circle, star);

                var so = new SerializedObject(builder);
                so.FindProperty("settings").objectReferenceValue = order;
                so.FindProperty("content").objectReferenceValue = content.gameObject;
                so.FindProperty("stepCounterText").objectReferenceValue = counter.GetComponent<LocalizedText>();
                so.FindProperty("stepTitleText").objectReferenceValue = title.GetComponent<LocalizedText>();
                so.FindProperty("ticketCake").objectReferenceValue = ticketCake;
                so.FindProperty("ticketRewardText").objectReferenceValue = ticketReward;
                so.FindProperty("buildCake").objectReferenceValue = buildCake;
                so.FindProperty("decorationInfo").objectReferenceValue = decorationInfo.gameObject;
                so.FindProperty("decorationCountText").objectReferenceValue = decorationCount.GetComponent<LocalizedText>();
                so.FindProperty("optionContainer").objectReferenceValue = optionList;
                so.FindProperty("optionPrefab").objectReferenceValue = optionPrefab;
                so.FindProperty("backButton").objectReferenceValue = back;
                so.FindProperty("nextButton").objectReferenceValue = next;
                so.FindProperty("nextButtonImage").objectReferenceValue = next.targetGraphic;
                so.FindProperty("nextLabel").objectReferenceValue = next.GetComponentInChildren<LocalizedText>(true);
                so.FindProperty("dragLayer").objectReferenceValue = dragLayer;
                so.FindProperty("dragGhost").objectReferenceValue = ghost;
                so.FindProperty("resultView").objectReferenceValue = result;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Kayıtta kapalı; CakeBuilder.Open açar.
                content.gameObject.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, CakeBuilderPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Perde + kart: başlık, benzerlik yüzdesi, yıldızlar, altın ve bahşiş satırları, "Panoya dön".
        // Kök (OrderResultView) hep açık; içerik kapalı kaydedilir.
        private static OrderResultView CreateResult(Transform content, RoundEndSettings ui, OrderUISettings order,
            Sprite rounded, Sprite circle, Sprite star)
        {
            RectTransform resultRoot = UIBuilderKit.CreateStretched(content, "Result");
            OrderResultView view = resultRoot.gameObject.AddComponent<OrderResultView>();

            RectTransform resultContent = UIBuilderKit.CreateStretched(resultRoot, "Content");
            UIBuilderKit.CreateDim(resultContent, ui);
            RectTransform card = UIBuilderKit.CreateCard(resultContent, "Card", ResultCardSize, rounded, ui);

            UIBuilderKit.CreateText(card, "Title", KeyResultTitle, new Vector2(0f, ResultTitleY), ResultTitleSize,
                ResultTitleFontSize, ui.textColor, TextAlignmentOptions.Center);
            UIBuilderKit.CreateText(card, "SimilarityLabel", KeySimilarity, new Vector2(0f, SimilarityLabelY),
                SimilarityLabelSize, SimilarityLabelFontSize, ui.textColor, TextAlignmentOptions.Center, bold: false);
            TMP_Text percent = UIBuilderKit.CreateText(card, "Percent", string.Empty, new Vector2(0f, PercentY), PercentSize,
                PercentFontSize, ui.textColor, TextAlignmentOptions.Center);

            RectTransform starsTf = UIBuilderKit.CreateRect(card, "Stars", new Vector2(0f, StarsY),
                new Vector2(StarSpacing * StarCount, StarSize));
            StarRow starRow = starsTf.gameObject.AddComponent<StarRow>();
            var stars = new Image[StarCount];
            for (int i = 0; i < StarCount; i++)
            {
                float x = (i - (StarCount - 1) * 0.5f) * StarSpacing;
                stars[i] = UIBuilderKit.CreateIcon(starsTf, $"Star_{i + 1}", new Vector2(x, 0f), StarSize, star, ui.starEmptyColor);
            }

            TMP_Text gold = CreateRewardRow(card, "GoldRow", KeyGold, GoldRowY, ui, circle);
            TMP_Text tip = CreateRewardRow(card, "TipRow", KeyTip, TipRowY, ui, circle);

            Button continueButton = UIBuilderKit.CreateButton(card, "ContinueButton", KeyContinue, ContinuePosition,
                ContinueSize, ContinueFontSize, ui.playAgainColor, rounded, ui);

            var starSo = new SerializedObject(starRow);
            SerializedProperty starArray = starSo.FindProperty("stars");
            starArray.arraySize = StarCount;
            for (int i = 0; i < StarCount; i++)
            {
                starArray.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            }

            starSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(view);
            so.FindProperty("settings").objectReferenceValue = order;
            so.FindProperty("roundEndSettings").objectReferenceValue = ui;
            so.FindProperty("content").objectReferenceValue = resultContent.gameObject;
            so.FindProperty("percentText").objectReferenceValue = percent.GetComponent<LocalizedText>();
            so.FindProperty("starRow").objectReferenceValue = starRow;
            so.FindProperty("goldText").objectReferenceValue = gold;
            so.FindProperty("tipText").objectReferenceValue = tip;
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            resultContent.gameObject.SetActive(false);
            return view;
        }

        // Altın simgesi + ad (yerelleştirilmiş) + "+miktar" (kod yazar).
        private static TMP_Text CreateRewardRow(Transform card, string name, string key, float y, RoundEndSettings ui,
            Sprite circle)
        {
            RectTransform row = UIBuilderKit.CreateRect(card, name, new Vector2(0f, y), new Vector2(ResultCardSize.x, RowLabelSize.y));
            UIBuilderKit.CreateIcon(row, "Icon", new Vector2(RowIconX, 0f), RowIconSize, circle, ui.goldIconColor);
            UIBuilderKit.CreateText(row, "Label", key, new Vector2(RowLabelX, 0f), RowLabelSize, RowLabelFontSize, ui.textColor,
                TextAlignmentOptions.MidlineLeft, bold: false);
            TMP_Text value = UIBuilderKit.CreateText(row, "Value", null, new Vector2(RowValueX, 0f), RowValueSize,
                RowValueFontSize, ui.textColor, TextAlignmentOptions.MidlineRight);
            value.text = "+0";
            return value;
        }

        // ------------------------------------------------------------------ Pano

        private static GameObject BuildBoard(RoundEndSettings ui, OrderUISettings order, Sprite rounded, OrderCard cardPrefab,
            GameObject builderPrefab)
        {
            var root = new GameObject("OrderBoard", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, BoardSortingOrder);
                OrderBoard board = root.AddComponent<OrderBoard>();

                // Sağ alan: yalnızca bu alan çizilir ve tıklama alır; Canvas'ın geri kalanı boştur (fırın ekranı ve üst şerit açık).
                RectTransform column = UIBuilderKit.CreateStretched(root.transform, "Board");
                column.anchorMin = AreaAnchorMin;
                column.anchorMax = AreaAnchorMax;
                column.offsetMin = column.offsetMax = Vector2.zero;
                UIBuilderKit.AddSlicedImage(column.gameObject, rounded, order.boardColor);

                TMP_Text title = UIBuilderKit.CreateText(column, "Title", KeyBoardTitle, Vector2.zero, Vector2.zero,
                    BoardTitleFontSize, ui.textColor, TextAlignmentOptions.Center);
                RectTransform titleTf = title.rectTransform;
                titleTf.anchorMin = new Vector2(0f, 1f);
                titleTf.anchorMax = new Vector2(1f, 1f);
                titleTf.pivot = new Vector2(0.5f, 1f);
                titleTf.sizeDelta = new Vector2(-2f * BoardPadding, BoardTitleHeight);
                titleTf.anchoredPosition = new Vector2(0f, -BoardTitleTop);

                RectTransform list = UIBuilderKit.CreateStretched(column, "Cards");
                list.offsetMin = new Vector2(0f, BoardPadding);
                list.offsetMax = new Vector2(0f, -CardListTop);
                VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = CardSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                // Yapım ekranı panonun içinde iç içe prefab (alt Canvas, sıralama 6); açılınca tüm ekranı kaplar.
                // Örnek doğrudan panonun altında oluşturulmalı: önce sahne kökünde oluşturulup sonra taşınırsa bir an kök Canvas olur,
                // Unity RectTransform'u kök Canvas gibi sürer ve PrepareNestedCanvas'ın değerleri kayda geçmez (0 ölçek / 0 boyut, görünmez).
                var builderInstance = (GameObject)PrefabUtility.InstantiatePrefab(builderPrefab, root.transform);
                UIBuilderKit.PrepareNestedCanvas(builderInstance, CakeBuilderSortingOrder);

                var so = new SerializedObject(board);
                so.FindProperty("cardContainer").objectReferenceValue = list;
                so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
                so.FindProperty("cakeBuilder").objectReferenceValue = builderInstance.GetComponent<CakeBuilder>();
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, BoardPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
