using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>İkonu olmayan bir ürünün yer tutucu rengi (SalonSettings listesinde).</summary>
    [System.Serializable]
    public struct ProductPlaceholderColor
    {
        public ProductData product;
        public Color color;
    }

    /// <summary>
    /// Salon'a özel denge ve görünüm ayarları. Koddaki tüm Salon değerleri buradan okunur.
    /// </summary>
    [CreateAssetMenu(fileName = "SalonSettings", menuName = "Cutie Pie/Salon/Salon Settings")]
    public class SalonSettings : ScriptableObject
    {
        [Header("Müşteri üretimi")]
        [Tooltip("İki müşteri denemesi arası en kısa süre (saniye). Geliş aralığı LevelData.spawnIntervalCurve'den gelir; " +
                 "eğri bundan küçük (veya 0) değer verirse bu kullanılır.")]
        [Min(0.1f)] public float minSpawnIntervalSeconds = 1f;

        [Tooltip("Üretilecek müşteri tipleri; her müşteri için rastgele biri seçilir.")]
        public List<CustomerTypeData> customerTypes = new List<CustomerTypeData>();

        [Header("Hareket")]
        [Tooltip("Müşterinin yürüme hızı (birim/saniye).")]
        [Min(0.1f)] public float walkSpeed = 3f;

        [Tooltip("Hedefe bu mesafeden yakınsa varılmış sayılır (birim).")]
        [Min(0.001f)] public float seatReachDistance = 0.05f;

        [Header("Sabır barı")]
        [Tooltip("Oran bu değerin altına inince bar sarı olur.")]
        [Range(0f, 1f)] public float patienceYellowThreshold = 0.5f;

        [Tooltip("Oran bu değerin altına inince bar kırmızı olur.")]
        [Range(0f, 1f)] public float patienceRedThreshold = 0.25f;

        public Color patienceGreenColor = new Color(0.35f, 0.8f, 0.4f, 1f);
        public Color patienceYellowColor = new Color(0.98f, 0.8f, 0.25f, 1f);
        public Color patienceRedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        public Color patienceBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [Header("Sipariş balonu")]
        [Tooltip("Ürün ikonu yokken pasta için kullanılan yer tutucu rengi.")]
        public Color cakePlaceholderColor = new Color(0.85f, 0.55f, 0.35f, 1f);

        [Tooltip("Ürün ikonu yokken içecek için kullanılan yer tutucu rengi.")]
        public Color drinkPlaceholderColor = new Color(0.45f, 0.7f, 0.95f, 1f);

        [Tooltip("İkonu olmayan ürünleri birbirinden ayırmak için ürüne özel yer tutucu renkleri. Listede olmayan ürün kategori rengini alır.")]
        public List<ProductPlaceholderColor> productPlaceholderColors = new List<ProductPlaceholderColor>();

        [Header("Hazırlama alanı")]
        [Tooltip("Bırakılan parça, hedefin oturma noktasına bu mesafeden yakınsa hedefe oturur (birim). Cömert tutulur.")]
        [Min(0.1f)] public float snapDistance = 1.5f;

        [Tooltip("Hedefe oturmayan parçanın başlangıç yerine dönüş hızı (birim/saniye).")]
        [Min(0.1f)] public float dragReturnSpeed = 25f;

        [Tooltip("Sürüklenen parçanın büyüme çarpanı (elde tutuluyor hissi).")]
        [Min(0.1f)] public float dragItemScale = 1.1f;

        [Tooltip("Tezgâhtaki tepsi yeri sayısı. Geliştirmeyle değişmez.")]
        [Min(1)] public int prepTraySlotCount = 1;

        [Tooltip("Vitrin ve içecek rafında sayfa başına ürün yeri.")]
        [Min(1)] public int shelfSlotsPerPage = 4;

        [Tooltip("Round başında menüdeki her pasta için vitrindeki dilim sayısı (Faz 1: round içinde yenilenmez).")]
        [Min(0)] public int startingSlicesPerCake = 15;

        [Tooltip("Müşteriye dokunma alanı (birim); gövdeden büyük tutulur.")]
        public Vector2 customerTapSize = new Vector2(1.3f, 1.6f);

        [Tooltip("İkonu olmayan tepsinin yer tutucu rengi.")]
        public Color trayColor = new Color(0.7f, 0.5f, 0.35f, 1f);

        [Tooltip("İkonu olmayan tabağın yer tutucu rengi.")]
        public Color plateColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Fişte tamamlanan satırın tik rengi.")]
        public Color ticketTickColor = new Color(0.3f, 0.75f, 0.35f, 1f);

        [Tooltip("Dilimi biten vitrin yuvasının saydamlığı (0-1).")]
        [Range(0f, 1f)] public float slotEmptyAlpha = 0.35f;

        [Header("Teslim")]
        [Tooltip("Bahşiş = ödeme × bu oran × müşteri tipinin bahşiş çarpanı (yalnızca sabır yeşilken).")]
        [Min(0f)] public float tipPaymentRatio = 0.2f;

        [Tooltip("Doğru teslimden sonra müşterinin yemek yeme süresi (saniye).")]
        [Min(0f)] public float eatDurationSeconds = 3f;

        [Tooltip("Masaya konan tepsinin ölçeği (tepsi masadan büyük olduğu için küçültülür).")]
        [Min(0.1f)] public float trayOnTableScale = 0.6f;

        [Tooltip("Yanlış siparişte müşterinin sallanma süresi (saniye).")]
        [Min(0f)] public float rejectShakeDuration = 0.4f;

        [Tooltip("Sallanmanın yatay genliği (birim).")]
        [Min(0f)] public float rejectShakeAmplitude = 0.12f;

        [Tooltip("Sallanma hızı (radyan/saniye).")]
        [Min(0f)] public float rejectShakeFrequency = 30f;

        [Tooltip("Altın yazısının ekranda kalma süresi (saniye).")]
        [Min(0.1f)] public float popupDuration = 1f;

        [Tooltip("Altın yazısının bu süre boyunca yükseldiği mesafe (birim).")]
        public float popupRiseDistance = 0.8f;

        [Tooltip("Altın yazısının müşteri merkezine göre başlangıç konumu (birim).")]
        public Vector2 popupOffset = new Vector2(0f, 1.9f);

        [Tooltip("Bahşiş yazısının ödeme yazısına göre kayması (birim).")]
        public Vector2 tipPopupOffset = new Vector2(0.9f, -0.35f);

        public Color goldPopupColor = new Color(0.95f, 0.75f, 0.1f, 1f);
        public Color tipPopupColor = new Color(0.35f, 0.8f, 0.4f, 1f);

        [Header("Masa temizliği")]
        [Tooltip("Temizlik parıltısının süresi (saniye); bitince masa boşalır.")]
        [Min(0.05f)] public float cleanSparkleDuration = 0.35f;

        [Tooltip("Parıltının sonunda ulaştığı ölçek (masa boyutuna göre çarpan).")]
        [Min(0.1f)] public float cleanSparkleMaxScale = 1.3f;

        [Tooltip("Parıltı rengi; saydamlığı süre boyunca bu değerden 0'a iner.")]
        public Color cleanSparkleColor = new Color(1f, 0.97f, 0.7f, 0.9f);

        [Header("Masa renkleri")]
        public Color tableLockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        public Color tableEmptyColor = new Color(0.8f, 0.6f, 0.4f, 1f);
        public Color tableOccupiedColor = new Color(0.7f, 0.5f, 0.3f, 1f);
        public Color tableDirtyColor = new Color(0.6f, 0.55f, 0.35f, 1f);

        /// <summary>İkonu olmayan ürünün yer tutucu rengi: önce ürüne özel renk, yoksa kategori rengi.</summary>
        public Color GetProductPlaceholderColor(ProductData product)
        {
            if (product == null)
            {
                return cakePlaceholderColor;
            }

            foreach (ProductPlaceholderColor entry in productPlaceholderColors)
            {
                if (entry.product == product)
                {
                    return entry.color;
                }
            }

            return product.category == ProductCategory.Drink ? drinkPlaceholderColor : cakePlaceholderColor;
        }

        /// <summary>Masa durumuna karşılık gelen gövde rengi.</summary>
        public Color GetTableColor(TableState state)
        {
            switch (state)
            {
                case TableState.Empty: return tableEmptyColor;
                case TableState.Occupied: return tableOccupiedColor;
                case TableState.Dirty: return tableDirtyColor;
                default: return tableLockedColor;
            }
        }

        /// <summary>Sabır barı yeşil mi? (Bahşiş yalnızca yeşilde verilir; bar rengiyle aynı kural.)</summary>
        public bool IsPatienceGreen(float ratio) => ratio > patienceYellowThreshold;

        /// <summary>Sabır oranına (0-1) göre bar rengi: yeşil → sarı → kırmızı.</summary>
        public Color GetPatienceColor(float ratio)
        {
            if (ratio <= patienceRedThreshold)
            {
                return patienceRedColor;
            }

            return ratio <= patienceYellowThreshold ? patienceYellowColor : patienceGreenColor;
        }

        private void OnValidate()
        {
            // Kırmızı eşik sarıdan büyük olamaz.
            if (patienceRedThreshold > patienceYellowThreshold)
            {
                patienceRedThreshold = patienceYellowThreshold;
            }
        }
    }
}
