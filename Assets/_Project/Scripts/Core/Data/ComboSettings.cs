using System;
using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>Art arda başarılı teslimlerde uygulanan kombo çarpanları.</summary>
    [CreateAssetMenu(fileName = "ComboSettings", menuName = "Cutie Pie/Combo Settings")]
    public class ComboSettings : ScriptableObject
    {
        // Hiçbir eşiğe ulaşılmadığında ödeme değişmez.
        private const float NoComboMultiplier = 1f;

        [Tooltip("Kombo basamakları. Seriye uyan en yüksek eşiğin çarpanı kullanılır.")]
        public ComboTier[] tiers;

        /// <summary>Verilen art arda teslim sayısının çarpanı.</summary>
        public float GetMultiplier(int streak)
        {
            float multiplier = NoComboMultiplier;
            int bestThreshold = 0;

            foreach (ComboTier tier in tiers)
            {
                if (streak >= tier.streakThreshold && tier.streakThreshold > bestThreshold)
                {
                    bestThreshold = tier.streakThreshold;
                    multiplier = tier.multiplier;
                }
            }

            return multiplier;
        }
    }

    /// <summary>Bir kombo basamağı: seri eşiği ve çarpanı.</summary>
    [Serializable]
    public class ComboTier
    {
        [Tooltip("Bu çarpan için gereken art arda başarılı teslim sayısı.")]
        [Min(1)] public int streakThreshold;

        [Tooltip("Ödemeye uygulanan çarpan.")]
        [Min(1f)] public float multiplier;
    }
}
