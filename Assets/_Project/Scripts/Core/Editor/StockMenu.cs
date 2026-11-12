using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Core.Editor
{
    /// <summary>Yalnızca editörde: Play sırasında stok test menüleri (Mutfak ekranları gelene kadar).</summary>
    public static class StockMenu
    {
        private const string Root = "Cutie Pie/Stok/";

        [MenuItem(Root + "Stoğu Yazdır")]
        private static void LogStock()
        {
            foreach (RecipeData recipe in FindRecipes())
            {
                string id = recipe.product.id;
                Debug.Log($"[StockMenu] {id}: {GameManager.Instance.Stock.Get(id)}");
            }
        }

        [MenuItem(Root + "Her Pastaya Bir Parti Ekle")]
        private static void AddBatch()
        {
            foreach (RecipeData recipe in FindRecipes())
            {
                GameManager.Instance.Stock.Add(recipe.product.id, recipe.slicesPerBatch);
            }

            LogStock();
        }

        [MenuItem(Root + "Her Pastadan Bir Dilim Al")]
        private static void TakeSlice()
        {
            foreach (RecipeData recipe in FindRecipes())
            {
                bool taken = GameManager.Instance.Stock.TryTake(recipe.product.id, 1);
                Debug.Log($"[StockMenu] {recipe.product.id} dilim alındı mı: {taken}");
            }

            LogStock();
        }

        [MenuItem(Root + "Stoğu Yazdır", true)]
        [MenuItem(Root + "Her Pastaya Bir Parti Ekle", true)]
        [MenuItem(Root + "Her Pastadan Bir Dilim Al", true)]
        private static bool ValidatePlaying() =>
            EditorApplication.isPlaying && GameManager.Instance != null && GameManager.Instance.Stock != null;

        // Tarifler GameConfig.kitchen'dan okunur; ürünü atanmamış tarifler atlanır.
        private static RecipeData[] FindRecipes()
        {
            KitchenSettings kitchen = GameManager.Instance.Config.kitchen;
            if (kitchen == null || kitchen.recipes == null)
            {
                Debug.LogWarning("[StockMenu] GameConfig.kitchen veya tarif listesi atanmamış.");
                return System.Array.Empty<RecipeData>();
            }

            return System.Array.FindAll(kitchen.recipes, r => r != null && r.product != null);
        }
    }
}
