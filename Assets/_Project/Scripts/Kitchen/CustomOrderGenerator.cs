using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Rastgele ama çözülebilir özel sipariş fişi üretir: KitchenSettings.cakeParts içinden 1 taban, 1 krema, 1 kutu ve
    /// minDecorations..maxDecorations süs. Süsler decorationArea içinde, birbirine en az decorationMinSpacing uzakta durur.
    /// Rastgelelik dışarıdan verilir (testte sabit tohum).
    /// </summary>
    public class CustomOrderGenerator
    {
        private readonly KitchenSettings _settings;
        private readonly System.Random _random;

        public CustomOrderGenerator(KitchenSettings settings, System.Random random)
        {
            _settings = settings;
            _random = random ?? new System.Random();
        }

        /// <summary>Gerekli parça türlerinden biri yoksa (veya ayar yoksa) false: fiş çözülebilir olmaz.</summary>
        public bool CanGenerate =>
            _settings != null &&
            HasPart(CakePartType.Base) && HasPart(CakePartType.Cream) && HasPart(CakePartType.Box) &&
            (_settings.maxDecorations <= 0 || HasPart(CakePartType.Decoration));

        /// <summary>Yeni fiş; üretilemiyorsa null.</summary>
        public CustomOrder Create()
        {
            if (!CanGenerate)
            {
                return null;
            }

            int rewardMin = Mathf.Min(_settings.orderBaseRewardMin, _settings.orderBaseRewardMax);
            int rewardMax = Mathf.Max(_settings.orderBaseRewardMin, _settings.orderBaseRewardMax);

            return new CustomOrder
            {
                id = Guid.NewGuid().ToString("N"),
                baseReward = _random.Next(rewardMin, rewardMax + 1),
                target = new CakeBuild
                {
                    baseId = PickPart(CakePartType.Base).id,
                    creamId = PickPart(CakePartType.Cream).id,
                    boxId = PickPart(CakePartType.Box).id,
                    decorations = CreateDecorations()
                }
            };
        }

        /// <summary>
        /// Fiş çözülebilir mi: parçalar ayarlarda doğru türde var, süs sayısı sınırlar içinde, konumlar 0–1 içinde.
        /// Açılışta kayıttan gelen eski fişleri ayıklamak için de kullanılır.
        /// </summary>
        public static bool IsSolvable(KitchenSettings settings, CustomOrder order)
        {
            if (settings == null || order == null || string.IsNullOrEmpty(order.id) || order.target == null)
            {
                return false;
            }

            CakeBuild t = order.target;
            if (!IsPart(settings, t.baseId, CakePartType.Base) || !IsPart(settings, t.creamId, CakePartType.Cream) ||
                !IsPart(settings, t.boxId, CakePartType.Box))
            {
                return false;
            }

            int count = t.decorations != null ? t.decorations.Count : 0;
            if (count > settings.maxDecorations)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                DecorationPlacement d = t.decorations[i];
                if (d == null || !IsPart(settings, d.decorationId, CakePartType.Decoration) ||
                    d.position.x < 0f || d.position.x > 1f || d.position.y < 0f || d.position.y > 1f)
                {
                    return false;
                }
            }

            return true;
        }

        private List<DecorationPlacement> CreateDecorations()
        {
            var result = new List<DecorationPlacement>();
            int max = Mathf.Max(0, _settings.maxDecorations);
            int min = Mathf.Clamp(_settings.minDecorations, 0, max);
            int count = _random.Next(min, max + 1);

            Vector2 areaMin = Vector2.Min(_settings.decorationAreaMin, _settings.decorationAreaMax);
            Vector2 areaMax = Vector2.Max(_settings.decorationAreaMin, _settings.decorationAreaMax);
            areaMin = Clamp01(areaMin);
            areaMax = Clamp01(areaMax);
            int attempts = Mathf.Max(1, _settings.decorationPlacementAttempts);

            for (int i = 0; i < count; i++)
            {
                // İlk süs her zaman yer bulur (çakışacak başka süs yok); sonrakiler bulamazsa atlanır.
                if (TryFindPosition(result, areaMin, areaMax, attempts, out Vector2 position))
                {
                    result.Add(new DecorationPlacement
                    {
                        decorationId = PickPart(CakePartType.Decoration).id,
                        position = position
                    });
                }
            }

            return result;
        }

        private bool TryFindPosition(List<DecorationPlacement> placed, Vector2 areaMin, Vector2 areaMax, int attempts,
            out Vector2 position)
        {
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                position = new Vector2(Lerp(areaMin.x, areaMax.x), Lerp(areaMin.y, areaMax.y));
                if (IsFarEnough(placed, position))
                {
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool IsFarEnough(List<DecorationPlacement> placed, Vector2 position)
        {
            foreach (DecorationPlacement d in placed)
            {
                if (Vector2.Distance(d.position, position) < _settings.decorationMinSpacing)
                {
                    return false;
                }
            }

            return true;
        }

        private float Lerp(float a, float b) => a + (float)_random.NextDouble() * (b - a);

        private static Vector2 Clamp01(Vector2 v) => new Vector2(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));

        private CakePartData PickPart(CakePartType type)
        {
            var candidates = new List<CakePartData>();
            foreach (CakePartData part in _settings.cakeParts)
            {
                if (part != null && part.type == type && !string.IsNullOrEmpty(part.id))
                {
                    candidates.Add(part);
                }
            }

            return candidates[_random.Next(candidates.Count)];
        }

        private bool HasPart(CakePartType type) =>
            _settings.cakeParts != null &&
            Array.Exists(_settings.cakeParts, p => p != null && p.type == type && !string.IsNullOrEmpty(p.id));

        private static bool IsPart(KitchenSettings settings, string id, CakePartType type)
        {
            CakePartData part = settings.FindPart(id);
            return part != null && part.type == type;
        }
    }
}
