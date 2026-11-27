using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.UI.Testing
{
    /// <summary>
    /// Yalnızca CabinetTest sahnesi için (Scenes/Test/CabinetTest.unity, Build Settings dışı; Boot atlanır).
    /// Kaydı yükler; GameConfig tariflerine bellekte iki test tarifi ekler (asset yazılmaz):
    /// "Limonlu (test)" stok 0 ile Bitti durumunu, "Frambuazlı (test)" LockLevelProvider ile Kilitli durumunu gösterir.
    /// Ekrandaki butonlar stoğu StockManager üzerinden değiştirir (gerçek StockChanged akışı; kayda yazılır,
    /// sıfırlamak için "Cutie Pie/Kaydı Sil"). RecipeChosen gelince "Tezgâh: &lt;tarif&gt; (yakında)" yazar.
    /// Geliştirici aracıdır; metinleri yerelleştirilmez. Test tariflerinin nameKey'i düz metindir (çeviri dosyasında yok,
    /// Localization anahtarı aynen gösterir ve bir kez uyarı yazar).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CabinetTestHarness : MonoBehaviour
    {
        private const string LemonId = "test_cake_lemon";
        private const string RaspberryId = "test_cake_raspberry";
        private const string LemonName = "Limonlu (test)";
        private const string RaspberryName = "Frambuazlı (test)";
        private const int TestLockLevel = 3;

        // Ön ayar: listedeki ilk üç tarif sırasıyla (çikolatalı, çilekli, limonlu).
        private static readonly int[] PresetAmounts = { 5, 2, 0 };
        private static readonly int[] QuickAmounts = { 0, 1, 2, 5, 8, 12 };

        [SerializeField] private CakeCabinetPanel panel;

        private readonly List<RecipeData> recipes = new List<RecipeData>();
        private readonly List<ScriptableObject> runtimeAssets = new List<ScriptableObject>();
        private RecipeData lockedRecipe;
        private bool raspberryLocked = true;
        private string chosenRecipeId;
        private Vector2 scroll;
        private GUIStyle chosenStyle;

        private static StockManager Stock => GameManager.Instance != null ? GameManager.Instance.Stock : null;

        private void Start()
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null || GameManager.Instance == null)
            {
                Debug.LogError("[CabinetTest] GameManager/SaveSystem yok; sahneyi 'Cutie Pie/Test/Pasta Dolabı Test Sahnesini Kur' ile yeniden kurun.", this);
                return;
            }

            // Boot'tan gelindiyse (kayıt yüklü) yeniden yüklenmez.
            if (!save.IsLoaded)
            {
                save.LoadAndApply();
            }

            BuildRecipes();

            // İlk açılışta limonlu boş görünsün (önceki testten kalan stok sıfırlanır).
            SetStock(LemonId, 0);

            if (panel != null)
            {
                panel.LockLevelProvider = GetLockLevel;
                panel.SetRecipes(recipes);
                panel.RecipeChosen += HandleRecipeChosen;
            }
        }

        private void OnDestroy()
        {
            if (panel != null)
            {
                panel.RecipeChosen -= HandleRecipeChosen;
            }

            foreach (ScriptableObject asset in runtimeAssets)
            {
                if (asset != null)
                {
                    Destroy(asset);
                }
            }
        }

        private void BuildRecipes()
        {
            KitchenSettings kitchen = GameManager.Instance.Config != null ? GameManager.Instance.Config.kitchen : null;
            if (kitchen != null && kitchen.recipes != null)
            {
                foreach (RecipeData recipe in kitchen.recipes)
                {
                    if (recipe != null && recipe.product != null)
                    {
                        recipes.Add(recipe);
                    }
                }
            }

            recipes.Add(CreateTestRecipe(LemonId, LemonName));
            lockedRecipe = CreateTestRecipe(RaspberryId, RaspberryName);
            recipes.Add(lockedRecipe);
        }

        // Yalnızca bellekte: ürün ve tarif id'si aynı, ad anahtarı düz metin.
        private RecipeData CreateTestRecipe(string id, string displayName)
        {
            var product = ScriptableObject.CreateInstance<ProductData>();
            product.name = id;
            product.id = id;
            product.nameKey = displayName;
            product.category = ProductCategory.Cake;

            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.name = id;
            recipe.id = id;
            recipe.nameKey = displayName;
            recipe.product = product;
            recipe.bakeSeconds = 60;
            recipe.slicesPerBatch = 8;

            runtimeAssets.Add(product);
            runtimeAssets.Add(recipe);
            return recipe;
        }

        private int GetLockLevel(RecipeData recipe) => raspberryLocked && recipe == lockedRecipe ? TestLockLevel : 0;

        private void HandleRecipeChosen(string recipeId)
        {
            chosenRecipeId = recipeId;
            Debug.Log($"[CabinetTest] RecipeChosen: {recipeId}");
        }

        // Hedef değere StockManager.Add/TryTake ile gidilir; ikisi de StockChanged yayınlar.
        private static void SetStock(string productId, int target)
        {
            StockManager stock = Stock;
            if (stock == null)
            {
                return;
            }

            int current = stock.Get(productId);
            if (target > current)
            {
                stock.Add(productId, target - current);
            }
            else if (target < current)
            {
                stock.TryTake(productId, current - target);
            }
        }

        private void OnGUI()
        {
            StockManager stock = Stock;
            if (stock == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 10f, 420f, 420f), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            if (GUILayout.Button("Ön ayar: çikolatalı 5, çilekli 2, limonlu 0"))
            {
                for (int i = 0; i < recipes.Count && i < PresetAmounts.Length; i++)
                {
                    SetStock(recipes[i].product.id, PresetAmounts[i]);
                }
            }

            foreach (RecipeData recipe in recipes)
            {
                string productId = recipe.product.id;
                int amount = stock.Get(productId);
                GUILayout.Label($"{recipe.id}: {amount} dilim");
                GUILayout.BeginHorizontal();
                foreach (int quick in QuickAmounts)
                {
                    if (GUILayout.Button(quick.ToString()))
                    {
                        SetStock(productId, quick);
                    }
                }

                if (GUILayout.Button("-1"))
                {
                    SetStock(productId, amount - 1);
                }

                if (GUILayout.Button("+1"))
                {
                    SetStock(productId, amount + 1);
                }

                GUILayout.EndHorizontal();
            }

            if (panel != null && GUILayout.Button(raspberryLocked ? "Frambuazlı kilidini aç" : "Frambuazlıyı kilitle"))
            {
                raspberryLocked = !raspberryLocked;

                // Sağlayıcıyı yeniden atamak kartları yeniler.
                panel.LockLevelProvider = GetLockLevel;
            }

            if (panel != null && GUILayout.Button(panel.IsVisible ? "Paneli gizle" : "Paneli göster"))
            {
                if (panel.IsVisible)
                {
                    panel.Hide();
                }
                else
                {
                    panel.Show();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(chosenRecipeId))
            {
                chosenStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                GUI.Box(new Rect(Screen.width - 560f, 10f, 550f, 60f), $"Tezgâh: {chosenRecipeId} (yakında)", chosenStyle);
            }
        }
    }
}
