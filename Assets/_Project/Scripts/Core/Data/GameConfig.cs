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

        [Header("Reklam (geliştirme)")]
        [Tooltip("FakeAdsService'in sahte reklamı bekleme süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float fakeAdDurationSeconds;
    }
}
