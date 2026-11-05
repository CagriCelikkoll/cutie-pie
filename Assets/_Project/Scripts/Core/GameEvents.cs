using System;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Bölümler arası ortak olay sözleşmesi. Sistemler birbirine yalnızca bu olaylarla bağlanır.
    /// Yeni olay eklemek ortak karardır (contract/... dalı).
    /// Bu dosya, Faz 1 Görev Planı ile çelişki olduğunda güncel sözleşme kaynağıdır.
    /// </summary>
    public static class GameEvents
    {
        // --- Müşteri olayları (yayınlayan: Oynanış / Customer) ---

        /// <summary>Müşteri masaya oturdu.</summary>
        public static event Action<CustomerInfo> CustomerSeated;

        /// <summary>Doğru sipariş teslim edildi. payment: ürün fiyatları toplamı, tip: bahşiş.</summary>
        public static event Action<CustomerInfo, int, int> CustomerServed;

        /// <summary>Sabır bitti, müşteri kızgın gitti (kaçırma).</summary>
        public static event Action<CustomerInfo> CustomerLeftAngry;

        /// <summary>Müşteriye yanlış sipariş götürüldü.</summary>
        public static event Action<CustomerInfo> WrongOrderDelivered;

        // --- Round olayları (yayınlayan: Altyapı / RoundManager) ---

        /// <summary>Round başladı; oynanan seviyenin verisini taşır.</summary>
        public static event Action<LevelData> RoundStarted;

        /// <summary>Round bitti; sonucu taşır.</summary>
        public static event Action<RoundResult> RoundEnded;

        // --- Ekonomi ve gelişim (yayınlayan: Altyapı) ---

        /// <summary>Bir para türünün miktarı değişti. newAmount: yeni toplam miktar.</summary>
        public static event Action<CurrencyType, int> CurrencyChanged;

        /// <summary>Geliştirme satın alındı. newLevel: geliştirmenin yeni seviyesi.</summary>
        public static event Action<UpgradeData, int> UpgradePurchased;

        public static void RaiseCustomerSeated(CustomerInfo customer) => CustomerSeated?.Invoke(customer);

        public static void RaiseCustomerServed(CustomerInfo customer, int payment, int tip) =>
            CustomerServed?.Invoke(customer, payment, tip);

        public static void RaiseCustomerLeftAngry(CustomerInfo customer) => CustomerLeftAngry?.Invoke(customer);

        public static void RaiseWrongOrderDelivered(CustomerInfo customer) => WrongOrderDelivered?.Invoke(customer);

        public static void RaiseRoundStarted(LevelData level) => RoundStarted?.Invoke(level);

        public static void RaiseRoundEnded(RoundResult result) => RoundEnded?.Invoke(result);

        public static void RaiseCurrencyChanged(CurrencyType currency, int newAmount) =>
            CurrencyChanged?.Invoke(currency, newAmount);

        public static void RaiseUpgradePurchased(UpgradeData upgrade, int newLevel) =>
            UpgradePurchased?.Invoke(upgrade, newLevel);

        // Domain reload kapalıyken önceki Play oturumundan kalan abonelikleri temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions()
        {
            CustomerSeated = null;
            CustomerServed = null;
            CustomerLeftAngry = null;
            WrongOrderDelivered = null;
            RoundStarted = null;
            RoundEnded = null;
            CurrencyChanged = null;
            UpgradePurchased = null;
        }
    }
}
