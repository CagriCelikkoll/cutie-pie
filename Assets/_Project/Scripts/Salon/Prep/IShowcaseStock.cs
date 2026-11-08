using System;
using System.Collections.Generic;
using CutiePie.Core.Data;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon vitrinindeki pasta dilimi stoğu. Salon yalnızca bu arayüzü bilir;
    /// Faz 1'de LocalShowcaseStock, Faz 2'de Core'daki StockManager'a bağlanan bir adaptör kullanılır.
    /// </summary>
    public interface IShowcaseStock
    {
        /// <summary>Vitrinde stoğu tutulan pastalar.</summary>
        IReadOnlyList<ProductData> Available { get; }

        /// <summary>Bir ürünün stoğu değişti.</summary>
        event Action<ProductData> Changed;

        int GetSlices(ProductData product);

        /// <summary>Bir dilim düşer; stok yoksa false.</summary>
        bool TryTakeSlice(ProductData product);

        /// <summary>Round başında stoğu seviyenin menüsüne göre doldurur.</summary>
        void Refill(LevelData level);
    }
}
