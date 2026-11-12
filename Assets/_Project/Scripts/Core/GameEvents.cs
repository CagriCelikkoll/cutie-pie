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

        // --- Stok (yayınlayan: Altyapı / StockManager) ---

        /// <summary>Bir ürünün stoğu değişti. amount: yeni toplam dilim sayısı (değişim miktarı değil).</summary>
        public static event Action<string, int> StockChanged;

        // --- Fırın (yayınlayan: Altyapı / OvenService) ---

        /// <summary>Fırına tarif kondu, pişirme başladı.</summary>
        public static event Action<int, string> BakeStarted;

        /// <summary>
        /// Fırındaki parti hazır. Süre oyun açıkken dolduğunda bir kez; oyun açılışında hazır bulunan fırınlar için de yayınlanır.
        /// </summary>
        public static event Action<int> OvenReady;

        /// <summary>Hazır parti toplandı ve stoğa eklendi. slices: eklenen dilim sayısı.</summary>
        public static event Action<string, int> BatchCollected;

        // --- Özel sipariş (yayınlayan: Altyapı / CustomOrderService) ---

        /// <summary>Panodaki fişler değişti (fiş eklendi veya kaldırıldı).</summary>
        public static event Action CustomOrderBoardChanged;

        /// <summary>Özel sipariş tamamlandı; altın verildi, fiş panodan kalktı.</summary>
        public static event Action<CustomOrderResult> CustomOrderCompleted;

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

        public static void RaiseStockChanged(string productId, int amount) => StockChanged?.Invoke(productId, amount);

        public static void RaiseBakeStarted(int ovenIndex, string recipeId) => BakeStarted?.Invoke(ovenIndex, recipeId);

        public static void RaiseOvenReady(int ovenIndex) => OvenReady?.Invoke(ovenIndex);

        public static void RaiseBatchCollected(string productId, int slices) => BatchCollected?.Invoke(productId, slices);

        public static void RaiseCustomOrderBoardChanged() => CustomOrderBoardChanged?.Invoke();

        public static void RaiseCustomOrderCompleted(CustomOrderResult result) => CustomOrderCompleted?.Invoke(result);

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
            StockChanged = null;
            BakeStarted = null;
            OvenReady = null;
            BatchCollected = null;
            CustomOrderBoardChanged = null;
            CustomOrderCompleted = null;
        }
    }
}
