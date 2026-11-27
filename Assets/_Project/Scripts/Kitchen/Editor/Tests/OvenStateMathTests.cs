using System;
using NUnit.Framework;

namespace CutiePie.Kitchen.Editor.Tests
{
    /// <summary>Fırın durumu ve ReadyCount hesabı (OvenStateMath) için Edit Mode birim testleri.</summary>
    public class OvenStateMathTests
    {
        private static readonly long Start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).Ticks;
        private static readonly long Bake = 60 * TimeSpan.TicksPerSecond;

        [Test]
        public void StateAt_NoBake_Empty()
        {
            Assert.AreEqual(OvenState.Empty, OvenStateMath.StateAt(false, Start, Bake, Start + Bake * 10));
        }

        [Test]
        public void StateAt_BeforeBakeTime_Baking_AtOrAfter_Ready()
        {
            Assert.AreEqual(OvenState.Baking, OvenStateMath.StateAt(true, Start, Bake, Start + Bake - 1));
            Assert.AreEqual(OvenState.Ready, OvenStateMath.StateAt(true, Start, Bake, Start + Bake));
            Assert.AreEqual(OvenState.Ready, OvenStateMath.StateAt(true, Start, Bake, Start + Bake * 100));
        }

        [Test]
        public void StateAt_StartInFuture_StaysBaking()
        {
            // Bozuk kayıt / geri alınmış saat: geçen süre negatif olmaz.
            Assert.AreEqual(OvenState.Baking, OvenStateMath.StateAt(true, Start + Bake * 5, Bake, Start));
            Assert.AreEqual(0L, OvenStateMath.Elapsed(Start + Bake, Start));
        }

        [Test]
        public void CountReady_CountsOnlyReady()
        {
            OvenState[] states = { OvenState.Ready, OvenState.Baking, OvenState.Empty, OvenState.Ready };
            Assert.AreEqual(2, OvenStateMath.CountReady(states.Length, i => states[i]));
            Assert.AreEqual(1, OvenStateMath.CountReady(2, i => states[i]), "Yalnızca açık fırınlar (0..count-1) sayılır.");
        }

        [Test]
        public void CountReady_NoOvensOrNullGetter_Zero()
        {
            Assert.AreEqual(0, OvenStateMath.CountReady(0, i => OvenState.Ready));
            Assert.AreEqual(0, OvenStateMath.CountReady(3, null));
        }
    }
}
