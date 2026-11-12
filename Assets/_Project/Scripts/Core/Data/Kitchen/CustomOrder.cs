using System;

namespace CutiePie.Core.Data
{
    /// <summary>Sipariş panosundaki bir fiş (çalışma zamanı verisi; pano kaydına da yazılır).</summary>
    [Serializable]
    public class CustomOrder
    {
        /// <summary>Fişin benzersiz kimliği.</summary>
        public string id;

        /// <summary>Oyuncunun yapması gereken pasta.</summary>
        public CakeBuild target = new CakeBuild();

        /// <summary>Tam benzerlikte kazanılacak taban altın (benzerlikle çarpılır).</summary>
        public int baseReward;
    }
}
