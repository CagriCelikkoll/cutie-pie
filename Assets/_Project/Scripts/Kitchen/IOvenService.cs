using System;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Fırın servisi sözleşmesi. Erişim: GameManager.Instance.Ovens.
    /// Süreler gerçek zamanlıdır (oyun kapalıyken de işler). Olaylar: GameEvents.BakeStarted, OvenReady, BatchCollected
    /// (dolap taşarsa StockManager'dan StockOverflow).
    /// </summary>
    public interface IOvenService
    {
        /// <summary>Açık fırın sayısı. Fırın indeksleri 0..Count-1.</summary>
        int Count { get; }

        /// <summary>Fırının durumu; geçersiz indekste Empty.</summary>
        OvenState GetState(int ovenIndex);

        /// <summary>Fırındaki tarifin RecipeData.id değeri; fırın boşsa veya indeks geçersizse null.</summary>
        string GetRecipeId(int ovenIndex);

        /// <summary>Pişmenin bitmesine kalan gerçek zamanlı saniye; pişmiyorsa 0.</summary>
        float GetRemainingSeconds(int ovenIndex);

        /// <summary>Boş fırına tarifi koyar ve BakeStarted yayınlar. Fırın dolu veya tarif bilinmiyorsa false.</summary>
        bool StartBake(int ovenIndex, string recipeId);

        /// <summary>
        /// Ready durumdaki açık fırın sayısı (ör. Shop'taki "N pasta hazır" rozeti; sahne açılışında sorgulanır,
        /// değişimi OvenReady / BatchCollected ile izlenir). Süslenmekte olan fırın da Ready sayılır.
        /// </summary>
        int ReadyCount { get; }

        /// <summary>
        /// Bonussuz toplama: hazır partiyi stoğa ekler (dolap kapasitesinde kırpılır, taşan dilim kaybolur ve StockOverflow
        /// yayınlanır), fırını boşaltır ve BatchCollected yayınlar. Dolap dolu olsa da fırın boşalır.
        /// Kullanım: süslemeden bırakılan parti, eski "Topla", editör menüleri. Hazır değilse false ve hiçbir şey değişmez.
        /// Fırın toplanınca hemen boşaldığı için aynı partiye ikinci bir Collect/CollectWithDecoration false/null döner.
        /// </summary>
        bool Collect(int ovenIndex);

        /// <summary>
        /// Süslenmiş partiyi toplar: krema ve süs tarifteki doğru değerlere eşitse (RecipeData.IsPerfectDecoration)
        /// KitchenSettings.perfectDecorationBonusSlices kadar fazla dilim eklenir; yanlışsa ceza yoktur.
        /// Gerisi Collect ile aynıdır (kırpma, fırını boşaltma, BatchCollected). Fırın süsleme boyunca Ready kalır;
        /// uygulama kapanırsa parti fırında bekler. Hazır değilse (boş, pişiyor, zaten toplandı) null döner, hiçbir şey değişmez.
        /// </summary>
        BatchCollectResult CollectWithDecoration(int ovenIndex, string frostingId, string toppingId);

        /// <summary>
        /// Ödüllü reklam izletir; reklam tamamlanırsa pişen parti anında hazır olur.
        /// callback: parti hızlandırıldıysa true, aksi halde false.
        /// </summary>
        void SpeedUpWithAd(int ovenIndex, Action<bool> callback);
    }
}
