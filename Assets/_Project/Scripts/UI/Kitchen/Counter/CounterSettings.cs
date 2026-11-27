using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Tezgâh ekranının (G23) renk, animasyon ve karıştırma hassasiyeti ayarları.
    /// Asset: Assets/_Project/Data/Tezgah/CounterSettings.asset ("Cutie Pie/UI/Tezgâh Oluştur" yoksa oluşturur).
    /// Uzunluklar 1920x1080 tasarım pikselidir (DesignAreaFitter ölçeğinden bağımsız); süreler gerçek zamanlı saniyedir.
    /// </summary>
    [CreateAssetMenu(fileName = "CounterSettings", menuName = "Cutie Pie/UI/Counter Settings")]
    public class CounterSettings : ScriptableObject
    {
        [Header("Ortak palet")]
        public Color backgroundColor = Hex(0xFF, 0xF4, 0xEA);
        public Color textColor = Hex(0x5B, 0x3A, 0x2E);
        public Color mutedTextColor = Hex(0xB2, 0x9C, 0x92);
        public Color pinkColor = Hex(0xF4, 0x8F, 0xA6);
        public Color pinkBorderColor = Hex(0xB4, 0x53, 0x6A);
        public Color goldColor = Hex(0xF6, 0xC6, 0x4E);
        public Color greenColor = Hex(0x7E, 0xD3, 0xA6);
        public Color grayColor = Hex(0xE7, 0xDC, 0xD6);
        public Color buttonTextColor = Color.white;

        [Tooltip("Bırakma onayının arkasındaki perde.")]
        public Color dimColor = new Color(0.36f, 0.23f, 0.18f, 0.45f);

        [Header("Üst bar ve ipucu çubuğu")]
        public Color barColor = Hex(0xFF, 0xE6, 0xEC);
        public Color progressBackColor = Hex(0xF3, 0xE3, 0xDC);
        public Color progressFillColor = Hex(0xF4, 0x8F, 0xA6);

        [Header("Tezgâh ve aşçı (yer tutucu)")]
        public Color counterColor = Hex(0xEC, 0xCB, 0xA8);
        public Color counterEdgeColor = Hex(0xC9, 0x9A, 0x72);
        public Color chefSkinColor = Hex(0xFC, 0xD9, 0xC0);
        public Color chefHatColor = Color.white;
        public Color chefCheekColor = Hex(0xF7, 0xA8, 0xB8);
        public Color bubbleColor = Color.white;

        [Header("Tepsi ve malzeme kartı")]
        public Color trayColor = Hex(0xFF, 0xFB, 0xF7);
        public Color trayBorderColor = Hex(0xF2, 0xC4, 0xCF);
        public Color cardColor = Hex(0xFF, 0xF4, 0xEA);

        [Tooltip("Kaseye eklenmiş kartın zemini.")]
        public Color cardAddedColor = Hex(0xF1, 0xEB, 0xE7);

        [Tooltip("Eklenmiş kartta görselin ve yazıların saydamlığı (soluk görünüm).")]
        [Range(0f, 1f)] public float addedAlpha = 0.45f;

        [Tooltip("Sürüklenirken kartta kalan görselin saydamlığı.")]
        [Range(0f, 1f)] public float draggingAlpha = 0.3f;

        [Tooltip("Sıradaki malzeme kartının yukarı kalkma miktarı.")]
        public float suggestedLift = 14f;

        [Tooltip("Sıradaki kartın parlamasının saniyedeki nabız sayısı.")]
        [Min(0.01f)] public float suggestedPulsesPerSecond = 1.4f;

        [Range(0f, 1f)] public float glowMinAlpha = 0.35f;
        [Range(0f, 1f)] public float glowMaxAlpha = 1f;

        [Tooltip("Kartın yükselip inme hızı (saniyede yaklaşma oranı).")]
        [Min(0.01f)] public float liftSpeed = 10f;

        [Tooltip("Kase dışında bırakılan malzemenin karta geri dönüş süresi.")]
        [Min(0.01f)] public float returnDuration = 0.25f;

        [Tooltip("Sürüklenen malzeme görselinin boyutu.")]
        [Min(1f)] public float dragIconSize = 110f;

        [Header("Kase")]
        public Color bowlColor = Hex(0xDD, 0xEE, 0xF4);
        public Color bowlRimColor = Hex(0xB9, 0xD9, 0xE6);

        [Tooltip("Malzeme sürüklenip kasenin üstüne gelince görünen parıltı.")]
        public Color bowlHoverColor = new Color(0.96f, 0.78f, 0.31f, 0.55f);

        public Color ringTrackColor = Hex(0xF0, 0xE4, 0xDE);
        public Color ringFillColor = Hex(0x7E, 0xD3, 0xA6);

        [Tooltip("Hamur seviyesi: ilk malzemede ve son malzemede hamurun ölçeği.")]
        [Range(0.1f, 1f)] public float batterMinScale = 0.55f;

        [Range(0.1f, 1f)] public float batterMaxScale = 0.92f;

        [Tooltip("Malzeme düşünce hamurun malzeme rengine kayıp geri dönme süresi.")]
        [Min(0.01f)] public float dropFlashDuration = 0.45f;

        [Tooltip("Malzeme düşünce hamur rengi malzeme rengine ne kadar kayar (0–1).")]
        [Range(0f, 1f)] public float dropFlashStrength = 0.6f;

        [Tooltip("Malzeme düşünce kasenin zıplama ölçeği ve süresi.")]
        [Min(1f)] public float dropPopScale = 1.07f;

        [Min(0.01f)] public float dropPopDuration = 0.2f;

        [Header("Karıştırma hassasiyeti")]
        [Tooltip("Kase merkezine bundan yakın hareketler sayılmaz (açı ölçülemez, titreme olur).")]
        [Min(0f)] public float minMixRadius = 40f;

        [Tooltip("Tek sürükleme olayında bundan büyük açı değişimi 'sıçrama' sayılır ve eklenmez.")]
        [Range(1f, 180f)] public float maxMixStepDegrees = 60f;

        [Tooltip("Kaşık görseli kase merkezinden en fazla bu kadar uzaklaşır.")]
        [Min(0f)] public float spoonMaxRadius = 140f;

        private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 0xFF);
    }
}
