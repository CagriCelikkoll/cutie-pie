using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.Salon
{
    /// <summary>Müşterinin yaşam döngüsündeki durumu.</summary>
    public enum CustomerState
    {
        WalkingIn = 0,
        Seated = 1,
        LeavingAngry = 2,

        /// <summary>G3: doğru teslimden sonra mutlu ayrılış.</summary>
        LeavingHappy = 3
    }

    /// <summary>
    /// Salon müşterisi: girişten masasına yürür, oturunca siparişini gösterir ve sabrı azalır.
    /// Sabır biterse kızgın olarak girişe yürüyüp yok olur.
    /// Otururken dokunulursa SalonEvents.CustomerTapped yayınlar (sipariş fişe düşer); dokunma alanı prefab'taki BoxCollider2D'dir.
    /// </summary>
    public class Customer : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private OrderBubble orderBubble;
        [SerializeField] private PatienceBar patienceBar;

        private SalonSettings settings;
        private Vector3 exitPosition;
        private float patienceLeft;
        private bool initialized;

        public int Id { get; private set; }
        public CustomerTypeData Type { get; private set; }
        public Table Table { get; private set; }
        public CustomerOrder Order { get; private set; }
        public CustomerState State { get; private set; }

        /// <summary>Gövde görseli; fişteki müşteri işareti bunu kopyalar.</summary>
        public SpriteRenderer Body => body;

        /// <summary>Kalan sabır oranı (0-1).</summary>
        public float PatienceRatio => Type != null && Type.patienceSeconds > 0f ? patienceLeft / Type.patienceSeconds : 0f;

        /// <summary>Olaylarda taşınan hafif bilgi.</summary>
        public CustomerInfo Info => new CustomerInfo(Id, Type, Table != null ? Table.Index : -1);

        /// <summary>CustomerSpawner oluşturduktan hemen sonra çağırır.</summary>
        public void Init(int id, CustomerTypeData type, Table table, CustomerOrder order, Vector3 exit, SalonSettings salonSettings)
        {
            Id = id;
            Type = type;
            Table = table;
            Order = order;
            exitPosition = exit;
            settings = salonSettings;
            patienceLeft = type != null ? type.patienceSeconds : 0f;
            State = CustomerState.WalkingIn;
            name = $"Customer_{id}";

            ApplyAppearance();

            if (orderBubble != null)
            {
                orderBubble.Hide();
            }

            if (patienceBar != null)
            {
                patienceBar.Initialize(settings);
                patienceBar.Hide();
            }

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            switch (State)
            {
                case CustomerState.WalkingIn:
                    if (Table == null)
                    {
                        Destroy(gameObject);
                        return;
                    }

                    if (MoveTowards(Table.SeatPoint.position))
                    {
                        Sit();
                    }

                    break;

                case CustomerState.Seated:
                    // Sabır yalnızca otururken akar.
                    patienceLeft -= Time.deltaTime;
                    if (patienceBar != null)
                    {
                        patienceBar.SetRatio(PatienceRatio);
                    }

                    if (patienceLeft <= 0f)
                    {
                        LeaveAngry();
                    }

                    break;

                case CustomerState.LeavingAngry:
                case CustomerState.LeavingHappy:
                    if (MoveTowards(exitPosition))
                    {
                        Destroy(gameObject);
                    }

                    break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Yalnızca oturan müşterinin siparişi alınır (yürürken veya giderken dokunma yok sayılır).
            if (initialized && State == CustomerState.Seated)
            {
                SalonEvents.RaiseCustomerTapped(this);
            }
        }

        private void Sit()
        {
            State = CustomerState.Seated;

            if (orderBubble != null)
            {
                orderBubble.Show(Order, settings);
            }

            if (patienceBar != null)
            {
                patienceBar.Show();
                patienceBar.SetRatio(1f);
            }

            GameEvents.RaiseCustomerSeated(Info);
        }

        private void LeaveAngry()
        {
            State = CustomerState.LeavingAngry;
            HideOverlays();

            // Olay masa boşaltılmadan önce yayınlanır; Info masa sırasını taşımaya devam eder.
            GameEvents.RaiseCustomerLeftAngry(Info);

            if (Table != null)
            {
                Table.Release();
            }
        }

        private void HideOverlays()
        {
            if (orderBubble != null)
            {
                orderBubble.Hide();
            }

            if (patienceBar != null)
            {
                patienceBar.Hide();
            }
        }

        /// <summary>Hedefe doğru yürür; varınca konumu hedefe sabitler ve true döner.</summary>
        private bool MoveTowards(Vector3 target)
        {
            target.z = transform.position.z;
            transform.position = Vector3.MoveTowards(transform.position, target, settings.walkSpeed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude <= settings.seatReachDistance * settings.seatReachDistance)
            {
                transform.position = target;
                return true;
            }

            return false;
        }

        private void ApplyAppearance()
        {
            if (body == null || Type == null)
            {
                return;
            }

            if (Type.sprite != null)
            {
                body.sprite = Type.sprite;
                body.color = Color.white;
            }
            else
            {
                body.color = Type.placeholderColor;
            }
        }
    }
}
