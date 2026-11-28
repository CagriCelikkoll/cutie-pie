using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Tezgâhta kaseye atılan malzeme (G23). Malzemeler sınırsızdır; satın alınmaz, yalnızca oyun adımıdır.
    /// Asset'ler: Assets/_Project/Data/Tezgah/Ingredients/ ("Cutie Pie/UI/Tezgâh Oluştur" yoksa oluşturur).
    /// </summary>
    [CreateAssetMenu(fileName = "Ingredient_", menuName = "Cutie Pie/Counter/Ingredient")]
    public class IngredientData : ScriptableObject
    {
        [Tooltip("Benzersiz, değişmeyen kimlik.")]
        public string id;

        [Tooltip("Yerelleştirme anahtarı: malzemenin adı (ör. kitchen.counter.ingredient.cocoa.name).")]
        public string nameKey;

        [Tooltip("Yerelleştirme anahtarı: aşçının bu malzemeyi önerdiği cümle (ör. \"Şimdi kakaoyu kaseye at!\"). " +
                 "Türkçede ek isme göre değiştiği için her malzemenin cümlesi ayrı yazılır.")]
        public string hintKey;

        [Tooltip("Malzeme görseli; boşsa bowlTint renginde yer tutucu kutu görünür.")]
        public Sprite icon;

        [Tooltip("Malzeme kaseye düşünce hamurun kısa süre aldığı renk (yer tutucu ikonun rengi de budur).")]
        public Color bowlTint = Color.white;
    }
}
