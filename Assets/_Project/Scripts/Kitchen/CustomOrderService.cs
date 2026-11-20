using System;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Özel sipariş servisi. Erişim: GameManager.Instance.Orders.
    /// Pano en fazla KitchenSettings.boardSize fiş tutar; tamamlanan fişin yerine orderRefreshSeconds sonra yenisi gelir.
    /// Pano ve bekleyen yenilemeler kayıtlıdır (SaveSystem). Saat kuralı fırınlarla aynıdır: şimdi = max(UTC şimdi, son görülen);
    /// cihaz saati geri alınırsa yenilenme ilerlemez. Oyun kapalıyken dolan yenilemeler açılışta eklenir (pano boyutunu aşmadan).
    /// GameManager açılışta Initialize, her karede Tick çağırır.
    /// </summary>
    public class CustomOrderService : ICustomOrderService
    {
        private readonly KitchenSettings _settings;
        private readonly CustomOrderGenerator _generator;

        private bool _initialized;

        public CustomOrderService(KitchenSettings settings, System.Random random = null)
        {
            _settings = settings;
            _generator = new CustomOrderGenerator(settings, random);

            if (_settings == null)
            {
                Debug.LogWarning("[CustomOrderService] KitchenSettings atanmamış; pano boş.");
            }
        }

        // ------------------------------------------------------------------ Yaşam döngüsü

        /// <summary>
        /// Kayıt yüklendikten sonra bir kez çağrılır: çözülemeyen fişleri ayıklar, pano boyutunu aşanları kırpar,
        /// dolan yenilemeleri ekler, eksik yerleri hemen doldurur (yeni ve eski kayıtta pano dolu başlar).
        /// </summary>
        public void Initialize()
        {
            SaveSystem save = SaveSystem.Instance;
            if (_settings == null || save == null || !save.IsLoaded)
            {
                Debug.LogWarning("[CustomOrderService] Kayıt yüklenmeden başlatılamaz.");
                return;
            }

            if (!_generator.CanGenerate)
            {
                Debug.LogWarning("[CustomOrderService] KitchenSettings.cakeParts eksik (taban/krema/süs/kutu); yeni fiş üretilemez.");
            }

            _initialized = true;
            bool changed = RemoveUnsolvable(save);
            changed |= TrimToBoardSize(save);
            changed |= ApplyDueRefreshes(save, NowTicks());
            changed |= FillEmptySlots(save);

            if (changed)
            {
                save.Save();
            }

            // Pano ekranı açılışta tutarlı durumu okusun diye her zaman yayınlanır.
            GameEvents.RaiseCustomOrderBoardChanged();
        }

        /// <summary>Her karede çağrılır: süresi dolan yenilemeleri fişe çevirir.</summary>
        public void Tick()
        {
            SaveSystem save = SaveSystem.Instance;
            if (!_initialized || save == null || !save.IsLoaded)
            {
                return;
            }

            // Eksik yer doldurma: kayıt oyun sırasında sıfırlanırsa (Kaydı Sil) pano boş kalmasın.
            bool changed = ApplyDueRefreshes(save, NowTicks());
            changed |= FillEmptySlots(save);
            if (changed)
            {
                save.Save();
                GameEvents.RaiseCustomOrderBoardChanged();
            }
        }

        // ------------------------------------------------------------------ Sorgular

        public IReadOnlyList<CustomOrder> Board
        {
            get
            {
                SaveSystem save = SaveSystem.Instance;
                return save != null && _initialized ? save.BoardOrders : Array.Empty<CustomOrder>();
            }
        }

        /// <summary>Bekleyen yenileme sayısı (boş pano yerleri).</summary>
        public int PendingRefreshCount
        {
            get
            {
                SaveSystem save = SaveSystem.Instance;
                return save != null && _initialized ? save.PendingOrderRefreshes.Count : 0;
            }
        }

        /// <summary>En yakın yenilemeye kalan gerçek zamanlı saniye; bekleyen yoksa 0.</summary>
        public float GetSecondsUntilNextRefresh()
        {
            SaveSystem save = SaveSystem.Instance;
            if (_settings == null || save == null || !_initialized || save.PendingOrderRefreshes.Count == 0)
            {
                return 0f;
            }

            return CustomOrderRefreshMath.SecondsUntilNext(save.PendingOrderRefreshes, NowTicks(), RefreshTicks());
        }

        public CustomOrderResult Score(string orderId, CakeBuild build) =>
            CustomOrderScoring.Score(_settings, Find(orderId), build);

        // ------------------------------------------------------------------ Komutlar

        public CustomOrderResult Complete(string orderId, CakeBuild build)
        {
            SaveSystem save = SaveSystem.Instance;
            CustomOrder order = Find(orderId);
            if (order == null || save == null)
            {
                return null;
            }

            CustomOrderResult result = CustomOrderScoring.Score(_settings, order, build);
            if (result == null)
            {
                return null;
            }

            int reward = result.gold + result.tip;
            if (reward > 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.Add(CurrencyType.Gold, reward);
                }
                else
                {
                    Debug.LogError("[CustomOrderService] EconomyManager yok; ödül verilemedi.");
                }
            }

            save.RemoveBoardOrder(order.id);
            save.AddPendingOrderRefresh(NowTicks());
            save.Save();

            GameEvents.RaiseCustomOrderCompleted(result);
            GameEvents.RaiseCustomOrderBoardChanged();
            return result;
        }

