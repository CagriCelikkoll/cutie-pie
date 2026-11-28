using NUnit.Framework;
using W = CutiePie.Kitchen.Editor.Tests.KitchenTestWorld;

namespace CutiePie.Kitchen.Editor.Tests
{
    /// <summary>
    /// OvenService toplama yolları için servis seviyesinde Edit Mode testleri: kusursuz bonus, çift/çapraz toplama güvenliği,
    /// dolap kapasitesinde kırpma ve ReadyCount. Kayıt bellektedir, diske yazılmaz (KitchenTestWorld).
    /// </summary>
    public class OvenCollectTests
    {
        private W _world;

        [SetUp]
        public void SetUp() => _world = new W();

        [TearDown]
        public void TearDown() => _world.Dispose();

        // ------------------------------------------------------------------ Kusursuz bonus

        [Test]
        public void CollectWithDecoration_Perfect_AddsBonusSlice()
        {
            _world.LoadWith(0, W.ReadyStart);

            BatchCollectResult result = _world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.perfect);
            Assert.AreEqual(W.ProductId, result.productId);
            Assert.AreEqual(W.SlicesPerBatch, result.baseSlices);
            Assert.AreEqual(W.Bonus, result.bonusSlices);
            Assert.AreEqual(W.SlicesPerBatch + W.Bonus, result.addedSlices);
            Assert.AreEqual(0, result.lostSlices);
            Assert.AreEqual(W.SlicesPerBatch + W.Bonus, _world.StockAmount);
            Assert.AreEqual(OvenState.Empty, _world.Ovens.GetState(0));
            CollectionAssert.AreEqual(new[] { (W.ProductId, W.SlicesPerBatch + W.Bonus) }, _world.Collected);
        }

