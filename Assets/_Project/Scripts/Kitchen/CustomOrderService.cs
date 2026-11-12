using System;
using System.Collections.Generic;
using CutiePie.Core.Data;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Özel sipariş servisi. G11'de yalnızca boş gövde: pano boştur. Mantık G14'te yazılacak.
    /// </summary>
    public class CustomOrderService : ICustomOrderService
    {
        public IReadOnlyList<CustomOrder> Board => Array.Empty<CustomOrder>();

        // TODO (G14): benzerlik ve ödül hesabı (KitchenSettings ağırlıkları ve ödül tablosu).
        public CustomOrderResult Score(string orderId, CakeBuild build) => null;

        // TODO (G14): altını ver, fişi kaldır, yenilemeyi planla, olayları yayınla.
        public CustomOrderResult Complete(string orderId, CakeBuild build) => null;
    }
}
