using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Tezgâhın Malzeme adımında kullanılan bir malzeme (un, şeker, yumurta…).
    /// v1'de malzemeler sınırsızdır ve yalnızca veridir: stok, fiyat veya satın alma yoktur.
    /// </summary>
    [CreateAssetMenu(fileName = "Ingredient_", menuName = "Cutie Pie/Kitchen/Ingredient")]
    public class IngredientData : ScriptableObject
    {
        [Tooltip("Benzersiz, değişmeyen kimlik.")]
        public string id;

        [Tooltip("Yerelleştirme anahtarı (ingredient.* önekli).")]
        public string nameKey;

        [Tooltip("İsteğe bağlı görsel.")]
        public Sprite icon;

        [Tooltip("Yer tutucu görsel rengi; sprite yoksa bu renk kullanılır.")]
        public Color color = Color.white;
    }
}
