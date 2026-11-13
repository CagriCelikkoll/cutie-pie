using System;
using System.Collections.Generic;
using CutiePie.Ads;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Upgrades;
using UnityEngine;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Fırın servisi. Erişim: GameManager.Instance.Ovens.
    /// Durumu kendisi tutmaz; tek kaynak SaveSystem'dir (tarif + UTC başlangıç). Süre başlangıç zaman damgasından hesaplanır,
    /// bu yüzden oyun kapalıyken de işler.
    /// Saat kuralı: hesaplarda max(UTC şimdi, son görülen zaman) kullanılır. Cihaz saati geri alınırsa süre ilerlemez;
    /// ileri alınırsa fırın başına tek parti olduğu için en fazla bir parti hazır olur.
    /// GameManager açılışta Initialize, her karede Tick çağırır.
    /// </summary>
    public class OvenService : IOvenService
    {
        private readonly KitchenSettings _settings;
        private readonly StockManager _stock;
        private readonly IAdsService _ads;

        // OvenReady'nin yayınlandığı fırınlar (fırın başına bir kez; fırın boşalınca/yeniden pişince silinir).
        private readonly HashSet<int> _readyNotified = new HashSet<int>();

        private bool _initialized;

        public OvenService(KitchenSettings settings, StockManager stock, IAdsService ads)
        {
            _settings = settings;
            _stock = stock;
            _ads = ads;

            if (_settings == null)
            {
                Debug.LogWarning("[OvenService] KitchenSettings atanmamış; fırın yok.");
            }
        }

        // ------------------------------------------------------------------ Yaşam döngüsü

        /// <summary>
        /// Kayıt yüklendikten sonra bir kez çağrılır: bilinmeyen tarifli fırınları boşaltır ve
        /// açılışta zaten hazır olan fırınlar için OvenReady yayınlar.
        /// </summary>
        public void Initialize()
        {
            SaveSystem save = SaveSystem.Instance;
            if (_settings == null || save == null || !save.IsLoaded)
            {
                Debug.LogWarning("[OvenService] Kayıt yüklenmeden başlatılamaz.");
                return;
            }

            bool changed = false;
            for (int i = 0; i < Count; i++)
            {
                if (save.TryGetOven(i, out string recipeId, out _) && _settings.FindRecipe(recipeId) == null)
                {
                    Debug.LogWarning($"[OvenService] Fırın {i}: bilinmeyen tarif '{recipeId}', fırın boşaltıldı.");
                    save.SetOven(i, null, 0L);
                    changed = true;
                }
            }

            if (changed)
            {
                save.Save();
            }

            _readyNotified.Clear();
            _initialized = true;
            Tick();
        }

        /// <summary>Her karede çağrılır: son görülen zamanı ilerletir, yeni hazır olan fırınlar için OvenReady yayınlar.</summary>
        public void Tick()
        {
            if (!_initialized || SaveSystem.Instance == null || !SaveSystem.Instance.IsLoaded)
            {
                return;
            }

            long now = NowTicks();
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (GetState(i, now) == OvenState.Ready)
                {
                    if (_readyNotified.Add(i))
                    {
                        GameEvents.RaiseOvenReady(i);
                    }
                }
                else
                {
                    _readyNotified.Remove(i);
                }
            }
        }

        // ------------------------------------------------------------------ Sorgular

        public int Count
        {
            get
            {
                if (_settings == null)
                {
                    return 0;
                }

                SaveSystem save = SaveSystem.Instance;
                int upgradeLevel = save != null ? save.GetUpgradeLevel(UpgradeIds.ExtraOven) : 0;
                return Mathf.Clamp(_settings.startingOvenCount + upgradeLevel, 0, _settings.maxOvenCount);
            }
        }

        public OvenState GetState(int ovenIndex) => GetState(ovenIndex, NowTicks());

        public string GetRecipeId(int ovenIndex) =>
            TryGetBake(ovenIndex, out RecipeData recipe, out _) ? recipe.id : null;

        public float GetRemainingSeconds(int ovenIndex)
        {
            if (!TryGetBake(ovenIndex, out RecipeData recipe, out long start))
            {
                return 0f;
            }

            long remaining = BakeTicks(recipe) - Elapsed(start, NowTicks());
            return remaining > 0 ? (float)remaining / TimeSpan.TicksPerSecond : 0f;
        }

        // ------------------------------------------------------------------ Komutlar

        public bool StartBake(int ovenIndex, string recipeId)
        {
            SaveSystem save = SaveSystem.Instance;
            if (!CanUse(ovenIndex) || GetState(ovenIndex) != OvenState.Empty)
            {
                return false;
            }

            RecipeData recipe = _settings.FindRecipe(recipeId);
            if (recipe == null || recipe.product == null)
            {
                Debug.LogWarning($"[OvenService] Bilinmeyen ya da ürünü atanmamış tarif: '{recipeId}'.");
                return false;
            }

            if (!save.SetOven(ovenIndex, recipe.id, NowTicks()))
            {
                return false;
            }

            _readyNotified.Remove(ovenIndex);
            save.Save();
            GameEvents.RaiseBakeStarted(ovenIndex, recipe.id);
            return true;
        }

        public bool Collect(int ovenIndex)
        {
            SaveSystem save = SaveSystem.Instance;
            if (!CanUse(ovenIndex) || GetState(ovenIndex) != OvenState.Ready ||
                !TryGetBake(ovenIndex, out RecipeData recipe, out long start))
            {
                return false;
            }

            // Önce fırın boşaltılır; StockManager.Add ikisini birlikte kaydeder.
            save.SetOven(ovenIndex, null, 0L);
            if (_stock == null || !_stock.Add(recipe.product.id, recipe.slicesPerBatch))
            {
                save.SetOven(ovenIndex, recipe.id, start);
                return false;
            }

            _readyNotified.Remove(ovenIndex);
            GameEvents.RaiseBatchCollected(recipe.product.id, recipe.slicesPerBatch);
            return true;
        }

        public void SpeedUpWithAd(int ovenIndex, Action<bool> callback)
        {
            if (!CanUse(ovenIndex) || GetState(ovenIndex) != OvenState.Baking || _ads == null || !_ads.IsRewardedReady ||
                !TryGetBake(ovenIndex, out RecipeData recipe, out long start))
            {
                callback?.Invoke(false);
                return;
            }

            _ads.ShowRewarded(watched =>
            {
                // Reklam sırasında fırın değişmiş olabilir (toplandı, süre doldu, kayıt silindi): aynı parti hâlâ pişiyor olmalı.
                bool samePartyBaking = watched &&
                                       TryGetBake(ovenIndex, out RecipeData current, out long currentStart) &&
                                       current == recipe && currentStart == start &&
                                       GetState(ovenIndex) == OvenState.Baking;

                if (samePartyBaking)
                {
                    MakeReady(ovenIndex, recipe);
                }

                callback?.Invoke(samePartyBaking);
            });
        }

