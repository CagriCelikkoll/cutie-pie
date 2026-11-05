using System.Collections.Generic;
using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>Bir Salon seviyesinin round ayarları.</summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "Cutie Pie/Level")]
    public class LevelData : ScriptableObject
    {
        public string id;

        [Tooltip("Yerelleştirme anahtarı; oyuncuya görünen seviye adı.")]
        public string nameKey;

        [Tooltip("Round süresi (saniye).")]
        [Min(1f)] public float roundDurationSeconds;

        [Tooltip("Müşteri geliş aralığı eğrisi. X: round ilerlemesi (0-1), Y: iki müşteri arası saniye.")]
        public AnimationCurve spawnIntervalCurve;

        [Tooltip("Hedef altın; 1 yıldız eşiği olarak da kullanılır.")]
        [Min(0)] public int targetGold;

        [Tooltip("1, 2 ve 3 yıldız için gereken altın (artan sırada, 3 değer).")]
        public int[] starGoldThresholds;

        [Tooltip("Bu kadar kaçırmada round erken biter.")]
        [Min(1)] public int maxMisses;

        [Tooltip("Bu seviyede sipariş edilebilen ürünler.")]
        public List<ProductData> menu = new List<ProductData>();
    }
}
