using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>Müşteri tipi: sabır, bahşiş ve görünüm.</summary>
    [CreateAssetMenu(fileName = "CustomerType_", menuName = "Cutie Pie/Customer Type")]
    public class CustomerTypeData : ScriptableObject
    {
        public string id;

        [Tooltip("Sabır barının tamamen bitme süresi (saniye).")]
        [Min(0f)] public float patienceSeconds;

        [Tooltip("Bahşiş hesabında kullanılan çarpan.")]
        [Min(0f)] public float tipMultiplier;

        [Header("Görünüm")]
        public Sprite sprite;

        [Tooltip("Sprite yokken kullanılan yer tutucu rengi.")]
        public Color placeholderColor = Color.white;
    }
}
