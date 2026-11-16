using System.Collections.Generic;
using System.Text;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Kitchen.Editor
{
    /// <summary>Yalnızca editörde: Play sırasında özel sipariş panosu test menüleri (sipariş ekranı G20'de gelene kadar).</summary>
    public static class OrderMenu
    {
        private const string Root = "Cutie Pie/Sipariş/";
        private const string LogPath = Root + "Panoyu Yazdır";
        private const string PerfectPath = Root + "İlk Fişi Mükemmel Tamamla";
        private const string RandomPath = Root + "İlk Fişi Rastgele Tamamla";
        private const string FinishPath = Root + "Yenilenmeyi Bitir";

        private static CustomOrderService Service => GameManager.Instance.Orders as CustomOrderService;

        [MenuItem(LogPath)]
        private static void LogBoard()
        {
            CustomOrderService service = Service;
            IReadOnlyList<CustomOrder> board = service.Board;
            var sb = new StringBuilder();
            sb.AppendLine($"[Sipariş] Pano: {board.Count} fiş, {service.PendingRefreshCount} bekleyen yenileme, " +
                          $"sonraki yenilemeye {service.GetSecondsUntilNextRefresh():0} sn");
            foreach (CustomOrder order in board)
            {
                CakeBuild t = order.target;
                sb.Append($"  {order.id.Substring(0, 8)} ödül={order.baseReward} taban={t.baseId} krema={t.creamId} kutu={t.boxId} süs=");
                foreach (DecorationPlacement d in t.decorations)
                {
                    sb.Append($"{d.decorationId}({d.position.x:0.00},{d.position.y:0.00}) ");
                }

                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem(PerfectPath)]
        private static void CompletePerfect()
        {
            CustomOrder first = FirstOrder();
            if (first != null)
            {
                // Hedefin kopyası (JsonUtility ile derin kopya), fişin kendisi değil.
                CakeBuild copy = JsonUtility.FromJson<CakeBuild>(JsonUtility.ToJson(first.target));
                LogResult(Service.Complete(first.id, copy));
            }
        }

        [MenuItem(RandomPath)]
        private static void CompleteRandom()
        {
            CustomOrder first = FirstOrder();
            if (first != null)
            {
                LogResult(Service.Complete(first.id, Service.CreateRandomBuildForDebug()));
            }
        }

        [MenuItem(FinishPath)]
        private static void FinishRefreshes()
        {
            Service.FinishRefreshesForDebug();
            LogBoard();
        }

        [MenuItem(LogPath, true)]
        [MenuItem(PerfectPath, true)]
        [MenuItem(RandomPath, true)]
        [MenuItem(FinishPath, true)]
        private static bool Validate() =>
            EditorApplication.isPlaying && GameManager.Instance != null && Service != null;

        private static CustomOrder FirstOrder()
        {
            IReadOnlyList<CustomOrder> board = Service.Board;
            if (board.Count == 0)
            {
                Debug.Log("[Sipariş] Panoda fiş yok.");
                return null;
            }

            return board[0];
        }

        private static void LogResult(CustomOrderResult r)
        {
            if (r == null)
            {
                Debug.Log("[Sipariş] Tamamlanamadı.");
                return;
            }

            Debug.Log($"[Sipariş] Tamamlandı: benzerlik %{r.similarity * 100f:0.0}, {r.stars} yıldız, {r.gold} altın + {r.tip} bahşiş. " +
                      $"Toplam altın: {EconomyManager.Instance.Get(CurrencyType.Gold)}");
            LogBoard();
        }
    }
}
