using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// GEÇİCİ yerel vitrin stoğu (Faz 1: Mutfak yok). Round başında menüdeki her pastaya
    /// SalonSettings.startingSlicesPerCake dilim yazar; round içinde yenilenmez, kaydedilmez.
    /// Faz 2'de StockManager adaptörüyle değişecek (IShowcaseStock arayüzü aynı kalır).
    /// SalonRound her GameEvents.RoundStarted'da doldurur.
    /// İçeceklerde stok yoktur (sınırsız).
    /// </summary>
    public class LocalShowcaseStock : MonoBehaviour, IShowcaseStock
    {
        [SerializeField] private SalonSettings settings;

        private readonly Dictionary<ProductData, int> slices = new Dictionary<ProductData, int>();
        private readonly List<ProductData> available = new List<ProductData>();

        public IReadOnlyList<ProductData> Available => available;

        public event Action<ProductData> Changed;

        public int GetSlices(ProductData product) =>
            product != null && slices.TryGetValue(product, out int count) ? count : 0;

        public bool TryTakeSlice(ProductData product)
        {
            int count = GetSlices(product);
            if (count <= 0)
            {
                return false;
            }

            slices[product] = count - 1;
            Changed?.Invoke(product);
            return true;
        }

        public void Refill(LevelData level)
        {
            slices.Clear();
            available.Clear();

            if (level == null || level.menu == null || settings == null)
            {
                Debug.LogWarning("[LocalShowcaseStock] LevelData veya SalonSettings yok; stok boş.", this);
                return;
            }

            foreach (ProductData product in level.menu)
            {
                if (product == null || product.category != ProductCategory.Cake || slices.ContainsKey(product))
                {
                    continue;
                }

                slices[product] = settings.startingSlicesPerCake;
                available.Add(product);
            }

            foreach (ProductData product in available)
            {
                Changed?.Invoke(product);
            }
        }
    }
}
