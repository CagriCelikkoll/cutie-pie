using System.IO;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: UI builder'larının ortak parçaları. Pastel, yuvarlak köşeli kart/buton/metin üretir;
    /// yer tutucu sprite'ları (yuvarlak köşeli kutu, yıldız, daire) ve RoundEndSettings asset'ini yoksa oluşturur.
    /// Tüm metinler Türkçe karakterli Nunito TMP fontlarıyla yazılır (Art/Fonts).
    /// Buradaki sayılar yalnızca yerleşimdir (1920x1080 referans); renkler RoundEndSettings'ten gelir.
    /// </summary>
    internal static class UIBuilderKit
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        public const string KitchenPrefabFolder = PrefabFolder + "/Kitchen";
        public const string SettingsPath = "Assets/_Project/Data/Salon/RoundEndSettings.asset";

        private const string PlaceholderFolder = "Assets/_Project/Art/Salon/Placeholders";
        private const string RoundedSpritePath = PlaceholderFolder + "/RoundedRect.png";
        private const string StarSpritePath = PlaceholderFolder + "/Star.png";
        private const string CircleSpritePath = PlaceholderFolder + "/Circle.png";

        private const string BoldFontPath = "Assets/_Project/Art/Fonts/Nunito-Bold SDF.asset";
        private const string RegularFontPath = "Assets/_Project/Art/Fonts/Nunito-Regular SDF.asset";

        // Yuvarlak köşeli kutu dokusu: 9-slice kenarı köşe yarıçapından biraz geniş tutulur.
        private const int RoundedTextureSize = 64;
        private const int RoundedCornerRadius = 26;
        private const int RoundedSliceBorder = 28;
        private const int StarTextureSize = 128;
        private const int StarPoints = 5;
        private const float StarInnerRatio = 0.48f;
        private const int CircleTextureSize = 128;
        private const int Supersample = 4;
        private const float SpritePixelsPerUnit = 100f;

        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        public const float MatchWidthOrHeight = 0.5f;
        public const float CardBorderWidth = 14f;
        public const float MinAutoFontSize = 24f;

        // Buton tonları (Image rengi butonun kendi rengidir; bunlar onun üstüne çarpılır).
        private static readonly Color ButtonHighlighted = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ButtonPressed = new Color(0.82f, 0.82f, 0.82f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        // ------------------------------------------------------------------ Canvas

        /// <summary>
        /// Kökü tam ekran Canvas yapar. Kök Canvas'ın RectTransform'unu Unity sürer ve prefab'a 0 ölçek / 0 boyutla kaydeder;
        /// prefab başka bir Canvas'ın içine konacaksa örneğe PrepareNestedCanvas uygulanmalıdır.
        /// </summary>
        public static void SetupCanvas(GameObject root, int sortingOrder)
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
            root.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// Canvas'lı bir prefab örneğini başka bir Canvas'ın içinde alt Canvas olarak hazırlar.
        /// Alt Canvas'ın RectTransform'u sürülmez: kayıttaki 0 ölçek / 0 boyut kalırsa panel görünmez ve tıklama almaz.
        /// Bu yüzden üst Canvas'a tam yayılır; overrideSorting yalnızca alt Canvas'ta geçerli olduğu için burada açılır.
        /// </summary>
        public static void PrepareNestedCanvas(GameObject instance, int sortingOrder)
        {
            var tf = (RectTransform)instance.transform;
            tf.localScale = Vector3.one;
            tf.localRotation = Quaternion.identity;
            tf.anchorMin = Vector2.zero;
            tf.anchorMax = Vector2.one;
            tf.pivot = new Vector2(0.5f, 0.5f);
            tf.anchoredPosition3D = Vector3.zero;
            tf.offsetMin = tf.offsetMax = Vector2.zero;

            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
            }
        }

        // ------------------------------------------------------------------ Parçalar

        public static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var tf = (RectTransform)go.transform;
            tf.SetParent(parent, false);
            tf.anchorMin = tf.anchorMax = tf.pivot = new Vector2(0.5f, 0.5f);
            tf.anchoredPosition = position;
            tf.sizeDelta = size;
            return tf;
        }

        public static RectTransform CreateStretched(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var tf = (RectTransform)go.transform;
            tf.SetParent(parent, false);
            tf.anchorMin = Vector2.zero;
            tf.anchorMax = Vector2.one;
            tf.offsetMin = tf.offsetMax = Vector2.zero;
            return tf;
        }

        public static Image AddSlicedImage(GameObject go, Sprite sprite, Color color)
        {
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        /// <summary>Basit (dilimlenmemiş) simge görseli; tıklamaları yakalamaz.</summary>
        public static Image CreateIcon(Transform parent, string name, Vector2 position, float size, Sprite sprite, Color color)
        {
            RectTransform tf = CreateRect(parent, name, position, new Vector2(size, size));
            Image image = tf.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Tam ekran perde; tıklamaları yutar, alttaki ekrana dokunulmaz.</summary>
        public static void CreateDim(Transform parent, RoundEndSettings settings)
        {
            RectTransform dim = CreateStretched(parent, "Dim");
            Image image = dim.gameObject.AddComponent<Image>();
            image.color = settings.dimColor;
            image.raycastTarget = true;
        }

        /// <summary>Kenarlıklı, yuvarlak köşeli kart; içindeki parçalar dönen RectTransform'a eklenir.</summary>
        public static RectTransform CreateCard(Transform parent, string name, Vector2 size, Sprite rounded, RoundEndSettings settings)
        {
            RectTransform border = CreateRect(parent, name, Vector2.zero, size + Vector2.one * (CardBorderWidth * 2f));
            AddSlicedImage(border.gameObject, rounded, settings.cardBorderColor);

            RectTransform card = CreateRect(border, "Inner", Vector2.zero, size);
            AddSlicedImage(card.gameObject, rounded, settings.cardColor);
            return card;
        }

        public static Button CreateButton(Transform parent, string name, string key, Vector2 position, Vector2 size,
            float fontSize, Color color, Sprite rounded, RoundEndSettings settings)
        {
            RectTransform tf = CreateRect(parent, name, position, size);
            Image image = AddSlicedImage(tf.gameObject, rounded, color);
            Button button = AddButton(tf.gameObject, image);

            CreateText(tf, "Label", key, Vector2.zero, size, fontSize, settings.buttonTextColor, TextAlignmentOptions.Center);
            return button;
        }

        /// <summary>Var olan bir nesneye ortak tonlu, seçili kalmayan Button ekler (ör. tamamı dokunulabilir kart).</summary>
        public static Button AddButton(GameObject go, Image target)
        {
            Button button = go.AddComponent<Button>();
            button.targetGraphic = target;
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
            return button;
        }

        /// <summary>
        /// Nunito TMP metni. key null ise düz metindir (kod yazar, ör. sayılar);
        /// boş olmayan ya da boş ("") key verilirse LocalizedText eklenir ve metin çalışma zamanında yerelleştirme dosyasından gelir
        /// (boş key: anahtarı kod sonradan SetKey ile verir).
        /// </summary>
        public static TMP_Text CreateText(Transform parent, string name, string key, Vector2 position, Vector2 size,
            float fontSize, Color color, TextAlignmentOptions alignment, bool bold = true)
        {
            RectTransform tf = CreateRect(parent, name, position, size);
            TextMeshProUGUI text = tf.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = bold ? BoldFont : RegularFont;

            // Kalınlık font dosyasından gelir; sahte kalın stil kullanılmaz.
            text.fontStyle = FontStyles.Normal;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(MinAutoFontSize, fontSize);
            text.fontSizeMax = fontSize;
            text.fontSize = fontSize;
            text.raycastTarget = false;

            if (key != null)
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

        /// <summary>
        /// Yatay ScrollRect: Viewport (maske) > içerik (HorizontalLayoutGroup + ContentSizeFitter).
        /// Öğeler alana sığarsa ortada durur; sığmazsa sürükleyerek kaydırılır. Dönen RectTransform öğelerin ekleneceği içeriktir.
        /// </summary>
        public static (ScrollRect, RectTransform) CreateHorizontalScroll(Transform parent, string name, string contentName,
            Vector2 position, Vector2 size, int paddingX, int paddingY, float spacing)
        {
            RectTransform scrollTf = CreateRect(parent, name, position, size);
            ScrollRect scroll = scrollTf.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = CreateStretched(scrollTf, "Viewport");
            Image viewportImage = viewport.gameObject.AddComponent<Image>();

            // Görünmez ama öğelerin arasındaki boşluktan da sürüklenebilsin.
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var listGo = new GameObject(contentName, typeof(RectTransform));
            var list = (RectTransform)listGo.transform;
            list.SetParent(viewport, false);
            list.anchorMin = new Vector2(0.5f, 0f);
            list.anchorMax = new Vector2(0.5f, 1f);
            list.pivot = new Vector2(0.5f, 0.5f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup layout = listGo.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = listGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = list;
            scroll.viewport = viewport;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbar = null;

            return (scroll, list);
        }

        // ------------------------------------------------------------------ Fontlar

        public static TMP_FontAsset BoldFont => LoadFont(BoldFontPath);

        public static TMP_FontAsset RegularFont => LoadFont(RegularFontPath);

        private static TMP_FontAsset LoadFont(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                return font;
            }

            Debug.LogWarning($"[UIBuilderKit] '{path}' bulunamadı; TMP varsayılan fontu kullanılıyor.");
            return TMP_Settings.defaultFontAsset;
        }

        // ------------------------------------------------------------------ Asset'ler

        public static RoundEndSettings GetOrCreateSettings()
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

        public static Sprite GetOrCreateRoundedSprite()
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

        public static Sprite GetOrCreateStarSprite()
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

        /// <summary>Altın simgesi için daire; Salon builder'ının ürettiği Circle.png varsa o kullanılır.</summary>
        public static Sprite GetOrCreateCircleSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            if (existing != null)
            {
                return existing;
            }

            float center = CircleTextureSize * 0.5f;
            float radius = center * 0.96f;
            return WriteSprite(CircleSpritePath, CircleTextureSize, (x, y) =>
            {
                float dx = x - center;
                float dy = y - center;
                return dx * dx + dy * dy <= radius * radius;
            }, Vector4.zero);
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

        public static void EnsureFolder(string folder)
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