#if UNITY_EDITOR
        /// <summary>Yalnızca editör (test menüsü): bekleyen tüm yenilemeleri hemen bitirir.</summary>
        public void FinishRefreshesForDebug()
        {
            SaveSystem save = SaveSystem.Instance;
            if (_settings == null || save == null || !_initialized)
            {
                return;
            }

            long finishedAt = NowTicks() - RefreshTicks();
            for (int i = 0; i < save.PendingOrderRefreshes.Count; i++)
            {
                save.SetPendingOrderRefreshAt(i, Math.Min(save.PendingOrderRefreshes[i], finishedAt));
            }

            Tick();
        }

        /// <summary>Yalnızca editör (test menüsü): bir fiş için rastgele bir pasta üretir (puanı rastgele çıkar).</summary>
        public CakeBuild CreateRandomBuildForDebug()
        {
            CustomOrder random = _generator.Create();
            return random != null ? random.target : new CakeBuild();
        }
#endif

        // ------------------------------------------------------------------ İç

        private CustomOrder Find(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return null;
            }

            foreach (CustomOrder order in Board)
            {
                if (order != null && order.id == orderId)
                {
                    return order;
                }
            }

            return null;
        }

        private bool RemoveUnsolvable(SaveSystem save)
        {
            bool changed = false;
            var orders = new List<CustomOrder>(save.BoardOrders);
            foreach (CustomOrder order in orders)
            {
                if (!CustomOrderGenerator.IsSolvable(_settings, order))
                {
                    Debug.LogWarning($"[CustomOrderService] Çözülemeyen kayıtlı fiş silindi: {order?.id}");
                    save.RemoveBoardOrder(order?.id);
                    changed = true;
                }
            }

            return changed;
        }

        // Fiş + bekleyen yenileme toplamı pano boyutunu aşmaz: önce en geç biten yenilemeler, sonra en yeni fişler silinir.
        private bool TrimToBoardSize(SaveSystem save)
        {
            bool changed = false;
            while (save.BoardOrders.Count + save.PendingOrderRefreshes.Count > _settings.boardSize &&
                   save.PendingOrderRefreshes.Count > 0)
            {
                save.RemovePendingOrderRefreshAt(LatestPendingIndex(save));
                changed = true;
            }

            while (save.BoardOrders.Count > _settings.boardSize)
            {
                save.RemoveBoardOrder(save.BoardOrders[save.BoardOrders.Count - 1].id);
                changed = true;
            }

            return changed;
        }

        private static int LatestPendingIndex(SaveSystem save)
        {
            int index = 0;
            for (int i = 1; i < save.PendingOrderRefreshes.Count; i++)
            {
                if (save.PendingOrderRefreshes[i] > save.PendingOrderRefreshes[index])
                {
                    index = i;
                }
            }

            return index;
        }

        // Süresi dolan yenilemeleri fişe çevirir. Fiş üretilemezse yenileme beklemede kalır.
        private bool ApplyDueRefreshes(SaveSystem save, long nowTicks)
        {
            bool changed = false;
            for (int i = save.PendingOrderRefreshes.Count - 1; i >= 0; i--)
            {
                if (nowTicks < DueTicks(save.PendingOrderRefreshes[i]) || save.BoardOrders.Count >= _settings.boardSize)
                {
                    continue;
                }

                CustomOrder order = _generator.Create();
                if (order == null)
                {
                    break;
                }

                save.RemovePendingOrderRefreshAt(i);
                save.AddBoardOrder(order);
                changed = true;
            }

            return changed;
        }

        // Fiş + bekleyen yenileme toplamı pano boyutundan azsa eksik yerler hemen fişle doldurulur.
        private bool FillEmptySlots(SaveSystem save)
        {
            bool changed = false;
            while (save.BoardOrders.Count + save.PendingOrderRefreshes.Count < _settings.boardSize)
            {
                CustomOrder order = _generator.Create();
                if (order == null)
                {
                    break;
                }

                save.AddBoardOrder(order);
                changed = true;
            }

            return changed;
        }

        private long RefreshTicks() => CustomOrderRefreshMath.RefreshTicks(_settings.orderRefreshSeconds);

        private long DueTicks(long completedTicks) => CustomOrderRefreshMath.DueTicks(completedTicks, RefreshTicks());

        // Saat kuralı: şimdiki zaman son görülen zamanın gerisine düşmez (saat geri alınırsa yenilenme donar).
        private static long NowTicks()
        {
            long now = GameClock.UtcNow.Ticks;
            SaveSystem save = SaveSystem.Instance;
            if (save == null)
            {
                return now;
            }

            save.AdvanceLastSeen(now);
            return CustomOrderRefreshMath.Now(now, save.LastSeenUtcTicks);
        }
    }
}
