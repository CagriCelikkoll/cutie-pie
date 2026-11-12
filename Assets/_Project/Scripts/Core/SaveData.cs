using System;
using System.Collections.Generic;
using CutiePie.Core.Data;

namespace CutiePie.Core
{
    /// <summary>
    /// Diske JSON olarak yazılan kayıt verisi. JsonUtility ile uyumlu olması için yalnızca alan ve liste kullanır.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Kaydı yazan kodun biçim sürümü.</summary>
        public int saveVersion;

        public int gold;
        public int gems;
        public int shopRating;

        /// <summary>Mevcut seviyenin LevelData.id değeri.</summary>
        public string currentLevelId;

        /// <summary>Geliştirme seviyeleri (JsonUtility sözlük desteklemediği için liste).</summary>
        public List<UpgradeLevelEntry> upgrades = new List<UpgradeLevelEntry>();

        // --- Mutfak (saveVersion 2) ---

        /// <summary>Pasta stokları (dilim). Listede olmayan ürünün stoğu 0'dır.</summary>
        public List<StockEntry> stock = new List<StockEntry>();

        /// <summary>Fırın durumları; sıra fırın indeksidir. G12'de doldurulur.</summary>
        public List<OvenSaveEntry> ovens = new List<OvenSaveEntry>();

        /// <summary>Son görülen UTC zaman (DateTime.Ticks). Saat geri alınmasını tespit etmek için. G12'de doldurulur.</summary>
        public long lastSeenUtcTicks;

        /// <summary>Panodaki fişler. G14'te doldurulur.</summary>
        public List<CustomOrder> boardOrders = new List<CustomOrder>();

        /// <summary>Bekleyen fiş yenilemelerinin UTC zamanları (DateTime.Ticks). G14'te doldurulur.</summary>
        public List<long> pendingOrderRefreshUtcTicks = new List<long>();

        /// <summary>Seçili dil kodu; boşsa varsayılan dil. G15'te doldurulur.</summary>
        public string languageCode;
    }

    /// <summary>Bir ürünün kayıtlı stoğu.</summary>
    [Serializable]
    public class StockEntry
    {
        /// <summary>ProductData.id değeri.</summary>
        public string productId;

        public int amount;
    }

    /// <summary>Tek bir fırının kayıtlı durumu. recipeId boşsa fırın boştur.</summary>
    [Serializable]
    public class OvenSaveEntry
    {
        /// <summary>Fırındaki RecipeData.id; boşsa fırın boş.</summary>
        public string recipeId;

        /// <summary>Pişirmenin başladığı UTC zaman (DateTime.Ticks).</summary>
        public long startUtcTicks;
    }

    /// <summary>Tek bir geliştirmenin kayıtlı seviyesi.</summary>
    [Serializable]
    public class UpgradeLevelEntry
    {
        /// <summary>UpgradeData.id değeri.</summary>
        public string id;

        public int level;
    }

    /// <summary>Kaydın nasıl yüklendiği. Save() yalnızca NotLoaded dışındaki durumlarda dosyaya yazar.</summary>
    public enum SaveLoadState
    {
        /// <summary>Henüz yükleme yapılmadı; dosyaya yazılmaz.</summary>
        NotLoaded = 0,

        /// <summary>Ana dosyadan sorunsuz yüklendi.</summary>
        LoadedFromFile = 1,

        /// <summary>Ana dosya yok/bozuk, yarım kalmış yazmadan kalan geçici dosyadan kurtarıldı.</summary>
        RecoveredFromTemp = 2,

        /// <summary>Kayıt dosyası yok; varsayılanlarla başlandı (ilk açılış).</summary>
        NewDefaults = 3,

        /// <summary>Kayıt bozuk; varsayılanlarla başlandı. İlk yazmadan önce bozuk dosya yedeklenir.</summary>
        CorruptDefaults = 4,

        /// <summary>Kayıt koddakinden yeni sürümde; yüklendi. İlk yazmadan önce yedeklenir.</summary>
        LoadedNewerVersion = 5
    }
}
