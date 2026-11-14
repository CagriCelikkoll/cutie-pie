using System;
using CutiePie.Core.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CutiePie.Salon
{
    /// <summary>
    /// Round boyunca müşteri üretir. İki deneme arası süre LevelData.spawnIntervalCurve'den okunur
    /// (X: round ilerlemesi 0-1, Y: saniye). Boş masa yoksa o deneme atlanır (kaçırma sayılmaz, olay yayınlanmaz).
    /// SalonRound, RoundStarted'da StartSpawning, RoundEnded'da StopSpawning çağırır.
    /// Stok ayırma (G16): yeni müşteri bir pastayı ancak o pastanın serbest dilimi varsa ister.
    /// Serbest = stok − ayrılan; ayrılan = max(0, bekleyen sipariş − tezgâhtaki tepsilerdeki dilim).
    /// Bekleyen sipariş: yürüyen veya oturan (henüz teslim almamış) müşterilerin bu pastayı isteyenleri.
    /// Tepsideki dilim stoktan zaten düştüğü için bekleyen siparişlerden biri onunla karşılanmış sayılır.
    /// Hiçbir pastanın serbest dilimi yoksa müşteri yalnızca içecek ister.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private SalonSettings settings;
        [SerializeField] private TableManager tableManager;
        [SerializeField] private Customer customerPrefab;

        [Tooltip("Müşterilerin girip çıktığı nokta (sağ üst).")]
        [SerializeField] private Transform entrance;

        [Tooltip("Oluşturulan müşterilerin konacağı boş nesne.")]
        [SerializeField] private Transform customerContainer;

        private LevelData level;
        private IShowcaseStock stock;
        private PrepCounter prepCounter;
        private Func<ProductData, bool> canOrderCake;
        private float elapsed;
        private float timeUntilNext;
        private int nextCustomerId = 1;

        public bool IsSpawning { get; private set; }

        /// <summary>
        /// Önceki round'dan kalan müşterileri siler ve üretimi başlatır; ilk müşteri hemen denenir.
        /// showcaseStock: pasta stoğu (null ise pasta seçimi stoğa bakmaz). counter: tepsideki dilimler için (null olabilir).
        /// </summary>
        public void StartSpawning(LevelData levelData, IShowcaseStock showcaseStock, PrepCounter counter)
        {
            if (showcaseStock == null)
            {
                Debug.LogWarning("[CustomerSpawner] Vitrin stoğu verilmedi; müşteriler stoğa bakmadan pasta ister.", this);
            }

            stock = showcaseStock;
            prepCounter = counter;
            canOrderCake ??= HasFreeSlice;

            if (levelData == null || settings == null || tableManager == null || customerPrefab == null || entrance == null)
            {
                Debug.LogError("[CustomerSpawner] LevelData, SalonSettings, TableManager, Customer prefab'ı veya giriş atanmamış.", this);
                return;
            }

            if (settings.customerTypes.Count == 0)
            {
                Debug.LogError("[CustomerSpawner] SalonSettings.customerTypes boş.", this);
                return;
            }

            ClearCustomers();
            level = levelData;
            elapsed = 0f;
            timeUntilNext = 0f;
            IsSpawning = true;
        }

        public void StopSpawning() => IsSpawning = false;

        private void Update()
        {
            if (!IsSpawning)
            {
                return;
            }

            elapsed += Time.deltaTime;
            timeUntilNext -= Time.deltaTime;
            if (timeUntilNext > 0f)
            {
                return;
            }

            TrySpawn();
            timeUntilNext += CurrentInterval();
        }

        /// <summary>Round ilerlemesine göre eğriden okunan aralık; ayarlardaki en kısa süreden aşağı inmez.</summary>
        private float CurrentInterval()
        {
            float progress = level.roundDurationSeconds > 0f ? Mathf.Clamp01(elapsed / level.roundDurationSeconds) : 1f;
            float interval = level.spawnIntervalCurve != null ? level.spawnIntervalCurve.Evaluate(progress) : 0f;
            return Mathf.Max(settings.minSpawnIntervalSeconds, interval);
        }

        private void ClearCustomers()
        {
            if (customerContainer == null)
            {
                return;
            }

            for (int i = customerContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(customerContainer.GetChild(i).gameObject);
            }
        }

        private void TrySpawn()
        {
            // Boş masa yoksa bu deneme sessizce atlanır.
            if (!tableManager.TryGetRandomFreeTable(out Table table))
            {
                return;
            }

            CustomerTypeData type = PickCustomerType();
            if (type == null)
            {
                return;
            }

            Vector3 entrancePosition = entrance.position;
            Customer customer = Instantiate(customerPrefab, entrancePosition, Quaternion.identity, customerContainer);

            // Masa aynı karede ayrılır; böylece iki müşteri aynı masayı seçemez.
            if (!table.Reserve(customer))
            {
                Destroy(customer.gameObject);
                return;
            }

            customer.Init(nextCustomerId++, type, table, CustomerOrder.Create(level, stock != null ? canOrderCake : null),
                entrancePosition, settings);
        }

        // Pastanın ayrılmamış (serbest) dilimi var mı?
        private bool HasFreeSlice(ProductData cake)
        {
            int slices = stock.GetSlices(cake);
            if (slices <= 0)
            {
                return false;
            }

            int onTrays = prepCounter != null ? prepCounter.CountSlices(cake) : 0;
            int reserved = Mathf.Max(0, CountPendingOrders(cake) - onTrays);
            return slices - reserved > 0;
        }

        // Bu pastayı isteyip henüz teslim almamış (yürüyen veya oturan) müşteri sayısı.
        // Yeni müşteri Instantiate edildiği anda henüz Init almadığı için kendi siparişi sayılmaz.
        private int CountPendingOrders(ProductData cake)
        {
            if (customerContainer == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < customerContainer.childCount; i++)
            {
                Customer customer = customerContainer.GetChild(i).GetComponent<Customer>();
                if (customer == null || customer.Order == null || customer.Order.Cake != cake)
                {
                    continue;
                }

                if (customer.State == CustomerState.WalkingIn || customer.State == CustomerState.Seated)
                {
                    count++;
                }
            }

            return count;
        }

        private CustomerTypeData PickCustomerType()
        {
            CustomerTypeData type = settings.customerTypes[Random.Range(0, settings.customerTypes.Count)];
            if (type == null)
            {
                Debug.LogWarning("[CustomerSpawner] SalonSettings.customerTypes içinde boş eleman var.", this);
            }

            return type;
        }
    }
}
