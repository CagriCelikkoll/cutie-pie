using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.UI
{
    /// <summary>
    /// CakeView'daki tek süs görselinin dokunma alanı. Dokunulunca CakeView'a CakeBuild.decorations içindeki sırasını bildirir.
    /// Yalnızca CakeView düzenlenebilirken (görselin raycastTarget'ı açıkken) dokunma alır.
    /// </summary>
    public class CakeDecorationHandle : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Bu görselin gösterdiği süsün CakeBuild.decorations içindeki sırası.</summary>
        public int DecorationIndex { get; set; } = -1;

        /// <summary>Süse dokunuldu (sürükleme değil, kısa dokunma).</summary>
        public event Action<CakeDecorationHandle> Tapped;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                Tapped?.Invoke(this);
            }
        }
    }
}
