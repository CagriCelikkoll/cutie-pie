using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'a özel denge ve görünüm ayarları. Koddaki tüm Salon değerleri buradan okunur.
    /// </summary>
    [CreateAssetMenu(fileName = "SalonSettings", menuName = "Cutie Pie/Salon/Salon Settings")]
    public class SalonSettings : ScriptableObject
    {
        [Header("Müşteri üretimi")]
        [Tooltip("İki müşteri denemesi arasındaki sabit süre (saniye). G4'te LevelData.spawnIntervalCurve ile değiştirilecek.")]
        [Min(0.1f)] public float spawnIntervalSeconds = 6f;

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

        [Header("Masa renkleri")]
        public Color tableLockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        public Color tableEmptyColor = new Color(0.8f, 0.6f, 0.4f, 1f);
        public Color tableOccupiedColor = new Color(0.7f, 0.5f, 0.3f, 1f);
        public Color tableDirtyColor = new Color(0.6f, 0.55f, 0.35f, 1f);

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
