using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Salon ile Mutfak'ın ortak pasta stoğu (dilim). Erişim: GameManager.Instance.Stock.
    /// Stoğu kendisi tutmaz; tek kaynak SaveSystem'dir. Her değişimde GameEvents.StockChanged yayınlanır.
    /// İçecekler stoklu değildir; stokta olmayan ürün için Get 0 döner.
    /// </summary>
    public class StockManager
    {
        /// <summary>Ürünün mevcut dilim sayısı; bilinmeyen ürün veya kayıt yüklenmemişse 0.</summary>
        public int Get(string productId)
        {
            SaveSystem save = SaveSystem.Instance;
            return save != null ? save.GetStock(productId) : 0;
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
        /// Stoğa amount dilim ekler ve hemen kaydeder. amount 0 veya negatifse ya da kayıt yoksa hiçbir şey değişmez ve false döner.
        /// Disk yazımı başarısız olsa da ekleme geçerlidir; değer sonraki kayıtta yazılır.
        /// </summary>
        public bool Add(string productId, int amount)
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null || string.IsNullOrEmpty(productId) || amount <= 0)
            {
                return false;
            }

            // Taşmaya karşı: int sınırında sabitlenir.
            long sum = (long)save.GetStock(productId) + amount;
            int newAmount = (int)Mathf.Min(sum, int.MaxValue);
            if (!save.SetStock(productId, newAmount))
            {
                return false;
            }

            save.Save();
            GameEvents.RaiseStockChanged(productId, newAmount);
            return true;
        }
    }
}