        [TestCase("cream_white", W.PerfectTopping)]
        [TestCase(W.PerfectFrosting, "deco_cherry")]
        [TestCase(null, null)]
        public void CollectWithDecoration_Wrong_NoBonusNoPenalty(string frosting, string topping)
        {
            _world.LoadWith(0, W.ReadyStart);

            BatchCollectResult result = _world.Ovens.CollectWithDecoration(0, frosting, topping);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.perfect);
            Assert.AreEqual(0, result.bonusSlices);
            Assert.AreEqual(W.SlicesPerBatch, result.addedSlices);
            Assert.AreEqual(W.SlicesPerBatch, _world.StockAmount);
        }

        [Test]
        public void Collect_IsBonuslessPath()
        {
            _world.LoadWith(0, W.ReadyStart);

            Assert.IsTrue(_world.Ovens.Collect(0));
            Assert.AreEqual(W.SlicesPerBatch, _world.StockAmount);
            Assert.AreEqual(OvenState.Empty, _world.Ovens.GetState(0));
        }

        // ------------------------------------------------------------------ Çift / çapraz toplama

        [Test]
        public void Collect_Twice_AddsOnce()
        {
            _world.LoadWith(0, W.ReadyStart);

            Assert.IsTrue(_world.Ovens.Collect(0));
            Assert.IsFalse(_world.Ovens.Collect(0));
            Assert.AreEqual(W.SlicesPerBatch, _world.StockAmount);
            Assert.AreEqual(1, _world.Collected.Count);
        }

        [Test]
        public void CollectWithDecoration_Twice_SecondReturnsNull()
        {
            _world.LoadWith(0, W.ReadyStart);

            Assert.IsNotNull(_world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping));
            Assert.IsNull(_world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping));
            Assert.AreEqual(W.SlicesPerBatch + W.Bonus, _world.StockAmount);
            Assert.AreEqual(1, _world.Collected.Count);
        }

        [Test]
        public void Collect_ThenCollectWithDecoration_AddsOnce()
        {
            // Süsleme ekranı açıkken parti başka yerden (ör. OvenPanel) bonussuz toplandı.
            _world.LoadWith(0, W.ReadyStart);

            Assert.IsTrue(_world.Ovens.Collect(0));
            Assert.IsNull(_world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping));
            Assert.AreEqual(W.SlicesPerBatch, _world.StockAmount);
            Assert.AreEqual(1, _world.Collected.Count);
        }

        [Test]
        public void CollectWithDecoration_ThenCollect_AddsOnce()
        {
            // Süsleme bitti; ekrandan çıkarken "bırakma" yolu yine Collect çağırdı.
            _world.LoadWith(0, W.ReadyStart);

            Assert.IsNotNull(_world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping));
            Assert.IsFalse(_world.Ovens.Collect(0));
            Assert.AreEqual(W.SlicesPerBatch + W.Bonus, _world.StockAmount);
            Assert.AreEqual(1, _world.Collected.Count);
        }

        [Test]
        public void CollectingOneOven_DoesNotTouchOther()
        {
            _world.LoadWith(0, W.ReadyStart, W.ReadyStart);

            Assert.IsNotNull(_world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping));
            Assert.AreEqual(OvenState.Ready, _world.Ovens.GetState(1));
            Assert.IsTrue(_world.Ovens.Collect(1));

            // 9 + 8 = 17 > 16: ikinci fırının 1 dilimi kapasitede kırpılır.
            Assert.AreEqual(W.Capacity, _world.StockAmount);
            CollectionAssert.AreEqual(
                new[] { (W.ProductId, W.SlicesPerBatch + W.Bonus), (W.ProductId, W.Capacity - W.SlicesPerBatch - W.Bonus) },
                _world.Collected);
            CollectionAssert.AreEqual(new[] { (W.ProductId, W.SlicesPerBatch * 2 + W.Bonus - W.Capacity) }, _world.Overflows);
        }

        // ------------------------------------------------------------------ Hazır olmayan fırın

        [Test]
        public void NotReady_CollectFalse_DecorationNull_NothingChanges()
        {
            _world.LoadWith(3, W.BakingStart, null);

            foreach (int oven in new[] { 0, 1, -1, W.OvenCount })
            {
                Assert.IsFalse(_world.Ovens.Collect(oven), $"Collect fırın {oven}");
                Assert.IsNull(_world.Ovens.CollectWithDecoration(oven, W.PerfectFrosting, W.PerfectTopping), $"Süslü fırın {oven}");
            }

            Assert.AreEqual(3, _world.StockAmount);
            Assert.AreEqual(OvenState.Baking, _world.Ovens.GetState(0));
            Assert.AreEqual(0, _world.Collected.Count);
            Assert.AreEqual(0, _world.Overflows.Count);
        }

        // ------------------------------------------------------------------ Dolap kapasitesi

        [Test]
        public void Collect_NearFull_ClipsToCapacity_RaisesOverflow()
        {
            _world.LoadWith(10, W.ReadyStart);

            BatchCollectResult result = _world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping);

            // 10 + 9 → 16; 3 dilim kaybolur.
            Assert.AreEqual(6, result.addedSlices);
            Assert.AreEqual(3, result.lostSlices);
            Assert.AreEqual(W.Capacity, _world.StockAmount);
            Assert.IsTrue(_world.Stock.IsFull(W.ProductId));
            CollectionAssert.AreEqual(new[] { (W.ProductId, 3) }, _world.Overflows);
            CollectionAssert.AreEqual(new[] { (W.ProductId, 6) }, _world.Collected);
        }

        [Test]
        public void Collect_FullCabinet_EmptiesOven_LosesAll()
        {
            _world.LoadWith(W.Capacity, W.ReadyStart);

            Assert.IsTrue(_world.Ovens.Collect(0));

            Assert.AreEqual(OvenState.Empty, _world.Ovens.GetState(0), "Dolap dolu olsa da fırın boşalmalı.");
            Assert.AreEqual(W.Capacity, _world.StockAmount);
            CollectionAssert.AreEqual(new[] { (W.ProductId, W.SlicesPerBatch) }, _world.Overflows);
            CollectionAssert.AreEqual(new[] { (W.ProductId, 0) }, _world.Collected);
        }

        [Test]
        public void StockManager_CapacityQueries()
        {
            _world.LoadWith(10);

            Assert.AreEqual(W.Capacity, _world.Stock.GetCapacity(W.ProductId));
            Assert.AreEqual(6, _world.Stock.GetFreeSpace(W.ProductId));
            Assert.IsFalse(_world.Stock.IsFull(W.ProductId));

            Assert.IsTrue(_world.Stock.Add(W.ProductId, 6, out int added));
            Assert.AreEqual(6, added);
            Assert.IsTrue(_world.Stock.IsFull(W.ProductId));
            Assert.AreEqual(0, _world.Stock.GetFreeSpace(W.ProductId));

            // Dolu dolap: istek geçerli (true) ama 0 dilim eklenir.
            Assert.IsTrue(_world.Stock.Add(W.ProductId, 2, out added));
            Assert.AreEqual(0, added);
            Assert.AreEqual(W.Capacity, _world.StockAmount);

            // Geçersiz istek: false, hiçbir şey değişmez.
            Assert.IsFalse(_world.Stock.Add(W.ProductId, 0, out added));
            Assert.IsFalse(_world.Stock.Add(null, 4, out added));
            Assert.AreEqual(0, added);
        }

        [Test]
        public void StockManager_TryTake_FreesCapacity()
        {
            _world.LoadWith(W.Capacity);

            Assert.IsTrue(_world.Stock.TryTake(W.ProductId, 1));
            Assert.IsFalse(_world.Stock.IsFull(W.ProductId));
            Assert.AreEqual(1, _world.Stock.GetFreeSpace(W.ProductId));
        }

        // ------------------------------------------------------------------ ReadyCount

        [Test]
        public void ReadyCount_CountsReadyOvens_DropsAfterCollect()
        {
            _world.LoadWith(0, W.ReadyStart, W.BakingStart);
            Assert.AreEqual(1, _world.Ovens.ReadyCount);

            _world.LoadWith(0, W.ReadyStart, W.ReadyStart);
            Assert.AreEqual(2, _world.Ovens.ReadyCount);

            _world.Ovens.CollectWithDecoration(0, W.PerfectFrosting, W.PerfectTopping);
            Assert.AreEqual(1, _world.Ovens.ReadyCount);

            _world.Ovens.Collect(1);
            Assert.AreEqual(0, _world.Ovens.ReadyCount);
        }

        [Test]
        public void ReadyCount_EmptyOvens_Zero()
        {
            _world.LoadWith(0);
            Assert.AreEqual(0, _world.Ovens.ReadyCount);
        }
    }
}
