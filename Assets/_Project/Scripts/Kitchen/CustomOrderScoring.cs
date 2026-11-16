using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Özel sipariş benzerlik ve ödül hesabı. Yan etkisizdir (hiçbir şeyi değiştirmez); birim testleri doğrudan bunu çağırır.
    /// Benzerlik = (taban, krema, süs, kutu puanlarının ağırlıklı toplamı) / ağırlık toplamı.
    /// </summary>
    public static class CustomOrderScoring
    {
        /// <summary>Fişi oyuncunun pastasıyla puanlar. build null ise boş pasta sayılır. settings veya order null ise null.</summary>
        public static CustomOrderResult Score(KitchenSettings settings, CustomOrder order, CakeBuild build)
        {
            if (settings == null || order == null)
            {
                return null;
            }

            CakeBuild target = order.target ?? new CakeBuild();
            build ??= new CakeBuild();

            float similarity = Similarity(settings, target, build);
            int stars = Stars(settings, similarity);
            int baseReward = Mathf.Max(0, order.baseReward);

            return new CustomOrderResult
            {
                orderId = order.id,
                similarity = similarity,
                stars = stars,
                gold = Mathf.RoundToInt(baseReward * similarity),
                tip = similarity >= settings.tipMinSimilarity ? Mathf.RoundToInt(baseReward * settings.tipRatio) : 0
            };
        }

        /// <summary>Hedef ile oyuncunun pastası arasındaki benzerlik, 0–1.</summary>
        public static float Similarity(KitchenSettings settings, CakeBuild target, CakeBuild build)
        {
            float totalWeight = settings.baseWeight + settings.creamWeight + settings.decorationWeight + settings.boxWeight;
            if (totalWeight <= 0f)
            {
                return 0f;
            }

            float weighted =
                settings.baseWeight * PartScore(target.baseId, build.baseId) +
                settings.creamWeight * PartScore(target.creamId, build.creamId) +
                settings.decorationWeight * DecorationScore(settings, target.decorations, build.decorations) +
                settings.boxWeight * PartScore(target.boxId, build.boxId);

            // Kayan nokta artığı (ör. 0,8999999) eşikte yıldız/bahşiş kaybettirmesin diye sabit hassasiyete yuvarlanır.
            float similarity = Mathf.Round(weighted / totalWeight * SimilarityPrecision) / SimilarityPrecision;
            return Mathf.Clamp01(similarity);
        }

        // Benzerliğin yuvarlandığı hassasiyet (1/10000 = %0,01). Denge değeri değil, sayısal kararlılık sabiti.
        private const float SimilarityPrecision = 10000f;

        /// <summary>Aşılan yıldız eşiği sayısı (starThresholds artan sırada).</summary>
        public static int Stars(KitchenSettings settings, float similarity)
        {
            int stars = 0;
            if (settings.starThresholds == null)
            {
                return stars;
            }

            foreach (float threshold in settings.starThresholds)
            {
                if (similarity >= threshold)
                {
                    stars++;
                }
            }

            return stars;
        }

        /// <summary>
        /// Süs puanı, 0–1. Süsler türe göre gruplanır; yalnızca aynı tür eşleşir. Bir eşin puanı
        /// typeShare + (1 − typeShare) × max(0, 1 − uzaklık / tolerans). Her türde toplamı en yüksek eşleştirme seçilir
        /// (tüm sıralamalar denenir; süs sayısı az). Toplam, max(hedef süs sayısı, oyuncu süs sayısı)'na bölünür:
        /// eksik ve fazla süs puanı düşürür. İkisi de boşsa 1.
        /// </summary>
        public static float DecorationScore(KitchenSettings settings, List<DecorationPlacement> target,
            List<DecorationPlacement> build)
        {
            int targetCount = CountValid(target);
            int buildCount = CountValid(build);
            int denominator = Mathf.Max(targetCount, buildCount);
            if (denominator == 0)
            {
                return 1f;
            }

            Dictionary<string, List<Vector2>> targetByType = GroupByType(target);
            Dictionary<string, List<Vector2>> buildByType = GroupByType(build);

            float total = 0f;
            foreach (KeyValuePair<string, List<Vector2>> group in targetByType)
            {
                if (buildByType.TryGetValue(group.Key, out List<Vector2> placed))
                {
                    total += BestMatchSum(settings, group.Value, placed);
                }
            }

            return Mathf.Clamp01(total / denominator);
        }

        /// <summary>Aynı türdeki iki süsün eş puanı, 0–1.</summary>
        public static float PairScore(KitchenSettings settings, Vector2 target, Vector2 placed)
        {
            float tolerance = settings.decorationPositionTolerance;
            float proximity = tolerance > 0f
                ? Mathf.Max(0f, 1f - Vector2.Distance(target, placed) / tolerance)
                : (target == placed ? 1f : 0f);
            float typeShare = Mathf.Clamp01(settings.decorationTypeShare);
            return typeShare + (1f - typeShare) * proximity;
        }

        private static float PartScore(string targetId, string buildId) =>
            !string.IsNullOrEmpty(targetId) && targetId == buildId ? 1f : 0f;

        private static int CountValid(List<DecorationPlacement> list)
        {
            int count = 0;
            if (list != null)
            {
                foreach (DecorationPlacement d in list)
                {
                    if (d != null && !string.IsNullOrEmpty(d.decorationId))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static Dictionary<string, List<Vector2>> GroupByType(List<DecorationPlacement> list)
        {
            var groups = new Dictionary<string, List<Vector2>>();
            if (list == null)
            {
                return groups;
            }

            foreach (DecorationPlacement d in list)
            {
                if (d == null || string.IsNullOrEmpty(d.decorationId))
                {
                    continue;
                }

                if (!groups.TryGetValue(d.decorationId, out List<Vector2> positions))
                {
                    positions = new List<Vector2>();
                    groups.Add(d.decorationId, positions);
                }

                positions.Add(d.position);
            }

            return groups;
        }

        // Küçük kümeyi büyük kümeye birebir eşleyen tüm seçenekleri dener; en yüksek eş puanı toplamını döner.
        private static float BestMatchSum(KitchenSettings settings, List<Vector2> a, List<Vector2> b)
        {
            List<Vector2> small = a.Count <= b.Count ? a : b;
            List<Vector2> large = a.Count <= b.Count ? b : a;

            var scores = new float[small.Count, large.Count];
            for (int i = 0; i < small.Count; i++)
            {
                for (int j = 0; j < large.Count; j++)
                {
                    scores[i, j] = PairScore(settings, small[i], large[j]);
                }
            }

            // Güvenlik: oyuncu ekranı süsü maxDecorations ile sınırlar; daha büyük (bozuk) girdide arama patlamasın diye açgözlü eşleme.
            if (large.Count > Mathf.Max(1, settings.maxDecorations))
            {
                return Greedy(scores, small.Count, large.Count);
            }

            return Search(scores, 0, new bool[large.Count], small.Count, large.Count);
        }

        private static float Greedy(float[,] scores, int rows, int columns)
        {
            var used = new bool[columns];
            float sum = 0f;
            for (int i = 0; i < rows; i++)
            {
                int bestJ = -1;
                for (int j = 0; j < columns; j++)
                {
                    if (!used[j] && (bestJ < 0 || scores[i, j] > scores[i, bestJ]))
                    {
                        bestJ = j;
                    }
                }

                if (bestJ >= 0)
                {
                    used[bestJ] = true;
                    sum += scores[i, bestJ];
                }
            }

            return sum;
        }

        private static float Search(float[,] scores, int row, bool[] used, int rows, int columns)
        {
            if (row == rows)
            {
                return 0f;
            }

            float best = 0f;
            for (int j = 0; j < columns; j++)
            {
                if (used[j])
                {
                    continue;
                }

                used[j] = true;
                best = Math.Max(best, scores[row, j] + Search(scores, row + 1, used, rows, columns));
                used[j] = false;
            }

            return best;
        }
    }
}
