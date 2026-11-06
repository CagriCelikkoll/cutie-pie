using System;
using System.Collections.Generic;

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
