using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Tabak yığını (servis adımı 2). Tezgâhta tabaksız bir tepsi varsa sürüklenince yeni tabak verir;
    /// tabak tepsiye oturmazsa yığına dönüp yok olur. Yığın sınırsızdır.
    /// TryPlacePlate sürüklemesiz yoldur; ileride "Hazır tepsi istasyonu" geliştirmesi bunu çağırır.
    /// </summary>
    [RequireComponent(typeof(DragItem))]
    public class PlateStack : MonoBehaviour, IDragSource
    {
        [SerializeField] private SalonSettings settings;
        [SerializeField] private ServingItem platePrefab;

        [Tooltip("Tabağın konacağı tepsiyi bulmak için tezgâh.")]
        [SerializeField] private PrepCounter counter;

        /// <summary>Sürüklemeden, doğrudan tepsiye tabak koyar. Tepside tabak varsa false.</summary>
        public bool TryPlacePlate(Tray tray)
        {
            if (tray == null || platePrefab == null || !tray.CanAcceptKind(ServingItemKind.Plate))
            {
                return false;
            }

            tray.Accept(SpawnPlate());
            return true;
        }

        public ServingItem BeginDrag()
        {
            Tray tray = counter != null ? counter.CurrentTray : null;
            if (tray == null || platePrefab == null || !tray.CanAcceptKind(ServingItemKind.Plate))
            {
                return null;
            }

            return SpawnPlate();
        }

        public void OnDropped(ServingItem item, IDropTarget target)
        {
            // Tabağın sahipliği artık tepside.
        }

        public void OnReturned(ServingItem item)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        private ServingItem SpawnPlate()
        {
            ServingItem plate = Instantiate(platePrefab, transform.position, Quaternion.identity, transform);
            plate.Setup(null, settings);
            return plate;
        }
    }
}
