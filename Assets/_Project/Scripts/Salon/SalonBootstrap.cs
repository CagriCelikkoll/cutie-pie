using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// GEÇİCİ test başlatıcısı (G4'e kadar). Salon açılınca seviyeyi Inspector'daki LevelData ile kurar
    /// ve müşteri üretimini başlatır. G4'te GameEvents.RoundStarted'a bağlanınca kaldırılacak veya autoStart kapatılacak.
    /// </summary>
    public class SalonBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private TableManager tableManager;
        [SerializeField] private CustomerSpawner customerSpawner;

        [Header("Hazırlama alanı")]
        [SerializeField] private LocalShowcaseStock showcaseStock;
        [SerializeField] private ProductShelf cakeShelf;
        [SerializeField] private ProductShelf drinkShelf;

        [Tooltip("Açıksa Start'ta seviye kendiliğinden kurulur.")]
        [SerializeField] private bool autoStart = true;

        private void Start()
        {
            if (!autoStart)
            {
                return;
            }

            if (level == null || tableManager == null || customerSpawner == null)
            {
                Debug.LogError("[SalonBootstrap] LevelData, TableManager veya CustomerSpawner atanmamış.", this);
                return;
            }

            tableManager.Setup(level.openTableCount);
            SetupPrepArea();
            customerSpawner.StartSpawning(level);
        }

        // Vitrin stoğu ve raflar round başında menüye göre kurulur. G4'te GameEvents.RoundStarted'a taşınacak.
        private void SetupPrepArea()
        {
            if (showcaseStock == null || cakeShelf == null || drinkShelf == null)
            {
                Debug.LogWarning("[SalonBootstrap] Vitrin stoğu veya raflar atanmamış; builder 4. adımı çalıştırın.", this);
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

#if UNITY_EDITOR
        // GEÇİCİ test dinleyicisi: müşteri olaylarının doğru bilgiyle geldiğini Console'da gösterir.
        private void OnEnable()
        {
            GameEvents.CustomerSeated += LogSeated;
            GameEvents.CustomerLeftAngry += LogLeftAngry;
        }

        private void OnDisable()
        {
            GameEvents.CustomerSeated -= LogSeated;
            GameEvents.CustomerLeftAngry -= LogLeftAngry;
        }

        private static void LogSeated(CustomerInfo info) =>
            Debug.Log($"[SalonTest] CustomerSeated → id={info.CustomerId}, table={info.TableIndex}, type={(info.Type != null ? info.Type.id : "null")}");

        private static void LogLeftAngry(CustomerInfo info) =>
            Debug.Log($"[SalonTest] CustomerLeftAngry → id={info.CustomerId}, table={info.TableIndex}");
#endif
    }
}
