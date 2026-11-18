using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.UI
{
    /// <summary>
    /// Seçenek kartından süs sürükleme (uGUI EventSystem; Input System paketi gerekmez). Aynı karttaki PartOption'ın parçasını taşır.
    /// Kendisi hiçbir şey yerleştirmez: sürükleme olaylarını yayınlar, CakeBuilder sürüklenen görseli yürütür ve bırakılan yeri
    /// pasta koordinatına çevirir (Salon'daki DragItem/IDropTarget fikrinin UI karşılığı; hedef tek: pasta yüzeyi).
    /// Kart kullanılamazken (süs sınırı doldu) sürükleme başlamaz.
    /// </summary>
    [RequireComponent(typeof(PartOption))]
    public class DecorationDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PartOption option;

        /// <summary>Sürükleme başladı (parça bu kartın parçasıdır).</summary>
        public event Action<DecorationDragSource, PointerEventData> DragStarted;

        public event Action<DecorationDragSource, PointerEventData> DragMoved;

        /// <summary>Parmak/fare bırakıldı.</summary>
        public event Action<DecorationDragSource, PointerEventData> DragEnded;

        public PartOption Option => option;

        private void Awake() => option = GetComponent<PartOption>();

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && option != null && option.Part != null &&
                option.IsAvailable)
            {
                DragStarted?.Invoke(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData) => DragMoved?.Invoke(this, eventData);

        public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke(this, eventData);
    }
}
