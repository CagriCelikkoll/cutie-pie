using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.Core.Shop
{
    /// <summary>
    /// Açılış ekranı (pastane). Kasa Salon'a, Mutfak Kitchen'a geçer; Vitrin şimdilik yalnızca "Yakında" mesajı gösterir.
    /// Geliştirmeler butonu sahnede doğrudan UpgradesPanel.Open()'a bağlıdır (kod bağımlılığı yok).
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        [SerializeField] private Button cashierButton;
        [SerializeField] private Button showcaseButton;
        [SerializeField] private Button kitchenButton;

        [Tooltip("\"Yakında\" mesaj paneli; metni LocalizedText'ten gelir.")]
        [SerializeField] private GameObject comingSoonPanel;

        private Coroutine _hideRoutine;

        private void Awake()
        {
            comingSoonPanel.SetActive(false);
            cashierButton.onClick.AddListener(HandleCashier);
            showcaseButton.onClick.AddListener(ShowComingSoon);
            kitchenButton.onClick.AddListener(HandleKitchen);
        }

        private void OnDestroy()
        {
            cashierButton.onClick.RemoveListener(HandleCashier);
            showcaseButton.onClick.RemoveListener(ShowComingSoon);
            kitchenButton.onClick.RemoveListener(HandleKitchen);
        }

        private void HandleCashier()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToSalon();
            }
        }

        private void HandleKitchen()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToKitchen();
            }
        }

        private void ShowComingSoon()
        {
            // Art arda basılırsa süre baştan başlar.
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            comingSoonPanel.SetActive(true);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            float duration = GameManager.Instance != null ? GameManager.Instance.Config.comingSoonDurationSeconds : 0f;
            yield return new WaitForSecondsRealtime(duration);
            comingSoonPanel.SetActive(false);
            _hideRoutine = null;
        }
    }
}
