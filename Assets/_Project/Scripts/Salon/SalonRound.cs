using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Upgrades;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'u RoundManager'ın round olaylarına bağlar ve kurulum sırasını tek yerde tutar.
    /// RoundStarted: masalar (LevelData + Ek Masa seviyesi) → tezgâh ve fiş temizliği → vitrin stoğu → raflar
    /// → hazır tepsi istasyonu → girdi açılır → müşteri üretimi başlar.
    /// RoundEnded: üretim ve istasyon durur, girdi kilitlenir (müşteriler kendileri donar). Sonuç ekranı: UI/RoundEndPanel.
    /// Round sonunda alınan geliştirmeler "Tekrar oyna" ile gelen RoundStarted'ta uygulanır.
    /// G16: vitrin stoğu Mutfak ile ortaktır (SharedShowcaseStock). Round başında hiç pasta stoğu yoksa
    /// girdi kilitli kalır, SalonEvents.NoCakeStockAtRoundStart yayınlanır (UI/NoStockPanel açılır, zamanı durdurur)
    /// ve oyuncu "Yine de aç" diyene kadar müşteri gelmez; sonra round yalnızca içecek siparişleriyle oynanır.
    /// </summary>
    public class SalonRound : MonoBehaviour
    {
        [SerializeField] private TableManager tableManager;
        [SerializeField] private CustomerSpawner customerSpawner;

        [Header("Hazırlama alanı")]
        [Tooltip("Mutfak ile ortak pasta stoğu (GameManager.Instance.Stock adaptörü).")]
        [SerializeField] private SharedShowcaseStock showcaseStock;
        [SerializeField] private ProductShelf cakeShelf;
        [SerializeField] private ProductShelf drinkShelf;

        [Tooltip("Yeni round başında üstündeki tepsiler silinir.")]
        [SerializeField] private PrepCounter prepCounter;

        [Tooltip("Yeni round başında boşaltılır.")]
        [SerializeField] private OrderTicket orderTicket;

        [Tooltip("Hazır tepsi istasyonu (ready_tray geliştirmesi); tezgâh temizlendikten sonra başlatılır.")]
        [SerializeField] private ReadyTrayStation readyTrayStation;

        // Stok uyarısında oyuncunun seçimini bekleyen round'un seviyesi; beklenmiyorsa null.
        private LevelData waitingLevel;

        // RoundManager.Start'ta yayınlar; OnEnable'da abone olunduğu için ilk round kaçmaz.
        private void OnEnable()
        {
            GameEvents.RoundStarted += HandleRoundStarted;
            GameEvents.RoundEnded += HandleRoundEnded;
            SalonEvents.NoStockContinueRequested += HandleNoStockContinue;
        }

        private void OnDisable()
        {
            GameEvents.RoundStarted -= HandleRoundStarted;
            GameEvents.RoundEnded -= HandleRoundEnded;
            SalonEvents.NoStockContinueRequested -= HandleNoStockContinue;
        }

        private void HandleRoundStarted(LevelData level)
        {
            if (level == null || tableManager == null || customerSpawner == null)
            {
                Debug.LogError("[SalonRound] LevelData, TableManager veya CustomerSpawner yok.", this);
                return;
            }

            waitingLevel = null;
            tableManager.Setup(GetOpenTableCount(level));
            ResetCounterAndTicket();
            SetupPrepArea(level);
            StartReadyTrayStation();

            if (showcaseStock != null && !showcaseStock.HasAnyCake)
            {
                SalonInput.Lock();
                waitingLevel = level;
                if (SalonEvents.RaiseNoCakeStockAtRoundStart())
                {
                    // Oyuncu seçene kadar beklenir (HandleNoStockContinue veya sahne değişimi).
                    return;
                }

                waitingLevel = null;
                Debug.LogWarning("[SalonRound] Pasta stoğu yok ama stok uyarı paneli sahnede değil; round yalnızca içecekle başlıyor. " +
                                 "'Cutie Pie/UI/Stok Uyarı Panelini Kur' menüsünü çalıştırın.", this);
            }

            BeginPlay(level);
        }

        // Girdi açılır ve müşteri üretimi başlar (G17 sırası: hazır tepsi istasyonundan sonra).
        private void BeginPlay(LevelData level)
        {
            SalonInput.Unlock();
            customerSpawner.StartSpawning(level, showcaseStock, prepCounter);
        }

        private void HandleNoStockContinue()
        {
            if (waitingLevel == null)
            {
                return;
            }

            LevelData level = waitingLevel;
            waitingLevel = null;
            BeginPlay(level);
        }

        private void HandleRoundEnded(RoundResult result)
        {
            waitingLevel = null;

            if (customerSpawner != null)
            {
                customerSpawner.StopSpawning();
            }

            if (readyTrayStation != null)
            {
                readyTrayStation.StopRound();
            }

            SalonInput.Lock();
        }

        // Açık masa = seviyenin masa sayısı + Ek Masa geliştirme seviyesi; masa yeri sayısını (8) aşamaz.
        private int GetOpenTableCount(LevelData level)
        {
            UpgradeManager upgrades = GameManager.Instance != null ? GameManager.Instance.Upgrades : null;
            int extraTables = upgrades != null ? upgrades.GetLevel(UpgradeIds.ExtraTable) : 0;
            return Mathf.Min(level.openTableCount + extraTables, tableManager.Tables.Count);
        }

        private void StartReadyTrayStation()
        {
            if (readyTrayStation == null)
            {
                Debug.LogWarning("[SalonRound] ReadyTrayStation atanmamış; hazır tepsi geliştirmesi çalışmaz. Builder 7. adımı yeniden çalıştırın.", this);
                return;
            }

            readyTrayStation.StartRound();
        }

        // Önceki round'dan tezgâhta kalan tepsi ve açık fiş temizlenir ("Tekrar oyna").
        private void ResetCounterAndTicket()
        {
            if (prepCounter == null || orderTicket == null)
            {
                Debug.LogWarning("[SalonRound] PrepCounter veya OrderTicket atanmamış; builder 7. adımı yeniden çalıştırın.", this);
            }

            if (prepCounter != null)
            {
                prepCounter.ClearAll();
            }

            if (orderTicket != null)
            {
                orderTicket.Clear();
            }
        }

        // Vitrin stoğu önce menüyü bağlar; pasta rafı ürünlerini ve dilim sayılarını ondan okur.
        private void SetupPrepArea(LevelData level)
        {
            if (showcaseStock == null || cakeShelf == null || drinkShelf == null)
            {
                Debug.LogWarning("[SalonRound] Vitrin stoğu veya raflar atanmamış; builder 7. adımı çalıştırın.", this);
            }

            if (showcaseStock != null)
            {
                showcaseStock.Refill(level);
            }

            if (cakeShelf != null)
            {
                cakeShelf.Setup(level, showcaseStock);
            }

            if (drinkShelf != null)
            {
                drinkShelf.Setup(level, null);
            }
        }
    }
}
