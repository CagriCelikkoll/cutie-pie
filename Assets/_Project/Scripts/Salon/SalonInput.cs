using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'daki oyuncu girdisinin (sürükleme, müşteriye ve masaya dokunma) ortak kilidi.
    /// SalonRound round bitince kilitler, yeni round başlayınca açar.
    /// </summary>
    public static class SalonInput
    {
        /// <summary>Girdi kapalı mı?</summary>
        public static bool IsLocked { get; private set; }

        /// <summary>Girdiyi kapatır; o an sürüklenen parça varsa yerine konur.</summary>
        public static void Lock()
        {
            IsLocked = true;
            DragItem.CancelActiveDrag();
        }

        public static void Unlock() => IsLocked = false;

        // Domain reload kapalıyken önceki Play oturumundan kalan kilidi temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsLocked = false;
        }
    }
}
