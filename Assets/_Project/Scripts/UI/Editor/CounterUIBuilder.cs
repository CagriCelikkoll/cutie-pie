using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G23 Tezgâh ekranını kurar / günceller ("Cutie Pie/UI/Tezgâh Oluştur").
    /// - Data/Tezgah/: CounterSettings, 7 malzeme, çikolatalı ve çilekli adımları, RecipeStepsLibrary (yoksa oluşturulur;
    ///   varsa dokunulmaz, elle yapılan ayarlar korunur).
    /// - Prefabs/UI/Kitchen/IngredientCard.prefab: tepsideki malzeme kartı.
    /// - Prefabs/UI/Kitchen/CounterPanel.prefab: kendi Canvas'ı olan ekran. Hiyerarşi:
    ///   Screen (tam ekran; panel açıp kapatır) > Background (krem zemin) + Content (1920x1080, DesignAreaFitter).
    ///   Content'te arkadan öne: üst bar → adım çubuğu → aşçı → tezgâh → konuşma balonu → malzeme tepsisi → kase
    ///   → ipucu çubuğu → sürükleme katmanı → bırakma onayı.
    /// Prefab'lar silinip yeniden yaratılmaz (UIBuilderKit.UpdatePrefabInPlace): fileID'ler korunur; değişiklik yoksa kaydedilmez.
    /// Var olan Image'ların sprite'ına ve rengine dokunulmaz; sonradan takılan sprite'lar (Chef/Head, Counter, Bowl/Visual/BowlBack,
    /// Bowl/Visual/BowlFront, Bowl/Spoon) kalır. Panelin içinde iç içe Canvas yoktur.
    /// Buradaki sayılar yalnızca yerleşimdir (1920x1080 tasarım alanı, merkez 0,0); renkler CounterSettings'ten gelir.
    /// </summary>
    public static class CounterUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Tezgâh Oluştur";
        private const string CardPrefabPath = UIBuilderKit.KitchenPrefabFolder + "/IngredientCard.prefab";
        private const string PanelPrefabPath = UIBuilderKit.KitchenPrefabFolder + "/CounterPanel.prefab";

        private const string DataFolder = "Assets/_Project/Data/Tezgah";
        private const string IngredientFolder = DataFolder + "/Ingredients";
        private const string SettingsPath = DataFolder + "/CounterSettings.asset";
        private const string LibraryPath = DataFolder + "/RecipeStepsLibrary.asset";

        // Fırın ekranının ve Pasta Dolabı'nın yerine açılacağı için aynı sıralama.
        public const int CanvasSortingOrder = OvenPanelUIBuilder.CanvasSortingOrder;

        private static readonly Vector2 DesignSize = new Vector2(1920f, 1080f);

        private static readonly string[] ContentChildren =
        {
            "TopBar", "StepTracker", "Chef", "Counter", "Bubble", "Tray", "Bowl", "HintBar", "DragLayer", "LeaveDialog"
        };

        // ---- Üst bar (440 … 524)
        private static readonly Vector2 TopBarPosition = new Vector2(0f, 482f);
        private static readonly Vector2 TopBarSize = new Vector2(1860f, 84f);
        private const float BackButtonX = -880f;
        private const float BackButtonSize = 72f;
        private const float BackGlyphSize = 30f;
        private const float TitleIconX = -250f;
        private const float TitleIconSize = 60f;
        private static readonly Vector2 TitlePosition = new Vector2(30f, 0f);
        private static readonly Vector2 TitleSize = new Vector2(480f, 64f);
        private const float TitleFontSize = 44f;
        private static readonly Vector2 GoldRowPosition = new Vector2(800f, 0f);
        private static readonly Vector2 GoldRowSize = new Vector2(200f, 64f);
        private const float GoldIconX = -60f;
        private const float GoldIconSize = 46f;
        private static readonly Vector2 GoldValuePosition = new Vector2(30f, 0f);
        private static readonly Vector2 GoldValueSize = new Vector2(120f, 60f);
        private const float GoldFontSize = 36f;

        // ---- Adım çubuğu (307 … 437)
        private static readonly Vector2 TrackerPosition = new Vector2(0f, 372f);
        private static readonly Vector2 TrackerSize = new Vector2(1400f, 130f);
        private const float NodeSpacing = 280f;
        private static readonly Vector2 NodeSize = new Vector2(240f, 130f);
        private const float NodeCircleY = 30f;
        private const float NodeCircleSize = 60f;
        private const float NodeNumberFontSize = 32f;
        private const float NodeCheckSize = 32f;
        private const float NodeLabelY = -18f;
        private static readonly Vector2 NodeLabelSize = new Vector2(240f, 34f);
        private const float NodeLabelFontSize = 26f;
        private const float NodeSubY = -48f;
        private static readonly Vector2 NodeSubSize = new Vector2(240f, 28f);
        private const float NodeSubFontSize = 22f;
        private const float ConnectorGap = 12f;
        private const float ConnectorHeight = 8f;

        private static readonly string[] StepKeys =
        {
            "kitchen.counter.step.ingredients",
            "kitchen.counter.step.mix",
            "kitchen.counter.step.bake",
            "kitchen.counter.step.decorate",
            "kitchen.counter.step.slice"
        };

        // ---- Aşçı: solda, tezgâhın arkasında (gövdesi tezgâhın altında kalır).
        private static readonly Vector2 ChefPosition = new Vector2(-760f, 120f);
        private static readonly Vector2 ChefSize = new Vector2(240f, 320f);
        private static readonly Vector2 ChefBodyPosition = new Vector2(0f, -90f);
        private static readonly Vector2 ChefBodySize = new Vector2(230f, 200f);
        private static readonly Vector2 ChefHeadPosition = new Vector2(0f, 40f);
        private const float ChefHeadSize = 170f;
        private static readonly Vector2 ChefHatPosition = new Vector2(0f, 140f);
        private static readonly Vector2 ChefHatSize = new Vector2(160f, 100f);
        private const float EyeOffsetX = 32f;
        private const float EyeY = 52f;
        private const float EyeSize = 18f;
        private const float CheekOffsetX = 52f;
        private const float CheekY = 18f;
        private const float CheekSize = 26f;

        // ---- Tezgâh yüzeyi (-450 … 20) ve üst kenarı.
        private static readonly Vector2 CounterPosition = new Vector2(0f, -215f);
        private static readonly Vector2 CounterSize = new Vector2(1920f, 470f);
        private const float CounterEdgeHeight = 24f;

        // ---- Konuşma balonu: aşçının sağ üstünde.
        private static readonly Vector2 BubblePosition = new Vector2(-440f, 235f);
        private static readonly Vector2 BubbleSize = new Vector2(400f, 120f);
        private const float BubbleBorder = 5f;
        private const float BubbleFontSize = 32f;
        private static readonly Vector2 TailLargePosition = new Vector2(-215f, -45f);
        private const float TailLargeSize = 28f;
        private static readonly Vector2 TailSmallPosition = new Vector2(-242f, -68f);
        private const float TailSmallSize = 18f;
        private const float TailBorder = 4f;

        // ---- Malzeme tepsisi (-410 … 110): başlık + 3 sütunlu ızgara.
        private static readonly Vector2 TrayPosition = new Vector2(-100f, -150f);
        private static readonly Vector2 TraySize = new Vector2(770f, 520f);
        private const float TrayBorder = 6f;
        private const float TrayTitleY = 225f;
        private static readonly Vector2 TrayTitleSize = new Vector2(700f, 50f);
        private const float TrayTitleFontSize = 32f;
        private static readonly Vector2 TrayGridPosition = new Vector2(0f, -28f);
        private static readonly Vector2 TrayGridSize = new Vector2(730f, 440f);
        private const int TrayColumns = 3;
        private static readonly Vector2 TrayGridSpacing = new Vector2(20f, 20f);

        // ---- Malzeme kartı (kart merkezine göre).
        private static readonly Vector2 CardSize = new Vector2(230f, 200f);
        private const float CardIconY = 30f;
        private const float CardIconSize = 100f;
        private const float CardNameY = -44f;
        private static readonly Vector2 CardNameSize = new Vector2(210f, 36f);
        private const float CardNameFontSize = 28f;
        private const float CardAmountY = -76f;
        private static readonly Vector2 CardAmountSize = new Vector2(210f, 30f);
        private const float CardAmountFontSize = 24f;
        private static readonly Vector2 CardCheckPosition = new Vector2(88f, 72f);
        private const float CardCheckSize = 46f;
        private const float CardCheckGlyphSize = 30f;
        private const float GlowOutset = 6f;
        private const float GlowBorderMultiplier = 2f;

        // ---- Kase (-350 … 90 halkayla).
        private static readonly Vector2 BowlPosition = new Vector2(560f, -130f);
        private static readonly Vector2 BowlSize = new Vector2(420f, 420f);
        private const float RingSize = 440f;
        private const float HoverSize = 400f;
        private const float BowlBackSize = 360f;
        private static readonly Vector2 BatterPosition = new Vector2(0f, 10f);
        private const float BatterSize = 300f;
        private const float SlotIconSize = 56f;
        private static readonly Vector2 BowlFrontPosition = new Vector2(0f, -140f);
        private static readonly Vector2 BowlFrontSize = new Vector2(380f, 80f);
        private const float SpoonSize = 150f;
        private const float BowlLabelY = 250f;
        private static readonly Vector2 BowlLabelSize = new Vector2(420f, 50f);
        private const float BowlLabelFontSize = 34f;
        private const float ReadyBannerY = 310f;
        private static readonly Vector2 ReadyBannerSize = new Vector2(360f, 70f);
        private const float ReadyBannerBorder = 5f;
        private const float ReadyFontSize = 38f;

        // ---- İpucu çubuğu (-514 … -430): adım, ne yapılacağı, sıradaki adım, ilerleme.
        private static readonly Vector2 HintBarPosition = new Vector2(0f, -472f);
        private static readonly Vector2 HintBarSize = new Vector2(1820f, 84f);
        private const float HintBarBorder = 4f;
        private static readonly Vector2 HintStepPosition = new Vector2(-800f, 0f);
        private static readonly Vector2 HintStepSize = new Vector2(160f, 60f);
        private const float HintStepFontSize = 32f;
        private static readonly Vector2 HintDoPosition = new Vector2(-340f, 0f);
        private static readonly Vector2 HintDoSize = new Vector2(700f, 60f);
        private const float HintDoFontSize = 30f;
        private static readonly Vector2 HintNextPosition = new Vector2(290f, 0f);
        private static readonly Vector2 HintNextSize = new Vector2(420f, 60f);
        private const float HintNextFontSize = 28f;
        private static readonly Vector2 HintProgressPosition = new Vector2(690f, 0f);
        private static readonly Vector2 HintProgressSize = new Vector2(340f, 22f);
        private const float ProgressCornerMultiplier = 3f;

        // ---- Bırakma onayı
        private const float DimOutset = -1200f;
        private static readonly Vector2 DialogSize = new Vector2(720f, 380f);
        private const float DialogBorder = 8f;
        private static readonly Vector2 QuestionPosition = new Vector2(0f, 70f);
        private static readonly Vector2 QuestionSize = new Vector2(640f, 140f);
        private const float QuestionFontSize = 42f;
        private static readonly Vector2 YesPosition = new Vector2(-150f, -110f);
        private static readonly Vector2 NoPosition = new Vector2(150f, -110f);
        private static readonly Vector2 DialogButtonSize = new Vector2(260f, 96f);
        private const float DialogButtonBorder = 6f;
        private const float DialogButtonFontSize = 40f;

        private const string KeyTrayTitle = "kitchen.counter.tray_title";
        private const string KeyBowl = "kitchen.counter.bowl";
        private const string KeyBatterReady = "kitchen.counter.batter_ready";
        private const string KeyLeaveQuestion = "kitchen.counter.leave.question";
        private const string KeyLeaveYes = "kitchen.counter.leave.yes";
        private const string KeyLeaveNo = "kitchen.counter.leave.no";

        [MenuItem(MenuPath)]
        private static void BuildFromMenu()
        {
            BuildPrefabs();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Veri asset'lerini (yoksa) ve iki prefab'ı yerinde günceller; panel prefab asset'ini döndürür.</summary>
        public static GameObject BuildPrefabs()
        {
            CounterSettings settings = GetOrCreateSettings();
            RecipeStepsLibrary library = EnsureDataAssets();

            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();
            Sprite circle = UIBuilderKit.GetOrCreateCircleSprite();
            Sprite arrow = UIBuilderKit.GetOrCreateArrowSprite();
            Sprite check = UIBuilderShapes.GetOrCreateCheckSprite();
            Sprite ring = UIBuilderShapes.GetOrCreateRingSprite();
            Sprite spoon = UIBuilderShapes.GetOrCreateSpoonSprite();
            UIBuilderKit.EnsureFolder(UIBuilderKit.KitchenPrefabFolder);

            GameObject cardPrefab = UIBuilderKit.UpdatePrefabInPlace(CardPrefabPath, "IngredientCard",
                root => EnsureCard(root, settings, rounded, circle, check));
            IngredientCard card = cardPrefab.GetComponent<IngredientCard>();

            var sprites = new Sprites(rounded, circle, arrow, check, ring, spoon);
            return UIBuilderKit.UpdatePrefabInPlace(PanelPrefabPath, "CounterPanel",
                root => EnsurePanel(root, settings, library, sprites, card));
        }

        public static CounterSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CounterSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            UIBuilderKit.EnsureFolder(DataFolder);
            settings = ScriptableObject.CreateInstance<CounterSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private readonly struct Sprites
        {
            public readonly Sprite Rounded;
            public readonly Sprite Circle;
            public readonly Sprite Arrow;
            public readonly Sprite Check;
            public readonly Sprite Ring;
            public readonly Sprite Spoon;

            public Sprites(Sprite rounded, Sprite circle, Sprite arrow, Sprite check, Sprite ring, Sprite spoon)
            {
                Rounded = rounded;
                Circle = circle;
                Arrow = arrow;
                Check = check;
                Ring = ring;
                Spoon = spoon;
            }
        }

        // ------------------------------------------------------------------ Veri

        // Malzeme id'si ve kasedeki renk (yer tutucu ikonun rengi de budur).
        private static readonly (string id, Color tint)[] IngredientDefaults =
        {
            ("flour", Hex(0xF3, 0xEA, 0xDB)),
            ("eggs", Hex(0xFF, 0xD8, 0x6B)),
            ("sugar", Hex(0xF4, 0xF0, 0xFA)),
            ("cocoa", Hex(0x7A, 0x4A, 0x33)),
            ("milk", Hex(0xE8, 0xF3, 0xFB)),
            ("butter", Hex(0xFF, 0xE5, 0x9A)),
            ("strawberry", Hex(0xF0, 0x6A, 0x86))
        };

        private static RecipeStepsLibrary EnsureDataAssets()
        {
            UIBuilderKit.EnsureFolder(IngredientFolder);

            var ingredients = new Dictionary<string, IngredientData>();
            foreach ((string id, Color tint) in IngredientDefaults)
            {
                string path = $"{IngredientFolder}/Ingredient_{id}.asset";
                var data = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<IngredientData>();
                    data.id = id;
                    data.nameKey = $"kitchen.counter.ingredient.{id}.name";
                    data.hintKey = $"kitchen.counter.ingredient.{id}.hint";
                    data.bowlTint = tint;
                    AssetDatabase.CreateAsset(data, path);
                    Debug.Log($"[CounterUIBuilder] '{path}' oluşturuldu.");
                }

                ingredients[id] = data;
            }

            RecipeStepsData chocolate = EnsureSteps("cake_chocolate", ingredients,
                new[] { ("flour", "cups_2"), ("eggs", "pieces_3"), ("sugar", "cups_1"), ("cocoa", "spoons_3"), ("milk", "cups_1"), ("butter", "grams_100") },
                Hex(0xEE, 0xDD, 0xC8), Hex(0x7B, 0x4B, 0x32));
            RecipeStepsData strawberry = EnsureSteps("cake_strawberry", ingredients,
                new[] { ("flour", "cups_2"), ("eggs", "pieces_3"), ("sugar", "cups_1"), ("strawberry", "handful_1"), ("milk", "cups_1"), ("butter", "grams_100") },
                Hex(0xF7, 0xE6, 0xDA), Hex(0xF2, 0x8B, 0xA4));

            var library = AssetDatabase.LoadAssetAtPath<RecipeStepsLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<RecipeStepsLibrary>();
                library.entries = new[] { chocolate, strawberry };
                AssetDatabase.CreateAsset(library, LibraryPath);
                Debug.Log($"[CounterUIBuilder] '{LibraryPath}' oluşturuldu.");
            }

            AssetDatabase.SaveAssets();
            return library;
        }

        private static RecipeStepsData EnsureSteps(string recipeId, Dictionary<string, IngredientData> ingredients,
            (string ingredient, string amount)[] list, Color start, Color ready)
        {
            string path = $"{DataFolder}/Steps_{recipeId}.asset";
            var steps = AssetDatabase.LoadAssetAtPath<RecipeStepsData>(path);
            if (steps != null)
            {
                return steps;
            }

            steps = ScriptableObject.CreateInstance<RecipeStepsData>();
            steps.recipeId = recipeId;
            steps.mixTurns = 3;
            steps.batterStartColor = start;
            steps.batterReadyColor = ready;
            steps.ingredients = new IngredientEntry[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                steps.ingredients[i] = new IngredientEntry
                {
                    ingredient = ingredients[list[i].ingredient],
                    amountKey = $"kitchen.counter.amount.{list[i].amount}"
                };
            }

            AssetDatabase.CreateAsset(steps, path);
            Debug.Log($"[CounterUIBuilder] '{path}' oluşturuldu.");
            return steps;
        }

        // ------------------------------------------------------------------ Malzeme kartı

        private static bool EnsureCard(GameObject root, CounterSettings s, Sprite rounded, Sprite circle, Sprite check)
        {
            bool changed = false;
            var tf = (RectTransform)root.transform;
            UIBuilderKit.SetIfDifferent(ref changed, tf.sizeDelta, CardSize, v => tf.sizeDelta = v);

            // Kök: görünmez tıklama/sürükleme alanı (gövde yükselse de kartın yeri sabit kalır).
            if (root.GetComponent<Image>() == null)
            {
                Image hit = root.AddComponent<Image>();
                hit.color = Color.clear;
                hit.raycastTarget = true;
                changed = true;
            }

            IngredientCard card = UIBuilderKit.EnsureComponent<IngredientCard>(root, ref changed);

            Image background = UIBuilderShapes.EnsureImage(tf, "Body", Vector2.zero, CardSize, rounded, s.cardColor, true, true,
                ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, background.raycastTarget, true, v => background.raycastTarget = v);
            RectTransform body = background.rectTransform;

            Image icon = UIBuilderShapes.EnsureImage(body, "Icon", new Vector2(0f, CardIconY), Vector2.one * CardIconSize, rounded,
                s.grayColor, true, true, ref changed);
            TMP_Text nameText = UIBuilderKit.EnsureText(body, "Name", string.Empty, new Vector2(0f, CardNameY), CardNameSize,
                CardNameFontSize, s.textColor, TextAlignmentOptions.Center, ref changed);
            TMP_Text amount = UIBuilderKit.EnsureText(body, "Amount", string.Empty, new Vector2(0f, CardAmountY), CardAmountSize,
                CardAmountFontSize, s.textColor, TextAlignmentOptions.Center, ref changed, bold: false);
            GameObject badge = EnsureCheckBadge(body, CardCheckPosition, CardCheckSize, CardCheckGlyphSize, circle, check, s.greenColor,
                ref changed);
            Image glow = EnsureFrame(body, "Glow", rounded, s.goldColor, ref changed);

            UIBuilderShapes.EnsureSiblingOrder(body, ref changed, "Icon", "Name", "Amount", "CheckBadge", "Glow");

            var so = new SerializedObject(card);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderKit.EnsureReference(so, "body", body, ref changed);
            UIBuilderKit.EnsureReference(so, "background", background, ref changed);
            UIBuilderKit.EnsureReference(so, "icon", icon, ref changed);
            UIBuilderKit.EnsureReference(so, "nameText", nameText.GetComponent<LocalizedText>(), ref changed);
            UIBuilderKit.EnsureReference(so, "amountText", amount.GetComponent<LocalizedText>(), ref changed);
            UIBuilderKit.EnsureReference(so, "checkBadge", badge, ref changed);
            UIBuilderKit.EnsureReference(so, "glow", glow, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        // Dışa taşan, içi boş çerçeve. Nesne açık, bileşen kapalı kaydedilir; çalışma zamanında açılır.
        private static Image EnsureFrame(RectTransform parent, string name, Sprite rounded, Color color, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateStretched(parent, name);
                Image image = UIBuilderKit.AddSlicedImage(created.gameObject, rounded, color);
                image.fillCenter = false;
                image.pixelsPerUnitMultiplier = GlowBorderMultiplier;
                image.raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureStretchedRect(tf, -GlowOutset, ref changed);
            UIBuilderKit.EnsureActive(tf.gameObject, true, ref changed);

            Image frame = tf.GetComponent<Image>();
            UIBuilderKit.SetIfDifferent(ref changed, frame.enabled, false, v => frame.enabled = v);
            return frame;
        }

        // Yeşil daire + beyaz ✓; kapalı kaydedilir.
        private static GameObject EnsureCheckBadge(RectTransform parent, Vector2 position, float size, float glyphSize, Sprite circle,
            Sprite check, Color color, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(parent, "CheckBadge", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, "CheckBadge", position, Vector2.one * size);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = circle;
                image.color = color;
                image.preserveAspect = true;
                image.raycastTarget = false;
                UIBuilderKit.CreateIcon(created, "Glyph", Vector2.zero, glyphSize, check, Color.white);
                created.gameObject.SetActive(false);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, Vector2.one * size, ref changed);

            Transform glyph = UIBuilderKit.FindChild(tf, "Glyph");
            if (glyph != null)
            {
                UIBuilderKit.EnsureRect((RectTransform)glyph, Vector2.zero, Vector2.one * glyphSize, ref changed);
            }

            return tf.gameObject;
        }

        // ------------------------------------------------------------------ Panel

        private static bool EnsurePanel(GameObject root, CounterSettings s, RecipeStepsLibrary library, Sprites sp,
            IngredientCard cardPrefab)
        {
            bool changed = false;
            if (root.GetComponent<Canvas>() == null)
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                changed = true;
            }

            CounterPanel panel = UIBuilderKit.EnsureComponent<CounterPanel>(root, ref changed);

            RectTransform screen = UIBuilderKit.EnsureStretchedChild(root.transform, "Screen", ref changed);
            UIBuilderShapes.EnsureStretchedImage(screen, "Background", 0f, null, s.backgroundColor, false, true, ref changed);
            RectTransform content = EnsureDesignArea(screen, ref changed);
            UIBuilderShapes.EnsureSiblingOrder(screen, ref changed, "Background", "Content");

            (Button back, LocalizedText title, Image titleIcon) = EnsureTopBar(content, s, sp, ref changed);
            StepTracker tracker = EnsureStepTracker(content, s, sp, ref changed);
            EnsureChef(content, s, sp, ref changed);
            EnsureCounter(content, s, ref changed);
            LocalizedText bubbleText = EnsureBubble(content, s, sp, ref changed);
            RectTransform tray = EnsureTray(content, s, sp, ref changed);
            (MixingBowl bowl, GameObject readyBanner) = EnsureBowl(content, s, sp, ref changed);
            HintBarRefs hint = EnsureHintBar(content, s, sp, ref changed);
            (RectTransform dragLayer, Image ghost) = EnsureDragLayer(content, sp, ref changed);
            (GameObject dialog, Button yes, Button no) = EnsureLeaveDialog(content, s, sp, ref changed);

            UIBuilderShapes.EnsureSiblingOrder(content, ref changed, ContentChildren);

            var so = new SerializedObject(panel);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderKit.EnsureReference(so, "library", library, ref changed);
            UIBuilderKit.EnsureReference(so, "screen", screen.gameObject, ref changed);
            UIBuilderKit.EnsureReference(so, "backButton", back, ref changed);
            UIBuilderKit.EnsureReference(so, "titleText", title, ref changed);
            UIBuilderKit.EnsureReference(so, "titleIcon", titleIcon, ref changed);
            UIBuilderKit.EnsureReference(so, "stepTracker", tracker, ref changed);
            UIBuilderKit.EnsureReference(so, "chefBubbleText", bubbleText, ref changed);
            UIBuilderKit.EnsureReference(so, "trayContainer", tray, ref changed);
            UIBuilderKit.EnsureReference(so, "cardPrefab", cardPrefab, ref changed);
            UIBuilderKit.EnsureReference(so, "bowl", bowl, ref changed);
            UIBuilderKit.EnsureReference(so, "readyBanner", readyBanner, ref changed);
            UIBuilderKit.EnsureReference(so, "dragLayer", dragLayer, ref changed);
            UIBuilderKit.EnsureReference(so, "dragGhost", ghost, ref changed);
            UIBuilderKit.EnsureReference(so, "hintStepText", hint.Step, ref changed);
            UIBuilderKit.EnsureReference(so, "hintDoText", hint.Do, ref changed);
            UIBuilderKit.EnsureReference(so, "hintNextText", hint.Next, ref changed);
            UIBuilderKit.EnsureReference(so, "hintProgressFill", hint.Fill, ref changed);
            UIBuilderKit.EnsureReference(so, "leaveDialog", dialog, ref changed);
            UIBuilderKit.EnsureReference(so, "leaveYesButton", yes, ref changed);
            UIBuilderKit.EnsureReference(so, "leaveNoButton", no, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Editörde görünsün diye açık kaydedilir; panel Awake'te kapatır, Begin açar.
            UIBuilderKit.EnsureActive(screen.gameObject, true, ref changed);
            UIBuilderKit.EnsureActive(content.gameObject, true, ref changed);
            return changed;
        }

        private static RectTransform EnsureDesignArea(RectTransform screen, ref bool changed)
        {
            RectTransform content = UIBuilderKit.EnsureRectChild(screen, "Content", Vector2.zero, DesignSize, ref changed);
            DesignAreaFitter fitter = UIBuilderKit.EnsureComponent<DesignAreaFitter>(content.gameObject, ref changed);
            var so = new SerializedObject(fitter);
            UIBuilderShapes.EnsureVector2(so, "designSize", DesignSize, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();
            return content;
        }

        // Geri butonu (yuvarlak, sola ok), ortada ikon + pastanın adı, sağda altın.
        private static (Button, LocalizedText, Image) EnsureTopBar(RectTransform content, CounterSettings s, Sprites sp,
            ref bool changed)
        {
            RectTransform bar = UIBuilderShapes.EnsureImage(content, "TopBar", TopBarPosition, TopBarSize, sp.Rounded, s.barColor,
                true, true, ref changed).rectTransform;

            var backPosition = new Vector2(BackButtonX, 0f);
            var backSize = Vector2.one * BackButtonSize;
            RectTransform backTf = UIBuilderKit.EnsureChild(bar, "BackButton", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(bar, "BackButton", backPosition, backSize);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = sp.Circle;
                image.color = s.pinkColor;
                image.preserveAspect = true;
                UIBuilderKit.AddButton(created.gameObject, image);
                UIBuilderKit.CreateIcon(created, "Glyph", Vector2.zero, BackGlyphSize, sp.Arrow, s.buttonTextColor);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(backTf, backPosition, backSize, ref changed);
            Transform glyph = UIBuilderKit.FindChild(backTf, "Glyph");
            if (glyph != null)
            {
                UIBuilderKit.EnsureRect((RectTransform)glyph, Vector2.zero, Vector2.one * BackGlyphSize, ref changed);
                UIBuilderKit.EnsureScale(glyph, new Vector3(-1f, 1f, 1f), ref changed);
            }

            Image titleIcon = UIBuilderShapes.EnsureImage(bar, "TitleIcon", new Vector2(TitleIconX, 0f), Vector2.one * TitleIconSize,
                sp.Rounded, s.pinkColor, true, true, ref changed);
            TMP_Text title = UIBuilderKit.EnsureText(bar, "Title", string.Empty, TitlePosition, TitleSize, TitleFontSize, s.textColor,
                TextAlignmentOptions.MidlineLeft, ref changed);

            // Altın: sayıyı Core'daki CurrencyDisplay yazar ve CurrencyChanged ile günceller.
            RectTransform goldRow = UIBuilderKit.EnsureRectChild(bar, "GoldRow", GoldRowPosition, GoldRowSize, ref changed);
            UIBuilderKit.EnsureIcon(goldRow, "GoldIcon", new Vector2(GoldIconX, 0f), GoldIconSize, sp.Circle, s.goldColor, ref changed);
            TMP_Text goldValue = UIBuilderKit.EnsureText(goldRow, "Value", null, GoldValuePosition, GoldValueSize, GoldFontSize,
                s.textColor, TextAlignmentOptions.MidlineLeft, ref changed);
            if (string.IsNullOrEmpty(goldValue.text))
            {
                goldValue.text = "0";
                changed = true;
            }

            CurrencyDisplay display = goldRow.GetComponent<CurrencyDisplay>();
            if (display == null)
            {
                display = goldRow.gameObject.AddComponent<CurrencyDisplay>();
                var currency = new SerializedObject(display);
                currency.FindProperty("currency").intValue = (int)CurrencyType.Gold;
                currency.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            var so = new SerializedObject(display);
            UIBuilderKit.EnsureReference(so, "amountLabel", goldValue, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            return (backTf.GetComponent<Button>(), title.GetComponent<LocalizedText>(), titleIcon);
        }

        // 5 adım + aralarındaki çizgiler; çizgiler dairelerin arkasında.
        private static StepTracker EnsureStepTracker(RectTransform content, CounterSettings s, Sprites sp, ref bool changed)
        {
            RectTransform trackerTf = UIBuilderKit.EnsureRectChild(content, "StepTracker", TrackerPosition, TrackerSize, ref changed);
            StepTracker tracker = UIBuilderKit.EnsureComponent<StepTracker>(trackerTf.gameObject, ref changed);

            int count = StepKeys.Length;
            var connectors = new Image[count - 1];
            var names = new List<string>();
            for (int i = 0; i < count - 1; i++)
            {
                string name = $"Connector_{i + 1}";
                var position = new Vector2((i - (count - 2) * 0.5f) * NodeSpacing, NodeCircleY);
                var size = new Vector2(NodeSpacing - NodeCircleSize - ConnectorGap * 2f, ConnectorHeight);
                connectors[i] = UIBuilderShapes.EnsureImage(trackerTf, name, position, size, null, s.grayColor, false, true, ref changed);
                names.Add(name);
            }

            var nodes = new StepNode[count];
            for (int i = 0; i < count; i++)
            {
                string name = $"Step_{i + 1}";
                nodes[i] = EnsureStepNode(trackerTf, name, i, new Vector2((i - (count - 1) * 0.5f) * NodeSpacing, 0f), s, sp,
                    ref changed);
                names.Add(name);
            }

            UIBuilderShapes.EnsureSiblingOrder(trackerTf, ref changed, names.ToArray());

            var so = new SerializedObject(tracker);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderShapes.EnsureReferenceArray(so, "nodes", nodes, ref changed);
            UIBuilderShapes.EnsureReferenceArray(so, "connectors", connectors, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();
            return tracker;
        }

        private static StepNode EnsureStepNode(RectTransform parent, string name, int index, Vector2 position, CounterSettings s,
            Sprites sp, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureRectChild(parent, name, position, NodeSize, ref changed);
            StepNode node = UIBuilderKit.EnsureComponent<StepNode>(tf.gameObject, ref changed);

            var circlePosition = new Vector2(0f, NodeCircleY);
            Image circle = UIBuilderShapes.EnsureImage(tf, "Circle", circlePosition, Vector2.one * NodeCircleSize, sp.Circle,
                s.grayColor, false, true, ref changed);
            TMP_Text number = UIBuilderKit.EnsureText(circle.rectTransform, "Number", null, Vector2.zero,
                Vector2.one * NodeCircleSize, NodeNumberFontSize, s.mutedTextColor, TextAlignmentOptions.Center, ref changed);
            string digit = (index + 1).ToString();
            if (number.text != digit)
            {
                number.text = digit;
                changed = true;
            }

            Image check = UIBuilderShapes.EnsureImage(circle.rectTransform, "Check", Vector2.zero, Vector2.one * NodeCheckSize,
                sp.Check, Color.white, false, false, ref changed);
            TMP_Text label = UIBuilderKit.EnsureText(tf, "Label", StepKeys[index], new Vector2(0f, NodeLabelY), NodeLabelSize,
                NodeLabelFontSize, s.mutedTextColor, TextAlignmentOptions.Center, ref changed);
            TMP_Text sub = UIBuilderKit.EnsureText(tf, "Sub", string.Empty, new Vector2(0f, NodeSubY), NodeSubSize, NodeSubFontSize,
                s.pinkBorderColor, TextAlignmentOptions.Center, ref changed, bold: false);

            var so = new SerializedObject(node);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderKit.EnsureReference(so, "circle", circle, ref changed);
            UIBuilderKit.EnsureReference(so, "numberText", number, ref changed);
            UIBuilderKit.EnsureReference(so, "check", check.gameObject, ref changed);
            UIBuilderKit.EnsureReference(so, "label", label, ref changed);
            UIBuilderKit.EnsureReference(so, "subText", sub.GetComponent<LocalizedText>(), ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();
            return node;
        }

        // Yer tutucu aşçı: gövde (önlük), kafa, şapka, gözler, yanaklar. Gerçek sprite Chef/Head'e takılıp diğerleri kapatılabilir.
        private static void EnsureChef(RectTransform content, CounterSettings s, Sprites sp, ref bool changed)
        {
            RectTransform chef = UIBuilderKit.EnsureRectChild(content, "Chef", ChefPosition, ChefSize, ref changed);
            UIBuilderShapes.EnsureImage(chef, "Body", ChefBodyPosition, ChefBodySize, sp.Rounded, s.chefHatColor, true, true, ref changed);
            UIBuilderShapes.EnsureImage(chef, "Head", ChefHeadPosition, Vector2.one * ChefHeadSize, sp.Circle, s.chefSkinColor, false,
                true, ref changed);
            UIBuilderShapes.EnsureImage(chef, "Hat", ChefHatPosition, ChefHatSize, sp.Rounded, s.chefHatColor, true, true, ref changed);
            UIBuilderShapes.EnsureImage(chef, "EyeLeft", new Vector2(-EyeOffsetX, EyeY), Vector2.one * EyeSize, sp.Circle, s.textColor,
                false, true, ref changed);
            UIBuilderShapes.EnsureImage(chef, "EyeRight", new Vector2(EyeOffsetX, EyeY), Vector2.one * EyeSize, sp.Circle, s.textColor,
                false, true, ref changed);
            UIBuilderShapes.EnsureImage(chef, "CheekLeft", new Vector2(-CheekOffsetX, CheekY), Vector2.one * CheekSize, sp.Circle,
                s.chefCheekColor, false, true, ref changed);
            UIBuilderShapes.EnsureImage(chef, "CheekRight", new Vector2(CheekOffsetX, CheekY), Vector2.one * CheekSize, sp.Circle,
                s.chefCheekColor, false, true, ref changed);
            UIBuilderShapes.EnsureSiblingOrder(chef, ref changed, "Body", "Head", "Hat", "EyeLeft", "EyeRight", "CheekLeft", "CheekRight");
        }

        // Tezgâh yüzeyi (sprite takılabilir) + üst kenar şeridi.
        private static void EnsureCounter(RectTransform content, CounterSettings s, ref bool changed)
        {
            RectTransform counter = UIBuilderShapes.EnsureImage(content, "Counter", CounterPosition, CounterSize, null, s.counterColor,
                false, true, ref changed).rectTransform;
            UIBuilderShapes.EnsureImage(counter, "Edge", new Vector2(0f, (CounterSize.y - CounterEdgeHeight) * 0.5f),
                new Vector2(CounterSize.x, CounterEdgeHeight), null, s.counterEdgeColor, false, true, ref changed);
        }

        private static LocalizedText EnsureBubble(RectTransform content, CounterSettings s, Sprites sp, ref bool changed)
        {
            RectTransform inner = UIBuilderShapes.EnsureBorderedBox(content, "Bubble", BubblePosition, BubbleSize, BubbleBorder,
                sp.Rounded, s.pinkBorderColor, s.bubbleColor, ref changed);
            var bubble = (RectTransform)inner.parent;
            EnsureDot(bubble, "TailLarge", TailLargePosition, TailLargeSize, sp.Circle, s, ref changed);
            EnsureDot(bubble, "TailSmall", TailSmallPosition, TailSmallSize, sp.Circle, s, ref changed);

            Vector2 textSize = BubbleSize - Vector2.one * (BubbleBorder * 2f) - new Vector2(30f, 12f);
            TMP_Text text = UIBuilderKit.EnsureText(inner, "Text", string.Empty, Vector2.zero, textSize, BubbleFontSize, s.textColor,
                TextAlignmentOptions.Center, ref changed);
            return text.GetComponent<LocalizedText>();
        }

        // Kenarlıklı küçük daire (konuşma balonunun kuyruğu). Daire sprite'ı 9-slice'sızdır; basit çizilir.
        private static void EnsureDot(RectTransform parent, string name, Vector2 position, float size, Sprite circle, CounterSettings s,
            ref bool changed)
        {
            Image outer = UIBuilderShapes.EnsureImage(parent, name, position, Vector2.one * size, circle, s.pinkBorderColor, false, true,
                ref changed);
            UIBuilderShapes.EnsureImage(outer.rectTransform, "Inner", Vector2.zero, Vector2.one * (size - TailBorder * 2f), circle,
                s.bubbleColor, false, true, ref changed);
        }

        // "BU TARİFİN MALZEMELERİ" başlıklı tepsi; kartlar 3 sütunlu ızgaraya eklenir.
        private static RectTransform EnsureTray(RectTransform content, CounterSettings s, Sprites sp, ref bool changed)
        {
            RectTransform inner = UIBuilderShapes.EnsureBorderedBox(content, "Tray", TrayPosition, TraySize, TrayBorder, sp.Rounded,
                s.trayBorderColor, s.trayColor, ref changed);
            UIBuilderKit.EnsureText(inner, "Title", KeyTrayTitle, new Vector2(0f, TrayTitleY), TrayTitleSize, TrayTitleFontSize,
                s.textColor, TextAlignmentOptions.Center, ref changed);

            RectTransform grid = UIBuilderKit.EnsureRectChild(inner, "Cards", TrayGridPosition, TrayGridSize, ref changed);
            GridLayoutGroup layout = UIBuilderKit.EnsureComponent<GridLayoutGroup>(grid.gameObject, ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, layout.cellSize, CardSize, v => layout.cellSize = v);
            UIBuilderKit.SetIfDifferent(ref changed, layout.spacing, TrayGridSpacing, v => layout.spacing = v);
            UIBuilderKit.SetIfDifferent(ref changed, layout.constraint, GridLayoutGroup.Constraint.FixedColumnCount,
                v => layout.constraint = v);
            UIBuilderKit.SetIfDifferent(ref changed, layout.constraintCount, TrayColumns, v => layout.constraintCount = v);
            UIBuilderKit.SetIfDifferent(ref changed, layout.startAxis, GridLayoutGroup.Axis.Horizontal, v => layout.startAxis = v);
            UIBuilderKit.SetIfDifferent(ref changed, layout.childAlignment, TextAnchor.MiddleCenter, v => layout.childAlignment = v);
            return grid;
        }

        // Kase: kök bırakma/karıştırma alanıdır (görünmez Image). Arkadan öne: halka zemini, halka, parıltı, görsel grup
        // (kase arkası, hamur, simgeler, kase önü), kaşık, sayaç yazısı, "Hamur hazır!".
        private static (MixingBowl, GameObject) EnsureBowl(RectTransform content, CounterSettings s, Sprites sp, ref bool changed)
        {
            RectTransform bowlTf = UIBuilderKit.EnsureRectChild(content, "Bowl", BowlPosition, BowlSize, ref changed);
            if (bowlTf.GetComponent<Image>() == null)
            {
                Image hit = bowlTf.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                hit.raycastTarget = true;
                changed = true;
            }

            MixingBowl bowl = UIBuilderKit.EnsureComponent<MixingBowl>(bowlTf.gameObject, ref changed);

            Image track = UIBuilderShapes.EnsureImage(bowlTf, "RingTrack", Vector2.zero, Vector2.one * RingSize, sp.Ring,
                s.ringTrackColor, false, false, ref changed);
            Image ring = UIBuilderShapes.EnsureImage(bowlTf, "Ring", Vector2.zero, Vector2.one * RingSize, sp.Ring, s.ringFillColor,
                false, false, ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, ring.type, Image.Type.Filled, v => ring.type = v);
            UIBuilderKit.SetIfDifferent(ref changed, ring.fillMethod, Image.FillMethod.Radial360, v => ring.fillMethod = v);
            UIBuilderKit.SetIfDifferent(ref changed, ring.fillOrigin, (int)Image.Origin360.Top, v => ring.fillOrigin = v);
            UIBuilderKit.SetIfDifferent(ref changed, ring.fillClockwise, true, v => ring.fillClockwise = v);

            Image hover = UIBuilderShapes.EnsureImage(bowlTf, "HoverGlow", Vector2.zero, Vector2.one * HoverSize, sp.Circle,
                s.bowlHoverColor, false, true, ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, hover.enabled, false, v => hover.enabled = v);

            RectTransform visual = UIBuilderKit.EnsureRectChild(bowlTf, "Visual", Vector2.zero, BowlSize, ref changed);
            UIBuilderShapes.EnsureImage(visual, "BowlBack", Vector2.zero, Vector2.one * BowlBackSize, sp.Circle, s.bowlColor, false, true,
                ref changed);
            Image batter = UIBuilderShapes.EnsureImage(visual, "Batter", BatterPosition, Vector2.one * BatterSize, sp.Circle,
                s.backgroundColor, false, false, ref changed);
            RectTransform slots = UIBuilderKit.EnsureRectChild(visual, "Slots", BatterPosition, Vector2.one * BatterSize, ref changed);
            Image template = UIBuilderShapes.EnsureImage(slots, "SlotTemplate", Vector2.zero, Vector2.one * SlotIconSize, sp.Rounded,
                s.grayColor, true, false, ref changed);
            UIBuilderShapes.EnsureImage(visual, "BowlFront", BowlFrontPosition, BowlFrontSize, sp.Rounded, s.bowlRimColor, true, true,
                ref changed);
            UIBuilderShapes.EnsureSiblingOrder(visual, ref changed, "BowlBack", "Batter", "Slots", "BowlFront");

            Image spoon = UIBuilderShapes.EnsureImage(bowlTf, "Spoon", Vector2.zero, Vector2.one * SpoonSize, sp.Spoon,
                s.counterEdgeColor, false, false, ref changed);

            TMP_Text label = UIBuilderKit.EnsureText(bowlTf, "Label", KeyBowl, new Vector2(0f, BowlLabelY), BowlLabelSize,
                BowlLabelFontSize, s.textColor, TextAlignmentOptions.Center, ref changed);

            RectTransform bannerInner = UIBuilderShapes.EnsureBorderedBox(bowlTf, "ReadyBanner", new Vector2(0f, ReadyBannerY),
                ReadyBannerSize, ReadyBannerBorder, sp.Rounded, s.pinkBorderColor, s.greenColor, ref changed);
            Vector2 bannerTextSize = ReadyBannerSize - Vector2.one * (ReadyBannerBorder * 2f) - new Vector2(20f, 6f);
            UIBuilderKit.EnsureText(bannerInner, "Text", KeyBatterReady, Vector2.zero, bannerTextSize, ReadyFontSize,
                s.buttonTextColor, TextAlignmentOptions.Center, ref changed);
            GameObject banner = bannerInner.parent.gameObject;

            UIBuilderShapes.EnsureSiblingOrder(bowlTf, ref changed,
                "RingTrack", "Ring", "HoverGlow", "Visual", "Spoon", "Label", "ReadyBanner");

            var so = new SerializedObject(bowl);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderKit.EnsureReference(so, "dropArea", bowlTf, ref changed);
            UIBuilderKit.EnsureReference(so, "visual", visual, ref changed);
            UIBuilderKit.EnsureReference(so, "batter", batter, ref changed);
            UIBuilderKit.EnsureReference(so, "slotContainer", slots, ref changed);
            UIBuilderKit.EnsureReference(so, "slotTemplate", template, ref changed);
            UIBuilderKit.EnsureReference(so, "progressRing", ring, ref changed);
            UIBuilderKit.EnsureReference(so, "progressTrack", track.gameObject, ref changed);
            UIBuilderKit.EnsureReference(so, "spoon", spoon.rectTransform, ref changed);
            UIBuilderKit.EnsureReference(so, "hoverGlow", hover, ref changed);
            UIBuilderKit.EnsureReference(so, "countText", label.GetComponent<LocalizedText>(), ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            return (bowl, banner);
        }

        private readonly struct HintBarRefs
        {
            public readonly LocalizedText Step;
            public readonly LocalizedText Do;
            public readonly LocalizedText Next;
            public readonly RectTransform Fill;

            public HintBarRefs(LocalizedText step, LocalizedText @do, LocalizedText next, RectTransform fill)
            {
                Step = step;
                Do = @do;
                Next = next;
                Fill = fill;
            }
        }

        private static HintBarRefs EnsureHintBar(RectTransform content, CounterSettings s, Sprites sp, ref bool changed)
        {
            RectTransform inner = UIBuilderShapes.EnsureBorderedBox(content, "HintBar", HintBarPosition, HintBarSize, HintBarBorder,
                sp.Rounded, s.trayBorderColor, s.barColor, ref changed);
            TMP_Text step = UIBuilderKit.EnsureText(inner, "Step", string.Empty, HintStepPosition, HintStepSize, HintStepFontSize,
                s.pinkBorderColor, TextAlignmentOptions.MidlineLeft, ref changed);
            TMP_Text doText = UIBuilderKit.EnsureText(inner, "Do", string.Empty, HintDoPosition, HintDoSize, HintDoFontSize, s.textColor,
                TextAlignmentOptions.MidlineLeft, ref changed);
            TMP_Text next = UIBuilderKit.EnsureText(inner, "Next", string.Empty, HintNextPosition, HintNextSize, HintNextFontSize,
                s.mutedTextColor, TextAlignmentOptions.MidlineLeft, ref changed, bold: false);

            Image back = UIBuilderShapes.EnsureImage(inner, "Progress", HintProgressPosition, HintProgressSize, sp.Rounded,
                s.progressBackColor, true, true, ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, back.pixelsPerUnitMultiplier, ProgressCornerMultiplier,
                v => back.pixelsPerUnitMultiplier = v);

            // Dolgu soldan büyür: anchorMax.x'i panel sürer (0–1); burada yalnızca ilk eklenişte 0 verilir.
            RectTransform fillTf = UIBuilderKit.EnsureChild(back.rectTransform, "Fill", () =>
            {
                RectTransform created = UIBuilderKit.CreateStretched(back.rectTransform, "Fill");
                created.anchorMax = new Vector2(0f, 1f);
                Image image = UIBuilderKit.AddSlicedImage(created.gameObject, sp.Rounded, s.progressFillColor);
                image.pixelsPerUnitMultiplier = ProgressCornerMultiplier;
                image.raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, fillTf.anchorMin, Vector2.zero, v => fillTf.anchorMin = v);
            UIBuilderKit.SetIfDifferent(ref changed, fillTf.offsetMin, Vector2.zero, v => fillTf.offsetMin = v);
            UIBuilderKit.SetIfDifferent(ref changed, fillTf.offsetMax, Vector2.zero, v => fillTf.offsetMax = v);

            return new HintBarRefs(step.GetComponent<LocalizedText>(), doText.GetComponent<LocalizedText>(),
                next.GetComponent<LocalizedText>(), fillTf);
        }

        // Tasarım alanını kaplayan, tıklama yakalamayan katman; sürüklenen görsel burada çizilir.
        private static (RectTransform, Image) EnsureDragLayer(RectTransform content, Sprites sp, ref bool changed)
        {
            RectTransform layer = UIBuilderKit.EnsureStretchedChild(content, "DragLayer", ref changed);
            Image ghost = UIBuilderShapes.EnsureImage(layer, "Ghost", Vector2.zero, Vector2.one * CardIconSize, sp.Rounded, Color.white,
                true, false, ref changed);
            return (layer, ghost);
        }

        // Perde (tasarım alanının dışına da taşar) + kutu: soru, Evet, Hayır. Kapalı kaydedilir.
        private static (GameObject, Button, Button) EnsureLeaveDialog(RectTransform content, CounterSettings s, Sprites sp,
            ref bool changed)
        {
            RectTransform dialog = UIBuilderKit.EnsureStretchedChild(content, "LeaveDialog", ref changed);
            UIBuilderShapes.EnsureStretchedImage(dialog, "Dim", DimOutset, null, s.dimColor, false, true, ref changed);
            RectTransform inner = UIBuilderShapes.EnsureBorderedBox(dialog, "Box", Vector2.zero, DialogSize, DialogBorder, sp.Rounded,
                s.pinkBorderColor, s.backgroundColor, ref changed);
            UIBuilderKit.EnsureText(inner, "Question", KeyLeaveQuestion, QuestionPosition, QuestionSize, QuestionFontSize, s.textColor,
                TextAlignmentOptions.Center, ref changed);
            Button yes = UIBuilderShapes.EnsureTextButton(inner, "YesButton", KeyLeaveYes, YesPosition, DialogButtonSize,
                DialogButtonBorder, DialogButtonFontSize, sp.Rounded, s.pinkBorderColor, s.pinkColor, s.buttonTextColor, ref changed);
            Button no = UIBuilderShapes.EnsureTextButton(inner, "NoButton", KeyLeaveNo, NoPosition, DialogButtonSize,
                DialogButtonBorder, DialogButtonFontSize, sp.Rounded, s.mutedTextColor, s.grayColor, s.textColor, ref changed);
            UIBuilderShapes.EnsureSiblingOrder(dialog, ref changed, "Dim", "Box");

            UIBuilderKit.EnsureActive(dialog.gameObject, false, ref changed);
            return (dialog.gameObject, yes, no);
        }

        private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 0xFF);
    }
}
