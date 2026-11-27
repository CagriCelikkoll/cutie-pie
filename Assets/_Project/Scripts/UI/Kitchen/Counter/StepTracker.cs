using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Tezgâhın üstündeki 5 adımlık çubuk (Malzemeler, Karıştır, Fırına koy, Süsle, Dilimle).
    /// Aktif adımdan öncekiler biter (yeşil ✓), aktif adım pembe ve alt yazılıdır (ör. "3 / 6 malzeme"), sonrakiler gri.
    /// Aralarındaki çizgiler biten adımlara kadar yeşil, sonrası gri.
    /// </summary>
    public class StepTracker : MonoBehaviour
    {
        [SerializeField] private CounterSettings settings;

        [Tooltip("Soldan sağa adımlar.")]
        [SerializeField] private StepNode[] nodes;

        [Tooltip("Adımlar arasındaki çizgiler (connectors[i]: nodes[i] ile nodes[i+1] arası).")]
        [SerializeField] private Image[] connectors;

        public int StepCount => nodes != null ? nodes.Length : 0;

        /// <summary>activeIndex: 0 tabanlı aktif adım. subKey ve args aktif adımın alt yazısıdır.</summary>
        public void Show(int activeIndex, string subKey = null, params object[] args)
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i] == null)
                    {
                        continue;
                    }

                    if (i < activeIndex)
                    {
                        nodes[i].SetState(StepNode.State.Done);
                    }
                    else if (i == activeIndex)
                    {
                        nodes[i].SetState(StepNode.State.Active, subKey, args);
                    }
                    else
                    {
                        nodes[i].SetState(StepNode.State.Pending);
                    }
                }
            }

            if (connectors != null && settings != null)
            {
                for (int i = 0; i < connectors.Length; i++)
                {
                    if (connectors[i] != null)
                    {
                        connectors[i].color = i < activeIndex ? settings.greenColor : settings.grayColor;
                    }
                }
            }
        }
    }
}
