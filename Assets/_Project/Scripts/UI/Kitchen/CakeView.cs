using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Bir CakeBuild'i üstten görünümde çizen tek bileşen (Prefabs/UI/Kitchen/CakeView.prefab).
    /// Fişteki küçük hedef pasta ile yapım ekranındaki büyük pasta aynı prefab'ı kullanır; her şey kökün boyutuna oranlıdır.
    /// Katmanlar (alttan üste): kutu (yuvarlak köşeli kare) → taban (daire) → krema (daire) → süs yüzeyi → süsler.
    /// Süs yüzeyi (Surface) kremayla aynı karedir ve CakeBuild sözleşmesinin referans dikdörtgenidir:
    /// (0,0) sol alt, (1,1) sağ üst, konum süsün merkezidir. Süsler yüzeye anchor ile oturur, boyut değişse de yerleri kaymaz.
    /// Parçanın sprite'ı varsa o çizilir; yoksa prefab'daki yer tutucu şekil, parçanın rengiyle boyanır.
    /// Seçilmemiş taban/krema soluk yer tutucuyla, seçilmemiş kutu hiç çizilmez.
    /// Düzenlenebilir modda (SetInteractive) konulmuş süse dokunulunca DecorationTapped yayınlanır;
    /// TryGetCakePosition ekran noktasını pasta koordinatına çevirir (yapım ekranında süs bırakma).
    /// </summary>
    public class CakeView : MonoBehaviour
    {
        [SerializeField] private OrderUISettings settings;

        [SerializeField] private Image boxImage;
        [SerializeField] private Image baseImage;
        [SerializeField] private Image creamImage;

        [Tooltip("Süslerin konduğu kare yüzey (CakeBuild pasta koordinatının referans dikdörtgeni).")]
        [SerializeField] private RectTransform surface;

        [Tooltip("Süs görseli şablonu (kapalı durur, kopyalanır).")]
        [SerializeField] private Image decorationTemplate;

        private readonly List<Image> decorations = new List<Image>();
        private readonly List<CakeDecorationHandle> handles = new List<CakeDecorationHandle>();
        private bool interactive;

        /// <summary>
        /// Düzenlenebilir modda konulmuş bir süse dokunuldu; değer CakeBuild.decorations içindeki sırasıdır.
        /// </summary>
        public event Action<int> DecorationTapped;

        // Prefab'daki yer tutucu şekiller (sprite'ı olmayan parça bunlarla çizilir).
        private PartLook boxLook;
        private PartLook baseLook;
        private PartLook creamLook;
        private PartLook decorationLook;
        private bool defaultsCaptured;

        /// <summary>Süs yüzeyi; ekran noktasını pasta koordinatına çevirmek için kullanılır.</summary>
        public RectTransform Surface => surface;

        /// <summary>Süs görselinin süs yüzeyine oranı (0–1).</summary>
        public float DecorationSizeRatio => settings != null ? settings.decorationSizeRatio : DefaultDecorationRatio;

        private void Awake() => CaptureDefaults();

        private void OnDestroy()
        {
            foreach (CakeDecorationHandle handle in handles)
            {
                if (handle != null)
                {
                    handle.Tapped -= HandleDecorationTapped;
                }
            }
        }

        /// <summary>Düzenlenebilir modda konulmuş süslere dokunulabilir (DecorationTapped); değilse süsler tıklama almaz.</summary>
        public void SetInteractive(bool value)
        {
            interactive = value;
            foreach (Image image in decorations)
            {
                if (image != null)
                {
                    image.raycastTarget = value;
                }
            }
        }

        /// <summary>
        /// Ekran noktasını pasta koordinatına çevirir (CakeBuild sözleşmesi): yüzeyin yerel noktası
        /// (ScreenPointToLocalPointInRectangle) → Rect.PointToNormalized, sonuç 0–1'e sınırlı.
        /// Nokta yüzeyin dışında ama yarım süs boyu içindeyse kabul edilip sınırlanır; daha uzaksa false döner.
        /// camera: Screen Space Overlay Canvas'ta null.
        /// </summary>
        public bool TryGetCakePosition(Vector2 screenPoint, Camera camera, out Vector2 position)
        {
            position = Vector2.zero;
            if (surface == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screenPoint, camera, out Vector2 local))
            {
                return false;
            }

            Rect rect = surface.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return false;
            }

            // Kabul sınırı için sınırlanmamış oran; yerleştirilen değer sözleşmedeki gibi PointToNormalized ile (0–1) alınır.
            float margin = DecorationSizeRatio * 0.5f;
            float x = (local.x - rect.xMin) / rect.width;
            float y = (local.y - rect.yMin) / rect.height;
            if (x < -margin || x > 1f + margin || y < -margin || y > 1f + margin)
            {
                return false;
            }

            Vector2 normalized = Rect.PointToNormalized(rect, local);
            position = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
            return true;
        }

        /// <summary>Verilen görsele süs parçasının görünümünü uygular (ör. sürüklenen süs); pastadaki süslerle aynı görünür.</summary>
        public void ApplyDecorationLook(Image image, CakePartData part)
        {
            CaptureDefaults();
            if (part != null)
            {
                ApplyPart(image, part.sprite, decorationLook, part.color);
            }
        }

        /// <summary>Pastayı çizer. build null ise boş pasta çizilir; kitchen null ise parçalar bulunamaz (yer tutucu kalır).</summary>
        public void Render(CakeBuild build, KitchenSettings kitchen)
        {
            CaptureDefaults();
            ApplyLayout();

            build ??= EmptyBuild;

            // Kutu yalnızca seçildiyse görünür; taban ve krema seçilmediyse soluk yer tutucu.
            CakePartData box = Find(kitchen, build.boxId);
            KitchenUIUtil.SetActive(boxImage, box != null);
            if (box != null)
            {
                Color boxColor = box.color;
                boxColor.a *= settings != null ? settings.boxAlpha : 1f;
                ApplyPart(boxImage, box.sprite, boxLook, boxColor);
            }

            ApplyPart(baseImage, kitchen, build.baseId, baseLook);
            ApplyPart(creamImage, kitchen, build.creamId, creamLook);

            RenderDecorations(build.decorations, kitchen);
        }

        // ------------------------------------------------------------------ Süsler

        private void RenderDecorations(List<DecorationPlacement> list, KitchenSettings kitchen)
        {
            int shown = 0;
            if (list != null && surface != null && decorationTemplate != null)
            {
                float half = DecorationSizeRatio * 0.5f;
                for (int index = 0; index < list.Count; index++)
                {
                    DecorationPlacement placement = list[index];
                    CakePartData part = placement != null ? Find(kitchen, placement.decorationId) : null;
                    if (part == null)
                    {
                        continue;
                    }

                    Image image = GetDecoration(shown);
                    handles[shown].DecorationIndex = index;
                    image.raycastTarget = interactive;
                    shown++;

                    // Konum süsün merkezidir: anchor kutusu merkez ± yarım boy; offset sıfır, yüzeyle birlikte ölçeklenir.
                    Vector2 center = new Vector2(Mathf.Clamp01(placement.position.x), Mathf.Clamp01(placement.position.y));
                    RectTransform tf = image.rectTransform;
                    tf.anchorMin = center - new Vector2(half, half);
                    tf.anchorMax = center + new Vector2(half, half);
                    tf.offsetMin = tf.offsetMax = Vector2.zero;
                    tf.SetAsLastSibling();

                    ApplyPart(image, part.sprite, decorationLook, part.color);
                    KitchenUIUtil.SetActive(image, true);
                }
            }

            for (int i = shown; i < decorations.Count; i++)
            {
                KitchenUIUtil.SetActive(decorations[i], false);
            }
        }

        private Image GetDecoration(int index)
        {
            while (decorations.Count <= index)
            {
                Image copy = Instantiate(decorationTemplate, surface, false);
                copy.name = $"Decoration_{decorations.Count + 1}";
                CakeDecorationHandle handle = copy.GetComponent<CakeDecorationHandle>();
                if (handle == null)
                {
                    handle = copy.gameObject.AddComponent<CakeDecorationHandle>();
                }

                handle.Tapped += HandleDecorationTapped;
                decorations.Add(copy);
                handles.Add(handle);
            }

            return decorations[index];
        }

        private void HandleDecorationTapped(CakeDecorationHandle handle)
        {
            if (interactive && handle != null && handle.DecorationIndex >= 0)
            {
                DecorationTapped?.Invoke(handle.DecorationIndex);
            }
        }

        // ------------------------------------------------------------------ Yerleşim

        // Oranlar ayarlardan her çizimde uygulanır; asset değişince prefab'ı yeniden kurmak gerekmez.
        private void ApplyLayout()
        {
            if (settings == null)
            {
                return;
            }

            SetCentered(baseImage != null ? baseImage.rectTransform : null, settings.baseSizeRatio);
            SetCentered(creamImage != null ? creamImage.rectTransform : null, settings.creamSizeRatio);
            SetCentered(surface, settings.creamSizeRatio);
        }

        private static void SetCentered(RectTransform tf, float ratio)
        {
            if (tf == null)
            {
                return;
            }

            float min = (1f - ratio) * 0.5f;
            tf.anchorMin = new Vector2(min, min);
            tf.anchorMax = new Vector2(1f - min, 1f - min);
            tf.offsetMin = tf.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ Parça görseli

        private void ApplyPart(Image image, KitchenSettings kitchen, string partId, PartLook look)
        {
            CakePartData part = Find(kitchen, partId);
            if (part != null)
            {
                ApplyPart(image, part.sprite, look, part.color);
            }
            else
            {
                ApplyPart(image, null, look, settings != null ? settings.missingPartColor : Color.clear);
            }
        }

        // Gerçek görsel varsa beyazla (renk katmadan) çizilir; yoksa yer tutucu şekil parçanın rengini alır.
        private static void ApplyPart(Image image, Sprite sprite, PartLook look, Color placeholderColor)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.sprite = look.Sprite;
                image.type = look.Type;
                image.preserveAspect = false;
                image.color = placeholderColor;
            }
        }

        private static CakePartData Find(KitchenSettings kitchen, string partId) =>
            kitchen != null ? kitchen.FindPart(partId) : null;

        private void CaptureDefaults()
        {
            if (defaultsCaptured)
            {
                return;
            }

            defaultsCaptured = true;
            boxLook = PartLook.From(boxImage);
            baseLook = PartLook.From(baseImage);
            creamLook = PartLook.From(creamImage);
            decorationLook = PartLook.From(decorationTemplate);

            if (decorationTemplate != null)
            {
                decorationTemplate.gameObject.SetActive(false);
            }
        }

        // Ayar asset'i atanmamışsa kullanılan süs oranı (yalnızca güvenlik; asıl değer OrderUISettings'te).
        private const float DefaultDecorationRatio = 0.2f;

        private static readonly CakeBuild EmptyBuild = new CakeBuild();

        /// <summary>Prefab'daki yer tutucu şekil: sprite ve çizim türü.</summary>
        private readonly struct PartLook
        {
            public readonly Sprite Sprite;
            public readonly Image.Type Type;

            private PartLook(Sprite sprite, Image.Type type)
            {
                Sprite = sprite;
                Type = type;
            }

            public static PartLook From(Image image) =>
                image != null ? new PartLook(image.sprite, image.type) : new PartLook(null, Image.Type.Simple);
        }
    }
}
