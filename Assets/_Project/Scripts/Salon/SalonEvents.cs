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

        public static void RaiseCustomerTapped(Customer customer) => CustomerTapped?.Invoke(customer);

        // Domain reload kapalıyken önceki Play oturumundan kalan abonelikleri temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions()
        {
            CustomerTapped = null;
        }
    }
}
