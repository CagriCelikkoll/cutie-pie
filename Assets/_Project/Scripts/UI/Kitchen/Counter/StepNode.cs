using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Adım çubuğundaki tek adım: numaralı daire, adım adı ve (yalnızca aktifken) alt yazı.
    /// Biten adım yeşil ✓, aktif adım pembe, diğerleri gri. Adım adının anahtarı prefab'da durur.
    /// </summary>
    public class StepNode : MonoBehaviour
    {
        public enum State
        {
            Pending,
            Active,
            Done
        }

        [SerializeField] private CounterSettings settings;
        [SerializeField] private Image circle;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private GameObject check;
        [SerializeField] private TMP_Text label;
        [SerializeField] private LocalizedText subText;

        public State CurrentState { get; private set; } = State.Pending;

        /// <summary>Durumu uygular; subKey yalnızca aktif adımda gösterilir (null/boş: alt yazı gizli).</summary>
        public void SetState(State state, string subKey = null, params object[] args)
        {
            CurrentState = state;
            if (settings == null)
            {
                return;
            }

            if (circle != null)
            {
                circle.color = state == State.Done ? settings.greenColor
                    : state == State.Active ? settings.pinkColor
                    : settings.grayColor;
            }

            KitchenUIUtil.SetActive(numberText, state != State.Done);
            KitchenUIUtil.SetActive(check, state == State.Done);

            if (numberText != null)
            {
                numberText.color = state == State.Pending ? settings.mutedTextColor : settings.buttonTextColor;
            }

            if (label != null)
            {
                label.color = state == State.Pending ? settings.mutedTextColor : settings.textColor;
            }

            bool showSub = state == State.Active && !string.IsNullOrEmpty(subKey);
            KitchenUIUtil.SetActive(subText, showSub);
            if (showSub)
            {
                KitchenUIUtil.SetKey(subText, subKey, args);
            }
        }
    }
}
