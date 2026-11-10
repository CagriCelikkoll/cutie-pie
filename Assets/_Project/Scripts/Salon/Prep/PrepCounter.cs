using System;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Hazırlık tezgâhı: tepsi yerleri olan bırakma hedefi. Yer sayısı SalonSettings.prepTraySlotCount'tan
    /// (builder o kadar yer noktası üretir); geliştirmeyle değişmez.
    /// </summary>
    public class PrepCounter : MonoBehaviour, IDropTarget
    {
        [SerializeField] private DropTargetRegistry registry;

        [Tooltip("Tepsi yerleri (sırayla doldurulur).")]
        [SerializeField] private Transform[] traySlots = new Transform[0];

        private Tray[] trays;

        /// <summary>Tezgâhtaki tepsiler değişti (konuldu veya ayrıldı).</summary>
        public event Action TraysChanged;

        /// <summary>Tezgâhtaki ilk tepsi; yoksa null.</summary>
        public Tray CurrentTray
        {
            get
            {
                EnsureArray();
                foreach (Tray tray in trays)
                {
                    if (tray != null)
                    {
                        return tray;
                    }
                }

                return null;
            }
        }

        public bool HasFreeSlot => FreeSlotIndex() >= 0;

        public Vector3 SnapPoint
        {
            get
            {
                int index = FreeSlotIndex();
                return index >= 0 ? traySlots[index].position : transform.position;
            }
        }

        private void Awake()
        {
            EnsureArray();
            if (traySlots.Length == 0)
            {
                Debug.LogWarning("[PrepCounter] Tepsi yeri atanmamış; builder 4. adımı yeniden çalıştırın.", this);
            }
        }

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
            if (item == null || item.Kind != ServingItemKind.Tray || !HasFreeSlot)
            {
                return false;
            }

            Tray tray = item.GetComponent<Tray>();
            return tray != null && IndexOf(tray) < 0;
        }

        public void Accept(ServingItem item)
        {
            Tray tray = item.GetComponent<Tray>();
            int index = FreeSlotIndex();
            if (tray == null || index < 0)
            {
                return;
            }

            trays[index] = tray;
            tray.PlaceOnCounter(this, traySlots[index]);
            TraysChanged?.Invoke();
        }

        /// <summary>Tepsi tezgâhtan ayrılır (G3b: masaya teslim). Tepsi nesnesi yok edilmez.</summary>
        public void Release(Tray tray)
        {
            int index = IndexOf(tray);
            if (index < 0)
            {
                return;
            }

            trays[index] = null;
            tray.DetachFromCounter();
            TraysChanged?.Invoke();
        }

        /// <summary>
        /// Tezgâhtaki tüm tepsileri içerikleriyle (tabak, dilim, bardak) siler. Yeni round başında SalonRound çağırır.
        /// Silinen dilimler stoğa dönmez; vitrin stoğu round başında zaten yeniden doldurulur.
        /// </summary>
        public void ClearAll()
        {
            EnsureArray();
            bool changed = false;
            for (int i = 0; i < trays.Length; i++)
            {
                Tray tray = trays[i];
                trays[i] = null;
                if (tray != null)
                {
                    // Parçalar tepsinin alt nesneleri olduğu için tepsiyle birlikte silinir.
                    Destroy(tray.gameObject);
                    changed = true;
                }
            }

            if (changed)
            {
                TraysChanged?.Invoke();
            }
        }

        private int FreeSlotIndex()
        {
            EnsureArray();
            for (int i = 0; i < trays.Length; i++)
            {
                // Yok edilmiş tepsi de boş yer sayılır.
                if (trays[i] == null && traySlots[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private int IndexOf(Tray tray)
        {
            EnsureArray();
            for (int i = 0; i < trays.Length; i++)
            {
                if (trays[i] == tray)
                {
                    return i;
                }
            }

            return -1;
        }

        private void EnsureArray()
        {
            if (trays == null || trays.Length != traySlots.Length)
            {
                trays = new Tray[traySlots.Length];
            }
        }
    }
}
