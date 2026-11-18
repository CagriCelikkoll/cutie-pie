using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Özel sipariş ekranlarının (G20: sipariş panosu, pasta yapımı, sonuç) renk, ölçü ve animasyon ayarları.
    /// Asset: Assets/_Project/Data/Salon/OrderUISettings.asset ("Cutie Pie/UI/Sipariş Ekranını Kur" yoksa oluşturur).
    /// Ortak palet (kart, kenarlık, yazı, altın simgesi) RoundEndSettings'ten gelir; burada yalnızca siparişe özel değerler durur.
    /// Süreler gerçek zamanlı saniyedir (Time.timeScale'den etkilenmez).
    /// </summary>
    [CreateAssetMenu(fileName = "OrderUISettings", menuName = "Cutie Pie/UI/Order UI Settings")]
    public class OrderUISettings : ScriptableObject
    {
        [Header("Pano")]
        [Tooltip("Mutfağın sağındaki pano sütununun zemini.")]
        public Color boardColor = new Color(1f, 0.89f, 0.91f, 1f);

        [Tooltip("Fiş kartının zemini.")]
        public Color ticketColor = new Color(1f, 0.98f, 0.94f, 1f);

        [Tooltip("Boş yerin (yeni sipariş bekleniyor) zemini.")]
        public Color emptySlotColor = new Color(0.96f, 0.9f, 0.92f, 1f);

        [Header("Pasta çizimi (üstten görünüm)")]
        [Tooltip("Taban dairesinin pasta kutusuna oranı (0–1).")]
        [Range(0.1f, 1f)] public float baseSizeRatio = 0.88f;

        [Tooltip("Krema dairesinin ve süs yüzeyinin pasta kutusuna oranı (0–1). Süs yüzeyi bu karedir.")]
        [Range(0.1f, 1f)] public float creamSizeRatio = 0.74f;

        [Tooltip("Süs görselinin süs yüzeyine oranı (0–1); küçük fişte ve büyük pastada aynı oranda görünür.")]
        [Range(0.02f, 0.5f)] public float decorationSizeRatio = 0.2f;

        [Tooltip("Henüz seçilmemiş taban/kremanın soluk yer tutucu rengi.")]
        public Color missingPartColor = new Color(0.93f, 0.88f, 0.9f, 0.6f);

        [Tooltip("Kutu renginin saydamlığı (pastanın arkasında zemin gibi durur).")]
        [Range(0f, 1f)] public float boxAlpha = 1f;

        [Header("Pasta yapım ekranı")]
        [Tooltip("Tam ekran zemin.")]
        public Color builderBackgroundColor = new Color(1f, 0.93f, 0.9f, 1f);

        [Tooltip("Alttaki seçenek şeridinin zemini.")]
        public Color optionBarColor = new Color(1f, 0.89f, 0.91f, 1f);

        [Tooltip("Seçenek kartının zemini.")]
        public Color optionColor = new Color(1f, 0.97f, 0.95f, 1f);

        [Tooltip("Seçenek kartının çerçevesi (seçili değil).")]
        public Color optionFrameColor = new Color(0.98f, 0.76f, 0.82f, 1f);

        [Tooltip("Seçili seçeneğin çerçevesi.")]
        public Color optionSelectedFrameColor = new Color(0.55f, 0.84f, 0.62f, 1f);

        public Color backButtonColor = new Color(0.98f, 0.7f, 0.76f, 1f);
        public Color nextButtonColor = new Color(0.62f, 0.8f, 0.98f, 1f);
        public Color confirmButtonColor = new Color(0.55f, 0.84f, 0.62f, 1f);

        [Tooltip("Sürüklenen süsün saydamlığı.")]
        [Range(0f, 1f)] public float dragGhostAlpha = 0.85f;

        [Tooltip("Sürüklenen süsün pastadaki boyuna göre ölçeği (elde tutuluyor hissi).")]
        [Min(0.1f)] public float dragGhostScale = 1.15f;

        [Header("Sonuç")]
        [Tooltip("Benzerlik yüzdesinin ve altının 0'dan sayarak artma süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float countUpDuration = 1.2f;
    }
}
