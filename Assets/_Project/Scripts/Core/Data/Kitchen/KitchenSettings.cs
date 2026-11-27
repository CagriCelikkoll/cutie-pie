using System;
using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Mutfak denge değerleri: fırınlar, tarifler, başlangıç stoğu, dolap kapasitesi, tezgâh, sipariş panosu,
    /// benzerlik ağırlıkları ve ödül tablosu.
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

        [Tooltip("Pasta dolabında ürün başına en fazla dilim. Stok buna ulaşınca ekleme kırpılır, taşan dilim kaybolur.")]
        [Min(1)] public int cabinetCapacityPerProduct;

        [Header("Tezgâh")]
        [Tooltip("Malzeme adımında gösterilen tüm malzemeler (v1: sınırsız, yalnızca veri).")]
        public IngredientData[] ingredients;

        [Tooltip("Krema ve süs tarifteki doğru değerlere eşitse partiye eklenen dilim.")]
        [Min(0)] public int perfectDecorationBonusSlices;

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

        [Header("Fiş üretimi (süsler)")]
        [Tooltip("Fişteki en az süs sayısı (maxDecorations'tan büyük olamaz).")]
        [Min(0)] public int minDecorations;

        [Tooltip("Fişte süslerin konabileceği alanın sol alt köşesi (CakeBuild pasta koordinatı, 0–1).")]
        public Vector2 decorationAreaMin;

        [Tooltip("Fişte süslerin konabileceği alanın sağ üst köşesi (CakeBuild pasta koordinatı, 0–1).")]
        public Vector2 decorationAreaMax;

        [Tooltip("Fişte iki süsün merkezleri arasındaki en az uzaklık (pasta koordinatı).")]
        [Range(0f, 1f)] public float decorationMinSpacing;

        [Tooltip("Bir süse uygun yer bulmak için en fazla deneme sayısı; bulunamazsa o süs atlanır.")]
        [Min(1)] public int decorationPlacementAttempts;

        [Tooltip("Doğru türdeki süsün yeri tutmasa da aldığı pay (0–1); kalan pay konum yakınlığından gelir.")]
        [Range(0f, 1f)] public float decorationTypeShare;

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

        /// <summary>id'si verilen malzeme; yoksa null.</summary>
        public IngredientData FindIngredient(string ingredientId)
        {
            if (string.IsNullOrEmpty(ingredientId) || ingredients == null)
            {
                return null;
            }

            return Array.Find(ingredients, i => i != null && i.id == ingredientId);
        }

        /// <summary>
        /// cakeParts içindeki verilen türdeki parçalar, listedeki sırayla (Süsle ekranı: Cream ve Decoration). Yoksa boş dizi.
        /// </summary>
        public CakePartData[] GetParts(CakePartType type)
        {
            if (cakeParts == null)
            {
                return Array.Empty<CakePartData>();
            }

            return Array.FindAll(cakeParts, p => p != null && p.type == type);
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
