using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'daki tek bir masa yeri. Durumunu tutar ve görünümünü duruma göre günceller.
    /// Tepsi için bırakma hedefidir: hazır tepsi masanın yakınına bırakılınca teslim OrderDelivery'ye devredilir.
    /// Müşteri yiyip kalkınca masa kirlenir (boş tabak kalır); kirli masaya dokununca temizlenir.
    /// Dokunma alanı prefab'taki BoxCollider2D'dir; masadaki tepsiye dokunulsa da tıklama masaya çıkar.
    /// Kilitli masa geliştirmeyle açılınca kısa bir "açılma" animasyonu oynar (gövde büyüyüp küçülür, parıltı söner).
    /// </summary>
    public class Table : MonoBehaviour, IDropTarget, IPointerClickHandler
    {
        [Tooltip("Masanın sırası (0-7). Üst sıra soldan sağa 0-3, alt sıra 4-7. Açık masalar ilk N sıradır.")]
        [SerializeField] private int index;

        [Tooltip("Müşterinin yürüyüp duracağı nokta (masanın üst kenarı).")]
        [SerializeField] private Transform seatPoint;

        [Tooltip("Rengi duruma göre değişen masa gövdesi.")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Kilitliyken görünen kapak.")]
        [SerializeField] private GameObject lockVisual;

        [Tooltip("Temizlikte büyüyüp sönen parıltı görseli (normalde kapalı).")]
        [SerializeField] private SpriteRenderer sparkle;

        private SalonSettings settings;
        private DropTargetRegistry registry;
        private OrderDelivery delivery;
        private Vector3 sparkleBaseScale = Vector3.one;
        private Vector3 bodyBaseScale = Vector3.one;
        private float cleanTimeLeft;
        private float unlockTimeLeft;

        public int Index => index;
        public TableState State { get; private set; } = TableState.Locked;
        public Transform SeatPoint => seatPoint != null ? seatPoint : transform;
        public bool IsFree => State == TableState.Empty;

        /// <summary>Temizlik parıltısı sürüyor (masa bu sırada hâlâ Dirty).</summary>
        public bool IsCleaning { get; private set; }

        /// <summary>Açılma animasyonu sürüyor (masa bu sırada Empty'dir ve müşteri alabilir).</summary>
        public bool IsUnlocking { get; private set; }

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
        /// animateUnlock: masa açık kurulacaksa açılma animasyonu oynatılır (TableManager yalnızca yeni açılan masada ister).
        /// </summary>
        public void Initialize(SalonSettings salonSettings, bool open, DropTargetRegistry dropTargets, OrderDelivery orderDelivery,
            bool animateUnlock = false)
        {
            settings = salonSettings;
            delivery = orderDelivery;
            Occupant = null;
            ClearServedTray();
            StopSparkle();
            StopUnlock();

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

            if (open && animateUnlock)
            {
                StartUnlock();
            }
        }

        private void Awake()
        {
            if (sparkle != null)
            {
                sparkleBaseScale = sparkle.transform.localScale;
                sparkle.enabled = false;
            }

            if (body != null)
            {
                bodyBaseScale = body.transform.localScale;
            }
        }

        private void OnDestroy()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }
        }

        private void Update()
        {
            if (IsCleaning)
            {
                UpdateCleaning();
            }
            else if (IsUnlocking)
            {
                UpdateUnlock();
            }
        }

        private void UpdateCleaning()
        {
            cleanTimeLeft -= Time.deltaTime;
            if (cleanTimeLeft > 0f)
            {
                UpdateSparkle(1f - cleanTimeLeft / settings.cleanSparkleDuration, settings.cleanSparkleMaxScale, settings.cleanSparkleColor);
                return;
            }

            StopSparkle();
            SetState(TableState.Empty);
        }

        public void Lock()
        {
            StopUnlock();
            SetState(TableState.Locked);
        }

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

        /// <summary>Müşteri yiyip kalkınca masa kirlenir: tepsideki dilim ve bardak kalkar, boş tabak masada kalır.</summary>
        public void MarkDirty()
        {
            if (State != TableState.Occupied)
            {
                return;
            }

            Occupant = null;
            if (ServedTray != null)
            {
                ServedTray.ClearFood();
            }

            SetState(TableState.Dirty);
        }

        /// <summary>
        /// Kirli masayı temizler: tabak kalkar, kısa parıltı oynar, parıltı bitince masa boşalır.
        /// Dokunmadan bağımsızdır; ileride "bulaşıkçı" geliştirmesi de bunu doğrudan çağıracak.
        /// Masa kirli değilse veya zaten temizleniyorsa false döner.
        /// </summary>
        public bool Clean()
        {
            if (State != TableState.Dirty || IsCleaning)
            {
                return false;
            }

            ClearServedTray();

            // Parıltı görseli ortaktır; açılma animasyonu sürüyorsa bitirilir.
            StopUnlock();

            // Ayarlar yoksa parıltı oynatılamaz; masa hemen boşalır.
            if (settings == null)
            {
                SetState(TableState.Empty);
                return true;
            }

            IsCleaning = true;
            cleanTimeLeft = settings.cleanSparkleDuration;
            if (sparkle != null)
            {
                sparkle.enabled = true;
            }

            UpdateSparkle(0f, settings.cleanSparkleMaxScale, settings.cleanSparkleColor);
            return true;
        }

        /// <summary>Oyuncu masaya dokundu: kirliyse temizlenir (round bittiyse yok sayılır).</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!SalonInput.IsLocked && State == TableState.Dirty)
            {
                Clean();
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

        // t: 0 → 1 parıltı ilerlemesi; maxScale'e büyür ve söner. Temizlik ve açılma aynı görseli kullanır.
        private void UpdateSparkle(float t, float maxScale, Color baseColor)
        {
            if (sparkle == null)
            {
                return;
            }

            sparkle.transform.localScale = sparkleBaseScale * Mathf.Lerp(1f, maxScale, t);
            Color color = baseColor;
            color.a = Mathf.Lerp(color.a, 0f, t);
            sparkle.color = color;
        }

        // --- Açılma animasyonu ---

        private void StartUnlock()
        {
            // Ayarlar yoksa animasyon oynatılamaz; masa yine de açıktır.
            if (settings == null)
            {
                return;
            }

            IsUnlocking = true;
            unlockTimeLeft = settings.tableUnlockDuration;
            if (sparkle != null)
            {
                sparkle.enabled = true;
            }

            ApplyUnlockProgress(0f);
        }

        private void UpdateUnlock()
        {
            unlockTimeLeft -= Time.deltaTime;
            if (unlockTimeLeft > 0f)
            {
                ApplyUnlockProgress(1f - unlockTimeLeft / settings.tableUnlockDuration);
                return;
            }

            StopUnlock();
        }

        // t: 0 → 1. Gövde sinüs eğrisiyle büyüyüp eski boyuna döner; parıltı büyüyerek söner.
        private void ApplyUnlockProgress(float t)
        {
            if (body != null)
            {
                float pop = Mathf.Sin(t * Mathf.PI);
                body.transform.localScale = bodyBaseScale * Mathf.Lerp(1f, settings.tableUnlockBodyMaxScale, pop);
            }

            UpdateSparkle(t, settings.tableUnlockSparkleMaxScale, settings.tableUnlockSparkleColor);
        }

        private void StopUnlock()
        {
            if (!IsUnlocking)
            {
                return;
            }

            IsUnlocking = false;
            unlockTimeLeft = 0f;
            if (body != null)
            {
                body.transform.localScale = bodyBaseScale;
            }

            if (sparkle != null)
            {
                sparkle.enabled = false;
                sparkle.transform.localScale = sparkleBaseScale;
            }
        }

        private void StopSparkle()
        {
            IsCleaning = false;
            cleanTimeLeft = 0f;
            if (sparkle != null)
            {
                sparkle.enabled = false;
                sparkle.transform.localScale = sparkleBaseScale;
            }
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
