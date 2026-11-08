using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.Salon
{
    /// <summary>Raf sayfa oku: dokununca rafı bir sayfa ileri veya geri alır. Sürükleme sürerken yok sayılır.</summary>
    public class PageArrow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ProductShelf shelf;

        [Tooltip("+1: sonraki sayfa, -1: önceki sayfa.")]
        [SerializeField] private int direction = 1;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (shelf == null || DragItem.IsAnyDragActive)
            {
                return;
            }

            if (direction >= 0)
            {
                shelf.NextPage();
            }
            else
            {
                shelf.PrevPage();
            }
        }
    }
}
