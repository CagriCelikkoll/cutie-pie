using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Hazırlık alanındaki sipariş fişi. Oturan müşteriye dokununca o müşterinin işareti, pastası ve içeceği görünür;
    /// tezgâhtaki tepside doğru ürün olan satıra tik konur. Fişteki müşteri kızgın giderse veya ona doğru teslim
    /// yapılırsa (CustomerServed, CustomerId eşleşmesi) fiş temizlenir; yanlış teslimde fiş açık kalır.
    /// </summary>
    public class OrderTicket : MonoBehaviour
    {
        [SerializeField] private SalonSettings settings;
        [SerializeField] private PrepCounter counter;

        [Tooltip("Fiş boşken gizlenen içerik (kâğıt, işaret, satırlar).")]
        [SerializeField] private GameObject content;

        [Tooltip("Hangi müşterinin fişi olduğunu gösteren işaret (müşteri gövdesinin görünümü kopyalanır).")]
        [SerializeField] private SpriteRenderer customerMarker;

        [SerializeField] private TicketLine cakeLine;
        [SerializeField] private TicketLine drinkLine;

        private Tray watchedTray;

        /// <summary>Fişteki müşteri; fiş boşsa null.</summary>
        public Customer CurrentCustomer { get; private set; }

        private void OnEnable()
        {
            SalonEvents.CustomerTapped += Show;
            GameEvents.CustomerLeftAngry += OnCustomerLeft;
            GameEvents.CustomerServed += OnCustomerServed;

            if (counter != null)
            {
                counter.TraysChanged += OnTraysChanged;
                WatchTray(counter.CurrentTray);
            }
        }

        private void OnDisable()
        {
            SalonEvents.CustomerTapped -= Show;
            GameEvents.CustomerLeftAngry -= OnCustomerLeft;
            GameEvents.CustomerServed -= OnCustomerServed;

            if (counter != null)
            {
                counter.TraysChanged -= OnTraysChanged;
            }

            WatchTray(null);
        }

        private void Start()
        {
            // İçerik Awake'ler bittikten sonra gizlenir (satırlar yer tutucu sprite'larını önce saklasın).
            if (CurrentCustomer == null)
            {
                Clear();
            }
        }

        private void Update()
        {
            // Müşteri olay yayınlamadan yok olduysa fiş boşaltılır.
            if (CurrentCustomer == null && content != null && content.activeSelf)
            {
                Clear();
            }
        }

        public void Show(Customer customer)
        {
            if (customer == null)
            {
                return;
            }

            CurrentCustomer = customer;
            if (content != null)
            {
                content.SetActive(true);
            }

            ApplyMarker(customer);

            CustomerOrder order = customer.Order;
            if (cakeLine != null)
            {
                cakeLine.Show(order?.Cake, settings);
            }

            if (drinkLine != null)
            {
                drinkLine.Show(order?.Drink, settings);
            }

            RefreshTicks();
        }

        public void Clear()
        {
            CurrentCustomer = null;
            if (content != null)
            {
                content.SetActive(false);
            }
        }

        private void ApplyMarker(Customer customer)
        {
            if (customerMarker == null || customer.Body == null)
            {
                return;
            }

            customerMarker.sprite = customer.Body.sprite;
            customerMarker.color = customer.Body.color;
        }

        private void OnCustomerLeft(CustomerInfo info) => ClearIfCurrent(info);

        private void OnCustomerServed(CustomerInfo info, int payment, int tip) => ClearIfCurrent(info);

        private void ClearIfCurrent(CustomerInfo info)
        {
            if (CurrentCustomer != null && CurrentCustomer.Id == info.CustomerId)
            {
                Clear();
            }
        }

        private void OnTraysChanged()
        {
            WatchTray(counter != null ? counter.CurrentTray : null);
            RefreshTicks();
        }

        private void WatchTray(Tray tray)
        {
            if (watchedTray != null)
            {
                watchedTray.ContentsChanged -= OnTrayContentsChanged;
            }

            watchedTray = tray;
            if (watchedTray != null)
            {
                watchedTray.ContentsChanged += OnTrayContentsChanged;
            }
        }

        private void OnTrayContentsChanged(Tray tray) => RefreshTicks();

        private void RefreshTicks()
        {
            if (CurrentCustomer == null || CurrentCustomer.Order == null)
            {
                return;
            }

            ProductData sliceProduct = null;
            ProductData drinkProduct = null;
            if (watchedTray != null)
            {
                if (watchedTray.Slice != null)
                {
                    sliceProduct = watchedTray.Slice.Product;
                }

                if (watchedTray.Drink != null)
                {
                    drinkProduct = watchedTray.Drink.Product;
                }
            }

            CustomerOrder order = CurrentCustomer.Order;
            if (cakeLine != null)
            {
                cakeLine.SetTicked(order.Cake != null && sliceProduct == order.Cake);
            }

            if (drinkLine != null)
            {
                drinkLine.SetTicked(order.Drink != null && drinkProduct == order.Drink);
            }
        }
    }
}
