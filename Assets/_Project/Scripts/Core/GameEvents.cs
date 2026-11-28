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

        /// <summary>
        /// Round'daki kaçırma sayısı değişti. misses: yeni kaçırma sayısı, maxMisses: round'u bitiren sınır (LevelData.maxMisses).
        /// Round başında (RoundStarted'dan hemen sonra) ilk değerle (0, maxMisses) de yayınlanır. Kaçırma round'u bitiriyorsa
        /// bu olay RoundEnded'dan önce gelir (gösterge son değeri de gösterir).
        /// </summary>
        public static event Action<int, int> MissesChanged;

        // --- Ekonomi ve gelişim (yayınlayan: Altyapı) ---

        /// <summary>Bir para türünün miktarı değişti. newAmount: yeni toplam miktar.</summary>
        public static event Action<CurrencyType, int> CurrencyChanged;

        /// <summary>Geliştirme satın alındı. newLevel: geliştirmenin yeni seviyesi.</summary>
        public static event Action<UpgradeData, int> UpgradePurchased;

        // --- Stok (yayınlayan: Altyapı / StockManager) ---

        /// <summary>Bir ürünün stoğu değişti. amount: yeni toplam dilim sayısı (değişim miktarı değil).</summary>
        public static event Action<string, int> StockChanged;

        /// <summary>
        /// Stoğa ekleme dolap kapasitesini aştı; fazlası kırpılıp kayboldu. lostSlices: stoğa giremeyen dilim sayısı (&gt; 0).
        /// Kırpılmayan kısım için StockChanged ayrıca yayınlanır (eklenen 0 değilse).
        /// </summary>
        public static event Action<string, int> StockOverflow;

        // --- Fırın (yayınlayan: Altyapı / OvenService) ---

        /// <summary>Fırına tarif kondu, pişirme başladı.</summary>
        public static event Action<int, string> BakeStarted;

        /// <summary>
        /// Fırındaki parti hazır. Süre oyun açıkken dolduğunda bir kez; oyun açılışında hazır bulunan fırınlar için de yayınlanır.
        /// </summary>
        public static event Action<int> OvenReady;

        /// <summary>
        /// Hazır parti toplandı, fırın boşaldı. slices: stoğa gerçekten eklenen dilim sayısı (kusursuz bonusu dahil,
        /// dolap kapasitesinde kırpıldıktan sonra; dolap doluysa 0 olabilir).
        /// </summary>
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

        public static void RaiseMissesChanged(int misses, int maxMisses) => MissesChanged?.Invoke(misses, maxMisses);

        public static void RaiseCurrencyChanged(CurrencyType currency, int newAmount) =>
            CurrencyChanged?.Invoke(currency, newAmount);

        public static void RaiseUpgradePurchased(UpgradeData upgrade, int newLevel) =>
            UpgradePurchased?.Invoke(upgrade, newLevel);

        public static void RaiseStockChanged(string productId, int amount) => StockChanged?.Invoke(productId, amount);

        public static void RaiseStockOverflow(string productId, int lostSlices) => StockOverflow?.Invoke(productId, lostSlices);

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
            MissesChanged = null;
            CurrencyChanged = null;
            UpgradePurchased = null;
            StockChanged = null;
            StockOverflow = null;
            BakeStarted = null;
            OvenReady = null;
            BatchCollected = null;
            CustomOrderBoardChanged = null;
            CustomOrderCompleted = null;
        }
    }
}
