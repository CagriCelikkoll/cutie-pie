using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde, Play sırasında: G16 testleri için Salon menüsü. Yalnızca StockManager'ın public API'sini kullanır
    /// (Get + TryTake); Core'a dokunmaz. "Stoğu Sıfırla (0)" GameConfig.kitchen tariflerindeki ve mevcut seviyenin menüsündeki
    /// her pastanın stoğunu 0'a indirir. TryTake gibi diske hemen yazmaz; kayıt round sonu / duraklatma / çıkışta yazılır.
    /// Stoğu geri getirmek için: "Cutie Pie/Stok/Her Pastaya Bir Parti Ekle".
    /// </summary>
    public static class SalonStockTestMenu
    {
        private const string MenuPath = "Cutie Pie/Salon/Stoğu Sıfırla (0)";

        [MenuItem(MenuPath)]
        private static void ClearStock()
        {
            StockManager stock = GameManager.Instance.Stock;
            int cleared = 0;

            foreach (ProductData product in FindCakes())
            {
                int amount = stock.Get(product.id);
                if (amount > 0 && stock.TryTake(product.id, amount))
                {
                    cleared++;
                }

                Debug.Log($"[SalonStockTestMenu] {product.id}: {stock.Get(product.id)}");
            }

            Debug.Log($"[SalonStockTestMenu] {cleared} pastanın stoğu 0'a indirildi.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidatePlaying() =>
            EditorApplication.isPlaying && GameManager.Instance != null && GameManager.Instance.Stock != null;

        // Tariflerin ürünleri + mevcut seviyenin menüsündeki pastalar (tekrarsız).
        private static List<ProductData> FindCakes()
        {
            var cakes = new List<ProductData>();

            KitchenSettings kitchen = GameManager.Instance.Config != null ? GameManager.Instance.Config.kitchen : null;
            if (kitchen != null && kitchen.recipes != null)
            {
                foreach (RecipeData recipe in kitchen.recipes)
                {
                    AddCake(cakes, recipe != null ? recipe.product : null);
                }
            }

            LevelData level = SaveSystem.Instance != null ? SaveSystem.Instance.CurrentLevel : null;
            if (level != null && level.menu != null)
            {
                foreach (ProductData product in level.menu)
                {
                    AddCake(cakes, product);
                }
            }

            if (cakes.Count == 0)
            {
                Debug.LogWarning("[SalonStockTestMenu] Sıfırlanacak pasta bulunamadı (GameConfig.kitchen ve seviye menüsü boş).");
            }

            return cakes;
        }

        private static void AddCake(List<ProductData> cakes, ProductData product)
        {
            if (product != null && product.category == ProductCategory.Cake && !string.IsNullOrEmpty(product.id) && !cakes.Contains(product))
            {
                cakes.Add(product);
            }
        }
    }
}
