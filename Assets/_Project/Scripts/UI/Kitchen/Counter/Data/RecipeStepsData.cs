using System;
using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Bir tarifin tezgâh adımları (G23: malzemeler ve karıştırma). RecipeData'ya (Core) dokunmadan, tarif id'siyle eşleşir.
    /// Asset'ler: Assets/_Project/Data/Tezgah/Steps_&lt;recipeId&gt;.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Steps_", menuName = "Cutie Pie/Counter/Recipe Steps")]
    public class RecipeStepsData : ScriptableObject
    {
        [Tooltip("RecipeData.id ile aynı (ör. cake_chocolate).")]
        public string recipeId;

        [Tooltip("Tepside görünen malzemeler; aşçı bu sırayla önerir, oyuncu istediği sırayla atabilir.")]
        public IngredientEntry[] ingredients;

        [Tooltip("Karıştırma için kasede çizilmesi gereken tam tur sayısı (yön serbest).")]
        [Min(1)] public int mixTurns = 3;

        [Tooltip("Karıştırma başlarken hamurun rengi.")]
        public Color batterStartColor = new Color(0.95f, 0.9f, 0.82f, 1f);

        [Tooltip("Karıştırma bitince hamurun rengi (çikolatalı kahve, çilekli pembe).")]
        public Color batterReadyColor = new Color(0.48f, 0.29f, 0.2f, 1f);
    }

    /// <summary>Tarifteki bir malzeme ve miktarı.</summary>
    [Serializable]
    public class IngredientEntry
    {
        public IngredientData ingredient;

        [Tooltip("Yerelleştirme anahtarı: miktar (ör. kitchen.counter.amount.cups_2 → \"2 bardak\").")]
        public string amountKey;
    }
}
