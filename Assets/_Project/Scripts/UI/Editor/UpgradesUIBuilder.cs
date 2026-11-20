using CutiePie.Core;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G8 geliştirme mağazasının prefab'larını kurar / günceller.
    /// - Prefabs/UI/UpgradeCard.prefab: tek kart (simge, ad, açıklama, seviye, fiyat, Satın al, odak çerçevesi).
    /// - Prefabs/UI/UpgradesPanel.prefab: kendi Canvas'ı olan panel (başlık, mevcut altın, yatay kaydırılan kartlar,
    ///   oklar, kenar solmaları, kaydırma çubuğu, Kapat).
    /// Prefab'lar silinip yeniden yaratılmaz: var olan nesneler adlarıyla bulunup yerleşimleri eşitlenir, eksikler eklenir
    /// (UIBuilderKit.UpdatePrefabInPlace). Böylece fileID'ler korunur; Shop sahnesindeki ve RoundEndPanel'deki referanslar kopmaz.
    /// Değişiklik yoksa prefab kaydedilmez.
    /// "Cutie Pie/UI/Geliştirme Panelini Güncelle" yalnızca bu iki prefab'ı günceller (sahne gerekmez);
    /// RoundEndUIBuilder da round sonu panelini kurarken BuildPrefabs'i çağırır.
    /// Buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    internal static class UpgradesUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Geliştirme Panelini Güncelle";
        private const string CardPrefabPath = UIBuilderKit.PrefabFolder + "/UpgradeCard.prefab";
        private const string PanelPrefabPath = UIBuilderKit.PrefabFolder + "/UpgradesPanel.prefab";

        // Round sonu ekranının (10) üstünde. İç içe kullanımda RoundEndUIBuilder da bunu uygular.
        public const int CanvasSortingOrder = 20;

        // Panel kartı (kart merkezine göre).
        private static readonly Vector2 PanelCardSize = new Vector2(1720f, 920f);
        private const float TitleY = 380f;
        private static readonly Vector2 TitleSize = new Vector2(800f, 100f);
        private const float TitleFontSize = 76f;

        // Simge ile yanındaki yazı arasındaki boşluk (altın satırı ve fiyat satırı).
        private const float IconTextGap = 16f;

        // Mevcut altın satırı (sağ üst): [Altının] boşluk (o) boşluk [813]. Simge X'i satır merkezine göre.
        private static readonly Vector2 GoldRowPosition = new Vector2(630f, 380f);
        private static readonly Vector2 GoldRowSize = new Vector2(430f, 90f);
        private const float GoldIconX = 25f;
        private const float GoldIconSize = 52f;
        private const float GoldLabelWidth = 180f;
        private const float GoldValueWidth = 130f;
        private const float GoldTextHeight = 80f;
        private const float GoldFontSize = 36f;

        // Kaydırma alanı ve kart dizilimi: 4 kart tam sığar, fazlası kaydırılır.
        private const float ScrollY = -10f;
        private static readonly Vector2 ScrollSize = new Vector2(1640f, 620f);
        private const int ListPaddingX = 15;
        private const int ListPaddingY = 10;
        private const float CardSpacing = 30f;

        // Kaydırma ipuçları (CardScroll merkezine göre). Oklar alanın kenarına biner; solmalar kenardan içeri doğru saydamlaşır.
        private const float FadeWidth = 60f;
        private const float ArrowSize = 64f;
        private const float ArrowX = 812f;
        private const float ArrowGlyphSize = 26f;

        // Üçgenin ağırlık merkezi sola kaydığı için ok simgesi uç yönüne biraz itilir.
        private const float ArrowGlyphNudge = 2f;

        // İnce kaydırma çubuğu: kartların (alt kenar -300) altında, Kapat butonunun (üst kenar -333) üstünde.
        private const float ScrollbarY = -316f;
        private static readonly Vector2 ScrollbarSize = new Vector2(1200f, 10f);

        private static readonly Vector2 CloseButtonPosition = new Vector2(0f, -395f);
        private static readonly Vector2 CloseButtonSize = new Vector2(480f, 104f);
        private const float CloseFontSize = 42f;

        // Tek kart (kart merkezine göre).
        private static readonly Vector2 CardSize = new Vector2(380f, 600f);
        private const float IconY = 180f;
        private const float IconSize = 180f;
        private const float NameY = 55f;
        private static readonly Vector2 NameSize = new Vector2(340f, 70f);
        private const float NameFontSize = 40f;
        private const float DescriptionY = -35f;
        private static readonly Vector2 DescriptionSize = new Vector2(340f, 110f);
        private const float DescriptionFontSize = 28f;
        private const float LevelY = -110f;
        private static readonly Vector2 LevelSize = new Vector2(340f, 50f);
        private const float LevelFontSize = 32f;
        private const float PriceY = -165f;
        // Fiyat satırı: (o) boşluk [120]. Simge X'i satır merkezine göre; 3-4 haneli fiyatla grup ortada durur.
        private static readonly Vector2 PriceRowSize = new Vector2(260f, 50f);
        private const float PriceIconX = -45f;
        private const float PriceIconSize = 44f;
        private const float PriceValueWidth = 130f;
        private const float PriceFontSize = 36f;
        private static readonly Vector2 BuyButtonPosition = new Vector2(0f, -248f);
        private static readonly Vector2 BuyButtonSize = new Vector2(320f, 96f);
        private const float BuyFontSize = 42f;

        // Odak çerçevesi: kartın dışına taşan, içi boş yuvarlak kenar. Çarpan 9-slice kenarını inceltir (28 → 14).
        private const float FocusOutset = 6f;
        private const float FocusBorderMultiplier = 2f;

        private const string KeyTitle = "upgrades.title";
        private const string KeyYourGold = "upgrades.your_gold";
        private const string KeyLevel = "upgrades.level";
        private const string KeyBuy = "upgrades.buy";
        private const string KeyClose = "common.close";

        [MenuItem(MenuPath)]
        private static void UpdateFromMenu()
        {
            RoundEndSettings settings = UIBuilderKit.GetOrCreateSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();
            BuildPrefabs(settings, rounded);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Kart ve panel prefab'larını yerinde günceller (yoksa oluşturur); panel prefab asset'ini döndürür.</summary>
        public static GameObject BuildPrefabs(RoundEndSettings settings, Sprite rounded)
        {
            UIBuilderKit.EnsureFolder(UIBuilderKit.PrefabFolder);
            Sprite circle = UIBuilderKit.GetOrCreateCircleSprite();
            Sprite arrow = UIBuilderKit.GetOrCreateArrowSprite();
            Sprite fade = UIBuilderKit.GetOrCreateHorizontalFadeSprite();

            GameObject cardPrefab = UIBuilderKit.UpdatePrefabInPlace(CardPrefabPath, "UpgradeCard",
                root => EnsureCard(root, settings, rounded, circle));
            UpgradeCard card = cardPrefab.GetComponent<UpgradeCard>();

            return UIBuilderKit.UpdatePrefabInPlace(PanelPrefabPath, "UpgradesPanel",
                root => EnsurePanel(root, settings, rounded, circle, arrow, fade, card));
        }

        // ------------------------------------------------------------------ Kart

        private static bool EnsureCard(GameObject root, RoundEndSettings settings, Sprite rounded, Sprite circle)
        {
            bool changed = false;
            var tf = (RectTransform)root.transform;
            UIBuilderKit.SetIfDifferent(ref changed, tf.sizeDelta, CardSize, v => tf.sizeDelta = v);

            // Zemin tıklamaları yakalar; sürükleme üstteki ScrollRect'e geçer.
            if (root.GetComponent<Image>() == null)
            {
                UIBuilderKit.AddSlicedImage(root, rounded, settings.upgradeCardColor);
                changed = true;
            }

            UpgradeCard card = UIBuilderKit.EnsureComponent<UpgradeCard>(root, ref changed);

            var iconPosition = new Vector2(0f, IconY);
            var iconSize = new Vector2(IconSize, IconSize);
            RectTransform iconTf = UIBuilderKit.EnsureChild(tf, "Icon", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(tf, "Icon", iconPosition, iconSize);
                UIBuilderKit.AddSlicedImage(created.gameObject, rounded, settings.upgradeIconColor).raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(iconTf, iconPosition, iconSize, ref changed);
            Image icon = iconTf.GetComponent<Image>();

            // Ad ve açıklama anahtarlarını kart, bağlandığı geliştirmeden alır.
            TMP_Text nameText = UIBuilderKit.EnsureText(tf, "Name", string.Empty, new Vector2(0f, NameY), NameSize,
                NameFontSize, settings.textColor, TextAlignmentOptions.Center, ref changed);
            TMP_Text description = UIBuilderKit.EnsureText(tf, "Description", string.Empty, new Vector2(0f, DescriptionY),
                DescriptionSize, DescriptionFontSize, settings.textColor, TextAlignmentOptions.Center, ref changed, bold: false);
            TMP_Text level = UIBuilderKit.EnsureText(tf, "Level", KeyLevel, new Vector2(0f, LevelY), LevelSize,
                LevelFontSize, settings.textColor, TextAlignmentOptions.Center, ref changed);

            RectTransform priceRow = UIBuilderKit.EnsureRectChild(tf, "PriceRow", new Vector2(0f, PriceY), PriceRowSize, ref changed);
            UIBuilderKit.EnsureIcon(priceRow, "GoldIcon", new Vector2(PriceIconX, 0f), PriceIconSize, circle,
                settings.goldIconColor, ref changed);
            TMP_Text priceValue = UIBuilderKit.EnsureText(priceRow, "Value", null,
                new Vector2(RightOfIcon(PriceIconX, PriceIconSize, PriceValueWidth), 0f),
                new Vector2(PriceValueWidth, PriceRowSize.y), PriceFontSize, settings.textColor,
                TextAlignmentOptions.MidlineLeft, ref changed);
            EnsurePlaceholderNumber(priceValue, ref changed);

            Button buy = UIBuilderKit.EnsureButton(tf, "BuyButton", KeyBuy, BuyButtonPosition, BuyButtonSize, BuyFontSize,
                settings.buyButtonColor, rounded, settings, ref changed);

            Image focus = EnsureFocusHighlight(tf, rounded, settings, ref changed);

            var so = new SerializedObject(card);
            UIBuilderKit.EnsureReference(so, "settings", settings, ref changed);
            UIBuilderKit.EnsureReference(so, "icon", icon, ref changed);
            UIBuilderKit.EnsureReference(so, "nameText", nameText.GetComponent<LocalizedText>(), ref changed);
            UIBuilderKit.EnsureReference(so, "descriptionText", description.GetComponent<LocalizedText>(), ref changed);
            UIBuilderKit.EnsureReference(so, "levelText", level.GetComponent<LocalizedText>(), ref changed);
            UIBuilderKit.EnsureReference(so, "priceRow", priceRow.gameObject, ref changed);
            UIBuilderKit.EnsureReference(so, "priceValue", priceValue, ref changed);
            UIBuilderKit.EnsureReference(so, "buyButton", buy, ref changed);
            UIBuilderKit.EnsureReference(so, "buyLabel", buy.GetComponentInChildren<LocalizedText>(true), ref changed);
            UIBuilderKit.EnsureReference(so, "focusHighlight", focus, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        // Kartın en üstünde çizilen, içi boş altın çerçeve. Nesne açık, bileşen kapalı kaydedilir; UpgradeCard yakıp söndürür.
        private static Image EnsureFocusHighlight(RectTransform card, Sprite rounded, RoundEndSettings settings, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(card, "FocusHighlight", () =>
            {
                RectTransform created = UIBuilderKit.CreateStretched(card, "FocusHighlight");
                Image image = UIBuilderKit.AddSlicedImage(created.gameObject, rounded, settings.goldIconColor);
                image.fillCenter = false;
                image.pixelsPerUnitMultiplier = FocusBorderMultiplier;
                image.raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureStretchedRect(tf, -FocusOutset, ref changed);
            UIBuilderKit.EnsureActive(tf.gameObject, true, ref changed);

            Image focus = tf.GetComponent<Image>();
            UIBuilderKit.SetIfDifferent(ref changed, focus.enabled, false, v => focus.enabled = v);

            if (tf.GetSiblingIndex() != card.childCount - 1)
            {
                tf.SetAsLastSibling();
                changed = true;
            }

            return focus;
        }

        // ------------------------------------------------------------------ Panel

        private static bool EnsurePanel(GameObject root, RoundEndSettings settings, Sprite rounded, Sprite circle, Sprite arrow,
            Sprite fade, UpgradeCard cardPrefab)
        {
            bool changed = false;
            if (root.GetComponent<Canvas>() == null)
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                changed = true;
            }

            UpgradesPanel panel = UIBuilderKit.EnsureComponent<UpgradesPanel>(root, ref changed);

            RectTransform content = UIBuilderKit.EnsureStretchedChild(root.transform, "Content", ref changed);
            RectTransform dim = UIBuilderKit.EnsureChild(content, "Dim", () =>
            {
                UIBuilderKit.CreateDim(content, settings);
                return (RectTransform)UIBuilderKit.FindChild(content, "Dim");
            }, ref changed);
            UIBuilderKit.EnsureStretchedRect(dim, 0f, ref changed);

            RectTransform card = EnsurePanelCard(content, rounded, settings, ref changed);

            UIBuilderKit.EnsureText(card, "Title", KeyTitle, new Vector2(0f, TitleY), TitleSize, TitleFontSize,
                settings.textColor, TextAlignmentOptions.Center, ref changed);

            EnsureGoldRow(card, rounded, circle, settings, ref changed);
            (ScrollRect scroll, RectTransform list) = EnsureCardList(card, rounded, arrow, fade, circle, settings, ref changed);

            Button close = UIBuilderKit.EnsureButton(card, "CloseButton", KeyClose, CloseButtonPosition, CloseButtonSize,
                CloseFontSize, settings.closeButtonColor, rounded, settings, ref changed);

            var so = new SerializedObject(panel);
            UIBuilderKit.EnsureReference(so, "content", content.gameObject, ref changed);
            UIBuilderKit.EnsureReference(so, "closeButton", close, ref changed);
            UIBuilderKit.EnsureReference(so, "scrollRect", scroll, ref changed);
            UIBuilderKit.EnsureReference(so, "cardContainer", list, ref changed);
            UIBuilderKit.EnsureReference(so, "cardPrefab", cardPrefab, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Prefab içerik kapalı kaydedilir; panel Awake'te de kendini kapatır.
            UIBuilderKit.EnsureActive(content.gameObject, false, ref changed);
            return changed;
        }

        // Kenarlıklı kart (Card > Inner); parçalar Inner'a eklenir.
        private static RectTransform EnsurePanelCard(RectTransform content, Sprite rounded, RoundEndSettings settings, ref bool changed)
        {
            Vector2 borderSize = PanelCardSize + Vector2.one * (UIBuilderKit.CardBorderWidth * 2f);
            RectTransform border = UIBuilderKit.EnsureChild(content, "Card",
                () => (RectTransform)UIBuilderKit.CreateCard(content, "Card", PanelCardSize, rounded, settings).parent, ref changed);
            UIBuilderKit.EnsureRect(border, Vector2.zero, borderSize, ref changed);

            RectTransform inner = UIBuilderKit.EnsureChild(border, "Inner", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(border, "Inner", Vector2.zero, PanelCardSize);
                UIBuilderKit.AddSlicedImage(created.gameObject, rounded, settings.cardColor);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(inner, Vector2.zero, PanelCardSize, ref changed);
            return inner;
        }

        // "Altının: (o) 120" satırı; sayıyı Core'daki CurrencyDisplay yazar ve CurrencyChanged ile günceller.
        private static void EnsureGoldRow(Transform card, Sprite rounded, Sprite circle, RoundEndSettings settings, ref bool changed)
        {
            RectTransform row = UIBuilderKit.EnsureChild(card, "GoldRow", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(card, "GoldRow", GoldRowPosition, GoldRowSize);
                UIBuilderKit.AddSlicedImage(created.gameObject, rounded, settings.statRowColor).raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(row, GoldRowPosition, GoldRowSize, ref changed);

            // Etiket simgenin soluna sağa yaslı, sayı simgenin sağına sola yaslı; aralarında IconTextGap kadar boşluk kalır.
            UIBuilderKit.EnsureText(row, "Label", KeyYourGold,
                new Vector2(LeftOfIcon(GoldIconX, GoldIconSize, GoldLabelWidth), 0f),
                new Vector2(GoldLabelWidth, GoldTextHeight), GoldFontSize, settings.textColor, TextAlignmentOptions.MidlineRight,
                ref changed);
            UIBuilderKit.EnsureIcon(row, "GoldIcon", new Vector2(GoldIconX, 0f), GoldIconSize, circle, settings.goldIconColor,
                ref changed);
            TMP_Text value = UIBuilderKit.EnsureText(row, "Value", null,
                new Vector2(RightOfIcon(GoldIconX, GoldIconSize, GoldValueWidth), 0f),
                new Vector2(GoldValueWidth, GoldTextHeight), GoldFontSize, settings.textColor, TextAlignmentOptions.MidlineLeft,
                ref changed);
            EnsurePlaceholderNumber(value, ref changed);

            CurrencyDisplay display = row.GetComponent<CurrencyDisplay>();
            if (display == null)
            {
                display = row.gameObject.AddComponent<CurrencyDisplay>();
                var currency = new SerializedObject(display);
                currency.FindProperty("currency").intValue = (int)CurrencyType.Gold;
                currency.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            var so = new SerializedObject(display);
            UIBuilderKit.EnsureReference(so, "amountLabel", value, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Yatay kaydırılan kart listesi (kurulum UIBuilderKit'te; fırın ekranı da aynısını kullanır) ve kaydırma ipuçları.
        private static (ScrollRect, RectTransform) EnsureCardList(Transform card, Sprite rounded, Sprite arrow, Sprite fade,
            Sprite circle, RoundEndSettings settings, ref bool changed)
        {
            var position = new Vector2(0f, ScrollY);
            RectTransform scrollTf = UIBuilderKit.EnsureChild(card, "CardScroll",
                () => (RectTransform)UIBuilderKit.CreateHorizontalScroll(card, "CardScroll", "Cards", position, ScrollSize,
                    ListPaddingX, ListPaddingY, CardSpacing).Item1.transform, ref changed);
            UIBuilderKit.EnsureRect(scrollTf, position, ScrollSize, ref changed);
            ScrollRect scroll = scrollTf.GetComponent<ScrollRect>();

            // Viewport'tan sonra eklenir ki kartların üstünde çizilsinler: solmalar, oklar, çubuk.
            GameObject leftFade = EnsureFade(scrollTf, "FadeLeft", -1f, fade, settings, ref changed);
            GameObject rightFade = EnsureFade(scrollTf, "FadeRight", 1f, fade, settings, ref changed);
            Button previous = EnsureArrow(scrollTf, "ArrowLeft", -1f, arrow, circle, settings, ref changed);
            Button next = EnsureArrow(scrollTf, "ArrowRight", 1f, arrow, circle, settings, ref changed);
            Scrollbar scrollbar = EnsureScrollbar(scrollTf, rounded, settings, ref changed);

            // Tekerleği ScrollAffordance adım adım işler; ScrollRect'in kendi tekerlek kaydırması kapatılır.
            UIBuilderKit.SetIfDifferent(ref changed, scroll.scrollSensitivity, 0f, v => scroll.scrollSensitivity = v);
            UIBuilderKit.SetIfDifferent(ref changed, scroll.horizontalScrollbar, scrollbar, v => scroll.horizontalScrollbar = v);
            UIBuilderKit.SetIfDifferent(ref changed, scroll.horizontalScrollbarVisibility, ScrollRect.ScrollbarVisibility.AutoHide,
                v => scroll.horizontalScrollbarVisibility = v);

            ScrollAffordance affordance = UIBuilderKit.EnsureComponent<ScrollAffordance>(scrollTf.gameObject, ref changed);
            var so = new SerializedObject(affordance);
            UIBuilderKit.EnsureReference(so, "scrollRect", scroll, ref changed);
            UIBuilderKit.EnsureReference(so, "previousButton", previous, ref changed);
            UIBuilderKit.EnsureReference(so, "nextButton", next, ref changed);
            UIBuilderKit.EnsureReference(so, "leftFade", leftFade, ref changed);
            UIBuilderKit.EnsureReference(so, "rightFade", rightFade, ref changed);
            UIBuilderKit.EnsureFloat(so, "step", CardSize.x + CardSpacing, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            return (scroll, scroll.content);
        }

        // Kenardan içeri doğru kart zeminine karışan solma; side -1 sol, +1 sağ (sprite aynalanır). Tıklama yakalamaz.
        private static GameObject EnsureFade(RectTransform parent, string name, float side, Sprite fade, RoundEndSettings settings,
            ref bool changed)
        {
            var position = new Vector2(side * (ScrollSize.x - FadeWidth) * 0.5f, 0f);
            var size = new Vector2(FadeWidth, ScrollSize.y);
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, name, position, size);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = fade;
                image.color = settings.cardColor;
                image.raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, size, ref changed);
            UIBuilderKit.EnsureScale(tf, new Vector3(side < 0f ? 1f : -1f, 1f, 1f), ref changed);
            return tf.gameObject;
        }

        // Yuvarlak ok butonu; side -1 sol (simge aynalanır), +1 sağ.
        private static Button EnsureArrow(RectTransform parent, string name, float side, Sprite arrow, Sprite circle,
            RoundEndSettings settings, ref bool changed)
        {
            var position = new Vector2(side * ArrowX, 0f);
            var size = new Vector2(ArrowSize, ArrowSize);
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, name, position, size);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = circle;
                image.color = settings.cardBorderColor;
                image.preserveAspect = true;
                UIBuilderKit.AddButton(created.gameObject, image);
                UIBuilderKit.CreateIcon(created, "Glyph", Vector2.zero, ArrowGlyphSize, arrow, settings.buttonTextColor);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, size, ref changed);

            Transform glyph = UIBuilderKit.FindChild(tf, "Glyph");
            if (glyph != null)
            {
                UIBuilderKit.EnsureRect((RectTransform)glyph, new Vector2(side * ArrowGlyphNudge, 0f),
                    new Vector2(ArrowGlyphSize, ArrowGlyphSize), ref changed);
                UIBuilderKit.EnsureScale(glyph, new Vector3(side < 0f ? -1f : 1f, 1f, 1f), ref changed);
            }

            return tf.GetComponent<Button>();
        }

        // İnce yatay kaydırma çubuğu; boyunu ve konumunu ScrollRect sürer, içerik sığınca kendiliğinden gizlenir (AutoHide).
        private static Scrollbar EnsureScrollbar(RectTransform parent, Sprite rounded, RoundEndSettings settings, ref bool changed)
        {
            var position = new Vector2(0f, ScrollbarY);
            RectTransform tf = UIBuilderKit.EnsureChild(parent, "Scrollbar", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, "Scrollbar", position, ScrollbarSize);
                UIBuilderKit.AddSlicedImage(created.gameObject, rounded, settings.statRowColor);
                Scrollbar bar = created.gameObject.AddComponent<Scrollbar>();

                RectTransform area = UIBuilderKit.CreateStretched(created, "SlidingArea");
                RectTransform handle = UIBuilderKit.CreateStretched(area, "Handle");
                Image handleImage = UIBuilderKit.AddSlicedImage(handle.gameObject, rounded, settings.cardBorderColor);

                bar.handleRect = handle;
                bar.targetGraphic = handleImage;
                bar.direction = Scrollbar.Direction.LeftToRight;

                // Dokunmatikte seçili kalma görünümü olmasın.
                Navigation navigation = bar.navigation;
                navigation.mode = Navigation.Mode.None;
                bar.navigation = navigation;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, ScrollbarSize, ref changed);
            return tf.GetComponent<Scrollbar>();
        }

        // ------------------------------------------------------------------ Yardımcılar

        // Kodun yazdığı sayı alanı editörde boş görünmesin.
        private static void EnsurePlaceholderNumber(TMP_Text text, ref bool changed)
        {
            if (text != null && string.IsNullOrEmpty(text.text))
            {
                text.text = "0";
                changed = true;
            }
        }

        // Simgenin sağındaki/solundaki metin kutusunun merkez X'i: kutu kenarı simge kenarından IconTextGap uzakta başlar.
        private static float RightOfIcon(float iconX, float iconSize, float textWidth) =>
            iconX + iconSize * 0.5f + IconTextGap + textWidth * 0.5f;

        private static float LeftOfIcon(float iconX, float iconSize, float textWidth) =>
            iconX - iconSize * 0.5f - IconTextGap - textWidth * 0.5f;
    }
}
