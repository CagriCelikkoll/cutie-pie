using System;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Fırın servisi sözleşmesi. Erişim: GameManager.Instance.Ovens.
    /// Süreler gerçek zamanlıdır (oyun kapalıyken de işler). Olaylar: GameEvents.BakeStarted, OvenReady, BatchCollected.
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

        /// <summary>Hazır partiyi stoğa ekler, fırını boşaltır ve BatchCollected yayınlar. Hazır değilse false.</summary>
        bool Collect(int ovenIndex);

        /// <summary>
        /// Ödüllü reklam izletir; reklam tamamlanırsa pişen parti anında hazır olur.
        /// callback: parti hızlandırıldıysa true, aksi halde false.
        /// </summary>
        void SpeedUpWithAd(int ovenIndex, Action<bool> callback);
    }
}
