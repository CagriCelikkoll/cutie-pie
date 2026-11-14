using System;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Vitrin stoğunu Mutfak ile ortak stoğa (GameManager.Instance.Stock) bağlar. Salon stoğu kendisi tutmaz.
    /// Dilim düşme StockManager.TryTake ile yapılır (diske hemen yazılmaz; SaveSystem round sonunda kaydeder).
    /// GameEvents.StockChanged'deki miktar kalan toplamdır; burada yalnızca "değişti" bilgisi olarak kullanılır,
    /// güncel sayı her zaman StockManager.Get'ten okunur.
    /// Available: menüdeki tüm pastalar (stoğu 0 olan da dahil; rafta "Tükendi" görünür). İçecekler stoklu değildir.
    /// </summary>
    public class SharedShowcaseStock : MonoBehaviour, IShowcaseStock
    {
        private readonly List<ProductData> available = new List<ProductData>();
        private readonly Dictionary<string, ProductData> productsById = new Dictionary<string, ProductData>();

        public IReadOnlyList<ProductData> Available => available;

        public event Action<ProductData> Changed;

        /// <summary>Menüdeki pastalardan en az birinin stoğu var mı?</summary>
        public bool HasAnyCake
        {
            get
            {
                foreach (ProductData product in available)
                {
                    if (GetSlices(product) > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static StockManager Stock => GameManager.Instance != null ? GameManager.Instance.Stock : null;

        private void OnEnable() => GameEvents.StockChanged += HandleStockChanged;

        private void OnDisable() => GameEvents.StockChanged -= HandleStockChanged;

        public int GetSlices(ProductData product)
        {
            StockManager stock = Stock;
            return stock != null && product != null && !string.IsNullOrEmpty(product.id) ? stock.Get(product.id) : 0;
        }

        /// <summary>Ortak stoktan bir dilim düşer; Changed, StockChanged olayı üzerinden yayınlanır.</summary>
        public bool TryTakeSlice(ProductData product)
        {
            StockManager stock = Stock;
            return stock != null && product != null && !string.IsNullOrEmpty(product.id) && stock.TryTake(product.id, 1);
        }

        /// <summary>
        /// Round başında menüdeki pastaları bağlar. Stok doldurmaz (stok Mutfak'ta üretilir);
        /// yalnızca rafların güncel sayıları okuması için her ürün için Changed yayınlar.
        /// </summary>
        public void Refill(LevelData level)
        {
            available.Clear();
            productsById.Clear();

            if (Stock == null)
            {
                Debug.LogWarning("[SharedShowcaseStock] GameManager veya StockManager yok (Boot sahnesi yüklenmedi mi?); vitrin boş görünür.", this);
            }

            if (level == null || level.menu == null)
            {
                Debug.LogWarning("[SharedShowcaseStock] LevelData veya menü yok; vitrin boş.", this);
                return;
            }

            foreach (ProductData product in level.menu)
            {
                if (product == null || product.category != ProductCategory.Cake || available.Contains(product))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(product.id))
                {
                    Debug.LogWarning($"[SharedShowcaseStock] '{product.name}' ürününün id'si boş; stoğu okunamaz.", this);
                    continue;
                }

                available.Add(product);
                productsById[product.id] = product;
            }

            foreach (ProductData product in available)
            {
                Changed?.Invoke(product);
            }
        }

        // Menüde olmayan ürünlerin değişimi yok sayılır.
        private void HandleStockChanged(string productId, int amount)
        {
            if (productId != null && productsById.TryGetValue(productId, out ProductData product))
            {
                Changed?.Invoke(product);
            }
        }
    }
}
