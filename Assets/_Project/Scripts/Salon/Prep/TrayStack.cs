using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Tepsi yığını (servis adımı 1). Tezgâhta boş yer varsa sürüklenince yeni tepsi verir;
    /// tepsi tezgâha oturmazsa yığına dönüp yok olur. Yığın sınırsızdır.
    /// TryPlaceTray sürüklemesiz yoldur; ileride "Hazır tepsi istasyonu" geliştirmesi bunu çağırır.
    /// </summary>
    [RequireComponent(typeof(DragItem))]
    public class TrayStack : MonoBehaviour, IDragSource
    {
        [SerializeField] private SalonSettings settings;
        [SerializeField] private Tray trayPrefab;

        [Tooltip("Sürüklenen tepsinin gideceği tezgâh; doluyken yığından tepsi çıkmaz.")]
        [SerializeField] private PrepCounter counter;

        /// <summary>Sürüklemeden, doğrudan tezgâha tepsi koyar. Yer yoksa false.</summary>
        public bool TryPlaceTray(PrepCounter target)
        {
            if (target == null || trayPrefab == null || !target.HasFreeSlot)
            {
                return false;
            }

            Tray tray = SpawnTray();
            if (!target.CanAccept(tray.Item))
            {
                Destroy(tray.gameObject);
                return false;
            }

            target.Accept(tray.Item);
            return true;
        }

        public ServingItem BeginDrag()
        {
            if (counter == null || trayPrefab == null || !counter.HasFreeSlot)
            {
                return null;
            }

            return SpawnTray().Item;
        }

        public void OnDropped(ServingItem item, IDropTarget target)
        {
            // Tepsinin sahipliği artık tezgâhta; yapılacak bir şey yok.
        }

        public void OnReturned(ServingItem item)
        {
            // Iskalanan tepsi yığına geri girer.
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        private Tray SpawnTray()
        {
            Tray tray = Instantiate(trayPrefab, transform.position, Quaternion.identity, transform);
            tray.Item.Setup(null, settings);
            return tray;
        }
    }
}
