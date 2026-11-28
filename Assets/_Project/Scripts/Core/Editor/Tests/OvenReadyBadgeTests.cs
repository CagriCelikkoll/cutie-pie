using CutiePie.Core.Shop;
using NUnit.Framework;

namespace CutiePie.Core.Editor.Tests
{
    /// <summary>Shop "N pasta hazır" rozetinin anahtar seçimi (OvenReadyBadge.KeyFor) için Edit Mode birim testleri.</summary>
    public class OvenReadyBadgeTests
    {
        private const string One = "shop.ovens_ready.one";
        private const string Other = "shop.ovens_ready.other";

        [Test]
        public void KeyFor_One_UsesOneKey()
        {
            Assert.AreEqual(One, OvenReadyBadge.KeyFor(1, One, Other));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        public void KeyFor_NotOne_UsesOtherKey(int count)
        {
            Assert.AreEqual(Other, OvenReadyBadge.KeyFor(count, One, Other));
        }
    }
}
