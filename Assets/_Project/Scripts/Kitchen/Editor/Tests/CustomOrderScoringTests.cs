using System.Collections.Generic;
using CutiePie.Core.Data;
using NUnit.Framework;
using UnityEngine;

namespace CutiePie.Kitchen.Editor.Tests
{
    /// <summary>
    /// Özel sipariş benzerlik hesabı ve fiş üretimi için Edit Mode birim testleri.
    /// Ayarlar testin içinde kurulur (asset değerlerinden bağımsız); ağırlıklar Faz 2 kararı 8'deki gibidir.
    /// </summary>
    public class CustomOrderScoringTests
    {
        private const float Epsilon = 0.0001f;

        private KitchenSettings _settings;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<KitchenSettings>();
            _created.Add(_settings);
            _settings.baseWeight = 0.3f;
            _settings.creamWeight = 0.3f;
            _settings.decorationWeight = 0.3f;
            _settings.boxWeight = 0.1f;
            _settings.decorationPositionTolerance = 0.2f;
            _settings.decorationTypeShare = 0.5f;
            _settings.maxDecorations = 5;
            _settings.minDecorations = 1;
            _settings.starThresholds = new[] { 0.5f, 0.7f, 0.9f };
            _settings.tipMinSimilarity = 0.9f;
            _settings.tipRatio = 0.2f;
            _settings.boardSize = 3;
            _settings.orderBaseRewardMin = 40;
            _settings.orderBaseRewardMax = 80;
            _settings.decorationAreaMin = new Vector2(0.15f, 0.2f);
            _settings.decorationAreaMax = new Vector2(0.85f, 0.8f);
            _settings.decorationMinSpacing = 0.2f;
            _settings.decorationPlacementAttempts = 30;
            _settings.cakeParts = new[]
            {
                Part(CakePartType.Base, "base_a"), Part(CakePartType.Base, "base_b"),
                Part(CakePartType.Cream, "cream_a"), Part(CakePartType.Cream, "cream_b"),
                Part(CakePartType.Decoration, "deco_a"), Part(CakePartType.Decoration, "deco_b"),
                Part(CakePartType.Box, "box_a"), Part(CakePartType.Box, "box_b")
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
            {
                Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        // ------------------------------------------------------------------ Benzerlik

        [Test]
        public void ExactMatch_Is100Percent_ThreeStars_WithTip()
        {
            CustomOrder order = Order(100, Target());
            CustomOrderResult r = CustomOrderScoring.Score(_settings, order, Target());

            Assert.AreEqual(1f, r.similarity, Epsilon);
            Assert.AreEqual(3, r.stars);
            Assert.AreEqual(100, r.gold);
            Assert.AreEqual(20, r.tip);
            Assert.AreEqual(order.id, r.orderId);
        }

        [Test]
        public void DecorationOrderDoesNotMatter()
        {
            CakeBuild build = Target();
            build.decorations.Reverse();
            Assert.AreEqual(1f, CustomOrderScoring.Score(_settings, Order(100, Target()), build).similarity, Epsilon);
        }

        [Test]
        public void NothingMatches_IsZero_NoStars_NoGold()
        {
            CakeBuild build = Build("base_b", "cream_b", "box_b", Deco("deco_b", 0.5f, 0.5f));
            CustomOrder order = Order(100, Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.5f, 0.5f)));
            CustomOrderResult r = CustomOrderScoring.Score(_settings, order, build);

            Assert.AreEqual(0f, r.similarity, Epsilon);
            Assert.AreEqual(0, r.stars);
            Assert.AreEqual(0, r.gold);
            Assert.AreEqual(0, r.tip);
        }

        [Test]
        public void EmptyOrNullBuild_IsZero()
        {
            CustomOrder order = Order(100, Target());
            Assert.AreEqual(0f, CustomOrderScoring.Score(_settings, order, new CakeBuild()).similarity, Epsilon);
            Assert.AreEqual(0f, CustomOrderScoring.Score(_settings, order, null).similarity, Epsilon);
        }

        [Test]
        public void OnlyDecorationsMovedFar_GetsTypeShare()
        {
            // Türler doğru, yerler toleransın çok dışında: süs puanı = typeShare (0,5) → 0,3+0,3+0,15+0,1 = 0,85.
            CakeBuild build = Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.9f, 0.9f), Deco("deco_b", 0.1f, 0.9f));
            CustomOrderResult r = CustomOrderScoring.Score(_settings, Order(100, Target()), build);

