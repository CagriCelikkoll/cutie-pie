using System;
using System.Collections.Generic;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Özel sipariş yenileme süresi hesapları. Unity'den ve SaveSystem'den bağımsızdır (Edit Mode testleri için);
    /// CustomOrderService bu yardımcıyı kullanır. Zamanlar UTC tick'tir.
    /// Saat kuralı fırınlarla (G12) aynıdır: şimdi = max(UTC şimdi, son görülen); cihaz saati geri alınırsa süre ilerlemez.
    /// </summary>
    public static class CustomOrderRefreshMath
    {
        /// <summary>Saat kuralına göre şimdiki zaman: son görülen zamanın gerisine düşmez.</summary>
        public static long Now(long utcNowTicks, long lastSeenUtcTicks) => Math.Max(utcNowTicks, lastSeenUtcTicks);

        /// <summary>Yenileme süresi (tick); negatif saniye 0 sayılır.</summary>
        public static long RefreshTicks(int refreshSeconds) => Math.Max(0, refreshSeconds) * TimeSpan.TicksPerSecond;

        /// <summary>Tamamlanma zamanı verilen yenilemenin biteceği an (tick).</summary>
        public static long DueTicks(long completedTicks, long refreshTicks) => completedTicks + refreshTicks;

        /// <summary>
        /// En yakın yenilemeye kalan saniye. Bekleyen yoksa (veya liste null ise) 0; süresi geçmiş yenileme 0 sayılır.
        /// </summary>
        public static float SecondsUntilNext(IReadOnlyList<long> pendingCompletedTicks, long nowTicks, long refreshTicks)
        {
            if (pendingCompletedTicks == null || pendingCompletedTicks.Count == 0)
            {
                return 0f;
            }

            long best = long.MaxValue;
            foreach (long completed in pendingCompletedTicks)
            {
                best = Math.Min(best, Math.Max(0L, DueTicks(completed, refreshTicks) - nowTicks));
            }

            return (float)best / TimeSpan.TicksPerSecond;
        }
    }
}
