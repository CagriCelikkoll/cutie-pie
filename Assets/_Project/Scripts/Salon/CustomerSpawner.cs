using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Round boyunca müşteri üretir. İki deneme arası süre LevelData.spawnIntervalCurve'den okunur
    /// (X: round ilerlemesi 0-1, Y: saniye). Boş masa yoksa o deneme atlanır (kaçırma sayılmaz, olay yayınlanmaz).
    /// SalonRound, RoundStarted'da StartSpawning, RoundEnded'da StopSpawning çağırır.
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
        private float elapsed;
        private float timeUntilNext;
        private int nextCustomerId = 1;

        public bool IsSpawning { get; private set; }

        /// <summary>Önceki round'dan kalan müşterileri siler ve üretimi başlatır; ilk müşteri hemen denenir.</summary>
        public void StartSpawning(LevelData levelData)
        {
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

            customer.Init(nextCustomerId++, type, table, CustomerOrder.Create(level), entrancePosition, settings);
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
