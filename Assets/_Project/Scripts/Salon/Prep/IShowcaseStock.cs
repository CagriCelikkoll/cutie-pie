using System;
using System.Collections.Generic;
using CutiePie.Core.Data;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon vitrinindeki pasta dilimi stoğu. Salon yalnızca bu arayüzü bilir;
    /// uygulaması Core'daki ortak stoğa (StockManager) bağlanan SharedShowcaseStock'tur.
    /// </summary>
    public interface IShowcaseStock
    {
        /// <summary>Vitrinde gösterilen pastalar (stoğu 0 olanlar dahil).</summary>
        IReadOnlyList<ProductData> Available { get; }

        /// <summary>Bir ürünün stoğu değişti.</summary>
        event Action<ProductData> Changed;

        int GetSlices(ProductData product);

        /// <summary>Bir dilim düşer; stok yoksa false.</summary>
        bool TryTakeSlice(ProductData product);

        /// <summary>Round başında seviyenin menüsünü bağlar ve her ürün için Changed yayınlar (stok eklemez).</summary>
        void Refill(LevelData level);
    }
}
