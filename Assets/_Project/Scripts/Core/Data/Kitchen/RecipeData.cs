using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Mutfak tarifi: fırında bir partide hangi pastadan kaç dilim, ne kadar sürede pişer.
    /// Tezgâh (Mutfak 3) için malzemeleri ve kusursuz süsleme bonusunun doğru krema/süsünü de taşır.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Cutie Pie/Kitchen/Recipe")]
    public class RecipeData : ScriptableObject
    {
        [Tooltip("Benzersiz, değişmeyen kimlik (fırın kaydı bu id'yi tutar).")]
        public string id;

        [Tooltip("Pişince stoğa eklenen ürün (pasta).")]
        public ProductData product;

        [Tooltip("Pişme süresi (gerçek zamanlı saniye).")]
        [Min(1)] public int bakeSeconds;

        [Tooltip("Bir partide çıkan dilim sayısı (kusursuz süsleme bonusu hariç).")]
        [Min(1)] public int slicesPerBatch;

        public Sprite icon;

        [Tooltip("Yerelleştirme anahtarı; oyuncuya görünen ad bu anahtarla çözülür.")]
        public string nameKey;

        [Header("Tezgâh")]
        [Tooltip("Malzeme adımında gereken malzemeler (v1: sınırsız, yalnızca veri).")]
        public IngredientData[] ingredients;

        [Tooltip("Kusursuz süsleme için doğru krema (CakePartData, tür: Cream).")]
        public CakePartData perfectFrosting;

        [Tooltip("Kusursuz süsleme için doğru süs (CakePartData, tür: Decoration).")]
        public CakePartData perfectTopping;

        /// <summary>
        /// Seçilen krema ve süs tarifteki doğru değerlere eşit mi. Tarifte doğru değer atanmamışsa false (bonus yok).
        /// </summary>
        public bool IsPerfectDecoration(string frostingId, string toppingId) =>
            KitchenBatchMath.IsPerfect(
                perfectFrosting != null ? perfectFrosting.id : null,
                perfectTopping != null ? perfectTopping.id : null,
                frostingId,
                toppingId);
    }
}
