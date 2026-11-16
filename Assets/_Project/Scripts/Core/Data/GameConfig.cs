using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Oyun genel ayarları: başlangıç kaynakları, ilk açılan sahne, geliştirme reklamı süresi.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Cutie Pie/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Başlangıç kaynakları")]
        [Min(0)] public int startingGold;
        [Min(0)] public int startingGems;
        [Min(0)] public int startingShopRating;

        [Header("Seviyeler")]
        [Tooltip("Kayıt yokken veya kayıttaki seviye bulunamazsa kullanılan seviye.")]
        public LevelData startingLevel;

        [Tooltip("Oyundaki tüm seviyeler; kayıttaki seviye id'si bu listede aranır.")]
        public LevelData[] levels;

        [Header("Geliştirmeler")]
        [Tooltip("Oyundaki tüm geliştirmeler (Assets/_Project/Data/Upgrades).")]
        public UpgradeData[] upgrades;

        [Header("Mutfak")]
        [Tooltip("Mutfak denge değerleri (Assets/_Project/Data/Kitchen/KitchenSettings).")]
        public KitchenSettings kitchen;

        [Header("Kayıt")]
        [Tooltip("persistentDataPath altındaki kayıt dosyasının adı.")]
        public string saveFileName;

        [Tooltip("Yazma sırasında kullanılan geçici dosyanın adı.")]
        public string saveTempFileName;

        [Tooltip("Yeni sürüm veya bozuk kayıt üzerine yazılmadan önce alınan yedeğin adı.")]
        public string saveBackupFileName;

        [Tooltip("Kodun yazdığı kayıt biçimi sürümü. Biçim değişince artırılır.")]
        [Min(1)] public int saveVersion;

        [Header("Sahneler")]
        [Tooltip("Boot'tan sonra yüklenecek sahnenin adı (Build Settings'te olmalı).")]
        public string firstSceneName;

        [Tooltip("Açılış ekranı (pastane) sahnesinin adı.")]
        public string shopSceneName;

        [Tooltip("Salon (satış) sahnesinin adı.")]
        public string salonSceneName;

        [Tooltip("Mutfak sahnesinin adı.")]
        public string kitchenSceneName;

        [Header("Açılış ekranı")]
        [Tooltip("\"Yakında\" mesajının ekranda kalma süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float comingSoonDurationSeconds;

        [Header("Dil")]
        [Tooltip("Son çare dil kodu: kayıtlı dil ve cihaz dili kuralı sonuç vermezse kullanılır (languages listesinde olmalı).")]
        public string defaultLanguage;

        [Tooltip("Oyuncu dil seçmediyse ve cihaz dili hiçbir dilin systemLanguage'ıyla eşleşmezse kullanılan dil kodu.")]
        public string deviceFallbackLanguage;

        [Tooltip("Desteklenen diller ve metin dosyaları (Assets/_Project/Localization).")]
        public LanguageFile[] languages;

        [Header("Reklam (geliştirme)")]
        [Tooltip("FakeAdsService'in sahte reklamı bekleme süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float fakeAdDurationSeconds;
    }

    /// <summary>
    /// Bir dilin kodu ve metin dosyaları. Dosyalar sahibine göre bölünür (ör. tr.altyapi.json, tr.oynanis.json)
    /// ve yüklenirken birleştirilir; bir anahtar yalnızca bir dosyada olmalıdır.
    /// </summary>
    [System.Serializable]
    public class LanguageFile
    {
        [Tooltip("Dil kodu, ör. tr, en.")]
        public string code;

        [Tooltip("{\"entries\":[{\"key\":..., \"value\":...}]} biçiminde JSON dosyaları; sırayla okunup birleştirilir.")]
        public TextAsset[] files;

        [Tooltip("Bu dile karşılık gelen cihaz dili; oyuncu dil seçmediyse ilk açılışta buna göre seçilir.")]
        public SystemLanguage systemLanguage = SystemLanguage.Unknown;

        /// <summary>En az bir metin dosyası atanmış mı.</summary>
        public bool HasFiles => files != null && System.Array.Exists(files, f => f != null);
    }
}
