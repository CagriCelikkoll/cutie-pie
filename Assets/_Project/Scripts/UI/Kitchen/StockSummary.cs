using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Fırın ekranının üstündeki stok şeridi: KitchenSettings.recipes'teki her pastanın (tekrarlar bir kez) kalan dilimi.
    /// Kalemler ilk kullanımda üretilir. Değerleri OvenPanel verir (StockChanged ve Topla animasyonu).
    /// </summary>
    public class StockSummary : MonoBehaviour
    {
        [Tooltip("Kalemlerin eklendiği yatay dizilim (HorizontalLayoutGroup).")]
        [SerializeField] private RectTransform entryContainer;

        [Tooltip("Prefabs/UI/Kitchen/StockEntry.prefab")]
        [SerializeField] private StockEntry entryPrefab;

        private readonly Dictionary<string, StockEntry> entries = new Dictionary<string, StockEntry>();
        private bool built;

        /// <summary>Kalemleri (yoksa) üretir. false: üretilemedi, sonra yeniden denenir.</summary>
        public bool EnsureEntries(KitchenSettings kitchen)
        {
            if (built)
            {
                return true;
            }

            if (kitchen == null || entryContainer == null || entryPrefab == null)
            {
                Debug.LogError("[StockSummary] KitchenSettings yok veya kalem referansları eksik; stok şeridi üretilemedi.", this);
                return false;
            }

            if (kitchen.recipes != null)
            {
                foreach (RecipeData recipe in kitchen.recipes)
                {
                    ProductData product = recipe != null ? recipe.product : null;
                    if (product == null || string.IsNullOrEmpty(product.id) || entries.ContainsKey(product.id))
                    {
                        continue;
                    }

                    StockEntry entry = Instantiate(entryPrefab, entryContainer, false);
                    entry.name = $"StockEntry_{product.id}";
                    entry.Bind(product);
                    entries.Add(product.id, entry);
                }
            }

            built = true;
            return true;
        }

        public bool TryGetEntry(string productId, out StockEntry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(productId) && entries.TryGetValue(productId, out entry) && entry != null;
        }

        /// <summary>Stok değişti; kalem yoksa (stok şeridinde olmayan ürün) yok sayılır.</summary>
        public void SetAmount(string productId, int amount)
        {
            if (TryGetEntry(productId, out StockEntry entry))
            {
                entry.SetAmount(amount);
            }
        }

        /// <summary>Uçuşları unutur ve tüm kalemleri gerçek stokla yeniler.</summary>
        public void ResetAll(System.Func<string, int> getStock)
        {
            foreach (KeyValuePair<string, StockEntry> pair in entries)
            {
                if (pair.Value != null)
                {
                    pair.Value.ResetFlights(getStock(pair.Key));
                }
            }
        }
    }
}
