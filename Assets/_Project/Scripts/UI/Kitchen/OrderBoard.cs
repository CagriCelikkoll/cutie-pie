using System;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Kitchen;
using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Özel sipariş panosu (G20). Kendi Canvas'ı olan prefab'dır (Prefabs/UI/Kitchen/OrderBoard.prefab, sıralama 5).
    /// Mutfak ekranının sağ sütununda hep görünür; kapatma düğmesi yoktur.
    /// - KitchenSettings.boardSize kadar yer: dolu yer fiş kartıdır (hedef pastanın küçük çizimi + ödül altını).
    /// - Boş yerlerin ilki "Yeni sipariş mm:ss" geri sayımını, diğerleri "Sırada" yazısını gösterir.
    /// - Fişe dokununca iç içe CakeBuilder (pasta yapım ekranı, tam ekran) açılır ve OrderSelected yayınlanır.
    /// Pano servisine yalnızca GameManager.Instance.Orders üzerinden erişilir. CustomOrderBoardChanged ile yenilenir;
    /// olay kaçsa da kare başı karşılaştırma görünümü düzeltir. Geri sayım gerçek zamanlıdır (Time.timeScale'den etkilenmez).
    /// </summary>
    public class OrderBoard : MonoBehaviour
    {
        [Tooltip("Kartların eklendiği kap (VerticalLayoutGroup).")]
        [SerializeField] private RectTransform cardContainer;

        [Tooltip("Prefabs/UI/Kitchen/OrderCard.prefab")]
        [SerializeField] private OrderCard cardPrefab;

        [Tooltip("İç içe CakeBuilder.prefab örneği (alt Canvas, sıralama 6); fişe dokununca açılır.")]
        [SerializeField] private CakeBuilder cakeBuilder;

        /// <summary>Fişe dokunuldu; panodaki güncel fiş verilir.</summary>
        public event Action<CustomOrder> OrderSelected;

        private readonly List<OrderCard> cards = new List<OrderCard>();
        private bool started;

        private static GameManager Game => GameManager.Instance;

        private static KitchenSettings Kitchen => Game != null && Game.Config != null ? Game.Config.kitchen : null;

        private static ICustomOrderService Orders => Game != null ? Game.Orders : null;

        // Geçici: PendingRefreshCount ve GetSecondsUntilNextRefresh henüz ICustomOrderService'te yok (altyapıdan istendi);
        // arayüze eklenince bu dönüşüm kaldırılacak.
        private static CustomOrderService OrderDetails => Orders as CustomOrderService;

        // ------------------------------------------------------------------ Yaşam döngüsü

        private void Awake()
        {
            if (cardContainer == null || cardPrefab == null || cakeBuilder == null)
            {
                Debug.LogError("[OrderBoard] Referanslar eksik; 'Cutie Pie/UI/Sipariş Ekranını Kur' menüsünü çalıştırın.", this);
            }
        }

        private void Start()
        {
            started = true;
            RefreshAll();
        }

        private void OnEnable()
        {
            GameEvents.CustomOrderBoardChanged += HandleBoardChanged;

            // İlk yenileme Start'ta (sahne yüklenirken GameManager henüz Awake olmamış olabilir);
            // sonradan yeniden etkinleşince kaçırılan olaylar burada telafi edilir.
            if (started)
            {
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            GameEvents.CustomOrderBoardChanged -= HandleBoardChanged;
        }

        private void OnDestroy()
        {
            foreach (OrderCard card in cards)
            {
                if (card != null)
                {
                    card.Clicked -= HandleCardClicked;
                }
            }
        }

        // Kartlar her karede panoyla karşılaştırılır (bellek ayırmadan); geri sayım da buradan akar.
        private void Update()
        {
            ICustomOrderService orders = Orders;
            if (cards.Count == 0 || orders == null)
            {
                return;
            }

            IReadOnlyList<CustomOrder> board = orders.Board;
            if (NeedsRefresh(board))
            {
                RefreshAll();
            }
            else
            {
                UpdateCountdown(board);
            }
        }

        // Geri sayım yalnızca ilk boş yerde: en yakın yenilemeye kalan gerçek zamanlı süre.
        private void UpdateCountdown(IReadOnlyList<CustomOrder> board)
        {
            int index = board.Count;
            if (index < cards.Count && cards[index].CurrentView == OrderCard.View.Countdown)
            {
                CustomOrderService details = OrderDetails;
                cards[index].SetCountdown(details != null ? details.GetSecondsUntilNextRefresh() : 0f);
            }
        }

        // ------------------------------------------------------------------ Yenileme

        private void RefreshAll()
        {
            KitchenSettings kitchen = Kitchen;
            ICustomOrderService orders = Orders;
            if (kitchen == null || orders == null)
            {
                Debug.LogError("[OrderBoard] GameManager, GameConfig.kitchen veya sipariş servisi yok; pano kurulamadı.", this);
                return;
            }

            IReadOnlyList<CustomOrder> board = orders.Board;
            EnsureCards(Mathf.Max(kitchen.boardSize, board.Count));

            for (int i = 0; i < cards.Count; i++)
            {
                OrderCard card = cards[i];
                if (i < board.Count)
                {
                    card.ShowTicket(board[i], kitchen);
                }
                else
                {
                    card.ShowEmpty(i == board.Count && HasPendingRefresh());
                }
            }

            UpdateCountdown(board);
        }

        private void EnsureCards(int count)
        {
            if (cardPrefab == null || cardContainer == null)
            {
                return;
            }

            while (cards.Count < count)
            {
                OrderCard card = Instantiate(cardPrefab, cardContainer, false);
                card.name = $"OrderCard_{cards.Count + 1}";
                card.Clicked += HandleCardClicked;
                cards.Add(card);
            }
        }

        // Fişler sırayla kartlarda mı, boş yerler doğru görünümde mi?
        private bool NeedsRefresh(IReadOnlyList<CustomOrder> board)
        {
            if (board.Count > cards.Count)
            {
                return true;
            }

            bool pending = HasPendingRefresh();
            for (int i = 0; i < cards.Count; i++)
            {
                OrderCard card = cards[i];
                if (i < board.Count)
                {
                    CustomOrder order = board[i];
                    if (card.CurrentView != OrderCard.View.Ticket || order == null || card.OrderId != order.id)
                    {
                        return true;
                    }
                }
                else
                {
                    OrderCard.View expected = i == board.Count && pending ? OrderCard.View.Countdown : OrderCard.View.Queued;
                    if (card.CurrentView != expected)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasPendingRefresh()
        {
            CustomOrderService details = OrderDetails;
            return details != null && details.PendingRefreshCount > 0;
        }

        // ------------------------------------------------------------------ Olaylar

        private void HandleBoardChanged()
        {
            if (started)
            {
                RefreshAll();
            }
        }

        private void HandleCardClicked(OrderCard card)
        {
            ICustomOrderService orders = Orders;
            if (orders == null || card == null)
            {
                return;
            }

            // Kartın gösterdiği fiş hâlâ panodaysa seçilir; değilse (az önce kalktıysa) pano yenilenir.
            foreach (CustomOrder order in orders.Board)
            {
                if (order != null && order.id == card.OrderId)
                {
                    if (cakeBuilder != null)
                    {
                        cakeBuilder.Open(order);
                    }

                    OrderSelected?.Invoke(order);
                    return;
                }
            }

            RefreshAll();
        }
    }
}
