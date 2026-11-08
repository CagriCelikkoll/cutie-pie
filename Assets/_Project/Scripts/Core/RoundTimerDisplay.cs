using TMPro;
using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>RoundManager'daki kalan süreyi yukarı yuvarlanmış tam saniye olarak gösterir.</summary>
    public class RoundTimerDisplay : MonoBehaviour
    {
        // Etiket yalnızca gösterilen saniye değiştiğinde yenilenir; ilk değerle mutlaka eşleşmesin diye -1.
        private const int NothingShown = -1;

        [SerializeField] private RoundManager roundManager;
        [SerializeField] private TMP_Text secondsLabel;

        private int _shownSeconds = NothingShown;

        private void Update()
        {
            // İlk round başlamadan sayaç boş kalır.
            if (roundManager.CurrentLevel == null)
            {
                return;
            }

            int seconds = Mathf.CeilToInt(roundManager.RemainingSeconds);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                secondsLabel.SetText("{0}", seconds);
            }
        }
    }
}