            Assert.AreEqual(0.85f, r.similarity, Epsilon);
            Assert.AreEqual(2, r.stars);
            Assert.AreEqual(85, r.gold);
            Assert.AreEqual(0, r.tip);
        }

        [Test]
        public void DecorationSlightlyMoved_GetsPartialProximity()
        {
            // deco_a 0,1 kaydı (tolerans 0,2 → yakınlık 0,5 → eş puanı 0,75); deco_b yerinde (1).
            // Süs puanı (0,75 + 1) / 2 = 0,875 → benzerlik 0,7 + 0,3 × 0,875 = 0,9625.
            CakeBuild build = Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.4f, 0.5f), Deco("deco_b", 0.7f, 0.5f));
            Assert.AreEqual(0.9625f, CustomOrderScoring.Score(_settings, Order(100, Target()), build).similarity, Epsilon);
        }

        [Test]
        public void MissingDecoration_LowersScore()
        {
            // 2 hedef süsten yalnız biri var: süs puanı 1/2 → 0,7 + 0,15 = 0,85.
            CakeBuild build = Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.3f, 0.5f));
            Assert.AreEqual(0.85f, CustomOrderScoring.Score(_settings, Order(100, Target()), build).similarity, Epsilon);
        }

        [Test]
        public void ExtraDecoration_LowersScore()
        {
            // 2 doğru süs + 1 fazla: süs puanı 2/3 → 0,7 + 0,2 = 0,9.
            CakeBuild build = Target();
            build.decorations.Add(Deco("deco_a", 0.5f, 0.8f));
            Assert.AreEqual(0.9f, CustomOrderScoring.Score(_settings, Order(100, Target()), build).similarity, Epsilon);
        }

        [Test]
        public void WrongDecorationType_GetsNothing()
        {
            // Hedef iki deco_a; oyuncu aynı yerlere iki deco_b koymuş: aynı tür eşleşmesi yok → süs puanı 0 → 0,7.
            CakeBuild target = Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.3f, 0.5f), Deco("deco_a", 0.7f, 0.5f));
            CakeBuild build = Build("base_a", "cream_a", "box_a", Deco("deco_b", 0.3f, 0.5f), Deco("deco_b", 0.7f, 0.5f));
            Assert.AreEqual(0.7f, CustomOrderScoring.Score(_settings, Order(100, target), build).similarity, Epsilon);
        }

        [Test]
        public void SwappedDecorations_KeepTypeShare()
        {
            // İki tür de var ama yerleri değişmiş (0,4 uzak, toleransın dışında): her tür yalnız tür payını alır → 0,85.
            CakeBuild build = Build("base_a", "cream_a", "box_a", Deco("deco_b", 0.3f, 0.5f), Deco("deco_a", 0.7f, 0.5f));
            Assert.AreEqual(0.85f, CustomOrderScoring.Score(_settings, Order(100, Target()), build).similarity, Epsilon);
        }

        [Test]
        public void SameTypeMatching_UsesBestAssignment()
        {
            // Hedef: iki deco_a (0,1) ve (0,9). Oyuncu: (0,85) ve (0,1). Açgözlü eşleme ilk hedefe en yakını alsa da
            // en iyi eşleme her iki süsü de yakın eşe verir: (0,1)↔(0,1) = 1, (0,9)↔(0,85) = 0,5 + 0,5 × 0,75 = 0,875.
            CakeBuild target = Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.1f, 0.5f), Deco("deco_a", 0.9f, 0.5f));
            CakeBuild build = Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.85f, 0.5f), Deco("deco_a", 0.1f, 0.5f));
            float expected = 0.7f + 0.3f * ((1f + 0.875f) / 2f);
            Assert.AreEqual(expected, CustomOrderScoring.Score(_settings, Order(100, target), build).similarity, Epsilon);
        }

        [Test]
        public void OnlyBoxWrong_Is90Percent_ThreeStars_WithTip()
        {
            CakeBuild build = Target();
            build.boxId = "box_b";
            CustomOrderResult r = CustomOrderScoring.Score(_settings, Order(50, Target()), build);

            Assert.AreEqual(0.9f, r.similarity, Epsilon);
            Assert.AreEqual(3, r.stars);
            Assert.AreEqual(45, r.gold);
            Assert.AreEqual(10, r.tip);
        }

        [Test]
        public void Stars_FollowThresholds()
        {
            Assert.AreEqual(0, CustomOrderScoring.Stars(_settings, 0.49f));
            Assert.AreEqual(1, CustomOrderScoring.Stars(_settings, 0.5f));
            Assert.AreEqual(2, CustomOrderScoring.Stars(_settings, 0.7f));
            Assert.AreEqual(3, CustomOrderScoring.Stars(_settings, 0.9f));
        }

        [Test]
        public void Score_DoesNotModifyInputs()
        {
            CustomOrder order = Order(100, Target());
            CakeBuild build = Build("base_b", "cream_a", "box_a", Deco("deco_a", 0.9f, 0.9f));
            string orderJson = JsonUtility.ToJson(order);
            string buildJson = JsonUtility.ToJson(build);

            CustomOrderScoring.Score(_settings, order, build);

            Assert.AreEqual(orderJson, JsonUtility.ToJson(order));
            Assert.AreEqual(buildJson, JsonUtility.ToJson(build));
        }

        // ------------------------------------------------------------------ Fiş üretimi

        [Test]
        public void GeneratedOrders_AreSolvable_AndScorePerfectAgainstThemselves()
        {
            var generator = new CustomOrderGenerator(_settings, new System.Random(1234));
            for (int i = 0; i < 1000; i++)
            {
                CustomOrder order = generator.Create();
                Assert.IsNotNull(order);
                Assert.IsTrue(CustomOrderGenerator.IsSolvable(_settings, order), "çözülemez fiş");

                List<DecorationPlacement> decos = order.target.decorations;
                Assert.That(decos.Count, Is.InRange(_settings.minDecorations, _settings.maxDecorations));
                Assert.That(order.baseReward, Is.InRange(_settings.orderBaseRewardMin, _settings.orderBaseRewardMax));

                for (int a = 0; a < decos.Count; a++)
                {
                    Vector2 p = decos[a].position;
                    Assert.That(p.x, Is.InRange(_settings.decorationAreaMin.x, _settings.decorationAreaMax.x));
                    Assert.That(p.y, Is.InRange(_settings.decorationAreaMin.y, _settings.decorationAreaMax.y));
                    for (int b = a + 1; b < decos.Count; b++)
                    {
                        Assert.GreaterOrEqual(Vector2.Distance(p, decos[b].position), _settings.decorationMinSpacing);
                    }
                }

                CakeBuild copy = JsonUtility.FromJson<CakeBuild>(JsonUtility.ToJson(order.target));
                Assert.AreEqual(1f, CustomOrderScoring.Score(_settings, order, copy).similarity, Epsilon);
            }
        }

        [Test]
        public void Generator_WithoutRequiredPart_ReturnsNull()
        {
            _settings.cakeParts = new[] { Part(CakePartType.Base, "base_a"), Part(CakePartType.Cream, "cream_a") };
            var generator = new CustomOrderGenerator(_settings, new System.Random(1));
            Assert.IsFalse(generator.CanGenerate);
            Assert.IsNull(generator.Create());
        }

        [Test]
        public void IsSolvable_RejectsUnknownPart()
        {
            CustomOrder order = Order(50, Target());
            order.target.creamId = "cream_missing";
            Assert.IsFalse(CustomOrderGenerator.IsSolvable(_settings, order));
        }

        // ------------------------------------------------------------------ Yardımcılar

        private CakePartData Part(CakePartType type, string id)
        {
            CakePartData part = ScriptableObject.CreateInstance<CakePartData>();
            part.type = type;
            part.id = id;
            _created.Add(part);
            return part;
        }

        // Hedef: base_a, cream_a, box_a; deco_a (0,3; 0,5), deco_b (0,7; 0,5).
        private static CakeBuild Target() =>
            Build("base_a", "cream_a", "box_a", Deco("deco_a", 0.3f, 0.5f), Deco("deco_b", 0.7f, 0.5f));

        private static CakeBuild Build(string baseId, string creamId, string boxId, params DecorationPlacement[] decos) =>
            new CakeBuild
            {
                baseId = baseId,
                creamId = creamId,
                boxId = boxId,
                decorations = new List<DecorationPlacement>(decos)
            };

        private static DecorationPlacement Deco(string id, float x, float y) =>
            new DecorationPlacement { decorationId = id, position = new Vector2(x, y) };

        private static CustomOrder Order(int reward, CakeBuild target) =>
            new CustomOrder { id = "order_test", baseReward = reward, target = target };
    }
}
