using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Fırın ekranının (G19) renk ve animasyon ayarları. Asset: Assets/_Project/Data/Salon/OvenPanelSettings.asset
    /// ("Cutie Pie/UI/Fırın Panelini Kur" yoksa oluşturur). Ortak palet (kart, kenarlık, yazı, reklam/kapat butonu)
    /// RoundEndSettings'ten gelir; burada yalnızca fırına özel değerler durur.
    /// Süreler gerçek zamanlı saniyedir (Time.timeScale'den etkilenmez).
    /// </summary>
    [CreateAssetMenu(fileName = "OvenPanelSettings", menuName = "Cutie Pie/UI/Oven Panel Settings")]
    public class OvenPanelSettings : ScriptableObject
    {
        [Header("Ekran")]
        [Tooltip("Panelin tam ekran zemini.")]
        public Color backgroundColor = new Color(1f, 0.93f, 0.9f, 1f);

        [Tooltip("Üstteki stok şeridinin zemini.")]
        public Color stockBarColor = new Color(1f, 0.89f, 0.91f, 1f);

        [Tooltip("Stok şeridindeki her pastanın zemini.")]
        public Color stockEntryColor = new Color(1f, 0.97f, 0.95f, 1f);

        [Header("Fırın kartı zeminleri")]
        public Color emptyCardColor = new Color(1f, 0.91f, 0.86f, 1f);
        public Color bakingCardColor = new Color(1f, 0.87f, 0.78f, 1f);
        public Color readyCardColor = new Color(0.88f, 0.96f, 0.86f, 1f);
        public Color lockedCardColor = new Color(0.9f, 0.88f, 0.89f, 1f);

        [Tooltip("Kilitli fırın kartının saydamlığı (soluk görünüm).")]
        [Range(0f, 1f)] public float lockedAlpha = 0.55f;

        [Header("Butonlar")]
        public Color bakeButtonColor = new Color(0.55f, 0.84f, 0.62f, 1f);
        public Color collectButtonColor = new Color(1f, 0.82f, 0.38f, 1f);
        public Color unlockButtonColor = new Color(0.82f, 0.7f, 0.96f, 1f);

        [Header("İlerleme çubuğu")]
        public Color progressBackColor = new Color(0.98f, 0.8f, 0.84f, 1f);
        public Color progressFillColor = new Color(0.95f, 0.55f, 0.62f, 1f);

        [Header("Simgeler")]
        [Tooltip("Tarifin/ürünün simgesi yoksa görünen yer tutucu kutunun rengi.")]
        public Color iconPlaceholderColor = new Color(0.96f, 0.72f, 0.78f, 1f);

        [Header("Hazır fırın parlaması")]
        public Color glowColor = new Color(1f, 0.93f, 0.55f, 1f);

        [Tooltip("Parlamanın saniyedeki nabız sayısı.")]
        [Min(0.01f)] public float glowPulsesPerSecond = 1.2f;

        [Range(0f, 1f)] public float glowMinAlpha = 0.25f;
        [Range(0f, 1f)] public float glowMaxAlpha = 0.9f;

        [Tooltip("Nabızda Topla butonunun ulaştığı en büyük ölçek.")]
        [Min(1f)] public float collectPulseScale = 1.06f;

        [Header("Topla: dilimlerin stoğa uçması")]
        [Tooltip("Bir partide en fazla kaç simge uçar (dilim sayısı daha azsa o kadar).")]
        [Min(1)] public int maxFlyIcons = 6;

        [Tooltip("Tek simgenin karttan stok kalemine uçma süresi.")]
        [Min(0.01f)] public float flyDuration = 0.55f;

        [Tooltip("Ardışık simgelerin çıkışı arasındaki süre.")]
        [Min(0f)] public float flyStagger = 0.07f;

        [Tooltip("Uçuş kavisinin yüksekliği (referans piksel).")]
        public float flyArcHeight = 180f;

        [Tooltip("Uçan simgenin boyutu (referans piksel).")]
        [Min(1f)] public float flyIconSize = 72f;

        [Tooltip("Simge inince stok kaleminin ulaştığı en büyük ölçek.")]
        [Min(1f)] public float landPopScale = 1.2f;

        [Min(0.01f)] public float landPopDuration = 0.15f;
    }
}
