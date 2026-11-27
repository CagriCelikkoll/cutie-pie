using System.Collections.Generic;
using System.IO;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G22 Pasta Dolabı prefab'larını kurar / günceller ("Cutie Pie/UI/Pasta Dolabı Oluştur").
    /// - Prefabs/UI/Kitchen/CakeCabinetCard.prefab: tek tatlı kartı (görsel, kilit, ad, dilim kutucukları, durum, seçim çerçevesi, ✓).
    /// - Prefabs/UI/Kitchen/CakeCabinetPanel.prefab: kendi Canvas'ı olan ekran (eski yol Prefabs/UI/'daysa oraya taşınır). Hiyerarşi:
    ///   Screen (tam ekran; Show/Hide bunu açıp kapatır) > Background (tam ekran krem zemin) + Content (1920x1080 tasarım alanı,
    ///   DesignAreaFitter ile her en-boy oranında ekrana sığar ve ölçeklenir; uzun ekranlarda yanlarda zemin görünür).
    ///   Content'te çizim sırası (arkadan öne): aşçı (kafası kornişin arkasından taşar) → dolap (cam gövde, 2 raf,
    ///   kaydırılan kartlar) → korniş ("PASTA DOLABI") → aşçının elleri (kornişin önünde) → konuşma balonu → "… yap" butonu (gölgeli).
    ///   G22'nin ilk sürümüyle kurulmuş prefab'da tam ekran "Content" kutusu "Screen" olarak yeniden adlandırılır ve objeler
    ///   yeni Content'e taşınır (yeniden yaratılmaz).
    /// Prefab'lar silinip yeniden yaratılmaz (UIBuilderKit.UpdatePrefabInPlace): fileID'ler korunur; değişiklik yoksa kaydedilmez.
    /// Var olan Image'ların sprite'ına ve rengine dokunulmaz; sonradan takılan sprite'lar (Chef/Head, Cabinet, Cornice, HandLeft/Right) kalır.
    /// Panelin içinde iç içe Canvas yoktur; sahneye doğrudan InstantiatePrefab ile konur (CabinetTestSceneBuilder).
    /// Buradaki sayılar yalnızca yerleşimdir (1920x1080 referans); renkler CakeCabinetSettings'ten gelir.
    /// </summary>
    public static class CakeCabinetUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Pasta Dolabı Oluştur";
        private const string CardPrefabPath = UIBuilderKit.KitchenPrefabFolder + "/CakeCabinetCard.prefab";
        private const string PanelPrefabPath = UIBuilderKit.KitchenPrefabFolder + "/CakeCabinetPanel.prefab";

        // G22'nin ilk sürümündeki yol; varsa BuildPrefabs PanelPrefabPath'e taşır.
        private const string LegacyPanelPrefabPath = UIBuilderKit.PrefabFolder + "/CakeCabinetPanel.prefab";
        private const string SettingsPath = "Assets/_Project/Data/Salon/CakeCabinetSettings.asset";

        private const string PlaceholderFolder = "Assets/_Project/Art/Salon/Placeholders";
        private const string LockSpritePath = PlaceholderFolder + "/Lock.png";
        private const string CheckSpritePath = PlaceholderFolder + "/Check.png";
        private const int ShapeTextureSize = 128;
        private const int Supersample = 4;
        private const float SpritePixelsPerUnit = 100f;

        // Fırın ekranının yerini alacağı için aynı sıralama.
        public const int CanvasSortingOrder = OvenPanelUIBuilder.CanvasSortingOrder;

        // ---- Tasarım alanı: Screen > Content (DesignAreaFitter ile ekrana sığar ve ölçeklenir).
        // Aşağıdaki tüm konumlar Content merkezine göredir (y: -540 … 540).
        // Boşluklar: şapkanın üstü 510 (üstte 30 px), buton gölgesinin altı -506 (altta 34 px).
        private static readonly Vector2 DesignSize = new Vector2(1920f, 1080f);

        // Eski sürümde (G22 ilk hali) tam ekran kutunun adı "Content" idi; "Screen" olarak yeniden adlandırılır.
        private const string ScreenName = "Screen";
        private const string ContentName = "Content";

        // Content'e taşınan, Content'te arkadan öne çizilen objeler (aşçı dolaptan önce, eller kornişten sonra).
        private static readonly string[] ContentChildren =
            { "Chef", "Cabinet", "Cornice", "HandLeft", "HandRight", "Bubble", "MakeButton" };

        // ---- Dolap: üst kenar 250 (korniş altında), alt kenar -360 (butonla arasında 34 px).
        private static readonly Vector2 CabinetPosition = new Vector2(0f, -55f);
        private static readonly Vector2 CabinetSize = new Vector2(1280f, 610f);
        private const float CabinetBorder = 14f;
        private const float GlassInset = 26f;
        private static readonly Vector2 GlassSize = CabinetSize - Vector2.one * ((CabinetBorder + GlassInset) * 2f);

        // Raflar (cam merkezine göre): üst raf iki kart sırası arasında, alt raf ikinci sıranın altında.
        private const float ShelfTopY = 0f;
        private const float ShelfBottomInset = 8f;
        private static readonly float ShelfBottomY = -GlassSize.y * 0.5f + ShelfBottomInset;
        private const float ShelfHeight = 12f;

        // Kart ızgarası: 2 raf (satır) x 3 kart tam sığar; fazlası sütun olarak sağa eklenir ve yatay kaydırılır.
        private const int GridRows = 2;
        private const int GridPaddingX = 20;
        private const int GridPaddingY = 15;
        private static readonly Vector2 GridSpacing = new Vector2(40f, 30f);

        // ---- Korniş
        private static readonly Vector2 CornicePosition = new Vector2(0f, 290f);
        private static readonly Vector2 CorniceSize = new Vector2(1380f, 100f);
        private const float CorniceBorder = 8f;
        private const float TitleFontSize = 58f;

        // ---- Aşçı: dolabın üst ortasında, kafası kornişin arkasından yukarı taşar.
        private static readonly Vector2 ChefPosition = new Vector2(0f, 370f);
        private static readonly Vector2 ChefSize = new Vector2(200f, 240f);
        private const float ChefHeadSize = 160f;
        private static readonly Vector2 ChefHatPosition = new Vector2(0f, 92f);
        private static readonly Vector2 ChefHatSize = new Vector2(150f, 96f);
        private const float EyeOffsetX = 30f;
        private const float EyeY = 14f;
        private const float EyeSize = 18f;
        private const float CheekOffsetX = 48f;
        private const float CheekY = -18f;
        private const float CheekSize = 26f;

        // Eller kornişin üst kenarına tutunur (ortadan ±HandOffsetX).
        private const float HandOffsetX = 60f;
        private const float HandY = 340f;
        private const float HandSize = 58f;

        // ---- Konuşma balonu: aşçının sağında; sol alttaki iki nokta aşçıya doğru iner.
        private static readonly Vector2 BubblePosition = new Vector2(340f, 440f);
        private static readonly Vector2 BubbleSize = new Vector2(440f, 100f);
        private const float BubbleBorder = 5f;
        private const float BubbleFontSize = 36f;
        private static readonly Vector2 TailLargePosition = new Vector2(-200f, -52f);
        private const float TailLargeSize = 30f;
        private static readonly Vector2 TailSmallPosition = new Vector2(-232f, -74f);
        private const float TailSmallSize = 18f;
        private const float TailBorder = 4f;

        // ---- Yap butonu: -498 … -394; gölge 8 px aşağıda (alt kenarı -506).
        private static readonly Vector2 MakeButtonPosition = new Vector2(0f, -446f);
        private static readonly Vector2 MakeButtonSize = new Vector2(660f, 104f);
        private const float MakeButtonBorder = 8f;
        private const float MakeFontSize = 48f;
        private const float MakeShadowOffset = 8f;

        // ---- Kart (kart merkezine göre). Izgara: 2 x 235 + 30 aralık + 2 x 15 kenar = cam yüksekliği (530).
        private static readonly Vector2 CardSize = new Vector2(350f, 235f);
        private const float CakeImageY = 48f;
        private const float CakeImageSize = 110f;
        private const float LockIconSize = 68f;
        private const float NameY = -28f;
        private static readonly Vector2 NameSize = new Vector2(320f, 42f);
        private const float NameFontSize = 32f;
        private const float SlicesY = -64f;
        private const float SliceTileSize = 24f;
        private const float SliceTileGap = 6f;

        // Yuvarlak kutu sprite'ının 9-slice kenarı (28) küçük kutucukta 7'ye iner.
        private const float SliceCornerMultiplier = 4f;
        private const float StatusY = -96f;
        private static readonly Vector2 StatusSize = new Vector2(320f, 36f);
        private const float StatusFontSize = 28f;
        private const float SelectionOutset = 6f;
        private const float SelectionBorderMultiplier = 2f;
        private static readonly Vector2 CheckBadgePosition = new Vector2(150f, 90f);
        private const float CheckBadgeSize = 54f;
        private const float CheckGlyphSize = 34f;

        private const string KeyTitle = "kitchen.cabinet.title";
        private const string KeyAsk = "kitchen.cabinet.ask";
        private const string KeyPick = "kitchen.cabinet.pick";

        [MenuItem(MenuPath)]
        private static void BuildFromMenu()
        {
            BuildPrefabs();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Kart ve panel prefab'larını yerinde günceller (yoksa oluşturur); panel prefab asset'ini döndürür.</summary>
        public static GameObject BuildPrefabs()
        {
            CakeCabinetSettings settings = GetOrCreateSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();
            Sprite circle = UIBuilderKit.GetOrCreateCircleSprite();
            Sprite lockSprite = GetOrCreateLockSprite();
            Sprite check = GetOrCreateCheckSprite();
            UIBuilderKit.EnsureFolder(UIBuilderKit.KitchenPrefabFolder);

            GameObject cardPrefab = UIBuilderKit.UpdatePrefabInPlace(CardPrefabPath, "CakeCabinetCard",
                root => EnsureCard(root, settings, rounded, circle, lockSprite, check));
            CakeCabinetCard card = cardPrefab.GetComponent<CakeCabinetCard>();

            string panelPath = MigrateLegacyPanelPath();
            return UIBuilderKit.UpdatePrefabInPlace(panelPath, "CakeCabinetPanel",
                root => EnsurePanel(root, settings, rounded, circle, card));
        }

        /// <summary>
        /// Eski yoldaki (Prefabs/UI/) panel prefab'ını Prefabs/UI/Kitchen/'a taşır; GUID ve .meta korunur, sahnelerdeki
        /// referanslar kopmaz. Güncellenecek prefab'ın yolunu döndürür. Taşıma başarısız olursa yeni yolda ikinci bir kopya
        /// yaratılmaz; eski yoldaki prefab yerinde güncellenir.
        /// </summary>
        private static string MigrateLegacyPanelPath()
        {
            bool legacyExists = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPanelPrefabPath) != null;
            bool currentExists = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) != null;

            if (!legacyExists)
            {
                return PanelPrefabPath;
            }

            if (currentExists)
            {
                Debug.LogWarning($"[CakeCabinetUIBuilder] Hem '{LegacyPanelPrefabPath}' hem '{PanelPrefabPath}' var; taşıma yapılmadı. " +
                                 "Yalnızca yeni yoldaki güncelleniyor; eskisini kullanılmıyorsa elle silin.");
                return PanelPrefabPath;
            }

            string error = AssetDatabase.MoveAsset(LegacyPanelPrefabPath, PanelPrefabPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[CakeCabinetUIBuilder] '{LegacyPanelPrefabPath}' taşınamadı: {error}. Eski yoldaki prefab güncelleniyor.");
                return LegacyPanelPrefabPath;
            }

            Debug.Log($"[CakeCabinetUIBuilder] Panel prefab'ı taşındı: '{LegacyPanelPrefabPath}' → '{PanelPrefabPath}' (GUID korundu).");
            return PanelPrefabPath;
        }

        /// <summary>Data/Salon/CakeCabinetSettings.asset; yoksa varsayılan değerlerle oluşturur.</summary>
        public static CakeCabinetSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CakeCabinetSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            UIBuilderKit.EnsureFolder(Path.GetDirectoryName(SettingsPath)?.Replace('\\', '/'));
            settings = ScriptableObject.CreateInstance<CakeCabinetSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        // ------------------------------------------------------------------ Kart

        private static bool EnsureCard(GameObject root, CakeCabinetSettings s, Sprite rounded, Sprite circle, Sprite lockSprite,
            Sprite check)
        {
            bool changed = false;
            var tf = (RectTransform)root.transform;
            UIBuilderKit.SetIfDifferent(ref changed, tf.sizeDelta, CardSize, v => tf.sizeDelta = v);

            // Zemin tıklamaları yakalar; sürükleme üstteki ScrollRect'e geçer.
            Image background = root.GetComponent<Image>();
            if (background == null)
            {
                background = UIBuilderKit.AddSlicedImage(root, rounded, s.cardColor);
                changed = true;
            }

            Button button = root.GetComponent<Button>();
            if (button == null)
            {
                button = UIBuilderKit.AddButton(root, background);
                changed = true;
            }

            CakeCabinetCard card = UIBuilderKit.EnsureComponent<CakeCabinetCard>(root, ref changed);

            var cakePosition = new Vector2(0f, CakeImageY);
            Image cake = EnsureImage(tf, "CakeImage", cakePosition, Vector2.one * CakeImageSize, rounded, s.cakePlaceholderColor,
                true, true, ref changed);
            Image lockIcon = EnsureImage(tf, "LockIcon", cakePosition, Vector2.one * LockIconSize, lockSprite, s.textColor,
                false, false, ref changed);

            // Ad anahtarını kart, bağlandığı tariften alır; durum anahtarını kart durumuna göre yazar.
            TMP_Text nameText = UIBuilderKit.EnsureText(tf, "Name", string.Empty, new Vector2(0f, NameY), NameSize, NameFontSize,
                s.textColor, TextAlignmentOptions.Center, ref changed);
            Image[] tiles = EnsureSliceTiles(tf, s, rounded, ref changed);
            TMP_Text status = UIBuilderKit.EnsureText(tf, "Status", string.Empty, new Vector2(0f, StatusY), StatusSize,
                StatusFontSize, s.textColor, TextAlignmentOptions.Center, ref changed);

            Image frame = EnsureSelectionFrame(tf, rounded, s, ref changed);
            GameObject badge = EnsureCheckBadge(tf, circle, check, s, ref changed);

            EnsureSiblingOrder(tf, ref changed, "CakeImage", "LockIcon", "Name", "Slices", "Status", "SelectionFrame", "CheckBadge");

            var so = new SerializedObject(card);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderKit.EnsureReference(so, "button", button, ref changed);
            UIBuilderKit.EnsureReference(so, "background", background, ref changed);
            UIBuilderKit.EnsureReference(so, "cakeImage", cake, ref changed);
            UIBuilderKit.EnsureReference(so, "lockIcon", lockIcon, ref changed);
            UIBuilderKit.EnsureReference(so, "nameText", nameText.GetComponent<LocalizedText>(), ref changed);
            UIBuilderKit.EnsureReference(so, "statusText", status.GetComponent<LocalizedText>(), ref changed);
            EnsureReferenceArray(so, "sliceTiles", tiles, ref changed);
            UIBuilderKit.EnsureReference(so, "selectionFrame", frame, ref changed);
            UIBuilderKit.EnsureReference(so, "checkBadge", badge, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        // CakeCabinetSettings.sliceTileCount kadar kutucuk; fazlası (sayı azaltıldıysa) silinmez, kapatılır.
        private static Image[] EnsureSliceTiles(RectTransform card, CakeCabinetSettings s, Sprite rounded, ref bool changed)
        {
            int count = Mathf.Max(1, s.sliceTileCount);
            float step = SliceTileSize + SliceTileGap;
            var rowSize = new Vector2(count * step - SliceTileGap, SliceTileSize);
            RectTransform row = UIBuilderKit.EnsureRectChild(card, "Slices", new Vector2(0f, SlicesY), rowSize, ref changed);

            var tiles = new List<Image>();
            for (int i = 0; i < count; i++)
            {
                var position = new Vector2((i - (count - 1) * 0.5f) * step, 0f);
                Image tile = EnsureImage(row, $"Slice_{i + 1}", position, Vector2.one * SliceTileSize, rounded, s.sliceEmptyColor,
                    true, true, ref changed);
                UIBuilderKit.SetIfDifferent(ref changed, tile.pixelsPerUnitMultiplier, SliceCornerMultiplier,
                    v => tile.pixelsPerUnitMultiplier = v);
                tiles.Add(tile);
            }

            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (child.name.StartsWith("Slice_") && int.TryParse(child.name.Substring("Slice_".Length), out int number) &&
                    number > count)
                {
                    UIBuilderKit.EnsureActive(child.gameObject, false, ref changed);
                }
            }

            return tiles.ToArray();
        }

        // Kartın dışına taşan, içi boş altın çerçeve. Nesne açık, bileşen kapalı kaydedilir; kart seçilince açar.
        private static Image EnsureSelectionFrame(RectTransform card, Sprite rounded, CakeCabinetSettings s, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(card, "SelectionFrame", () =>
            {
                RectTransform created = UIBuilderKit.CreateStretched(card, "SelectionFrame");
                Image image = UIBuilderKit.AddSlicedImage(created.gameObject, rounded, s.selectionColor);
                image.fillCenter = false;
                image.pixelsPerUnitMultiplier = SelectionBorderMultiplier;
                image.raycastTarget = false;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureStretchedRect(tf, -SelectionOutset, ref changed);
            UIBuilderKit.EnsureActive(tf.gameObject, true, ref changed);

            Image frame = tf.GetComponent<Image>();
            UIBuilderKit.SetIfDifferent(ref changed, frame.enabled, false, v => frame.enabled = v);
            return frame;
        }

        // Sağ üst köşede yeşil daire + beyaz ✓ (font yerine sprite: Nunito'da ✓ karakteri yok). Kapalı kaydedilir.
        private static GameObject EnsureCheckBadge(RectTransform card, Sprite circle, Sprite check, CakeCabinetSettings s,
            ref bool changed)
        {
            var size = Vector2.one * CheckBadgeSize;
            RectTransform tf = UIBuilderKit.EnsureChild(card, "CheckBadge", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(card, "CheckBadge", CheckBadgePosition, size);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = circle;
                image.color = s.checkColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
                UIBuilderKit.CreateIcon(created, "Glyph", Vector2.zero, CheckGlyphSize, check, Color.white);
                created.gameObject.SetActive(false);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, CheckBadgePosition, size, ref changed);

            Transform glyph = UIBuilderKit.FindChild(tf, "Glyph");
            if (glyph != null)
            {
                UIBuilderKit.EnsureRect((RectTransform)glyph, Vector2.zero, Vector2.one * CheckGlyphSize, ref changed);
            }

            return tf.gameObject;
        }

        // ------------------------------------------------------------------ Panel

        private static bool EnsurePanel(GameObject root, CakeCabinetSettings s, Sprite rounded, Sprite circle,
            CakeCabinetCard cardPrefab)
        {
            bool changed = false;
            if (root.GetComponent<Canvas>() == null)
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                changed = true;
            }

            CakeCabinetPanel panel = UIBuilderKit.EnsureComponent<CakeCabinetPanel>(root, ref changed);

            // Screen: tam ekran, Show/Hide'ın açıp kapattığı kutu (panelin 'content' alanı).
            RectTransform screen = EnsureScreen(root.transform, ref changed);

            // Tam ekran krem zemin (tasarım alanının dışındaki yan boşlukları da doldurur); tıklamaları yutar.
            RectTransform background = UIBuilderKit.EnsureChild(screen, "Background", () =>
            {
                RectTransform created = UIBuilderKit.CreateStretched(screen, "Background");
                Image image = created.gameObject.AddComponent<Image>();
                image.color = s.backgroundColor;
                image.raycastTarget = true;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureStretchedRect(background, 0f, ref changed);

            RectTransform content = EnsureDesignArea(screen, ref changed);
            EnsureSiblingOrder(screen, ref changed, "Background", ContentName);

            // Eski sürümde Screen'in doğrudan çocukları olan objeler Content'e taşınır (yeniden yaratılmaz, fileID'ler korunur).
            foreach (string childName in ContentChildren)
            {
                Transform legacy = UIBuilderKit.FindChild(screen, childName);
                if (legacy != null && UIBuilderKit.FindChild(content, childName) == null)
                {
                    legacy.SetParent(content, false);
                    changed = true;
                }
            }

            EnsureChef(content, s, rounded, circle, ref changed);
            (ScrollRect scroll, RectTransform list) = EnsureCabinet(content, s, rounded, ref changed);
            EnsureCornice(content, s, rounded, ref changed);
            EnsureImage(content, "HandLeft", new Vector2(-HandOffsetX, HandY), Vector2.one * HandSize, circle, s.chefSkinColor,
                false, true, ref changed);
            EnsureImage(content, "HandRight", new Vector2(HandOffsetX, HandY), Vector2.one * HandSize, circle, s.chefSkinColor,
                false, true, ref changed);
            EnsureBubble(content, s, rounded, circle, ref changed);
            (Button make, LocalizedText makeLabel) = EnsureMakeButton(content, s, rounded, ref changed);

            // Aşçı dolaptan önce (arkada), eller kornişten sonra (önde) çizilir.
            EnsureSiblingOrder(content, ref changed, ContentChildren);

            var so = new SerializedObject(panel);
            UIBuilderKit.EnsureReference(so, "settings", s, ref changed);
            UIBuilderKit.EnsureReference(so, "content", screen.gameObject, ref changed);
            UIBuilderKit.EnsureReference(so, "scrollRect", scroll, ref changed);
            UIBuilderKit.EnsureReference(so, "cardContainer", list, ref changed);
            UIBuilderKit.EnsureReference(so, "cardPrefab", cardPrefab, ref changed);
            UIBuilderKit.EnsureReference(so, "makeButton", make, ref changed);
            UIBuilderKit.EnsureReference(so, "makeLabel", makeLabel, ref changed);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Editörde görünsün diye içerik açık kaydedilir; panel Start'ta showOnStart'a göre açar/kapatır.
            UIBuilderKit.EnsureActive(screen.gameObject, true, ref changed);
            UIBuilderKit.EnsureActive(content.gameObject, true, ref changed);
            return changed;
        }

        // Tam ekran Screen kutusu. Eski sürümün (Background'u içeren) "Content" kutusu varsa yeniden adlandırılır;
        // böylece fileID'si ve panelin 'content' referansı korunur.
        private static RectTransform EnsureScreen(Transform root, ref bool changed)
        {
            if (UIBuilderKit.FindChild(root, ScreenName) == null)
            {
                Transform legacy = UIBuilderKit.FindChild(root, ContentName);
                if (legacy != null && UIBuilderKit.FindChild(legacy, "Background") != null)
                {
                    legacy.name = ScreenName;
                    changed = true;
                }
            }

            return UIBuilderKit.EnsureStretchedChild(root, ScreenName, ref changed);
        }

        // 1920x1080 tasarım alanı: ortaya sabitli; DesignAreaFitter ekrana sığdırıp ölçekler (AspectRatioFitter kullanılmaz:
        // o yalnızca kutuyu küçültür, içeriği ölçeklemez).
        private static RectTransform EnsureDesignArea(RectTransform screen, ref bool changed)
        {
            RectTransform content = UIBuilderKit.EnsureRectChild(screen, ContentName, Vector2.zero, DesignSize, ref changed);
            DesignAreaFitter fitter = UIBuilderKit.EnsureComponent<DesignAreaFitter>(content.gameObject, ref changed);

            var so = new SerializedObject(fitter);
            SerializedProperty size = so.FindProperty("designSize");
            if (size != null && size.vector2Value != DesignSize)
            {
                size.vector2Value = DesignSize;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            return content;
        }

        // Yer tutucu aşçı: kafa (daire), şapka, gözler, yanaklar. Gerçek sprite Chef/Head'e takılıp diğerleri kapatılabilir.
        private static void EnsureChef(RectTransform content, CakeCabinetSettings s, Sprite rounded, Sprite circle, ref bool changed)
        {
            RectTransform chef = UIBuilderKit.EnsureRectChild(content, "Chef", ChefPosition, ChefSize, ref changed);
            EnsureImage(chef, "Head", Vector2.zero, Vector2.one * ChefHeadSize, circle, s.chefSkinColor, false, true, ref changed);
            EnsureImage(chef, "Hat", ChefHatPosition, ChefHatSize, rounded, s.chefHatColor, true, true, ref changed);
            EnsureImage(chef, "EyeLeft", new Vector2(-EyeOffsetX, EyeY), Vector2.one * EyeSize, circle, s.textColor, false, true,
                ref changed);
            EnsureImage(chef, "EyeRight", new Vector2(EyeOffsetX, EyeY), Vector2.one * EyeSize, circle, s.textColor, false, true,
                ref changed);
            EnsureImage(chef, "CheekLeft", new Vector2(-CheekOffsetX, CheekY), Vector2.one * CheekSize, circle, s.chefCheekColor,
                false, true, ref changed);
            EnsureImage(chef, "CheekRight", new Vector2(CheekOffsetX, CheekY), Vector2.one * CheekSize, circle, s.chefCheekColor,
                false, true, ref changed);
            EnsureSiblingOrder(chef, ref changed, "Head", "Hat", "EyeLeft", "EyeRight", "CheekLeft", "CheekRight");
        }

        // Pembe gövde (Cabinet > Inner) > cam (Glass) > raflar + kaydırılan kart ızgarası.
        private static (ScrollRect, RectTransform) EnsureCabinet(RectTransform content, CakeCabinetSettings s, Sprite rounded,
            ref bool changed)
        {
            RectTransform inner = EnsureBorderedBox(content, "Cabinet", CabinetPosition, CabinetSize, CabinetBorder, rounded,
                s.cabinetBorderColor, s.cabinetColor, ref changed);
            RectTransform glass = EnsureImage(inner, "Glass", Vector2.zero, GlassSize, rounded, s.glassColor, true, true,
                ref changed).rectTransform;

            EnsureImage(glass, "ShelfTop", new Vector2(0f, ShelfTopY), new Vector2(GlassSize.x, ShelfHeight), null, s.shelfColor,
                false, true, ref changed);
            EnsureImage(glass, "ShelfBottom", new Vector2(0f, ShelfBottomY), new Vector2(GlassSize.x, ShelfHeight), null,
                s.shelfColor, false, true, ref changed);

            (ScrollRect scroll, RectTransform list) = EnsureCardScroll(glass, ref changed);
            EnsureSiblingOrder(glass, ref changed, "ShelfTop", "ShelfBottom", "CardScroll");
            return (scroll, list);
        }

        // Yatay ScrollRect: Viewport (maske) > Cards (GridLayoutGroup, sabit 2 satır + ContentSizeFitter).
        // Kartlar sığarsa ortada durur; sığmazsa sürükleyerek kaydırılır.
        private static (ScrollRect, RectTransform) EnsureCardScroll(RectTransform glass, ref bool changed)
        {
            RectTransform scrollTf = UIBuilderKit.EnsureChild(glass, "CardScroll", () => CreateGridScroll(glass), ref changed);
            UIBuilderKit.EnsureRect(scrollTf, Vector2.zero, GlassSize, ref changed);
            ScrollRect scroll = scrollTf.GetComponent<ScrollRect>();
            RectTransform list = scroll.content;

            GridLayoutGroup grid = list.GetComponent<GridLayoutGroup>();
            UIBuilderKit.SetIfDifferent(ref changed, grid.cellSize, CardSize, v => grid.cellSize = v);
            UIBuilderKit.SetIfDifferent(ref changed, grid.spacing, GridSpacing, v => grid.spacing = v);
            UIBuilderKit.SetIfDifferent(ref changed, grid.constraint, GridLayoutGroup.Constraint.FixedRowCount,
                v => grid.constraint = v);
            UIBuilderKit.SetIfDifferent(ref changed, grid.constraintCount, GridRows, v => grid.constraintCount = v);
            UIBuilderKit.SetIfDifferent(ref changed, grid.startAxis, GridLayoutGroup.Axis.Horizontal, v => grid.startAxis = v);
            UIBuilderKit.SetIfDifferent(ref changed, grid.startCorner, GridLayoutGroup.Corner.UpperLeft, v => grid.startCorner = v);
            UIBuilderKit.SetIfDifferent(ref changed, grid.childAlignment, TextAnchor.UpperCenter, v => grid.childAlignment = v);

            RectOffset padding = grid.padding;
            if (padding.left != GridPaddingX || padding.right != GridPaddingX || padding.top != GridPaddingY ||
                padding.bottom != GridPaddingY)
            {
                grid.padding = new RectOffset(GridPaddingX, GridPaddingX, GridPaddingY, GridPaddingY);
                changed = true;
            }

            return (scroll, list);
        }

        private static RectTransform CreateGridScroll(Transform parent)
        {
            RectTransform scrollTf = UIBuilderKit.CreateRect(parent, "CardScroll", Vector2.zero, GlassSize);
            ScrollRect scroll = scrollTf.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = UIBuilderKit.CreateStretched(scrollTf, "Viewport");
            Image viewportImage = viewport.gameObject.AddComponent<Image>();

            // Görünmez ama kartların arasındaki boşluktan da sürüklenebilsin.
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var listGo = new GameObject("Cards", typeof(RectTransform));
            var list = (RectTransform)listGo.transform;
            list.SetParent(viewport, false);
            list.anchorMin = new Vector2(0.5f, 0f);
            list.anchorMax = new Vector2(0.5f, 1f);
            list.pivot = new Vector2(0.5f, 0.5f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = Vector2.zero;

            listGo.AddComponent<GridLayoutGroup>();
            ContentSizeFitter fitter = listGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = list;
            scroll.viewport = viewport;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            return scrollTf;
        }

        private static void EnsureCornice(RectTransform content, CakeCabinetSettings s, Sprite rounded, ref bool changed)
        {
            RectTransform inner = EnsureBorderedBox(content, "Cornice", CornicePosition, CorniceSize, CorniceBorder, rounded,
                s.buttonBorderColor, s.corniceColor, ref changed);
            Vector2 titleSize = CorniceSize - Vector2.one * (CorniceBorder * 2f) - new Vector2(40f, 10f);
            UIBuilderKit.EnsureText(inner, "Title", KeyTitle, Vector2.zero, titleSize, TitleFontSize, s.corniceTextColor,
                TextAlignmentOptions.Center, ref changed);
        }

        private static void EnsureBubble(RectTransform content, CakeCabinetSettings s, Sprite rounded, Sprite circle,
            ref bool changed)
        {
            RectTransform inner = EnsureBorderedBox(content, "Bubble", BubblePosition, BubbleSize, BubbleBorder, rounded,
                s.buttonBorderColor, s.bubbleColor, ref changed);
            RectTransform bubble = (RectTransform)inner.parent;

            RectTransform tailLarge = EnsureBorderedBox(bubble, "TailLarge", TailLargePosition, Vector2.one * TailLargeSize,
                TailBorder, circle, s.buttonBorderColor, s.bubbleColor, ref changed);
            RectTransform tailSmall = EnsureBorderedBox(bubble, "TailSmall", TailSmallPosition, Vector2.one * TailSmallSize,
                TailBorder, circle, s.buttonBorderColor, s.bubbleColor, ref changed);
            UseSimpleDrawing(tailLarge, ref changed);
            UseSimpleDrawing(tailSmall, ref changed);

            Vector2 textSize = BubbleSize - Vector2.one * (BubbleBorder * 2f) - new Vector2(30f, 10f);
            UIBuilderKit.EnsureText(inner, "Text", KeyAsk, Vector2.zero, textSize, BubbleFontSize, s.textColor,
                TextAlignmentOptions.Center, ref changed);
        }

        // Kenarlıklı büyük buton: dış Image kenar rengi (tıklamayı alır), Inner buton rengi (Button tonu buna uygulanır).
        private static (Button, LocalizedText) EnsureMakeButton(RectTransform content, CakeCabinetSettings s, Sprite rounded,
            ref bool changed)
        {
            Vector2 innerSize = MakeButtonSize - Vector2.one * (MakeButtonBorder * 2f);
            Vector2 labelSize = innerSize - new Vector2(40f, 10f);
            RectTransform tf = UIBuilderKit.EnsureChild(content, "MakeButton", () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(content, "MakeButton", MakeButtonPosition, MakeButtonSize);
                UIBuilderKit.AddSlicedImage(created.gameObject, rounded, s.buttonBorderColor);

                RectTransform innerTf = UIBuilderKit.CreateRect(created, "Inner", Vector2.zero, innerSize);
                Image innerImage = UIBuilderKit.AddSlicedImage(innerTf.gameObject, rounded, s.buttonColor);
                innerImage.raycastTarget = false;

                Button button = UIBuilderKit.AddButton(created.gameObject, innerImage);
                button.interactable = false;
                UIBuilderKit.CreateText(innerTf, "Label", KeyPick, Vector2.zero, labelSize, MakeFontSize, s.buttonTextColor,
                    TextAlignmentOptions.Center);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, MakeButtonPosition, MakeButtonSize, ref changed);

            // Gölge: butonla aynı boyda, 8 px aşağıda. Çocuk olduğu için butonun kendi (kenar) görselinin üstünde, Inner'ın
            // altında çizilir; kenarla aynı koyu pembe olduğundan butonun altında kalın bir "taban" gibi görünür.
            Image shadow = EnsureImage(tf, "Shadow", new Vector2(0f, -MakeShadowOffset), MakeButtonSize, rounded,
                s.buttonShadowColor, true, true, ref changed);
            UIBuilderKit.SetIfDifferent(ref changed, shadow.raycastTarget, false, v => shadow.raycastTarget = v);

            RectTransform inner = (RectTransform)UIBuilderKit.FindChild(tf, "Inner");
            UIBuilderKit.EnsureRect(inner, Vector2.zero, innerSize, ref changed);
            RectTransform label = (RectTransform)UIBuilderKit.FindChild(inner, "Label");
            UIBuilderKit.EnsureRect(label, Vector2.zero, labelSize, ref changed);
            EnsureSiblingOrder(tf, ref changed, "Shadow", "Inner");

            // Pasif buton yarı saydam olmasın (kenar rengi içinden görünürdü); yalnızca hafif koyulaşır.
            Button make = tf.GetComponent<Button>();
            ColorBlock colors = make.colors;
            if (colors.disabledColor != s.buttonDisabledTint)
            {
                colors.disabledColor = s.buttonDisabledTint;
                make.colors = colors;
                changed = true;
            }

            return (make, label.GetComponent<LocalizedText>());
        }

        // ------------------------------------------------------------------ Yardımcılar

        /// <summary>
        /// Image'lı kutu; yoksa verilen sprite ve renkle üretilir. Var olanın yalnızca yerleşimi eşitlenir
        /// (sonradan takılan sprite ve renk korunur). startActive yalnızca ilk eklenişte uygulanır.
        /// </summary>
        private static Image EnsureImage(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite, Color color,
            bool sliced, bool startActive, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, name, position, size);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = !sliced && sprite != null;
                image.color = color;
                image.raycastTarget = false;
                created.gameObject.SetActive(startActive);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, size, ref changed);
            return tf.GetComponent<Image>();
        }

        // Dış kutu (kenar rengi) > Inner (iç renk, inset kadar küçük). Inner döner; parçalar ona eklenir.
        private static RectTransform EnsureBorderedBox(Transform parent, string name, Vector2 position, Vector2 size, float inset,
            Sprite sprite, Color borderColor, Color innerColor, ref bool changed)
        {
            RectTransform outer = EnsureImage(parent, name, position, size, sprite, borderColor, true, true, ref changed)
                .rectTransform;
            return EnsureImage(outer, "Inner", Vector2.zero, size - Vector2.one * (inset * 2f), sprite, innerColor, true, true,
                ref changed).rectTransform;
        }

        // Daire sprite'ı 9-slice kenarsızdır; dilimli yerine basit çizilir ki halka bozulmasın.
        private static void UseSimpleDrawing(RectTransform inner, ref bool changed)
        {
            foreach (Image image in new[] { inner.parent.GetComponent<Image>(), inner.GetComponent<Image>() })
            {
                if (image != null)
                {
                    UIBuilderKit.SetIfDifferent(ref changed, image.type, Image.Type.Simple, v => image.type = v);
                }
            }
        }

        /// <summary>Adı verilen doğrudan çocukları bu sırayla en başa dizer (arkadan öne çizim sırası).</summary>
        private static void EnsureSiblingOrder(Transform parent, ref bool changed, params string[] names)
        {
            int index = 0;
            foreach (string name in names)
            {
                Transform child = UIBuilderKit.FindChild(parent, name);
                if (child == null)
                {
                    continue;
                }

                if (child.GetSiblingIndex() != index)
                {
                    child.SetSiblingIndex(index);
                    changed = true;
                }

                index++;
            }
        }

        private static void EnsureReferenceArray(SerializedObject so, string property, Object[] values, ref bool changed)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null || !prop.isArray)
            {
                Debug.LogError($"[CakeCabinetUIBuilder] {so.targetObject.GetType().Name}.{property} dizi alanı bulunamadı.",
                    so.targetObject);
                return;
            }

            if (prop.arraySize != values.Length)
            {
                prop.arraySize = values.Length;
                changed = true;
            }

            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != values[i])
                {
                    element.objectReferenceValue = values[i];
                    changed = true;
                }
            }
        }

        // ------------------------------------------------------------------ Yer tutucu sprite'lar

        // Asma kilit: yuvarlak köşeli gövde + üstte yay; gövdede anahtar deliği boş kalır.
        private static Sprite GetOrCreateLockSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(LockSpritePath);
            if (existing != null)
            {
                return existing;
            }

            return WriteShapeSprite(LockSpritePath, (x, y) =>
            {
                bool body = InsideRoundedRect(x, y, 22f, 8f, 106f, 72f, 14f);
                bool keyhole = Sqr(x - 64f) + Sqr(y - 46f) <= Sqr(10f) || (Mathf.Abs(x - 64f) <= 4f && y >= 22f && y <= 46f);

                // Yay: merkezi (64, 84) olan yarım halka + gövdeye inen iki bacak.
                float distance = Mathf.Sqrt(Sqr(x - 64f) + Sqr(y - 84f));
                bool arc = y >= 84f && distance >= 20f && distance <= 32f;
                bool legs = y >= 68f && y < 84f && (Mathf.Abs(x - 38f) <= 6f || Mathf.Abs(x - 90f) <= 6f);
                return (body && !keyhole) || arc || legs;
            });
        }

        // Kalın ✓: iki çizgi parçası (sol kısa kol, sağ uzun kol).
        private static Sprite GetOrCreateCheckSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(CheckSpritePath);
            if (existing != null)
            {
                return existing;
            }

            var a = new Vector2(24f, 66f);
            var b = new Vector2(52f, 36f);
            var c = new Vector2(104f, 94f);
            const float halfWidth = 10f;
            return WriteShapeSprite(CheckSpritePath, (x, y) =>
            {
                var p = new Vector2(x, y);
                return DistanceToSegment(p, a, b) <= halfWidth || DistanceToSegment(p, b, c) <= halfWidth;
            });
        }

        private static float Sqr(float v) => v * v;

        private static bool InsideRoundedRect(float x, float y, float minX, float minY, float maxX, float maxY, float radius)
        {
            if (x < minX || x > maxX || y < minY || y > maxY)
            {
                return false;
            }

            float dx = Mathf.Max(0f, Mathf.Max(minX + radius - x, x - (maxX - radius)));
            float dy = Mathf.Max(0f, Mathf.Max(minY + radius - y, y - (maxY - radius)));
            return dx * dx + dy * dy <= radius * radius;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // Beyaz şekil dokusu (kenarlar Supersample x Supersample örnekle yumuşatılır); Sprite olarak içe aktarılır.
        // Doku koordinatında y yukarı doğrudur (0 alt satır).
        private static Sprite WriteShapeSprite(string path, System.Func<float, float, bool> inside)
        {
            int size = ShapeTextureSize;
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

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (Supersample * Supersample)));
                }
            }

            UIBuilderKit.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
