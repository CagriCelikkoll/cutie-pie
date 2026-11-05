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

        [Header("Sahneler")]
        [Tooltip("Boot'tan sonra yüklenecek sahnenin adı (Build Settings'te olmalı).")]
        public string firstSceneName;

        [Header("Reklam (geliştirme)")]
        [Tooltip("FakeAdsService'in sahte reklamı bekleme süresi (gerçek zamanlı saniye).")]
        [Min(0f)] public float fakeAdDurationSeconds;
    }
}
