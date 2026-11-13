using System;

namespace CutiePie.Core
{
    /// <summary>
    /// Gerçek zamanlı sistemlerin (fırın, sipariş panosu) ortak UTC saati.
    /// Build'de doğrudan DateTime.UtcNow'dur; saat kaydırma yalnızca editörde (test menüleri) vardır.
    /// </summary>
    public static class GameClock
    {
        /// <summary>Şimdiki UTC zaman (editörde test kaydırması eklenmiş hâliyle).</summary>
        public static DateTime UtcNow
        {
            get
            {
#if UNITY_EDITOR
                return DateTime.UtcNow + DebugOffset;
#else
                return DateTime.UtcNow;
#endif
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Yalnızca editör: saate eklenen test kaydırması (cihaz saatini ileri/geri almayı taklit eder).
        /// Bellekte tutulur; Play'den çıkınca (domain reload) sıfırlanır.
        /// </summary>
        public static TimeSpan DebugOffset { get; set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DebugOffset = TimeSpan.Zero;
#endif
    }
}
