using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.Core.Shop
{
    /// <summary>
    /// Açılış ekranı (pastane). Kasa Salon'a geçer; Vitrin ve Mutfak şimdilik yalnızca "Yakında" mesajı gösterir.
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
            kitchenButton.onClick.AddListener(ShowComingSoon);
        }

        private void OnDestroy()
        {
            cashierButton.onClick.RemoveListener(HandleCashier);
            showcaseButton.onClick.RemoveListener(ShowComingSoon);
            kitchenButton.onClick.RemoveListener(ShowComingSoon);
        }

        private void HandleCashier()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToSalon();
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
