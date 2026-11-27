using CutiePie.Core;
using CutiePie.UI;
using CutiePie.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Mutfak sahnesinin ana ekranı: "Pastaneye dön" butonu ve paneller arası bağlantı.
    /// Fırın paneli (G19), sipariş panosu (G20) ve geliştirme mağazası kendi Canvas'ı olan prefab örnekleri olarak sahnenin
    /// kökündedir; KitchenCanvas'taki OvenPanelSlot / OrderBoardSlot yalnızca yerleşim kılavuzudur (görüntüleri kapalı).
    /// Kilitli fırında "Geliştirmeyle aç" (OvenPanel.UnlockRequested) geliştirme mağazasını Ek Fırın kartına odaklanarak açar
    /// (UpgradesPanel.Open(UpgradeIds.ExtraOven)).
    /// UnlockRequested düz bir C# olayı olduğu için bağlantı Inspector'dan değil buradan kurulur.
    /// </summary>
    public class KitchenScreen : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        [Tooltip("Sahnedeki fırın paneli örneği.")]
        [SerializeField] private OvenPanel ovenPanel;

        [Tooltip("Sahnedeki geliştirme mağazası örneği; kilitli fırına dokununca açılır.")]
        [SerializeField] private UpgradesPanel upgradesPanel;

        private void Awake() => backButton.onClick.AddListener(HandleBack);

        private void OnDestroy() => backButton.onClick.RemoveListener(HandleBack);

        private void OnEnable()
        {
            if (ovenPanel == null || upgradesPanel == null)
            {
                Debug.LogWarning("[KitchenScreen] OvenPanel veya UpgradesPanel atanmamış; kilitli fırın geliştirmeleri açamaz.", this);
                return;
            }

            // Çift abonelik olmasın diye önce çıkarılır (OnEnable üst üste çağrılsa da tek abonelik kalır).
            ovenPanel.UnlockRequested -= HandleUnlockRequested;
            ovenPanel.UnlockRequested += HandleUnlockRequested;
        }

        private void OnDisable()
        {
            if (ovenPanel != null)
            {
                ovenPanel.UnlockRequested -= HandleUnlockRequested;
            }
        }

        private void HandleUnlockRequested()
        {
            if (upgradesPanel != null)
            {
                // Mağaza Ek Fırın kartına kaydırılıp vurgulanarak açılır.
                upgradesPanel.Open(UpgradeIds.ExtraOven);
            }
        }

        private void HandleBack()
        {
            // GameManager Boot'ta yaşar; sahnede doğrudan bağlanamadığı için buradan çağrılır.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToShop();
            }
        }
    }
}
