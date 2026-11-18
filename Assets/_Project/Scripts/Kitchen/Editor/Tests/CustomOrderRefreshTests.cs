using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CutiePie.Kitchen.Editor.Tests
{
    /// <summary>
    /// Özel sipariş yenileme süresi (CustomOrderRefreshMath) için Edit Mode birim testleri.
    /// Saat kuralı G12 fırınlarıyla aynıdır: şimdi = max(UTC şimdi, son görülen).
    /// </summary>
    public class CustomOrderRefreshTests
    {
        private const float Epsilon = 0.001f;
        private const int RefreshSeconds = 60;

        // Sabit bir başlangıç anı (testler gerçek saate bağlı değildir).
        private static readonly long Start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).Ticks;

        private static long Seconds(double s) => (long)(s * TimeSpan.TicksPerSecond);

        private static long Refresh => CustomOrderRefreshMath.RefreshTicks(RefreshSeconds);

        [Test]
        public void SecondsUntilNext_NoPending_ReturnsZero()
        {
            Assert.AreEqual(0f, CustomOrderRefreshMath.SecondsUntilNext(new List<long>(), Start, Refresh));
            Assert.AreEqual(0f, CustomOrderRefreshMath.SecondsUntilNext(null, Start, Refresh));
        }

        [Test]
        public void SecondsUntilNext_PicksNearestRefresh()
        {
            // Biri 10 sn önce, biri 40 sn önce tamamlandı: en yakını 60 - 40 = 20 sn sonra.
            var pending = new List<long> { Start - Seconds(10), Start - Seconds(40) };
            Assert.AreEqual(20f, CustomOrderRefreshMath.SecondsUntilNext(pending, Start, Refresh), Epsilon);
        }

        [Test]
        public void SecondsUntilNext_OverdueRefresh_ReturnsZero()
        {
            var pending = new List<long> { Start - Seconds(90), Start - Seconds(5) };
            Assert.AreEqual(0f, CustomOrderRefreshMath.SecondsUntilNext(pending, Start, Refresh), Epsilon);
        }

        [Test]
        public void Now_ClockMovedBack_DoesNotGoBelowLastSeen()
        {
            // Cihaz saati 30 sn geri alındı: şimdi son görülen anda kalır, kalan süre artmaz (ilerlemez).
            long lastSeen = Start;
            long now = CustomOrderRefreshMath.Now(Start - Seconds(30), lastSeen);
            Assert.AreEqual(lastSeen, now);

            var pending = new List<long> { Start - Seconds(10) };
            Assert.AreEqual(50f, CustomOrderRefreshMath.SecondsUntilNext(pending, now, Refresh), Epsilon);
        }

        [Test]
        public void Now_ClockMovedForward_UsesNewTime()
        {
            long now = CustomOrderRefreshMath.Now(Start + Seconds(30), Start);
            Assert.AreEqual(Start + Seconds(30), now);

            // Saat çok ileri alınsa da kalan süre negatif olmaz.
            var pending = new List<long> { Start };
            long farFuture = CustomOrderRefreshMath.Now(Start + Seconds(3600), Start);
            Assert.AreEqual(0f, CustomOrderRefreshMath.SecondsUntilNext(pending, farFuture, Refresh), Epsilon);
        }

        [Test]
        public void RefreshTicks_ZeroOrNegativeSeconds_RefreshIsImmediate()
        {
            Assert.AreEqual(0L, CustomOrderRefreshMath.RefreshTicks(0));
            Assert.AreEqual(0L, CustomOrderRefreshMath.RefreshTicks(-5));

            var pending = new List<long> { Start };
            Assert.AreEqual(0f, CustomOrderRefreshMath.SecondsUntilNext(pending, Start, CustomOrderRefreshMath.RefreshTicks(0)), Epsilon);
        }

        [Test]
        public void DueTicks_AddsRefreshDuration()
        {
            Assert.AreEqual(Start + Seconds(RefreshSeconds), CustomOrderRefreshMath.DueTicks(Start, Refresh));
        }
    }
}
