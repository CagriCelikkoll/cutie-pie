using System;

namespace CutiePie.Core.Data
{
    /// <summary>Bir özel siparişin puanlama sonucu.</summary>
    [Serializable]
    public class CustomOrderResult
    {
        /// <summary>Puanlanan fişin id'si.</summary>
        public string orderId;

        /// <summary>Hedefe benzerlik, 0–1 arası.</summary>
        public float similarity;

        /// <summary>Yıldız sayısı, 0–3 arası.</summary>
        public int stars;

        /// <summary>Kazanılan altın (bahşiş hariç).</summary>
        public int gold;

        /// <summary>Bahşiş altını.</summary>
        public int tip;
    }
}
