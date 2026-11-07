using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// GEÇİCİ test başlatıcısı (G4'e kadar). Salon açılınca seviyeyi Inspector'daki LevelData ile kurar.
    /// G4'te GameEvents.RoundStarted'a bağlanınca kaldırılacak veya autoStart kapatılacak.
    /// </summary>
    public class SalonBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private TableManager tableManager;

        [Tooltip("Açıksa Start'ta seviye kendiliğinden kurulur.")]
        [SerializeField] private bool autoStart = true;

        private void Start()
        {
            if (!autoStart)
            {
                return;
            }

            if (level == null || tableManager == null)
            {
                Debug.LogError("[SalonBootstrap] LevelData veya TableManager atanmamış.", this);
                return;
            }

            tableManager.Setup(level.openTableCount);
        }
    }
}
