using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Upgrades;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// "Hazır Tepsi İstasyonu" geliştirmesi (ready_tray): round sürerken tezgâhta boş yer varsa belli aralıkla
    /// tezgâha boş tabaklı bir tepsi koyar. Aralık geliştirmenin etki değeridir (saniye, GetEffect); seviye 0'da çalışmaz.
    /// Tezgâh tek tepsiliktir, bu yüzden aynı anda en fazla bir hazır tepsi olur.
    /// Sayaç yalnızca tepsi konabilirken ilerler; tezgâh doluyken sıfırlanır, yer açılınca baştan sayar.
    /// Round'u SalonRound başlatır/durdurur (kurulum sırası tek yerde); round dışında ve girdi kilitliyken tepsi üretilmez.
    /// </summary>
    public class ReadyTrayStation : MonoBehaviour
    {
        [SerializeField] private SalonSettings settings;

        [Tooltip("Hazır tepsinin konacağı tezgâh.")]
        [SerializeField] private PrepCounter counter;

        [Tooltip("Tepsiyi üreten yığın (sürüklemesiz yol: TryPlaceTray).")]
        [SerializeField] private TrayStack trayStack;

        [Tooltip("Tabağı üreten yığın (sürüklemesiz yol: TryPlacePlate).")]
        [SerializeField] private PlateStack plateStack;

        private bool running;
        private bool hasUpgrade;
        private float intervalSeconds;
        private float elapsed;

        /// <summary>Geliştirme alınmış ve round sürüyor.</summary>
        public bool IsActive => running && hasUpgrade;

        private void OnEnable()
        {
            GameEvents.UpgradePurchased += HandleUpgradePurchased;
        }

        private void OnDisable()
        {
            GameEvents.UpgradePurchased -= HandleUpgradePurchased;
        }

        /// <summary>SalonRound round başında, tezgâh temizlendikten sonra çağırır: seviyeyi okur, sayacı sıfırlar.</summary>
        public void StartRound()
        {
            if (settings == null || counter == null || trayStack == null || plateStack == null)
            {
                Debug.LogWarning("[ReadyTrayStation] Referanslar eksik; builder 4. adımı yeniden çalıştırın.", this);
            }

            RefreshLevel();
            elapsed = 0f;
            running = true;
        }

        /// <summary>SalonRound round bitince çağırır.</summary>
        public void StopRound()
        {
            running = false;
            elapsed = 0f;
        }

        private void Update()
        {
            if (!IsActive || SalonInput.IsLocked || counter == null || trayStack == null)
            {
                return;
            }

            // Yer yokken sayaç ilerlemez; yer açılınca süre baştan sayılır.
            if (!counter.HasFreeSlot)
            {
                elapsed = 0f;
                return;
            }

            elapsed += Time.deltaTime;

            // Oyuncu bir parça sürüklerken (ör. yığından tepsi) yer elinden alınmasın; bırakınca konur.
            if (elapsed < intervalSeconds || DragItem.IsAnyDragActive)
            {
                return;
            }

            elapsed = 0f;
            PlaceReadyTray();
        }

        private void PlaceReadyTray()
        {
            if (!trayStack.TryPlaceTray(counter, out Tray tray))
            {
                return;
            }

            if (plateStack == null || !plateStack.TryPlacePlate(tray))
            {
                Debug.LogWarning("[ReadyTrayStation] Hazır tepsiye tabak konamadı; tepsi tabaksız kaldı.", this);
            }
        }

        private void HandleUpgradePurchased(UpgradeData upgrade, int newLevel)
        {
            if (upgrade != null && upgrade.id == UpgradeIds.ReadyTray)
            {
                RefreshLevel();
            }
        }

        // Seviye 0 ise istasyon kapalıdır; aralık geliştirmenin etki değerinden, en az SalonSettings'teki alt sınır.
        private void RefreshLevel()
        {
            hasUpgrade = false;

            UpgradeManager upgrades = GameManager.Instance != null ? GameManager.Instance.Upgrades : null;
            if (upgrades == null || settings == null || upgrades.GetLevel(UpgradeIds.ReadyTray) <= 0)
            {
                return;
            }

            if (!upgrades.TryGetEffect(UpgradeIds.ReadyTray, out float effectSeconds))
            {
                return;
            }

            intervalSeconds = Mathf.Max(effectSeconds, settings.readyTrayMinIntervalSeconds);
            hasUpgrade = true;
        }
    }
}
