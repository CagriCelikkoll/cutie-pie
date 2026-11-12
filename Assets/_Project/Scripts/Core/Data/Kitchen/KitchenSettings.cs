using System;
using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Mutfak denge değerleri: fırınlar, tarifler, başlangıç stoğu, sipariş panosu, benzerlik ağırlıkları ve ödül tablosu.
    /// </summary>
    [CreateAssetMenu(fileName = "KitchenSettings", menuName = "Cutie Pie/Kitchen/Kitchen Settings")]
    public class KitchenSettings : ScriptableObject
    {
        [Header("Fırınlar")]
        [Tooltip("Yeni oyuncunun fırın sayısı.")]
        [Min(1)] public int startingOvenCount;

        [Tooltip("Geliştirmelerle ulaşılabilecek en fazla fırın sayısı.")]
        [Min(1)] public int maxOvenCount;

        [Header("Tarifler ve stok")]
        [Tooltip("Mutfakta pişirilebilen tüm tarifler.")]
        public RecipeData[] recipes;

        [Tooltip("Yeni oyuncuya (ve stok alanı olmayan eski kayıtlara) bir kez verilen stok.")]
        public StockAmount[] startingStock;

        [Header("Sipariş panosu")]
        [Tooltip("Panoda aynı anda duran en fazla fiş sayısı.")]
        [Min(1)] public int boardSize;

        [Tooltip("Tamamlanan fişin yerine yenisinin gelme süresi (gerçek zamanlı saniye).")]
        [Min(0)] public int orderRefreshSeconds;

        [Tooltip("Yeni fişin taban ödül aralığı (altın, iki uç dahil).")]
        [Min(0)] public int orderBaseRewardMin;

        [Min(0)] public int orderBaseRewardMax;

        [Header("Pasta parçaları")]
        [Tooltip("Özel siparişte seçilebilen tüm parçalar (taban, krema, süs, kutu).")]
        public CakePartData[] cakeParts;

        [Tooltip("Pastaya konabilecek en fazla süs sayısı.")]
        [Min(0)] public int maxDecorations;

        [Tooltip("Süs konumunun 'yaklaşık doğru' sayıldığı en büyük uzaklık (0–1 pasta koordinatında).")]
        [Range(0f, 1f)] public float decorationPositionTolerance;

        [Header("Benzerlik ağırlıkları (toplamı 1 olmalı)")]
        [Range(0f, 1f)] public float baseWeight;
        [Range(0f, 1f)] public float creamWeight;
        [Range(0f, 1f)] public float decorationWeight;
        [Range(0f, 1f)] public float boxWeight;

        [Header("Ödül tablosu")]
        [Tooltip("1, 2 ve 3 yıldız için gereken en düşük benzerlik (0–1), artan sırada.")]
        public float[] starThresholds;

        [Tooltip("Bahşiş için gereken en düşük benzerlik (0–1).")]
        [Range(0f, 1f)] public float tipMinSimilarity;

        [Tooltip("Bahşiş miktarı = taban ödül × bu oran.")]
        [Min(0f)] public float tipRatio;

        /// <summary>id'si verilen tarif; yoksa null.</summary>
        public RecipeData FindRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId) || recipes == null)
            {
                return null;
            }

            return Array.Find(recipes, r => r != null && r.id == recipeId);
        }

        /// <summary>id'si verilen parça; yoksa null.</summary>
        public CakePartData FindPart(string partId)
        {
            if (string.IsNullOrEmpty(partId) || cakeParts == null)
            {
                return null;
            }

            return Array.Find(cakeParts, p => p != null && p.id == partId);
        }
    }

    /// <summary>Bir ürün ve adedi (ör. başlangıç stoğu satırı).</summary>
    [Serializable]
    public class StockAmount
    {
        public ProductData product;

        [Min(0)] public int amount;
    }
}
