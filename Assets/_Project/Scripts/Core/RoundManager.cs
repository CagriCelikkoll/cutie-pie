using System;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Round süresini, kaçırmaları, komboyu ve round toplamlarını yönetir; RoundStarted ve RoundEnded yayınlar.
    /// Ödeme ve bahşişi EconomyManager ekler; burada yalnızca round sonunda kombo farkı eklenir.
    /// Singleton değildir: Salon sahnesindeki prefab'a Inspector referansıyla erişilir.
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        [SerializeField] private ComboSettings comboSettings;

        [Tooltip("Açıksa sahne başında SaveSystem'deki mevcut seviyeyle round başlar.")]
        [SerializeField] private bool autoStart = true;

        private int _paymentTotal;
        private int _tipsTotal;
        private int _comboBonusTotal;
        private int _highestStreak;
        private int _misses;

        /// <summary>Round sürüyor mu.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Son başlatılan round'un seviyesi; henüz round başlamadıysa null.</summary>
        public LevelData CurrentLevel { get; private set; }

        /// <summary>Kalan süre (saniye).</summary>
        public float RemainingSeconds { get; private set; }

        /// <summary>Şu anki art arda başarılı teslim sayısı.</summary>
        public int CurrentStreak { get; private set; }

        /// <summary>Şu anki seriye karşılık gelen kombo çarpanı.</summary>
        public float CurrentMultiplier => comboSettings.GetMultiplier(CurrentStreak);

        private void OnEnable()
        {
            GameEvents.CustomerServed += HandleCustomerServed;
            GameEvents.CustomerLeftAngry += HandleCustomerLeftAngry;
            GameEvents.WrongOrderDelivered += HandleWrongOrderDelivered;
        }

        private void OnDisable()
        {
            GameEvents.CustomerServed -= HandleCustomerServed;
            GameEvents.CustomerLeftAngry -= HandleCustomerLeftAngry;
            GameEvents.WrongOrderDelivered -= HandleWrongOrderDelivered;
        }

        // OnEnable yerine Start: sahnedeki dinleyiciler RoundStarted'ı kaçırmasın.
        private void Start()
        {
            if (autoStart)
            {
                StartRound();
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            if (RemainingSeconds <= 0f)
            {
                EndRound(true);
            }
        }

        /// <summary>SaveSystem'deki mevcut seviyeyle round başlatır ("Tekrar oyna" bunu kullanır).</summary>
        public void StartRound()
        {
            if (SaveSystem.Instance == null)
            {
                Debug.LogError("[RoundManager] SaveSystem yok, mevcut seviye alınamadı.");
                return;
            }

            StartRound(SaveSystem.Instance.CurrentLevel);
        }

        /// <summary>Belirli bir seviyeyle round başlatır.</summary>
        public void StartRound(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("[RoundManager] Seviye verisi boş, round başlatılamadı.");
                return;
            }

            if (IsRunning)
            {
                Debug.LogWarning("[RoundManager] Round zaten sürüyor, yeni başlatma yok sayıldı.");
                return;
            }

            CurrentLevel = level;
            RemainingSeconds = level.roundDurationSeconds;
            CurrentStreak = 0;
            _paymentTotal = 0;
            _tipsTotal = 0;
            _comboBonusTotal = 0;
            _highestStreak = 0;
            _misses = 0;
            IsRunning = true;

            GameEvents.RaiseRoundStarted(level);
        }

        private void HandleCustomerServed(CustomerInfo customer, int payment, int tip)
        {
            if (!IsRunning)
            {
                return;
            }

            CurrentStreak++;
            _highestStreak = Mathf.Max(_highestStreak, CurrentStreak);
            _paymentTotal += payment;
            _tipsTotal += tip;
            _comboBonusTotal += CalculateComboBonus(payment, CurrentMultiplier);
        }

        private void HandleCustomerLeftAngry(CustomerInfo customer)
        {
            if (!IsRunning)
            {
                return;
            }

            CurrentStreak = 0;
            _misses++;
            if (_misses >= CurrentLevel.maxMisses)
            {
                EndRound(false);
            }
        }

        private void HandleWrongOrderDelivered(CustomerInfo customer)
        {
            if (IsRunning)
            {
                CurrentStreak = 0;
            }
        }

        private void EndRound(bool completed)
        {
            IsRunning = false;

            // RoundEnded'da SaveSystem kayıt yapar; kaydın kombo farkını içermesi için önce eklenir.
            AddComboBonusToEconomy();

            int goldEarned = _paymentTotal + _tipsTotal + _comboBonusTotal;
            GameEvents.RaiseRoundEnded(new RoundResult
            {
                goldEarned = goldEarned,
                tipsEarned = _tipsTotal,
                comboBonusEarned = _comboBonusTotal,
                highestCombo = _highestStreak,
                stars = CalculateStars(CurrentLevel, goldEarned),
                misses = _misses,
                completed = completed
            });
        }

        private void AddComboBonusToEconomy()
        {
            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[RoundManager] EconomyManager yok, kombo farkı eklenemedi.");
                return;
            }

            EconomyManager.Instance.Add(CurrencyType.Gold, _comboBonusTotal);
        }

        // Yarım değerler yukarı yuvarlanır; Mathf.RoundToInt 2,5'i 2'ye, 3,5'i 4'e yuvarlardı.
        private static int CalculateComboBonus(int payment, float multiplier) =>
            (int)Math.Round(payment * (multiplier - 1f), MidpointRounding.AwayFromZero);

        private static int CalculateStars(LevelData level, int goldEarned)
        {
            int stars = 0;
            foreach (int threshold in level.starGoldThresholds)
            {
                if (goldEarned < threshold)
                {
                    break;
                }

                stars++;
            }

            return stars;
        }
    }
}
