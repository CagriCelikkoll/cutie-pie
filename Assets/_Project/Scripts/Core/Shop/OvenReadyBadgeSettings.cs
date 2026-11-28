using TMPro;
using UnityEngine;

namespace CutiePie.Core.Shop
{
    /// <summary>
    /// Shop'taki "N pasta hazır" rozetinin görünüm ve yerleşim değerleri. Rozet (OvenReadyBadge) bunları Awake'te uygular.
    /// Uzunluk değerleri Canvas birimidir (referans çözünürlük 1920x1080).
    /// </summary>
    [CreateAssetMenu(fileName = "OvenReadyBadgeSettings", menuName = "Cutie Pie/Shop/Oven Ready Badge Settings")]
    public class OvenReadyBadgeSettings : ScriptableObject
    {
        [Header("Renkler")]
        [Tooltip("Hap zemini (tasarım: #3E8E6E).")]
        public Color backgroundColor;

        [Tooltip("Hap kenarı.")]
        public Color borderColor;

        [Tooltip("Yazı rengi.")]
        public Color textColor;

        [Header("Kenar ve boyut")]
        [Tooltip("Beyaz kenarın kalınlığı.")]
        [Min(0f)] public float borderWidth;

        [Tooltip("Hapın yüksekliği (kenar dahil). Köşeler bunun yarısı kadar yuvarlanır.")]
        [Min(1f)] public float height;

        [Tooltip("Yazının iki yanındaki boşluk (kenarın içinden ölçülür).")]
        [Min(0f)] public float horizontalPadding;

        [Header("Yazı")]
        public TMP_FontAsset font;

        [Min(1f)] public float fontSize;

        [Tooltip("TMP Bold stili (projede Nunito Black yok; Nunito-Bold + Bold kullanılır).")]
        public bool bold;

        [Header("Yerleşim (Mutfak butonuna göre)")]
        [Tooltip("Hapın sağ kenarının butonun sağ kenarından içeri doğru uzaklığı. Rozet yatayda butonun dışına taşmaz.")]
        [Min(0f)] public float insetFromButtonRight;

        [Tooltip("Hap ortasının butonun üst kenarına göre dikey kayması (0: üst kenara ortalı, + yukarı).")]
        public float offsetFromButtonTop;

        [Tooltip("Güvenli alan (çentik) kenarına bırakılacak en az boşluk. Taşarsa rozet içeri kaydırılır.")]
        [Min(0f)] public float safeAreaMargin;

        [Header("Yerelleştirme anahtarları")]
        [Tooltip("Sayı 1 iken kullanılan anahtar (ör. en: \"1 cake ready\").")]
        public string oneKey;

        [Tooltip("Sayı 1 değilken kullanılan anahtar; {0} sayıdır (ör. en: \"{0} cakes ready\").")]
        public string otherKey;
    }
}
