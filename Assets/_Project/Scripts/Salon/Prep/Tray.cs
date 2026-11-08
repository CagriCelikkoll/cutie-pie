using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Servis tepsisi: tabak yuvası ve bardak yuvası olan taşınabilir parça.
    /// Tezgâha konunca o tezgâh yerine bağlanır (Counter/HomeSlot); G3b'de buradan masaya teslim edilir.
    /// Tabak, dilim ve bardak kabulü sonraki adımlarda eklenir.
    /// </summary>
    [RequireComponent(typeof(ServingItem))]
    public class Tray : MonoBehaviour
    {
        [SerializeField] private Transform plateSlot;
        [SerializeField] private Transform drinkSlot;

        private ServingItem item;

        public ServingItem Item
        {
            get
            {
                if (item == null)
                {
                    item = GetComponent<ServingItem>();
                }

                return item;
            }
        }

        /// <summary>Tepsinin durduğu tezgâh; tezgâhta değilse null.</summary>
        public PrepCounter Counter { get; private set; }

        /// <summary>Tepsinin tezgâhtaki yeri; çöpten veya ıskalanan bırakmadan sonra buraya döner.</summary>
        public Transform HomeSlot { get; private set; }

        public Transform PlateSlot => plateSlot;
        public Transform DrinkSlot => drinkSlot;

        /// <summary>PrepCounter.Accept çağırır: tepsiyi tezgâh yerine oturtur.</summary>
        public void PlaceOnCounter(PrepCounter counter, Transform slot)
        {
            Counter = counter;
            HomeSlot = slot;

            transform.SetParent(slot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Tepsi tezgâhtan ayrıldı (G3b: masaya teslim).</summary>
        public void DetachFromCounter()
        {
            Counter = null;
            HomeSlot = null;
        }
    }
}
