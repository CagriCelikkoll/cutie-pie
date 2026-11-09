using System;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Upgrades
{
    /// <summary>
    /// Geliştirme satın alma ve sorgulama servisi. Erişim: GameManager.Instance.Upgrades.
    /// Seviyeleri tutmaz; tek kaynak SaveSystem'dir.
    /// </summary>
    public class UpgradeManager
    {
        /// <summary>Sonraki seviye yoksa veya id bilinmiyorsa GetNextPrice'ın döndürdüğü değer.</summary>
        public const int NoPrice = -1;

        private readonly UpgradeData[] _upgrades;

        public UpgradeManager(UpgradeData[] upgrades)
        {
            _upgrades = upgrades ?? Array.Empty<UpgradeData>();
        }

        /// <summary>Mevcut seviye; id bilinmiyorsa 0.</summary>
        public int GetLevel(string id)
        {
            UpgradeData upgrade = Find(id);
            return upgrade != null ? GetLevel(upgrade) : 0;
        }

        /// <summary>En üst seviye; id bilinmiyorsa 0.</summary>
        public int GetMaxLevel(string id)
        {
            UpgradeData upgrade = Find(id);
            return upgrade != null ? upgrade.LevelCount : 0;
        }

        /// <summary>Sonraki seviyenin fiyatı; en üst seviyedeyse veya id bilinmiyorsa NoPrice.</summary>
        public int GetNextPrice(string id)
        {
            UpgradeData upgrade = Find(id);
            return upgrade != null ? GetNextPrice(upgrade) : NoPrice;
        }

        /// <summary>Kayıt yüklü, seviye en üstte değil ve altın yetiyorsa true.</summary>
        public bool CanPurchase(string id)
        {
            UpgradeData upgrade = Find(id);
            return upgrade != null && CanPurchase(upgrade);
        }

        /// <summary>
        /// Bir sonraki seviyeyi satın alır: altını düşer, seviyeyi artırır, kaydeder ve UpgradePurchased yayınlar.
        /// Satın alınamıyorsa hiçbir şey değişmez ve false döner.
        /// Disk yazımı başarısız olsa da satın alma geçerlidir; değer sonraki kayıtta yazılır.
        /// </summary>
        public bool TryPurchase(string id)
        {
            UpgradeData upgrade = Find(id);
            if (upgrade == null || !CanPurchase(upgrade))
            {
                return false;
            }

            if (!EconomyManager.Instance.TrySpend(CurrencyType.Gold, GetNextPrice(upgrade)))
            {
                return false;
            }

            int newLevel = GetLevel(upgrade) + 1;
            SaveSystem.Instance.SetUpgradeLevel(upgrade.id, newLevel);
            SaveSystem.Instance.Save();
            GameEvents.RaiseUpgradePurchased(upgrade, newLevel);
            return true;
        }

        /// <summary>
        /// Mevcut seviyenin etki değeri. Geliştirme alınmamışsa (seviye 0) veya id bilinmiyorsa 0 döner.
        /// Süre etkilerinde (ready_tray, auto_drink, waiter) 0 "anında" gibi yorumlanabileceği için TryGetEffect kullanılmalıdır.
        /// </summary>
        public float GetEffect(string id)
        {
            TryGetEffect(id, out float value);
            return value;
        }

        /// <summary>Geliştirme alınmışsa etki değerini verir ve true döner; seviye 0'da veya id bilinmiyorsa false.</summary>
        public bool TryGetEffect(string id, out float value)
        {
            value = 0f;

            UpgradeData upgrade = Find(id);
            if (upgrade == null || upgrade.levelEffects == null)
            {
                return false;
            }

            int level = GetLevel(upgrade);
            if (level == 0 || level > upgrade.levelEffects.Length)
            {
                return false;
            }

            value = upgrade.levelEffects[level - 1];
            return true;
        }

        // Kayıttaki seviye veriden büyükse (ör. seviye sayısı azaltıldıysa) en üst seviye kabul edilir.
        private static int GetLevel(UpgradeData upgrade)
        {
            if (SaveSystem.Instance == null)
            {
                return 0;
            }

            return Mathf.Min(SaveSystem.Instance.GetUpgradeLevel(upgrade.id), upgrade.LevelCount);
        }

        private static int GetNextPrice(UpgradeData upgrade)
        {
            int level = GetLevel(upgrade);
            return level < upgrade.LevelCount ? upgrade.levelPrices[level] : NoPrice;
        }

        private static bool CanPurchase(UpgradeData upgrade)
        {
            if (SaveSystem.Instance == null || SaveSystem.Instance.LoadState == SaveLoadState.NotLoaded ||
                EconomyManager.Instance == null)
            {
                return false;
            }

            int price = GetNextPrice(upgrade);
            return price != NoPrice && EconomyManager.Instance.Get(CurrencyType.Gold) >= price;
        }

        private UpgradeData Find(string id)
        {
            UpgradeData upgrade = Array.Find(_upgrades, u => u != null && u.id == id);
            if (upgrade == null)
            {
                Debug.LogWarning($"[UpgradeManager] Bilinmeyen geliştirme id'si: '{id}'.");
            }

            return upgrade;
        }
    }
}
