using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Çöp kutusu: tezgâhtaki tepsi üzerine bırakılınca tepsinin yalnızca içeriği (tabak, dilim, bardak) silinir;
    /// boş tepsi tezgâhtaki yerine döner (Tray.OnDropped). Silinen dilim stoğa geri dönmez.
    /// </summary>
    public class TrashBin : MonoBehaviour, IDropTarget
    {
        [SerializeField] private DropTargetRegistry registry;

        public Vector3 SnapPoint => transform.position;

        private void OnEnable()
        {
            if (registry != null)
            {
                registry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }
        }

        public bool CanAccept(ServingItem item)
        {
            if (item == null || item.Kind != ServingItemKind.Tray)
            {
                return false;
            }

            Tray tray = item.GetComponent<Tray>();
            return tray != null && tray.Counter != null;
        }

        public void Accept(ServingItem item)
        {
            Tray tray = item.GetComponent<Tray>();
            if (tray != null)
            {
                tray.Clear();
            }
        }
    }
}
