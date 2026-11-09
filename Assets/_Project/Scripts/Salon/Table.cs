using System;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'daki tek bir masa yeri. Durumunu tutar ve görünümünü duruma göre günceller.
    /// Tepsi için bırakma hedefidir: hazır tepsi masanın yakınına bırakılınca teslim OrderDelivery'ye devredilir.
    /// </summary>
    public class Table : MonoBehaviour, IDropTarget
    {
        [Tooltip("Masanın sırası (0-7). Üst sıra soldan sağa 0-3, alt sıra 4-7. Açık masalar ilk N sıradır.")]
        [SerializeField] private int index;

        [Tooltip("Müşterinin yürüyüp duracağı nokta (masanın üst kenarı).")]
        [SerializeField] private Transform seatPoint;

        [Tooltip("Rengi duruma göre değişen masa gövdesi.")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Kilitliyken görünen kapak.")]
        [SerializeField] private GameObject lockVisual;

        private SalonSettings settings;
        private DropTargetRegistry registry;
        private OrderDelivery delivery;

        public int Index => index;
        public TableState State { get; private set; } = TableState.Locked;
        public Transform SeatPoint => seatPoint != null ? seatPoint : transform;
        public bool IsFree => State == TableState.Empty;

        /// <summary>Masaya ayrılmış müşteri (yoldayken de); yoksa null.</summary>
        public Customer Occupant { get; private set; }

        /// <summary>Masaya teslim edilmiş tepsi; yoksa null.</summary>
        public Tray ServedTray { get; private set; }

        /// <summary>Tepsinin bırakılacağı nokta: masanın merkezi (üstündeki müşteri de snapDistance içinde kalır).</summary>
        public Vector3 SnapPoint => transform.position;

        /// <summary>Durum her değiştiğinde tetiklenir.</summary>
        public event Action<Table> StateChanged;

        /// <summary>
        /// TableManager kurulumda çağırır: ayarları verir, başlangıç durumunu belirler ve masayı bırakma hedefi olarak kaydeder.
        /// registry veya delivery yoksa masaya tepsi bırakılamaz.
        /// </summary>
        public void Initialize(SalonSettings salonSettings, bool open, DropTargetRegistry dropTargets, OrderDelivery orderDelivery)
        {
            settings = salonSettings;
            delivery = orderDelivery;
            Occupant = null;
            ClearServedTray();

            if (registry != null && registry != dropTargets)
            {
                registry.Unregister(this);
            }

            registry = dropTargets;
            if (registry != null)
            {
                registry.Register(this);
            }

            SetState(open ? TableState.Empty : TableState.Locked, true);
        }

        private void OnDestroy()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }
        }

        public void Lock() => SetState(TableState.Locked);

        public void Unlock()
        {
            if (State == TableState.Locked)
            {
                SetState(TableState.Empty);
            }
        }

        /// <summary>Boş masayı bir müşteriye ayırır. Masa boş değilse false döner.</summary>
        public bool Reserve(Customer occupant)
        {
            if (State != TableState.Empty)
            {
                Debug.LogWarning($"[Table] {index} numaralı masa boş değil ({State}); ayrılamadı.", this);
                return false;
            }

            Occupant = occupant;
            SetState(TableState.Occupied);
            return true;
        }

        /// <summary>Müşteri gidince masayı yeniden boşaltır; masadaki tepsi de kaldırılır.</summary>
        public void Release()
        {
            if (State == TableState.Occupied)
            {
                Occupant = null;
                ClearServedTray();
                SetState(TableState.Empty);
            }
        }

        /// <summary>Mutlu teslimden sonra masa kirlenir (G4: tepsi kirli tabak olarak masada kalır).</summary>
        public void MarkDirty()
        {
            if (State == TableState.Occupied)
            {
                Occupant = null;
                SetState(TableState.Dirty);
            }
        }

        /// <summary>Kirli masa temizlenince boşalır (G4).</summary>
        public void Clean()
        {
            if (State == TableState.Dirty)
            {
                ClearServedTray();
                SetState(TableState.Empty);
            }
        }

        /// <summary>OrderDelivery doğru teslimde çağırır: tepsiyi masaya küçültülmüş olarak oturtur.</summary>
        public void PlaceServedTray(Tray tray)
        {
            if (tray == null)
            {
                return;
            }

            ClearServedTray();
            ServedTray = tray;
            tray.PlaceOnTable(transform, settings != null ? settings.trayOnTableScale : 1f, SalonSortOrder.TrayOnTable);
        }

        private void ClearServedTray()
        {
            if (ServedTray != null)
            {
                Destroy(ServedTray.gameObject);
            }

            ServedTray = null;
        }

        // --- Bırakma hedefi (IDropTarget): kurallar OrderDelivery'de, masa yalnızca yönlendirir ---

        public bool CanAccept(ServingItem item)
        {
            if (delivery == null || item == null || item.Kind != ServingItemKind.Tray || State != TableState.Occupied)
            {
                return false;
            }

            return delivery.CanDeliver(item.GetComponent<Tray>(), Occupant);
        }

        public void Accept(ServingItem item)
        {
            // Yanlış siparişte tepsi tezgâhta kalır; Tray.OnDropped onu tezgâhtaki yerine geri gönderir.
            delivery.Deliver(item.GetComponent<Tray>(), Occupant);
        }

        private void SetState(TableState newState, bool force = false)
        {
            if (!force && newState == State)
            {
                return;
            }

            State = newState;
            RefreshVisual();
            StateChanged?.Invoke(this);
        }

        private void RefreshVisual()
        {
            if (lockVisual != null)
            {
                lockVisual.SetActive(State == TableState.Locked);
            }

            if (body != null && settings != null)
            {
                body.color = settings.GetTableColor(State);
            }
        }
    }
}
