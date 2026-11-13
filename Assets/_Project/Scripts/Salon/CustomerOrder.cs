using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CutiePie.Salon
{
    /// <summary>
    /// Bir müşterinin siparişi: 1 pasta dilimi + 1 içecek. Teslim kontrolü (OrderDelivery) Matches'i kullanır.
    /// Hiçbir pasta sipariş edilebilir değilse (stok bitti / hepsi ayrıldı) Cake null olur ve sipariş yalnızca içecektir;
    /// balon ve fiş boş pasta satırını gizler, teslimde tepside dilim olmamalıdır.
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

        /// <summary>
        /// Seviyenin menüsünden rastgele 1 pasta + 1 içecek seçer. Pasta yalnızca canOrderCake'in kabul ettikleri arasından seçilir
        /// (null verilirse menüdeki tüm pastalar adaydır). Uygun pasta yoksa sipariş yalnızca içecektir.
        /// İçecekler stoklu değildir, her zaman seçilebilir.
        /// </summary>
        public static CustomerOrder Create(LevelData level, Func<ProductData, bool> canOrderCake)
        {
            if (level == null)
            {
                Debug.LogError("[CustomerOrder] LevelData yok; sipariş oluşturulamadı.");
                return new CustomerOrder(null, null);
            }

            ProductData cake = PickRandom(level.menu, ProductCategory.Cake, canOrderCake);
            ProductData drink = PickRandom(level.menu, ProductCategory.Drink, null);

            // Pastasız sipariş stok bitince beklenen durumdur; yalnızca içecek eksikliği hatadır.
            if (drink == null)
            {
                Debug.LogWarning($"[CustomerOrder] '{level.id}' menüsünde içecek yok.");
            }

            return new CustomerOrder(cake, drink);
        }

        private static ProductData PickRandom(List<ProductData> menu, ProductCategory category, Func<ProductData, bool> filter)
        {
            CandidateBuffer.Clear();
            if (menu != null)
            {
                foreach (ProductData product in menu)
                {
                    if (product != null && product.category == category && (filter == null || filter(product)))
                    {
                        CandidateBuffer.Add(product);
                    }
                }
            }

            return CandidateBuffer.Count > 0 ? CandidateBuffer[Random.Range(0, CandidateBuffer.Count)] : null;
        }
    }
}
