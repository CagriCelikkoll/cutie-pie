using CutiePie.Core.Data;
using NUnit.Framework;
using UnityEngine;

namespace CutiePie.Core.Editor.Tests
{
    /// <summary>
    /// Kusursuz süsleme bonusu ve dolap kapasitesi kırpması (KitchenBatchMath) için Edit Mode birim testleri.
    /// </summary>
    public class KitchenBatchMathTests
    {
        private const int Capacity = 16;

        // ------------------------------------------------------------------ Kusursuz süsleme

        [Test]
        public void IsPerfect_BothMatch_True()
        {
            Assert.IsTrue(KitchenBatchMath.IsPerfect("cream_pink", "deco_heart", "cream_pink", "deco_heart"));
        }

        [TestCase("cream_white", "deco_heart")]
        [TestCase("cream_pink", "deco_cherry")]
        [TestCase("cream_white", "deco_cherry")]
        [TestCase(null, "deco_heart")]
        [TestCase("cream_pink", null)]
        [TestCase("", "")]
        [TestCase("CREAM_PINK", "deco_heart")]
        public void IsPerfect_AnyMismatch_False(string frosting, string topping)
        {
            Assert.IsFalse(KitchenBatchMath.IsPerfect("cream_pink", "deco_heart", frosting, topping));
        }

        [Test]
        public void IsPerfect_RecipeWithoutExpectedValues_NeverPerfect()
        {
            // Tarifte doğru krema/süs atanmamışsa boş seçim de "eşit" sayılmaz.
            Assert.IsFalse(KitchenBatchMath.IsPerfect(null, null, null, null));
            Assert.IsFalse(KitchenBatchMath.IsPerfect("", "", "", ""));
            Assert.IsFalse(KitchenBatchMath.IsPerfect("cream_pink", null, "cream_pink", null));
        }

        [Test]
        public void TotalSlices_AddsBonusOnlyWhenPerfect()
        {
            Assert.AreEqual(9, KitchenBatchMath.TotalSlices(8, true, 1));
            Assert.AreEqual(8, KitchenBatchMath.TotalSlices(8, false, 1));
            Assert.AreEqual(8, KitchenBatchMath.TotalSlices(8, true, 0));
            Assert.AreEqual(8, KitchenBatchMath.TotalSlices(8, true, -3));
            Assert.AreEqual(1, KitchenBatchMath.BonusSlices(true, 1));
            Assert.AreEqual(0, KitchenBatchMath.BonusSlices(false, 1));
        }

        [Test]
        public void RecipeData_IsPerfectDecoration_UsesAssignedParts()
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            var cream = ScriptableObject.CreateInstance<CakePartData>();
            var deco = ScriptableObject.CreateInstance<CakePartData>();
            try
            {
                cream.type = CakePartType.Cream;
                cream.id = "cream_pink";
                deco.type = CakePartType.Decoration;
                deco.id = "deco_heart";

                Assert.IsFalse(recipe.IsPerfectDecoration("cream_pink", "deco_heart"), "Atanmamış tarif bonus vermemeli.");

                recipe.perfectFrosting = cream;
                recipe.perfectTopping = deco;
                Assert.IsTrue(recipe.IsPerfectDecoration("cream_pink", "deco_heart"));
                Assert.IsFalse(recipe.IsPerfectDecoration("cream_pink", "deco_cherry"));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(cream);
                Object.DestroyImmediate(deco);
            }
        }

        // ------------------------------------------------------------------ Kapasite kırpma

        [Test]
        public void Clamp_EnoughSpace_AddsAll()
        {
            KitchenBatchMath.ClampToCapacity(4, 8, Capacity, out int added, out int lost);
            Assert.AreEqual(8, added);
            Assert.AreEqual(0, lost);
        }

        [Test]
        public void Clamp_ExactlyToCapacity_AddsAll()
        {
            KitchenBatchMath.ClampToCapacity(8, 8, Capacity, out int added, out int lost);
            Assert.AreEqual(8, added);
            Assert.AreEqual(0, lost);
        }

        [Test]
        public void Clamp_OverCapacity_ClipsAndReportsLost()
        {
            KitchenBatchMath.ClampToCapacity(10, 9, Capacity, out int added, out int lost);
            Assert.AreEqual(6, added);
            Assert.AreEqual(3, lost);
        }

        [Test]
        public void Clamp_AlreadyFull_AddsNothing()
        {
            KitchenBatchMath.ClampToCapacity(Capacity, 8, Capacity, out int added, out int lost);
            Assert.AreEqual(0, added);
            Assert.AreEqual(8, lost);
        }

        [Test]
        public void Clamp_OldStockAboveCapacity_IsNotTrimmed_AddsNothing()
        {
            // Eski kayıtta (debug menüsü) stok 20 olabilir: budanmaz, yeni ekleme tamamen taşar.
            KitchenBatchMath.ClampToCapacity(20, 8, Capacity, out int added, out int lost);
            Assert.AreEqual(0, added);
            Assert.AreEqual(8, lost);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Clamp_NonPositiveAmount_Nothing(int amount)
        {
            KitchenBatchMath.ClampToCapacity(4, amount, Capacity, out int added, out int lost);
            Assert.AreEqual(0, added);
            Assert.AreEqual(0, lost);
        }

        [Test]
        public void Clamp_NoCapacity_IsUnlimitedButNeverOverflowsInt()
        {
            KitchenBatchMath.ClampToCapacity(1000, 8, 0, out int added, out int lost);
            Assert.AreEqual(8, added);
            Assert.AreEqual(0, lost);

            KitchenBatchMath.ClampToCapacity(int.MaxValue - 2, 8, 0, out added, out lost);
            Assert.AreEqual(2, added);
            Assert.AreEqual(6, lost);
        }
    }
}
