using System;

namespace CutiePie.Core.Data
{
    /// <summary>Bir round'un sonucu. RoundEnded olayıyla taşınır.</summary>
    [Serializable]
    public class RoundResult
    {
        /// <summary>Round'da kazanılan toplam altın (ödeme + bahşiş + kombo farkı).</summary>
        public int goldEarned;

        /// <summary>Bu toplamın bahşişten gelen kısmı.</summary>
        public int tipsEarned;

        /// <summary>Round içindeki en yüksek art arda doğru teslim sayısı.</summary>
        public int highestCombo;

        /// <summary>Kazanılan yıldız (0-3).</summary>
        public int stars;

        /// <summary>Kaçırılan müşteri sayısı.</summary>
        public int misses;

        /// <summary>Round süre dolarak mı bitti (true), kaçırma sınırıyla erken mi bitti (false).</summary>
        public bool completed;
    }
}
