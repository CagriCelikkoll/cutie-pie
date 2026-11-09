using System;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Servis tepsisi: tabak yuvası ve bardak yuvası olan taşınabilir parça.
    /// Tezgâhtayken bırakma hedefidir (tabak, dilim, bardak alır) ve kendisi de sürüklenebilir (çöpe veya masaya).
    /// Kurallar: dilim yalnızca boş bir tabağa konur; bardak tabaksız da konabilir; her yuvada tek parça.
    /// </summary>
    [RequireComponent(typeof(ServingItem))]
    [RequireComponent(typeof(DragItem))]
    public class Tray : MonoBehaviour, IDropTarget, IDragSource
    {
        [SerializeField] private Transform plateSlot;
        [SerializeField] private Transform drinkSlot;

        private ServingItem item;
        private DragItem dragItem;
        private DropTargetRegistry registry;

        /// <summary>Tabak, dilim veya bardak eklendi ya da tepsi boşaltıldı.</summary>
        public event Action<Tray> ContentsChanged;

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

        public ServingItem Plate { get; private set; }
        public ServingItem Slice { get; private set; }
        public ServingItem Drink { get; private set; }

        /// <summary>Tepsinin durduğu tezgâh; tezgâhta değilse null.</summary>
        public PrepCounter Counter { get; private set; }

        /// <summary>Tepsinin tezgâhtaki yeri; çöpten veya ıskalanan bırakmadan sonra buraya döner.</summary>
        public Transform HomeSlot { get; private set; }

        public Vector3 SnapPoint => transform.position;

        /// <summary>TrayStack üretir üretmez çağırır: görünüm, sürükleme ve hedef kaydı.</summary>
        public void Init(SalonSettings settings, DropTargetRegistry dropTargets)
        {
            Item.Setup(null, settings);
            registry = dropTargets;

            dragItem = GetComponent<DragItem>();
            dragItem.Init(settings, dropTargets);

            if (registry != null)
            {
                registry.Register(this);
            }
        }

        private void OnDestroy()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }
        }

        // --- Tezgâh ---

        /// <summary>PrepCounter.Accept çağırır: tepsiyi tezgâh yerine oturtur.</summary>
        public void PlaceOnCounter(PrepCounter counter, Transform slot)
        {
            Counter = counter;
            HomeSlot = slot;

            transform.SetParent(slot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Tepsi tezgâhtan ayrıldı (masaya teslim).</summary>
        public void DetachFromCounter()
        {
            Counter = null;
            HomeSlot = null;
        }

        /// <summary>
        /// Teslim edilen tepsiyi masaya oturtur. Tezgâhta olmadığı için artık sürüklenemez, çöpe atılamaz ve içine parça konamaz.
        /// </summary>
        public void PlaceOnTable(Transform parent, float scale, int sortingOrder)
        {
            transform.SetParent(parent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * scale;
            Item.SortingGroup.sortingOrder = sortingOrder;
        }

        // --- İçerik (IDropTarget) ---

        /// <summary>Bu türde bir parça şu an tepsiye konabilir mi? Kaynaklar parça üretmeden önce sorar.</summary>
        public bool CanAcceptKind(ServingItemKind kind)
        {
            // Yalnızca tezgâhtaki tepsi doldurulur (yığından yeni çıkan tepsi henüz hedef değildir).
            if (Counter == null)
            {
                return false;
            }

            switch (kind)
            {
                case ServingItemKind.Plate: return Plate == null;
                case ServingItemKind.CakeSlice: return Plate != null && Slice == null;
                case ServingItemKind.Drink: return Drink == null;
                default: return false;
            }
        }

        public bool CanAccept(ServingItem serving) => serving != null && serving != Item && CanAcceptKind(serving.Kind);

        public void Accept(ServingItem serving)
        {
            switch (serving.Kind)
            {
                case ServingItemKind.Plate:
                    Plate = serving;
                    Attach(serving, plateSlot, SalonSortOrder.PlateInTray);
                    break;

                case ServingItemKind.CakeSlice:
                    Slice = serving;
                    Attach(serving, Plate.AttachPoint, SalonSortOrder.SliceOnPlate);
                    break;

                case ServingItemKind.Drink:
                    Drink = serving;
                    Attach(serving, drinkSlot, SalonSortOrder.DrinkInTray);
                    break;

                default:
                    return;
            }

            ContentsChanged?.Invoke(this);
        }

        /// <summary>Tepsinin içeriğini (tabak, dilim, bardak) siler; tepsi yerinde kalır. Silinen dilim stoğa dönmez.</summary>
        public void Clear()
        {
            DestroyPart(Slice);
            DestroyPart(Plate);
            DestroyPart(Drink);
            Slice = null;
            Plate = null;
            Drink = null;

            ContentsChanged?.Invoke(this);
        }

        /// <summary>Müşteri yedikten sonra dilimi ve bardağı siler; masada boş tabak kalır (kirli masa görünümü).</summary>
        public void ClearFood()
        {
            DestroyPart(Slice);
            DestroyPart(Drink);
            Slice = null;
            Drink = null;

            ContentsChanged?.Invoke(this);
        }

        private static void Attach(ServingItem serving, Transform parent, int sortingOrder)
        {
            Transform tf = serving.transform;
            tf.SetParent(parent, false);
            tf.localPosition = Vector3.zero;
            tf.localRotation = Quaternion.identity;
            serving.SortingGroup.sortingOrder = sortingOrder;
        }

        private static void DestroyPart(ServingItem part)
        {
            if (part != null)
            {
                Destroy(part.gameObject);
            }
        }

        // --- Tepsinin kendisini sürükleme (IDragSource) ---

        public ServingItem BeginDrag() => Counter != null ? Item : null;

        public void OnDropped(ServingItem dropped, IDropTarget target)
        {
            // Hedef tepsiyi tezgâhtan almadıysa (ör. çöp yalnızca içeriği sildi) tepsi yerine döner.
            if (Counter != null && dragItem != null)
            {
                dragItem.StartReturn(dropped);
            }
        }

        public void OnReturned(ServingItem returned)
        {
            if (HomeSlot != null)
            {
                transform.localPosition = Vector3.zero;
            }
        }
    }
}
