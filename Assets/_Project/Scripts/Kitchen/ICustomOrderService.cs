using System.Collections.Generic;
using CutiePie.Core.Data;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Özel sipariş servisi sözleşmesi. Erişim: GameManager.Instance.Orders.
    /// Olaylar: GameEvents.CustomOrderBoardChanged, CustomOrderCompleted.
    /// </summary>
    public interface ICustomOrderService
    {
        /// <summary>Panodaki fişler (en fazla KitchenSettings.boardSize).</summary>
        IReadOnlyList<CustomOrder> Board { get; }

        /// <summary>Bekleyen yenileme sayısı (panodaki boş yerler; her biri süresi dolunca yeni fiş olur).</summary>
        int PendingRefreshCount { get; }

        /// <summary>
        /// En yakın yenilemeye kalan gerçek zamanlı saniye; bekleyen yoksa 0. Saat kuralı fırınlarla aynıdır:
        /// şimdi = max(UTC şimdi, son görülen); cihaz saati geri alınırsa süre ilerlemez.
        /// </summary>
        float GetSecondsUntilNextRefresh();

        /// <summary>Oyuncunun pastasını fişle karşılaştırır; hiçbir şeyi değiştirmez. Fiş yoksa null.</summary>
        CustomOrderResult Score(string orderId, CakeBuild build);

        /// <summary>
        /// Puanlar, altını (ve bahşişi) verir, fişi panodan kaldırır; CustomOrderCompleted ve CustomOrderBoardChanged yayınlar.
        /// Fiş yoksa null döner ve hiçbir şey değişmez.
        /// </summary>
        CustomOrderResult Complete(string orderId, CakeBuild build);
    }
}
