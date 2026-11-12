using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>Özel sipariş pastasının parça türü.</summary>
    public enum CakePartType
    {
        Base = 0,
        Cream = 1,
        Decoration = 2,
        Box = 3
    }

    /// <summary>Özel sipariş pastasında seçilebilen bir parça (taban, krema rengi, süs veya kutu).</summary>
    [CreateAssetMenu(fileName = "CakePart_", menuName = "Cutie Pie/Kitchen/Cake Part")]
    public class CakePartData : ScriptableObject
    {
        public CakePartType type;

        [Tooltip("Benzersiz, değişmeyen kimlik (CakeBuild bu id'leri tutar).")]
        public string id;

        [Tooltip("Yer tutucu görsel rengi; sprite yoksa bu renk kullanılır.")]
        public Color color = Color.white;

        [Tooltip("İsteğe bağlı görsel.")]
        public Sprite sprite;

        [Tooltip("Yerelleştirme anahtarı (order.* önekli).")]
        public string nameKey;
    }
}
