using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Sabit aralıkla müşteri üretir. Boş masa yoksa o deneme atlanır (kaçırma sayılmaz, olay yayınlanmaz).
    /// G4'te aralık LevelData.spawnIntervalCurve'den okunacak.
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
        private float timer;
        private int nextCustomerId = 1;

        public bool IsSpawning { get; private set; }

        /// <summary>Üretimi başlatır; ilk müşteri hemen denenir.</summary>
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

            level = levelData;
            timer = settings.spawnIntervalSeconds;
            IsSpawning = true;
        }

        public void StopSpawning() => IsSpawning = false;

        private void Update()
        {
            if (!IsSpawning)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer < settings.spawnIntervalSeconds)
            {
                return;
            }

            timer -= settings.spawnIntervalSeconds;
            TrySpawn();
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
