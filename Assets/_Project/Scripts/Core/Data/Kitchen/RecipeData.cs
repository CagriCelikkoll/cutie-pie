using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>Mutfak tarifi: fırında bir partide hangi pastadan kaç dilim, ne kadar sürede pişer.</summary>
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Cutie Pie/Kitchen/Recipe")]
    public class RecipeData : ScriptableObject
    {
        [Tooltip("Benzersiz, değişmeyen kimlik (fırın kaydı bu id'yi tutar).")]
        public string id;

        [Tooltip("Pişince stoğa eklenen ürün (pasta).")]
        public ProductData product;

        [Tooltip("Pişme süresi (gerçek zamanlı saniye).")]
        [Min(1)] public int bakeSeconds;

        [Tooltip("Bir partide çıkan dilim sayısı.")]
        [Min(1)] public int slicesPerBatch;

        public Sprite icon;

        [Tooltip("Yerelleştirme anahtarı; oyuncuya görünen ad bu anahtarla çözülür.")]
        public string nameKey;
    }
}
