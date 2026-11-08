using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Raftaki tek ürün yeri (vitrin yuvası veya içecek makinesi) için ortak taban.
    /// Sürüklenince ürünün parçasını (dilim / bardak) üretir; parça tepsiye oturmazsa yuvaya dönüp yok olur.
    /// TryPlaceOn sürüklemesiz yoldur; ileride otomasyon geliştirmeleri bunu çağırır.
    /// </summary>
    [RequireComponent(typeof(DragItem))]
    public abstract class ShelfSlot : MonoBehaviour, IDragSource
    {
        [SerializeField] protected SalonSettings settings;
        [SerializeField] private ServingItem itemPrefab;
        [SerializeField] protected SpriteRenderer icon;

        private Sprite placeholderSprite;
        private bool placeholderCached;

        /// <summary>Yuvadaki ürün; sayfada bu yuvaya ürün düşmediyse null.</summary>
        public ProductData Product { get; private set; }

        /// <summary>Bu yuvanın ürettiği parça türü.</summary>
        protected abstract ServingItemKind ItemKind { get; }

        /// <summary>Şu an parça alınabilir mi (ürün var, stok var...)?</summary>
        protected virtual bool CanTake => Product != null;

        /// <summary>Parça tepsiye kabul edildi; kalıcı işlem (stoktan düşme) burada yapılır. Başarısızsa false.</summary>
        protected virtual bool Commit(ProductData product) => true;

        /// <summary>ProductShelf sayfa gösterirken çağırır. stock içecek rafında null'dır.</summary>
        public virtual void Bind(ProductData product, IShowcaseStock stock)
        {
            Product = product;
            ApplyIcon();
            RefreshState();
        }

        /// <summary>Sürüklemeden, doğrudan tepsiye parça koyar. Kural veya stok izin vermezse false.</summary>
        public bool TryPlaceOn(Tray tray)
        {
            if (!CanTake || tray == null || itemPrefab == null || !tray.CanAcceptKind(ItemKind))
            {
                return false;
            }

            if (!Commit(Product))
            {
                return false;
            }

            tray.Accept(Spawn());
            return true;
        }

        public ServingItem BeginDrag()
        {
            if (!CanTake || itemPrefab == null)
            {
                return null;
            }

            return Spawn();
        }

        public void OnDropped(ServingItem item, IDropTarget target)
        {
            // Parçanın kendi ürünü kullanılır; sürükleme sırasında sayfa değişmiş olsa da doğru ürün düşer.
            if (!Commit(item.Product))
            {
                Debug.LogWarning($"[ShelfSlot] '{(item.Product != null ? item.Product.id : "null")}' için stok yetmedi.", this);
            }
        }

        public void OnReturned(ServingItem item)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        /// <summary>Ürün yoksa yuva boş görünür; alınamıyorsa (stok 0) sönük görünür.</summary>
        protected virtual void RefreshState()
        {
            if (icon == null)
            {
                return;
            }

            icon.enabled = Product != null;
            Color color = icon.color;
            color.a = CanTake ? 1f : SlotEmptyAlpha;
            icon.color = color;
        }

        protected float SlotEmptyAlpha => settings != null ? settings.slotEmptyAlpha : 1f;

        private void ApplyIcon()
        {
            if (icon == null || Product == null)
            {
                return;
            }

            if (!placeholderCached)
            {
                placeholderSprite = icon.sprite;
                placeholderCached = true;
            }

            if (Product.icon != null)
            {
                icon.sprite = Product.icon;
                icon.color = Color.white;
            }
            else
            {
                icon.sprite = placeholderSprite;
                icon.color = settings != null ? settings.GetProductPlaceholderColor(Product) : Color.white;
            }
        }

        private ServingItem Spawn()
        {
            ServingItem item = Instantiate(itemPrefab, transform.position, Quaternion.identity, transform);
            item.Setup(Product, settings);
            return item;
        }
    }
}
