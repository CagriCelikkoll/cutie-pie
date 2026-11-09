using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'u RoundManager'ın round olaylarına bağlar ve kurulum sırasını tek yerde tutar.
    /// RoundStarted: masalar → vitrin stoğu → raflar → girdi açılır → müşteri üretimi başlar.
    /// RoundEnded: üretim durur, girdi kilitlenir (müşteriler kendileri donar). Sonuç ekranı G6'da.
    /// </summary>
    public class SalonRound : MonoBehaviour
    {
        [SerializeField] private TableManager tableManager;
        [SerializeField] private CustomerSpawner customerSpawner;

        [Header("Hazırlama alanı")]
        [SerializeField] private LocalShowcaseStock showcaseStock;
        [SerializeField] private ProductShelf cakeShelf;
        [SerializeField] private ProductShelf drinkShelf;

        // RoundManager.Start'ta yayınlar; OnEnable'da abone olunduğu için ilk round kaçmaz.
        private void OnEnable()
        {
            GameEvents.RoundStarted += HandleRoundStarted;
            GameEvents.RoundEnded += HandleRoundEnded;
        }

        private void OnDisable()
        {
            GameEvents.RoundStarted -= HandleRoundStarted;
            GameEvents.RoundEnded -= HandleRoundEnded;
        }

        private void HandleRoundStarted(LevelData level)
        {
            if (level == null || tableManager == null || customerSpawner == null)
            {
                Debug.LogError("[SalonRound] LevelData, TableManager veya CustomerSpawner yok.", this);
                return;
            }

            tableManager.Setup(level.openTableCount);
            SetupPrepArea(level);
            SalonInput.Unlock();
            customerSpawner.StartSpawning(level);
        }

        private void HandleRoundEnded(RoundResult result)
        {
            if (customerSpawner != null)
            {
                customerSpawner.StopSpawning();
            }

            SalonInput.Lock();
        }

        // Vitrin stoğu önce dolar; pasta rafı ürünlerini ve dilim sayılarını ondan okur.
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
