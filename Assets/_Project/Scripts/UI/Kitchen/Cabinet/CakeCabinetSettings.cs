using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Pasta Dolabı ekranının (G22) eşik, kutucuk ve renk ayarları. Asset: Assets/_Project/Data/Salon/CakeCabinetSettings.asset
    /// ("Cutie Pie/UI/Pasta Dolabı Oluştur" yoksa oluşturur).
    /// Kutucuk sayısı değişirse prefab'daki kutucukların eşitlenmesi için builder menüsü yeniden çalıştırılmalıdır.
    /// </summary>
    [CreateAssetMenu(fileName = "CakeCabinetSettings", menuName = "Cutie Pie/UI/Cake Cabinet Settings")]
    public class CakeCabinetSettings : ScriptableObject
    {
        [Header("Stok")]
        [Tooltip("Dilim sayısı bu değer ve altındaysa (0 hariç) kart 'Az kaldı!' gösterir.")]
        [Min(0)] public int lowStockThreshold = 2;

        [Tooltip("Kartta gösterilen küçük dilim kutucuğu sayısı. Stok bundan fazlaysa hepsi dolu görünür.")]
        [Min(1)] public int sliceTileCount = 8;

        [Header("Ortak palet")]
        [Tooltip("Ekran zemini (krem).")]
        public Color backgroundColor = Hex(0xFF, 0xF4, 0xEA);

        [Tooltip("Yazı rengi (kakao).")]
        public Color textColor = Hex(0x5B, 0x3A, 0x2E);

        [Header("Yap butonu")]
        public Color buttonColor = Hex(0xF4, 0x8F, 0xA6);
        public Color buttonBorderColor = Hex(0xB4, 0x53, 0x6A);
        public Color buttonTextColor = Color.white;

        [Tooltip("Butonun 8 px altındaki gölge (koyu pembe).")]
        public Color buttonShadowColor = Hex(0xB4, 0x53, 0x6A);

        [Tooltip("Seçim yokken (pasif) butonun iç rengine çarpılan ton.")]
        public Color buttonDisabledTint = new Color(0.82f, 0.82f, 0.82f, 1f);

        [Header("Dolap")]
        public Color cabinetColor = Hex(0xF7, 0xB8, 0xC6);
        public Color cabinetBorderColor = Hex(0xB4, 0x53, 0x6A);
        public Color corniceColor = Hex(0xF4, 0x8F, 0xA6);
        public Color corniceTextColor = Hex(0xFF, 0xF4, 0xEA);

        [Tooltip("Cam gövde (yarı saydam).")]
        public Color glassColor = new Color(1f, 1f, 1f, 0.55f);

        public Color shelfColor = Hex(0xE8, 0x9A, 0xAE);

        [Header("Aşçı ve konuşma balonu (yer tutucu)")]
        public Color chefSkinColor = Hex(0xFC, 0xD9, 0xC0);
        public Color chefHatColor = Color.white;
        public Color chefCheekColor = Hex(0xF7, 0xA8, 0xB8);
        public Color bubbleColor = Color.white;

        [Header("Kart")]
        public Color cardColor = Hex(0xFF, 0xF4, 0xEA);

        [Tooltip("Kilitli kartın zemini.")]
        public Color lockedColor = Hex(0xE7, 0xDC, 0xD6);

        [Tooltip("Seçili kartın çerçevesi (altın).")]
        public Color selectionColor = Hex(0xF6, 0xC6, 0x4E);

        [Tooltip("Seçili kartın ✓ rozeti (yeşil).")]
        public Color checkColor = Hex(0x7E, 0xD3, 0xA6);

        [Tooltip("Pasta görseli yoksa görünen yer tutucu kutunun rengi.")]
        public Color cakePlaceholderColor = Hex(0xF4, 0xB0, 0xC0);

        public Color sliceFilledColor = Hex(0xF4, 0x8F, 0xA6);
        public Color sliceEmptyColor = Hex(0xEF, 0xDF, 0xD7);

        [Header("Durum yazıları")]
        [Tooltip("'Az kaldı!' (turuncu).")]
        public Color lowStockColor = Hex(0xF0, 0x8A, 0x2E);

        [Tooltip("'Bitti!' (pembe).")]
        public Color outOfStockColor = Hex(0xE0, 0x5A, 0x80);

        [Tooltip("Bitti ve kilitli kartlarda pasta görselinin saydamlığı (soluk görünüm).")]
        [Range(0f, 1f)] public float dimmedAlpha = 0.4f;

        private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 0xFF);
    }
}
