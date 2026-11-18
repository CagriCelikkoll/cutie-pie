using CutiePie.Core.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace CutiePie.Salon
{
    /// <summary>Servis parçasının türü.</summary>
    public enum ServingItemKind
    {
        Tray = 0,
        Plate = 1,
        CakeSlice = 2,
        Drink = 3
    }

    /// <summary>
    /// Hazırlama alanında taşınan bir servis parçası (tepsi, tabak, pasta dilimi, içecek bardağı) ve verisi.
    /// Görsel: dilim/bardakta ürünün ikonu, tepsi/tabakta SalonSettings.traySprite/plateSprite; yoksa yer tutucu rengi
    /// (OrderBubble ile aynı kural). Görsel SpriteFitter ile parçanın kutusuna sığdırılır; collider ve yuvalar değişmez.
    /// SortingGroup sayesinde alt parçalarıyla birlikte tek blok olarak sıralanır (sürüklerken üste çıkar).
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class ServingItem : MonoBehaviour
    {
        [SerializeField] private ServingItemKind kind;

        [Tooltip("Ürün ikonunun ya da yer tutucu rengin uygulandığı görsel.")]
        [SerializeField] private SpriteRenderer visual;

        [Tooltip("Üstüne konan parçanın oturduğu nokta (ör. tabakta dilim). Boşsa parçanın merkezi.")]
        [SerializeField] private Transform attachPoint;

        private SortingGroup sortingGroup;
        private Sprite placeholderSprite;

        public ServingItemKind Kind => kind;

        public Transform AttachPoint => attachPoint != null ? attachPoint : transform;

        /// <summary>Pasta dilimi veya içecekte ürün; tepsi ve tabakta null.</summary>
        public ProductData Product { get; private set; }

        public SortingGroup SortingGroup
        {
            get
            {
                if (sortingGroup == null)
                {
                    sortingGroup = GetComponent<SortingGroup>();
                }

                return sortingGroup;
            }
        }

        private void Awake()
        {
            placeholderSprite = visual != null ? visual.sprite : null;
        }

        /// <summary>Parçayı ürünle kurar ve görünümünü uygular. Prefab'tan üretildikten hemen sonra çağrılır.</summary>
        public void Setup(ProductData product, SalonSettings settings)
        {
            Product = product;
            ApplyAppearance(settings);
        }

        private void ApplyAppearance(SalonSettings settings)
        {
            if (visual == null)
            {
                return;
            }

            // Gerçek görsel oranı korunarak parçanın kutusuna sığar; yer tutucu kutuyu doldurur.
            Sprite art = GetArtSprite(settings);
            if (art != null)
            {
                SpriteFitter.Apply(visual, art, Color.white, true);
                return;
            }

            Color color = settings != null ? GetPlaceholderColor(settings) : visual.color;
            SpriteFitter.Apply(visual, placeholderSprite, color, false);
        }

        /// <summary>Dilim ve bardakta ürünün ikonu; tepsi ve tabakta SalonSettings'teki görsel. Yoksa null (yer tutucu).</summary>
        private Sprite GetArtSprite(SalonSettings settings)
        {
            switch (kind)
            {
                case ServingItemKind.Tray: return settings != null ? settings.traySprite : null;
                case ServingItemKind.Plate: return settings != null ? settings.plateSprite : null;
                default: return Product != null ? Product.icon : null;
            }
        }

        private Color GetPlaceholderColor(SalonSettings settings)
        {
            switch (kind)
            {
                case ServingItemKind.Tray: return settings.trayColor;
                case ServingItemKind.Plate: return settings.plateColor;
                default: return settings.GetProductPlaceholderColor(Product);
            }
        }
    }
}
