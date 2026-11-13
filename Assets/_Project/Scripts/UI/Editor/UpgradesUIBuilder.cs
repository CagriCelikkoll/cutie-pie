using CutiePie.Core;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G8 geliştirme mağazasının prefab'larını baştan kurar.
    /// - Prefabs/UI/UpgradeCard.prefab: tek kart (simge, ad, açıklama, seviye, fiyat, Satın al).
    /// - Prefabs/UI/UpgradesPanel.prefab: kendi Canvas'ı olan panel (başlık, mevcut altın, yatay kaydırılan kartlar, Kapat).
    /// RoundEndUIBuilder tarafından çağrılır ("Cutie Pie/UI/Round Sonu Panelini Kur"); panel RoundEndPanel'e iç içe eklenir.
    /// Buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    internal static class UpgradesUIBuilder
    {
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

        private const string KeyTitle = "upgrades.title";
        private const string KeyYourGold = "upgrades.your_gold";
        private const string KeyLevel = "upgrades.level";
        private const string KeyBuy = "upgrades.buy";
        private const string KeyClose = "common.close";

        /// <summary>Kart ve panel prefab'larını kurar; panel prefab asset'ini döndürür.</summary>
        public static GameObject BuildPrefabs(RoundEndSettings settings, Sprite rounded)
        {
            UIBuilderKit.EnsureFolder(UIBuilderKit.PrefabFolder);
            Sprite circle = UIBuilderKit.GetOrCreateCircleSprite();

            GameObject cardPrefab = BuildCardPrefab(settings, rounded, circle);
            return BuildPanelPrefab(settings, rounded, circle, cardPrefab.GetComponent<UpgradeCard>());
        }

        // ------------------------------------------------------------------ Kart

        private static GameObject BuildCardPrefab(RoundEndSettings settings, Sprite rounded, Sprite circle)
        {
            var root = new GameObject("UpgradeCard", typeof(RectTransform));
            try
            {
                var tf = (RectTransform)root.transform;
                tf.sizeDelta = CardSize;

                // Zemin tıklamaları yakalar; sürükleme üstteki ScrollRect'e geçer.
                UIBuilderKit.AddSlicedImage(root, rounded, settings.upgradeCardColor);
                UpgradeCard card = root.AddComponent<UpgradeCard>();

                RectTransform iconTf = UIBuilderKit.CreateRect(tf, "Icon", new Vector2(0f, IconY), new Vector2(IconSize, IconSize));
                Image icon = UIBuilderKit.AddSlicedImage(iconTf.gameObject, rounded, settings.upgradeIconColor);
                icon.raycastTarget = false;

                // Ad ve açıklama anahtarlarını kart, bağlandığı geliştirmeden alır.
                TMP_Text nameText = UIBuilderKit.CreateText(tf, "Name", string.Empty, new Vector2(0f, NameY), NameSize,
                    NameFontSize, settings.textColor, TextAlignmentOptions.Center);
                TMP_Text description = UIBuilderKit.CreateText(tf, "Description", string.Empty, new Vector2(0f, DescriptionY),
                    DescriptionSize, DescriptionFontSize, settings.textColor, TextAlignmentOptions.Center, bold: false);
                TMP_Text level = UIBuilderKit.CreateText(tf, "Level", KeyLevel, new Vector2(0f, LevelY), LevelSize,
                    LevelFontSize, settings.textColor, TextAlignmentOptions.Center);

                RectTransform priceRow = UIBuilderKit.CreateRect(tf, "PriceRow", new Vector2(0f, PriceY), PriceRowSize);
                UIBuilderKit.CreateIcon(priceRow, "GoldIcon", new Vector2(PriceIconX, 0f), PriceIconSize, circle,
                    settings.goldIconColor);
                TMP_Text priceValue = UIBuilderKit.CreateText(priceRow, "Value", null,
                    new Vector2(RightOfIcon(PriceIconX, PriceIconSize, PriceValueWidth), 0f),
                    new Vector2(PriceValueWidth, PriceRowSize.y), PriceFontSize, settings.textColor,
                    TextAlignmentOptions.MidlineLeft);
                priceValue.text = "0";

                Button buy = UIBuilderKit.CreateButton(tf, "BuyButton", KeyBuy, BuyButtonPosition, BuyButtonSize, BuyFontSize,
                    settings.buyButtonColor, rounded, settings);

                var so = new SerializedObject(card);
                so.FindProperty("settings").objectReferenceValue = settings;
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("nameText").objectReferenceValue = nameText.GetComponent<LocalizedText>();
                so.FindProperty("descriptionText").objectReferenceValue = description.GetComponent<LocalizedText>();
                so.FindProperty("levelText").objectReferenceValue = level.GetComponent<LocalizedText>();
                so.FindProperty("priceRow").objectReferenceValue = priceRow.gameObject;
                so.FindProperty("priceValue").objectReferenceValue = priceValue;
                so.FindProperty("buyButton").objectReferenceValue = buy;
                so.FindProperty("buyLabel").objectReferenceValue = buy.GetComponentInChildren<LocalizedText>(true);
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ Panel

        private static GameObject BuildPanelPrefab(RoundEndSettings settings, Sprite rounded, Sprite circle, UpgradeCard cardPrefab)
        {
            var root = new GameObject("UpgradesPanel", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                UpgradesPanel panel = root.AddComponent<UpgradesPanel>();

                RectTransform content = UIBuilderKit.CreateStretched(root.transform, "Content");
                UIBuilderKit.CreateDim(content, settings);
                RectTransform card = UIBuilderKit.CreateCard(content, "Card", PanelCardSize, rounded, settings);

                UIBuilderKit.CreateText(card, "Title", KeyTitle, new Vector2(0f, TitleY), TitleSize, TitleFontSize,
                    settings.textColor, TextAlignmentOptions.Center);

                CreateGoldRow(card, rounded, circle, settings);
                (ScrollRect scroll, RectTransform list) = CreateCardList(card);

                Button close = UIBuilderKit.CreateButton(card, "CloseButton", KeyClose, CloseButtonPosition, CloseButtonSize,
                    CloseFontSize, settings.closeButtonColor, rounded, settings);

                var so = new SerializedObject(panel);
                so.FindProperty("content").objectReferenceValue = content.gameObject;
                so.FindProperty("closeButton").objectReferenceValue = close;
                so.FindProperty("scrollRect").objectReferenceValue = scroll;
                so.FindProperty("cardContainer").objectReferenceValue = list;
                so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Prefab içerik kapalı kaydedilir; panel Awake'te de kendini kapatır.
                content.gameObject.SetActive(false);

                return PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // "Altının: (o) 120" satırı; sayıyı Core'daki CurrencyDisplay yazar ve CurrencyChanged ile günceller.
        private static void CreateGoldRow(Transform card, Sprite rounded, Sprite circle, RoundEndSettings settings)
        {
            RectTransform row = UIBuilderKit.CreateRect(card, "GoldRow", GoldRowPosition, GoldRowSize);
            UIBuilderKit.AddSlicedImage(row.gameObject, rounded, settings.statRowColor).raycastTarget = false;

            // Etiket simgenin soluna sağa yaslı, sayı simgenin sağına sola yaslı; aralarında IconTextGap kadar boşluk kalır.
            UIBuilderKit.CreateText(row, "Label", KeyYourGold,
                new Vector2(LeftOfIcon(GoldIconX, GoldIconSize, GoldLabelWidth), 0f),
                new Vector2(GoldLabelWidth, GoldTextHeight), GoldFontSize, settings.textColor, TextAlignmentOptions.MidlineRight);
            UIBuilderKit.CreateIcon(row, "GoldIcon", new Vector2(GoldIconX, 0f), GoldIconSize, circle, settings.goldIconColor);
            TMP_Text value = UIBuilderKit.CreateText(row, "Value", null,
                new Vector2(RightOfIcon(GoldIconX, GoldIconSize, GoldValueWidth), 0f),
                new Vector2(GoldValueWidth, GoldTextHeight), GoldFontSize, settings.textColor, TextAlignmentOptions.MidlineLeft);
            value.text = "0";

            CurrencyDisplay display = row.gameObject.AddComponent<CurrencyDisplay>();
            var so = new SerializedObject(display);
            so.FindProperty("currency").intValue = (int)CurrencyType.Gold;
            so.FindProperty("amountLabel").objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Simgenin sağındaki/solundaki metin kutusunun merkez X'i: kutu kenarı simge kenarından IconTextGap uzakta başlar.
        private static float RightOfIcon(float iconX, float iconSize, float textWidth) =>
            iconX + iconSize * 0.5f + IconTextGap + textWidth * 0.5f;

        private static float LeftOfIcon(float iconX, float iconSize, float textWidth) =>
            iconX - iconSize * 0.5f - IconTextGap - textWidth * 0.5f;

        // Yatay kaydırılan kart listesi; kurulum UIBuilderKit'te (fırın ekranı da aynısını kullanır).
        private static (ScrollRect, RectTransform) CreateCardList(Transform card) =>
            UIBuilderKit.CreateHorizontalScroll(card, "CardScroll", "Cards", new Vector2(0f, ScrollY), ScrollSize,
                ListPaddingX, ListPaddingY, CardSpacing);
    }
}
