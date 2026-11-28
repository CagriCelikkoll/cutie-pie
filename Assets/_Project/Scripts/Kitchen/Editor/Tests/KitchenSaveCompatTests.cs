using System;
using NUnit.Framework;
using W = CutiePie.Kitchen.Editor.Tests.KitchenTestWorld;

namespace CutiePie.Kitchen.Editor.Tests
{
    /// <summary>
    /// Mutfak 3 öncesi kayıtlarla uyumluluk: kayıt biçimi değişmedi (saveVersion 2); eski JSON aynı doğrulama ve göç
    /// yolundan (SaveSystem.CreateForTests) yüklenir. Ready fırın süslenip toplanabilmeli, kapasiteyi aşmış stok budanmamalı.
    /// </summary>
    public class KitchenSaveCompatTests
    {
        private W _world;

        [SetUp]
        public void SetUp() => _world = new W();

        [TearDown]
        public void TearDown() => _world.Dispose();

        // Mutfak 3 öncesi bir v2 kaydı (alan adları o sürümdeki SaveData ile aynı; yeni alan yok).
        private static string V2Json(int stock, long ovenStart) =>
            "{\"saveVersion\":2,\"gold\":120,\"gems\":0,\"shopRating\":0,\"currentLevelId\":\"" + W.LevelId + "\"," +
            "\"upgrades\":[]," +
            "\"stock\":[{\"productId\":\"" + W.ProductId + "\",\"amount\":" + stock + "}]," +
            "\"ovens\":[{\"recipeId\":\"" + W.RecipeId + "\",\"startUtcTicks\":" + ovenStart + "},{\"recipeId\":\"\",\"startUtcTicks\":0}]," +
            "\"lastSeenUtcTicks\":" + ovenStart + "," +
            "\"boardOrders\":[],\"pendingOrderRefreshUtcTicks\":[],\"languageCode\":\"tr\"}";

        [Test]
        public void OldV2Save_ReadyOven_LoadsAndCanBeDecorated()
        {
            Assert.IsTrue(_world.Load(V2Json(4, W.ReadyStart)));

            Assert.AreEqual(4, _world.StockAmount);
            Assert.AreEqual(OvenState.Ready, _world.Ovens.GetState(0));
            Assert.AreEqual(OvenState.Empty, _world.Ovens.GetState(1));
            Assert.AreEqual(1, _world.Ovens.ReadyCount);

            BatchCollectResult result = _world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping);
            Assert.IsNotNull(result);
            Assert.AreEqual(4 + W.SlicesPerBatch + W.Bonus, _world.StockAmount);
        }

        [Test]
        public void OldV2Save_StockAboveCapacity_NotTrimmed_AddsNothing()
        {
            Assert.IsTrue(_world.Load(V2Json(20, W.ReadyStart)));

            Assert.AreEqual(20, _world.StockAmount, "Eski stok yüklenirken budanmamalı.");
            Assert.IsTrue(_world.Stock.IsFull(W.ProductId));
            Assert.AreEqual(0, _world.Stock.GetFreeSpace(W.ProductId));

            Assert.IsTrue(_world.Ovens.Collect(0));
            Assert.AreEqual(20, _world.StockAmount);
            CollectionAssert.AreEqual(new[] { (W.ProductId, W.SlicesPerBatch) }, _world.Overflows);
        }

        [Test]
        public void OldV2Save_BakingOven_StaysBaking()
        {
            Assert.IsTrue(_world.Load(V2Json(0, W.BakingStart)));

            Assert.AreEqual(OvenState.Baking, _world.Ovens.GetState(0));
            Assert.AreEqual(0, _world.Ovens.ReadyCount);
            Assert.IsNull(_world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping));
        }

        [Test]
        public void V1Save_WithoutKitchenFields_GetsStartingStock()
        {
            // Mutfak alanları olmayan v1 kaydı: başlangıç stoğu bir kez verilir, fırınlar boştur.
            string v1 = "{\"saveVersion\":1,\"gold\":50,\"gems\":0,\"shopRating\":0,\"currentLevelId\":\"" + W.LevelId +
                        "\",\"upgrades\":[]}";
            Assert.IsTrue(_world.Load(v1));

            Assert.AreEqual(W.SlicesPerBatch, _world.StockAmount);
            Assert.AreEqual(0, _world.Ovens.ReadyCount);
            Assert.IsFalse(_world.Ovens.Collect(0));
        }

        [Test]
        public void OldSave_ClockMovedBack_ReadyOvenStaysReady()
        {
            // Son görülen zaman ileride (saat geri alınmış): hazır fırın hazır kalır, toplanabilir.
            long start = W.ReadyStart;
            long future = DateTime.UtcNow.Ticks + TimeSpan.TicksPerDay;
            string json = V2Json(0, start).Replace(
                "\"lastSeenUtcTicks\":" + start, "\"lastSeenUtcTicks\":" + future);
            StringAssert.Contains("\"lastSeenUtcTicks\":" + future, json);
            Assert.IsTrue(_world.Load(json));

            Assert.AreEqual(OvenState.Ready, _world.Ovens.GetState(0));
            Assert.IsTrue(_world.Ovens.Collect(0));
        }
    }
}
