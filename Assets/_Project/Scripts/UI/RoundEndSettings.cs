using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Round sonu ekranının ve geliştirme mağazasının zamanlama ve renk ayarları. Asset: Assets/_Project/Data/Salon/RoundEndSettings.asset
    /// (RoundEndUIBuilder yoksa oluşturur). Süreler gerçek zamanlı saniyedir (Time.timeScale'den etkilenmez).
    /// </summary>
    [CreateAssetMenu(fileName = "RoundEndSettings", menuName = "Cutie Pie/UI/Round End Settings")]
    public class RoundEndSettings : ScriptableObject
    {
        [Header("Değer sayma animasyonu")]
        [Tooltip("Altın ve bahşişin 0'dan sonuca sayarak çıkma süresi.")]
        [Min(0f)] public float countUpDuration = 0.8f;

        [Tooltip("2x ödülden sonra altının yeni toplama sayarak çıkma süresi.")]
        [Min(0f)] public float bonusCountUpDuration = 0.6f;

        [Header("Yıldızlar")]
        [Tooltip("Sayma bittikten sonra ilk yıldıza kadar bekleme.")]
        [Min(0f)] public float firstStarDelay = 0.2f;

        [Tooltip("İki yıldızın dolmaya başlaması arasındaki süre.")]
        [Min(0f)] public float starInterval = 0.45f;

        [Tooltip("Bir yıldızın dolma (büyüyüp yerine oturma) süresi.")]
        [Min(0.01f)] public float starFillDuration = 0.3f;

        [Tooltip("Dolarken yıldızın ulaştığı en büyük ölçek.")]
        [Min(1f)] public float starPopScale = 1.35f;

        public Color starEmptyColor = new Color(0.86f, 0.82f, 0.84f, 1f);
        public Color starFilledColor = new Color(1f, 0.8f, 0.3f, 1f);

        [Header("Panel renkleri (builder prefab'ı kurarken kullanır)")]
        [Tooltip("Arkadaki salonu soluklaştıran perde.")]
        public Color dimColor = new Color(0.25f, 0.18f, 0.22f, 0.55f);

        public Color cardColor = new Color(1f, 0.96f, 0.93f, 1f);
        public Color cardBorderColor = new Color(0.98f, 0.76f, 0.82f, 1f);
        public Color statRowColor = new Color(1f, 0.89f, 0.91f, 1f);
        public Color textColor = new Color(0.42f, 0.27f, 0.32f, 1f);
        public Color earlyEndColor = new Color(0.9f, 0.42f, 0.45f, 1f);

        [Header("Buton renkleri")]
        public Color playAgainColor = new Color(0.55f, 0.84f, 0.62f, 1f);
        public Color adButtonColor = new Color(1f, 0.82f, 0.38f, 1f);
        public Color shopButtonColor = new Color(0.62f, 0.8f, 0.98f, 1f);
        public Color upgradesButtonColor = new Color(0.82f, 0.7f, 0.96f, 1f);
        public Color closeButtonColor = new Color(0.98f, 0.7f, 0.76f, 1f);
        public Color buttonTextColor = new Color(0.3f, 0.2f, 0.25f, 1f);

        [Header("Geliştirme mağazası")]
        [Tooltip("Her geliştirme kartının zemini.")]
        public Color upgradeCardColor = new Color(1f, 0.91f, 0.86f, 1f);

        [Tooltip("Geliştirmenin simgesi yoksa görünen yer tutucu kutunun rengi.")]
        public Color upgradeIconColor = new Color(0.82f, 0.7f, 0.96f, 1f);

        [Tooltip("Satın alınabilir geliştirmenin buton rengi. Altın yetmezse buton soluk görünür.")]
        public Color buyButtonColor = new Color(0.55f, 0.84f, 0.62f, 1f);

        [Tooltip("En üst seviyedeki geliştirmenin \"Tam\" buton rengi (soluklaşmaz).")]
        public Color maxedButtonColor = new Color(1f, 0.82f, 0.38f, 1f);

        [Tooltip("Altın simgesi (fiyat ve mevcut altın).")]
        public Color goldIconColor = new Color(1f, 0.76f, 0.2f, 1f);
    }
}
