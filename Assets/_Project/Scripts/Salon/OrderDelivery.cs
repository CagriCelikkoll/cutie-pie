using System;
using CutiePie.Core;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>Teslim denemesinin sonucu.</summary>
    public enum DeliveryResult
    {
        /// <summary>Teslim yapılamaz (tepsi tezgâhta değil veya müşteri oturmuyor); hiçbir şey değişmedi.</summary>
        Invalid = 0,

        /// <summary>Doğru sipariş: müşteri yiyor, CustomerServed yayınlandı.</summary>
        Correct = 1,

        /// <summary>Yanlış sipariş: müşteri reddetti, tepsi tezgâhta kaldı, WrongOrderDelivered yayınlandı.</summary>
        Wrong = 2
    }

    /// <summary>
    /// Tezgâhtaki tepsiyi bir müşteriye teslim eder. Sürüklemeden bağımsızdır: masaya bırakma (Table) bunu çağırır,
    /// ileride garson geliştirmesi de aynı Deliver metodunu doğrudan çağıracak.
    /// Altın burada eklenmez; EconomyManager CustomerServed'ı dinleyip ödeme + bahşişi ekler.
    /// </summary>
    public class OrderDelivery : MonoBehaviour
    {
        [SerializeField] private SalonSettings settings;

        [Tooltip("Doğru teslimde müşterinin üstünde yükselen altın yazısı.")]
        [SerializeField] private GoldPopup goldPopupPrefab;

        [Tooltip("Üretilen altın yazılarının konacağı boş nesne.")]
        [SerializeField] private Transform popupContainer;

        /// <summary>Bu tepsi şu an bu müşteriye götürülebilir mi? Durumu değiştirmez.</summary>
        public bool CanDeliver(Tray tray, Customer customer) =>
            tray != null && tray.Counter != null &&
            customer != null && customer.State == CustomerState.Seated && customer.Order != null;

        /// <summary>
        /// Teslimi yapar. Doğruysa tepsi tezgâhtan masaya geçer ve müşteri yemeye başlar;
        /// yanlışsa müşteri reddeder ve tepsi (içeriğiyle) tezgâhta kalır.
        /// </summary>
        public DeliveryResult Deliver(Tray tray, Customer customer)
        {
            if (!CanDeliver(tray, customer) || settings == null)
            {
                return DeliveryResult.Invalid;
            }

            CustomerInfo info = customer.Info;
            CustomerOrder order = customer.Order;
            var slice = tray.Slice != null ? tray.Slice.Product : null;
            var drink = tray.Drink != null ? tray.Drink.Product : null;

            if (!order.Matches(slice, drink))
            {
                customer.RejectOrder();
                GameEvents.RaiseWrongOrderDelivered(info);
                return DeliveryResult.Wrong;
            }

            // Ödeme ve bahşiş müşteri durumu değişmeden (sabır hâlâ akarken) hesaplanır.
            int payment = order.TotalPrice;
            int tip = customer.IsPatienceGreen ? CalculateTip(payment, customer) : 0;

            // Tepsi müşteriye gider: tezgâh boşalır (fiş tikleri de güncellenir), tepsi masaya oturur.
            tray.Counter.Release(tray);
            customer.Table.PlaceServedTray(tray);
            customer.StartEating();

            // Fiş bu olayla kapanır (OrderTicket), altını EconomyManager ekler, seriyi RoundManager sayar.
            GameEvents.RaiseCustomerServed(info, payment, tip);

            ShowPopups(customer.transform.position, payment, tip);
            return DeliveryResult.Correct;
        }

        private int CalculateTip(int payment, Customer customer)
        {
            float multiplier = customer.Type != null ? customer.Type.tipMultiplier : 0f;

            // Yarım değerler yukarı yuvarlanır (RoundManager'daki kombo hesabıyla aynı kural).
            return (int)Math.Round(payment * settings.tipPaymentRatio * multiplier, MidpointRounding.AwayFromZero);
        }

        private void ShowPopups(Vector3 customerPosition, int payment, int tip)
        {
            if (goldPopupPrefab == null)
            {
                return;
            }

            Vector3 basePosition = customerPosition + (Vector3)settings.popupOffset;
            SpawnPopup(basePosition, payment, settings.goldPopupColor);

            if (tip > 0)
            {
                SpawnPopup(basePosition + (Vector3)settings.tipPopupOffset, tip, settings.tipPopupColor);
            }
        }

        private void SpawnPopup(Vector3 position, int amount, Color color)
        {
            GoldPopup popup = Instantiate(goldPopupPrefab, position, Quaternion.identity, popupContainer);
            popup.Play(amount, color, settings);
        }
    }
}
