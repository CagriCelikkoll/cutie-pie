using System;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Fırın durumu hesapları. Unity'den ve SaveSystem'den bağımsızdır (Edit Mode testleri için); OvenService kullanır.
    /// Zamanlar UTC tick'tir; şimdiki zaman saat kuralına göre verilmelidir (max(UTC şimdi, son görülen)).
    /// </summary>
    public static class OvenStateMath
    {
        /// <summary>
        /// Fırının durumu: parti yoksa Empty, geçen süre pişme süresine ulaştıysa Ready, değilse Baking.
        /// Başlangıç ileride görünse de (bozuk kayıt) geçen süre negatif olmaz.
        /// </summary>
        public static OvenState StateAt(bool hasBake, long startTicks, long bakeTicks, long nowTicks)
        {
            if (!hasBake)
            {
                return OvenState.Empty;
            }

            return Elapsed(startTicks, nowTicks) >= bakeTicks ? OvenState.Ready : OvenState.Baking;
        }

        /// <summary>0..count-1 fırınlarından Ready olanların sayısı. getState null veya count ≤ 0 ise 0.</summary>
        public static int CountReady(int count, Func<int, OvenState> getState)
        {
            if (getState == null)
            {
                return 0;
            }

            int ready = 0;
            for (int i = 0; i < count; i++)
            {
                if (getState(i) == OvenState.Ready)
                {
                    ready++;
                }
            }

            return ready;
        }

        /// <summary>Başlangıçtan bu yana geçen süre (tick, en az 0).</summary>
        public static long Elapsed(long startTicks, long nowTicks) => Math.Max(0L, nowTicks - startTicks);
    }
}
