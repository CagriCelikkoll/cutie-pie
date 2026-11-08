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
    /// Görsel: ürünün ikonu varsa o, yoksa SalonSettings'teki yer tutucu rengi (OrderBubble ile aynı kural).
    /// SortingGroup sayesinde alt parçalarıyla birlikte tek blok olarak sıralanır (sürüklerken üste çıkar).
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class ServingItem : MonoBehaviour
    {
        [SerializeField] private ServingItemKind kind;

        [Tooltip("Ürün ikonunun ya da yer tutucu rengin uygulandığı görsel.")]
        [SerializeField] private SpriteRenderer visual;

        private SortingGroup sortingGroup;
        private Sprite placeholderSprite;

        public ServingItemKind Kind => kind;

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

            if (Product != null && Product.icon != null)
            {
                visual.sprite = Product.icon;
                visual.color = Color.white;
                return;
            }

            visual.sprite = placeholderSprite;
            if (settings != null)
            {
                visual.color = GetPlaceholderColor(settings);
            }
        }

        private Color GetPlaceholderColor(SalonSettings settings)
        {
            switch (kind)
            {
                case ServingItemKind.Tray: return settings.trayColor;
                case ServingItemKind.Plate: return settings.plateColor;
                case ServingItemKind.CakeSlice: return settings.cakePlaceholderColor;
                default: return settings.drinkPlaceholderColor;
            }
        }
    }
}
