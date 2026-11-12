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

        /// <summary>Oyuncunun pastasını fişle karşılaştırır; hiçbir şeyi değiştirmez. Fiş yoksa null.</summary>
        CustomOrderResult Score(string orderId, CakeBuild build);

        /// <summary>
        /// Puanlar, altını (ve bahşişi) verir, fişi panodan kaldırır; CustomOrderCompleted ve CustomOrderBoardChanged yayınlar.
        /// Fiş yoksa null döner ve hiçbir şey değişmez.
        /// </summary>
        CustomOrderResult Complete(string orderId, CakeBuild build);
    }
}
