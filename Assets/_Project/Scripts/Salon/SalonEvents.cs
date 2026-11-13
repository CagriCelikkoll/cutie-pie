using System;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Yalnızca Salon içinde kullanılan olaylar (GameEvents ortak sözleşmesinin parçası değildir).
    /// Bölümler arası iletişim için GameEvents kullanılır.
    /// </summary>
    public static class SalonEvents
    {
        /// <summary>Oyuncu masada oturan bir müşteriye dokundu (siparişi fişe düşer).</summary>
        public static event Action<Customer> CustomerTapped;

        /// <summary>
        /// Round başladı ama vitrinde hiç pasta stoğu yok (G16). SalonRound yayınlar; UI/NoStockPanel açılır.
        /// Girdi ve müşteri üretimi, oyuncu "Yine de aç" diyene kadar bekler.
        /// </summary>
        public static event Action NoCakeStockAtRoundStart;

        /// <summary>Oyuncu stok uyarısında "Yine de aç"ı seçti; round yalnızca içecek siparişleriyle başlar.</summary>
        public static event Action NoStockContinueRequested;

        public static void RaiseCustomerTapped(Customer customer) => CustomerTapped?.Invoke(customer);

        /// <summary>Olayı yayınlar. Dinleyen yoksa (panel sahnede değil) false döner; çağıran beklemeden devam etmelidir.</summary>
        public static bool RaiseNoCakeStockAtRoundStart()
        {
            if (NoCakeStockAtRoundStart == null)
            {
                return false;
            }

            NoCakeStockAtRoundStart.Invoke();
            return true;
        }

        public static void RaiseNoStockContinueRequested() => NoStockContinueRequested?.Invoke();

        // Domain reload kapalıyken önceki Play oturumundan kalan abonelikleri temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions()
        {
            CustomerTapped = null;
            NoCakeStockAtRoundStart = null;
            NoStockContinueRequested = null;
        }
    }
}
