using System;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CutiePie.Kitchen.Editor.Tests
{
    /// <summary>
    /// Servis seviyesindeki mutfak testleri için bellek içi dünya: test ayarları, tek tarif, SaveSystem (diske yazmaz),
    /// StockManager ve OvenService. Asset değerlerinden ve gerçek save.json'dan bağımsızdır. Dispose her şeyi temizler.
    /// </summary>
    public sealed class KitchenTestWorld : IDisposable
    {
        public const string ProductId = "cake_test";
        public const string RecipeId = "recipe_test";
        public const string LevelId = "level_test";
        public const string PerfectFrosting = "cream_pink";
        public const string PerfectTopping = "deco_heart";
        public const int SlicesPerBatch = 8;
        public const int Capacity = 16;
        public const int Bonus = 1;
        public const int OvenCount = 2;
        public const int BakeSeconds = 60;

        public KitchenSettings Settings { get; }
        public RecipeData Recipe { get; }
        public GameConfig Config { get; }
        public SaveSystem Save { get; private set; }
        public StockManager Stock { get; private set; }
        public OvenService Ovens { get; private set; }

        /// <summary>Yayınlanan BatchCollected olayları (ürün, dilim).</summary>
        public List<(string productId, int slices)> Collected { get; } = new List<(string, int)>();

        /// <summary>Yayınlanan StockOverflow olayları (ürün, kaybolan).</summary>
        public List<(string productId, int lost)> Overflows { get; } = new List<(string, int)>();

        private readonly List<Object> _created = new List<Object>();
        private GameObject _host;

        public KitchenTestWorld()
        {
            var product = Create<ProductData>();
            product.id = ProductId;
            product.category = ProductCategory.Cake;

            var cream = Create<CakePartData>();
            cream.type = CakePartType.Cream;
            cream.id = PerfectFrosting;

            var deco = Create<CakePartData>();
            deco.type = CakePartType.Decoration;
            deco.id = PerfectTopping;

            Recipe = Create<RecipeData>();
            Recipe.id = RecipeId;
            Recipe.product = product;
            Recipe.bakeSeconds = BakeSeconds;
            Recipe.slicesPerBatch = SlicesPerBatch;
            Recipe.perfectFrosting = cream;
            Recipe.perfectTopping = deco;

            Settings = Create<KitchenSettings>();
            Settings.startingOvenCount = OvenCount;
            Settings.maxOvenCount = OvenCount;
            Settings.recipes = new[] { Recipe };
            Settings.cakeParts = new[] { cream, deco };
            Settings.cabinetCapacityPerProduct = Capacity;
            Settings.perfectDecorationBonusSlices = Bonus;
            Settings.startingStock = new[] { new StockAmount { product = product, amount = SlicesPerBatch } };

            var level = Create<LevelData>();
            level.id = LevelId;
            level.maxMisses = 3;

            Config = Create<GameConfig>();
            Config.saveVersion = 2;
            Config.startingLevel = level;
            Config.levels = new[] { level };
            Config.kitchen = Settings;

            GameEvents.BatchCollected += HandleBatchCollected;
            GameEvents.StockOverflow += HandleStockOverflow;
        }

        /// <summary>Kaydı JSON'dan yükler (diskteki kayıt gibi doğrulanır ve göç ettirilir), servisleri kurar.</summary>
        public bool Load(string json)
        {
            ReleaseSave();
            _host = new GameObject("KitchenTestWorld") { hideFlags = HideFlags.HideAndDontSave };
            Save = SaveSystem.CreateForTests(_host, Config, json);
            if (Save == null)
            {
                return false;
            }

            Stock = new StockManager(Settings);
            Ovens = new OvenService(Settings, Stock, null, null);
            Ovens.Initialize();
            return true;
        }

        /// <summary>Verilen fırınlar ve stokla güncel biçimde (v2) bir kayıt kurar. null fırın boştur.</summary>
        public void LoadWith(int stock, params long?[] ovenStartTicks)
        {
            var data = new SaveData
            {
                saveVersion = 2,
                currentLevelId = LevelId,
                stock = new List<StockEntry> { new StockEntry { productId = ProductId, amount = stock } }
            };

            foreach (long? start in ovenStartTicks)
            {
                data.ovens.Add(start.HasValue
                    ? new OvenSaveEntry { recipeId = RecipeId, startUtcTicks = start.Value }
                    : new OvenSaveEntry());
            }

            if (!Load(JsonUtility.ToJson(data)))
            {
                throw new InvalidOperationException("Test kaydı yüklenemedi.");
            }
        }

        /// <summary>Çoktan pişmiş bir partinin başlangıcı (1 saat önce).</summary>
        public static long ReadyStart => GameClock.UtcNow.Ticks - TimeSpan.TicksPerHour;

        /// <summary>Yeni başlamış (pişmekte olan) bir partinin başlangıcı.</summary>
        public static long BakingStart => GameClock.UtcNow.Ticks;

        public int StockAmount => Stock.Get(ProductId);

        public void Dispose()
        {
            GameEvents.BatchCollected -= HandleBatchCollected;
            GameEvents.StockOverflow -= HandleStockOverflow;
            ReleaseSave();
            foreach (Object obj in _created)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }

        private void ReleaseSave()
        {
            if (Save != null)
            {
                Save.ReleaseForTests();
                Save = null;
            }

            if (_host != null)
            {
                Object.DestroyImmediate(_host);
                _host = null;
            }
        }

        private T Create<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            _created.Add(obj);
            return obj;
        }

        private void HandleBatchCollected(string productId, int slices) => Collected.Add((productId, slices));

        private void HandleStockOverflow(string productId, int lost) => Overflows.Add((productId, lost));
    }
}
