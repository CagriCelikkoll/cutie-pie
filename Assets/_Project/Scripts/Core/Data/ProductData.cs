using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>Ürün türü.</summary>
    public enum ProductCategory
    {
        Cake = 0,
        Drink = 1
    }

    /// <summary>Salon'da satılan ürün (pasta dilimi veya içecek).</summary>
    [CreateAssetMenu(fileName = "Product_", menuName = "Cutie Pie/Product")]
    public class ProductData : ScriptableObject
    {
        [Tooltip("Benzersiz, değişmeyen kimlik (kayıt ve karşılaştırma için).")]
        public string id;

        [Tooltip("Yerelleştirme anahtarı; oyuncuya görünen ad bu anahtarla çözülür.")]
        public string nameKey;

        [Min(0)] public int price;

        public Sprite icon;

        public ProductCategory category;
    }
}
