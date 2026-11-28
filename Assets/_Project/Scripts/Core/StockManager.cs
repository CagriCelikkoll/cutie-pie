using CutiePie.Core.Data;

namespace CutiePie.Core
{
    /// <summary>
    /// Salon ile Mutfak'ın ortak pasta stoğu (dilim). Erişim: GameManager.Instance.Stock.
    /// Stoğu kendisi tutmaz; tek kaynak SaveSystem'dir. Her değişimde GameEvents.StockChanged yayınlanır.
    /// İçecekler stoklu değildir; stokta olmayan ürün için Get 0 döner.
    /// Pasta dolabının ürün başına kapasitesi vardır (KitchenSettings.cabinetCapacityPerProduct): Add kapasitede kırpar,
    /// taşan dilimler kaybolur ve GameEvents.StockOverflow ile bildirilir. Kapasiteyi aşmış eski stok budanmaz.
    /// </summary>
    public class StockManager
    {
        private readonly KitchenSettings _settings;

        /// <param name="settings">Kapasite buradan okunur; null ise (veya değer 0 ise) kapasite sınırı yoktur.</param>
        public StockManager(KitchenSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Ürünün mevcut dilim sayısı; bilinmeyen ürün veya kayıt yüklenmemişse 0.</summary>
        public int Get(string productId)
        {
            SaveSystem save = SaveSystem.Instance;
            return save != null ? save.GetStock(productId) : 0;
        }

        /// <summary>
        /// Ürünün dolap kapasitesi (dilim). v1'de tüm pastalar için KitchenSettings.cabinetCapacityPerProduct.
        /// 0: sınır yok (ayar atanmamış).
        /// </summary>
        public int GetCapacity(string productId) => _settings != null ? _settings.cabinetCapacityPerProduct : 0;

        /// <summary>Stok kapasiteye ulaştı mı (ya da aştı mı). Kapasite sınırı yoksa her zaman false.</summary>
        public bool IsFull(string productId)
        {
            int capacity = GetCapacity(productId);
            return capacity > 0 && Get(productId) >= capacity;
        }

        /// <summary>Kapasiteye kadar eklenebilecek dilim (en az 0). Kapasite sınırı yoksa int.MaxValue - stok.</summary>
        public int GetFreeSpace(string productId)
        {
            KitchenBatchMath.ClampToCapacity(Get(productId), int.MaxValue, GetCapacity(productId), out int added, out _);
            return added;
        }

        /// <summary>
        /// Stoktan amount dilim düşer. Stok yetmezse, amount 0 veya negatifse ya da kayıt yoksa hiçbir şey değişmez ve false döner.
        /// Diske hemen yazmaz; round sonu, duraklatma ve çıkıştaki kayıtlarla yazılır.
        /// </summary>
        public bool TryTake(string productId, int amount)
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null || string.IsNullOrEmpty(productId) || amount <= 0)
            {
                return false;
            }

            int current = save.GetStock(productId);
            if (current < amount)
            {
                return false;
            }

            int newAmount = current - amount;
            if (!save.SetStock(productId, newAmount))
            {
                return false;
            }

            GameEvents.RaiseStockChanged(productId, newAmount);
            return true;
        }

        /// <summary>
        /// Stoğa amount dilim eklemeyi dener (kapasitede kırpılır). Ayrıntı için out'lu sürüme bakın.
        /// Dönüş "istek geçerli" demektir, "dilim eklendi" demek değildir: dolu dolapta da true döner (0 eklenir).
        /// </summary>
        public bool Add(string productId, int amount) => Add(productId, amount, out _);

        /// <summary>
        /// Stoğa amount dilim ekler; kapasiteyi aşan kısım kırpılır. added: stoğa gerçekten giren dilim (0 olabilir).
        /// amount 0 veya negatifse, ürün id'si boşsa ya da kayıt yoksa hiçbir şey değişmez, added 0 ve false döner.
        /// Aksi halde true döner (dolap doluyken de). added &gt; 0 ise hemen kaydeder ve StockChanged yayınlar;
        /// taşan dilim varsa StockOverflow(productId, taşan) yayınlanır.
        /// Disk yazımı başarısız olsa da ekleme geçerlidir; değer sonraki kayıtta yazılır.
        /// </summary>
        public bool Add(string productId, int amount, out int added)
        {
            added = 0;
            SaveSystem save = SaveSystem.Instance;
            if (save == null || string.IsNullOrEmpty(productId) || amount <= 0)
            {
                return false;
            }

            int current = save.GetStock(productId);
            KitchenBatchMath.ClampToCapacity(current, amount, GetCapacity(productId), out int toAdd, out int lost);

            if (toAdd > 0)
            {
                if (!save.SetStock(productId, current + toAdd))
                {
                    return false;
                }

                added = toAdd;
                save.Save();
                GameEvents.RaiseStockChanged(productId, current + toAdd);
            }

            if (lost > 0)
            {
                GameEvents.RaiseStockOverflow(productId, lost);
            }

            return true;
        }
    }
}