#if UNITY_EDITOR
        /// <summary>Yalnızca editör (test menüsü): pişen tüm partileri anında hazır yapar.</summary>
        public void FinishAllForDebug()
        {
            for (int i = 0; i < Count; i++)
            {
                if (GetState(i) == OvenState.Baking && TryGetBake(i, out RecipeData recipe, out _))
                {
                    MakeReady(i, recipe);
                }
            }
        }
#endif

        // ------------------------------------------------------------------ Yardımcılar

        // Partiyi hemen hazır yapar: başlangıç, pişme süresi kadar geriye çekilir. Kaydeder ve OvenReady yayınlar.
        private void MakeReady(int ovenIndex, RecipeData recipe)
        {
            SaveSystem save = SaveSystem.Instance;
            save.SetOven(ovenIndex, recipe.id, NowTicks() - BakeTicks(recipe));
            save.Save();

            if (_readyNotified.Add(ovenIndex))
            {
                GameEvents.RaiseOvenReady(ovenIndex);
            }
        }

        private OvenState GetState(int ovenIndex, long nowTicks)
        {
            if (!TryGetBake(ovenIndex, out RecipeData recipe, out long start))
            {
                return OvenState.Empty;
            }

            return Elapsed(start, nowTicks) >= BakeTicks(recipe) ? OvenState.Ready : OvenState.Baking;
        }

        // Fırın geçerli ve içinde bilinen bir tarif varsa true.
        private bool TryGetBake(int ovenIndex, out RecipeData recipe, out long startUtcTicks)
        {
            recipe = null;
            startUtcTicks = 0L;
            SaveSystem save = SaveSystem.Instance;
            if (_settings == null || save == null || ovenIndex < 0 || ovenIndex >= Count ||
                !save.TryGetOven(ovenIndex, out string recipeId, out startUtcTicks))
            {
                return false;
            }

            recipe = _settings.FindRecipe(recipeId);
            return recipe != null && recipe.product != null;
        }

        private bool CanUse(int ovenIndex)
        {
            SaveSystem save = SaveSystem.Instance;
            return _settings != null && save != null && save.IsLoaded && ovenIndex >= 0 && ovenIndex < Count;
        }

        // Saat kuralı: şimdiki zaman son görülen zamanın gerisine düşmez (saat geri alınırsa süre donar).
        private static long NowTicks()
        {
            long now = GameClock.UtcNow.Ticks;
            SaveSystem save = SaveSystem.Instance;
            if (save == null)
            {
                return now;
            }

            save.AdvanceLastSeen(now);
            return Math.Max(now, save.LastSeenUtcTicks);
        }

        // Başlangıç ileride görünse de (bozuk kayıt) geçen süre negatif olmaz.
        private static long Elapsed(long startTicks, long nowTicks) => Math.Max(0L, nowTicks - startTicks);

        private static long BakeTicks(RecipeData recipe) => recipe.bakeSeconds * TimeSpan.TicksPerSecond;
    }
}
