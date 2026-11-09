using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Bir müşterinin siparişi: 1 pasta dilimi + 1 içecek. Teslim kontrolü (OrderDelivery) Matches'i kullanır.
    /// </summary>
    public class CustomerOrder
    {
        // Rastgele seçimde her seferinde yeni liste oluşturmamak için tekrar kullanılır.
        private static readonly List<ProductData> CandidateBuffer = new List<ProductData>();

        public ProductData Cake { get; }
        public ProductData Drink { get; }

        public CustomerOrder(ProductData cake, ProductData drink)
        {
            Cake = cake;
            Drink = drink;
        }

        /// <summary>Ürün fiyatları toplamı (teslimdeki ödeme).</summary>
        public int TotalPrice => (Cake != null ? Cake.price : 0) + (Drink != null ? Drink.price : 0);

        /// <summary>Tepsideki dilim ve içecek bu siparişle birebir aynı mı? Eksik veya fazla parça yanlış sayılır.</summary>
        public bool Matches(ProductData slice, ProductData drink) => slice == Cake && drink == Drink;

        /// <summary>Seviyenin menüsünden kategoriye göre rastgele 1 pasta + 1 içecek seçer.</summary>
        public static CustomerOrder Create(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("[CustomerOrder] LevelData yok; sipariş oluşturulamadı.");
                return new CustomerOrder(null, null);
            }

            ProductData cake = PickRandom(level.menu, ProductCategory.Cake);
            ProductData drink = PickRandom(level.menu, ProductCategory.Drink);

            if (cake == null || drink == null)
            {
                Debug.LogWarning($"[CustomerOrder] '{level.id}' menüsünde pasta veya içecek eksik.");
            }

            return new CustomerOrder(cake, drink);
        }

        private static ProductData PickRandom(List<ProductData> menu, ProductCategory category)
        {
            CandidateBuffer.Clear();
            if (menu != null)
            {
                foreach (ProductData product in menu)
                {
                    if (product != null && product.category == category)
                    {
                        CandidateBuffer.Add(product);
                    }
                }
            }

            return CandidateBuffer.Count > 0 ? CandidateBuffer[Random.Range(0, CandidateBuffer.Count)] : null;
        }
    }
}
