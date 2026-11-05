using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Satın alınabilir geliştirme. levelPrices[i]: (i+1). seviyenin fiyatı, levelEffects[i]: o seviyedeki etki değeri.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "Cutie Pie/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string id;

        [Tooltip("Yerelleştirme anahtarı; oyuncuya görünen geliştirme adı.")]
        public string nameKey;

        public Sprite icon;

        [Tooltip("Her seviyenin altın fiyatı. Dizi uzunluğu seviye sayısını belirler.")]
        public int[] levelPrices;

        [Tooltip("Her seviyedeki etki değeri (ör. masa sayısı, hız çarpanı). levelPrices ile aynı uzunlukta olmalı.")]
        public float[] levelEffects;

        /// <summary>Seviye sayısı.</summary>
        public int LevelCount => levelPrices != null ? levelPrices.Length : 0;

        private void OnValidate()
        {
            if (levelPrices != null && levelEffects != null && levelPrices.Length != levelEffects.Length)
            {
                Debug.LogWarning($"[UpgradeData] '{name}': levelPrices ve levelEffects uzunlukları farklı.", this);
            }
        }
    }
}
