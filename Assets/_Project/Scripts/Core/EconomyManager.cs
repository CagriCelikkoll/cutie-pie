using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Altın, elmas ve pastane puanını tutar. Her değişimde CurrencyChanged yayınlar.
    /// CustomerServed'ı dinleyip ödeme + bahşişi altına ekler; kombo farkını RoundManager ayrıca ekler.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private GameConfig config;

        // CurrencyType değerleriyle indekslenir.
        private int[] _amounts;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _amounts = new int[System.Enum.GetValues(typeof(CurrencyType)).Length];
            _amounts[(int)CurrencyType.Gold] = config.startingGold;
            _amounts[(int)CurrencyType.Gem] = config.startingGems;
            _amounts[(int)CurrencyType.ShopRating] = config.startingShopRating;
        }

        private void OnEnable() => GameEvents.CustomerServed += HandleCustomerServed;

        private void OnDisable() => GameEvents.CustomerServed -= HandleCustomerServed;

        /// <summary>Mevcut miktar.</summary>
        public int Get(CurrencyType currency) => _amounts[(int)currency];

        /// <summary>Miktar ekler (amount pozitif olmalı).</summary>
        public void Add(CurrencyType currency, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _amounts[(int)currency] += amount;
            GameEvents.RaiseCurrencyChanged(currency, _amounts[(int)currency]);
        }

        /// <summary>Yeterliyse harcar ve true döner; yetmiyorsa hiçbir şey değiştirmez.</summary>
        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (amount < 0 || _amounts[(int)currency] < amount)
            {
                return false;
            }

            _amounts[(int)currency] -= amount;
            GameEvents.RaiseCurrencyChanged(currency, _amounts[(int)currency]);
            return true;
        }

        /// <summary>Kayıttan yükleme gibi durumlarda miktarı doğrudan ayarlar.</summary>
        public void Set(CurrencyType currency, int amount)
        {
            _amounts[(int)currency] = Mathf.Max(0, amount);
            GameEvents.RaiseCurrencyChanged(currency, _amounts[(int)currency]);
        }

        private void HandleCustomerServed(CustomerInfo customer, int payment, int tip)
        {
            Add(CurrencyType.Gold, payment + tip);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
