using System;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Sipariş panosundaki tek yer (Prefabs/UI/Kitchen/OrderCard.prefab). Yalnızca görünümdür; servis çağrılarını OrderBoard yapar.
    /// Görünümler:
    /// - Fiş: hedef pastanın küçük çizimi (CakeView) + ödül altını; kartın tamamına dokunulur.
    /// - Boş, geri sayım: "Yeni sipariş" + mm:ss (en yakın yenileme).
    /// - Boş, sırada: "Sırada" (bekleyen diğer yenilemeler).
    /// </summary>
    public class OrderCard : MonoBehaviour
    {
        /// <summary>Kartın gösterdiği durum.</summary>
        public enum View
        {
            None = 0,
            Ticket = 1,
            Countdown = 2,
            Queued = 3
        }

        private const string KeyNewOrder = "order.board.new_order";
        private const string KeyQueued = "order.board.queued";

        [SerializeField] private OrderUISettings settings;

        [Tooltip("Kartın zemini; rengi fiş/boş duruma göre değişir. Butonun hedef görselidir.")]
        [SerializeField] private Image background;

        [SerializeField] private Button button;

        [Header("Fiş")]
        [SerializeField] private GameObject ticketGroup;
        [SerializeField] private CakeView cakeView;
        [SerializeField] private TMP_Text rewardText;

        [Header("Boş")]
        [SerializeField] private GameObject emptyGroup;
        [SerializeField] private LocalizedText emptyLabel;
        [SerializeField] private TMP_Text countdownText;

        /// <summary>Fişe dokunuldu.</summary>
        public event Action<OrderCard> Clicked;

        private int shownCountdown = -1;

        public View CurrentView { get; private set; } = View.None;

        /// <summary>Gösterilen fişin id'si; boş yerde null.</summary>
        public string OrderId { get; private set; }

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        // ------------------------------------------------------------------ Görünümler

        /// <summary>Fişi gösterir: hedef pasta ve taban ödül.</summary>
        public void ShowTicket(CustomOrder order, KitchenSettings kitchen)
        {
            SetView(View.Ticket);
            OrderId = order != null ? order.id : null;

            if (cakeView != null)
            {
                cakeView.Render(order != null ? order.target : null, kitchen);
            }

            if (rewardText != null)
            {
                rewardText.text = order != null ? Mathf.Max(0, order.baseReward).ToString() : "0";
            }
        }

        /// <summary>Boş yer. countdown: en yakın yenilemeyi bu yer mi gösteriyor (değilse "Sırada").</summary>
        public void ShowEmpty(bool countdown)
        {
            SetView(countdown ? View.Countdown : View.Queued);
            OrderId = null;
            KitchenUIUtil.SetKey(emptyLabel, countdown ? KeyNewOrder : KeyQueued);
            KitchenUIUtil.SetActive(countdownText, countdown);
        }

        /// <summary>Geri sayımı yazar (yalnızca saniye değişince).</summary>
        public void SetCountdown(float remainingSeconds)
        {
            int seconds = KitchenUIUtil.ToCountdownSeconds(remainingSeconds);
            if (countdownText != null && seconds != shownCountdown)
            {
                shownCountdown = seconds;
                countdownText.text = KitchenUIUtil.FormatCountdownMinSec(seconds);
            }
        }

        // ------------------------------------------------------------------ İç işler

        private void SetView(View view)
        {
            CurrentView = view;
            shownCountdown = -1;

            bool ticket = view == View.Ticket;
            KitchenUIUtil.SetActive(ticketGroup, ticket);
            KitchenUIUtil.SetActive(emptyGroup, !ticket);
            KitchenUIUtil.SetInteractable(button, ticket);

            if (background != null && settings != null)
            {
                background.color = ticket ? settings.ticketColor : settings.emptySlotColor;
            }
        }

        private void HandleClick()
        {
            if (CurrentView == View.Ticket && !string.IsNullOrEmpty(OrderId))
            {
                Clicked?.Invoke(this);
            }
        }
    }
}
