using System;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Fırın servisi. G11'de yalnızca boş gövde: hiç fırın yokmuş gibi davranır. Mantık G12'de yazılacak.
    /// </summary>
    public class OvenService : IOvenService
    {
        public int Count => 0;

        public OvenState GetState(int ovenIndex) => OvenState.Empty;

        public string GetRecipeId(int ovenIndex) => null;

        public float GetRemainingSeconds(int ovenIndex) => 0f;

        // TODO (G12): pişirmeyi başlat, kaydet, BakeStarted yayınla.
        public bool StartBake(int ovenIndex, string recipeId) => false;

        // TODO (G12): StockManager.Add ile stoğa ekle, fırını boşalt, BatchCollected yayınla.
        public bool Collect(int ovenIndex) => false;

        // TODO (G12): GameManager.Instance.Ads üzerinden ödüllü reklam.
        public void SpeedUpWithAd(int ovenIndex, Action<bool> callback) => callback?.Invoke(false);
    }
}
