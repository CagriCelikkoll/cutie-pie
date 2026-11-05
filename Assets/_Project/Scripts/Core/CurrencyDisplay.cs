using TMPro;
using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Seçilen para türünün miktarını yalnızca sayı olarak gösterir. Sabit metin içermez.
    /// </summary>
    public class CurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private CurrencyType currency = CurrencyType.Gold;
        [SerializeField] private TMP_Text amountLabel;

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += HandleCurrencyChanged;
            Refresh();
        }

        private void OnDisable() => GameEvents.CurrencyChanged -= HandleCurrencyChanged;

        private void Refresh()
        {
            if (EconomyManager.Instance != null)
            {
                Show(EconomyManager.Instance.Get(currency));
            }
        }

        private void HandleCurrencyChanged(CurrencyType changed, int newAmount)
        {
            if (changed == currency)
            {
                Show(newAmount);
            }
        }

        private void Show(int amount) => amountLabel.SetText("{0}", amount);
    }
}
