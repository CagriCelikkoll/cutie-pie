namespace CutiePie.Kitchen
{
    /// <summary>
    /// Süslenmiş partinin toplanma sonucu (IOvenService.CollectWithDecoration). Toplanamadıysa sonuç null'dur.
    /// baseSlices + bonusSlices = addedSlices + lostSlices.
    /// </summary>
    public class BatchCollectResult
    {
        /// <summary>Stoğa eklenen ürünün ProductData.id değeri.</summary>
        public string productId;

        /// <summary>Tarifin parti dilimi (RecipeData.slicesPerBatch).</summary>
        public int baseSlices;

        /// <summary>Kusursuz süsleme bonusu (KitchenSettings.perfectDecorationBonusSlices); kusursuz değilse 0.</summary>
        public int bonusSlices;

        /// <summary>Krema ve süs tarifteki doğru değerlere eşit miydi.</summary>
        public bool perfect;

        /// <summary>Dolap kapasitesinde kırpıldıktan sonra stoğa gerçekten giren dilim (0 olabilir).</summary>
        public int addedSlices;

        /// <summary>Dolap dolu olduğu için kaybolan dilim (GameEvents.StockOverflow ile de bildirilir).</summary>
        public int lostSlices;
    }
}
