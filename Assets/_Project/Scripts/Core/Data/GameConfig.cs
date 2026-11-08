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

        [Header("Açılış ekranı")]
        [Tooltip("\"Yakında\" mesajının ekranda kalma süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float comingSoonDurationSeconds;

        [Header("Dil")]
        [Tooltip("Açılışta kullanılan dil kodu (languages listesinde olmalı).")]
        public string defaultLanguage;

        [Tooltip("Desteklenen diller ve metin dosyaları (Assets/_Project/Localization).")]
        public LanguageFile[] languages;

        [Header("Reklam (geliştirme)")]
        [Tooltip("FakeAdsService'in sahte reklamı bekleme süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float fakeAdDurationSeconds;
    }

    /// <summary>Bir dilin kodu ve metin dosyası.</summary>
    [System.Serializable]
    public class LanguageFile
    {
        [Tooltip("Dil kodu, ör. tr, en.")]
        public string code;

        [Tooltip("{\"entries\":[{\"key\":..., \"value\":...}]} biçiminde JSON dosyası.")]
        public TextAsset file;
    }
}
