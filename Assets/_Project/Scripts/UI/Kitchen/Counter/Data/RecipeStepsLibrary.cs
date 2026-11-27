using System;
using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Tarif id'sinden tezgâh adımlarını bulur. Asset: Assets/_Project/Data/Tezgah/RecipeStepsLibrary.asset.
    /// Yeni tarif eklenince Steps_ asset'i oluşturulup buraya eklenmelidir.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeStepsLibrary", menuName = "Cutie Pie/Counter/Recipe Steps Library")]
    public class RecipeStepsLibrary : ScriptableObject
    {
        public RecipeStepsData[] entries;

        /// <summary>recipeId'si verilen adımlar; yoksa null.</summary>
        public RecipeStepsData Find(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId) || entries == null)
            {
                return null;
            }

            return Array.Find(entries, e => e != null && e.recipeId == recipeId);
        }
    }
}
