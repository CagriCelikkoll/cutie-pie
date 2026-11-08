using System.Collections.Generic;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Hazırlama alanındaki bırakma hedeflerinin listesi (PrepArea üzerinde tek bileşen).
    /// Hedef seçimi collider'a çarpmaya değil mesafeye bakar: hedefler görselinden büyük "hissedilir",
    /// küçük çocukların tam isabet tutturması gerekmez.
    /// </summary>
    public class DropTargetRegistry : MonoBehaviour
    {
        private readonly List<IDropTarget> targets = new List<IDropTarget>();

        public void Register(IDropTarget target)
        {
            if (target != null && !targets.Contains(target))
            {
                targets.Add(target);
            }
        }

        public void Unregister(IDropTarget target)
        {
            targets.Remove(target);
        }

        /// <summary>
        /// Parçayı kabul eden ve SnapPoint'i maxDistance içinde olan en yakın hedefi döner; yoksa null.
        /// </summary>
        public IDropTarget FindBestTarget(ServingItem item, Vector3 dropPosition, float maxDistance)
        {
            IDropTarget best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < targets.Count; i++)
            {
                IDropTarget target = targets[i];
                if (target == null || !target.CanAccept(item))
                {
                    continue;
                }

                Vector2 delta = target.SnapPoint - dropPosition; // z farkı yok sayılır
                float sqr = delta.sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = target;
                }
            }

            return best;
        }
    }
}
