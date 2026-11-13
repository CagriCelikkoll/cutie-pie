using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Yerel JSON kayıt sistemi. Boot'taki Managers objesinde durur, sahne değişse de yaşar.
    /// Yükleme GameManager tarafından Salon'a geçmeden önce yapılır; yükleme bitmeden dosyaya yazılmaz.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        // Stok alanının eklendiği kayıt biçimi sürümü (göç için; denge değeri değildir).
        private const int StockIntroducedVersion = 2;

        [SerializeField] private GameConfig config;

        private SaveData _data;

        // İlk yazmadan önce mevcut ana dosyanın yedeklenmesi gerekiyor mu (yeni sürüm veya bozuk kayıt).
        private bool _backupPending;

        /// <summary>Kaydın nasıl yüklendiği.</summary>
        public SaveLoadState LoadState { get; private set; } = SaveLoadState.NotLoaded;

        /// <summary>Mevcut seviyenin id'si. Değiştirmek için CurrentLevel'ı atayın.</summary>
        public string CurrentLevelId => _data?.currentLevelId;

        /// <summary>Mevcut seviye; id bulunamazsa GameConfig.startingLevel.</summary>
        public LevelData CurrentLevel
        {
            get => FindLevel(CurrentLevelId) ?? config.startingLevel;
            set
            {
                if (_data != null && value != null)
                {
                    _data.currentLevelId = value.id;
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => GameEvents.RoundEnded += HandleRoundEnded;

        private void OnDisable() => GameEvents.RoundEnded -= HandleRoundEnded;

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit() => Save();

        private void HandleRoundEnded(RoundResult result) => Save();

        // ------------------------------------------------------------------ Yükleme

        /// <summary>Kaydı diskten yükler ve EconomyManager'a uygular. Boot'tan çıkmadan önce bir kez çağrılır.</summary>
        public void LoadAndApply()
        {
            Load();
            ApplyToEconomy();
        }

        private void Load()
        {
            string mainPath = GetPath(config.saveFileName);
            string tempPath = GetPath(config.saveTempFileName);
            bool mainExists = File.Exists(mainPath);

            if (TryRead(mainPath, out SaveData data))
            {
                SetLoaded(data, SaveLoadState.LoadedFromFile);
            }
            else if (TryRead(tempPath, out data))
            {
                // Ana dosya değiştirilirken yarıda kalmış bir yazma olabilir.
                Debug.LogWarning($"[SaveSystem] Ana kayıt okunamadı, geçici dosyadan kurtarıldı: {tempPath}");
                SetLoaded(data, SaveLoadState.RecoveredFromTemp);
            }
            else if (mainExists)
            {
                Debug.LogWarning($"[SaveSystem] Kayıt dosyası bozuk, varsayılanlarla başlanıyor: {mainPath}");
                _data = CreateDefaults();
                LoadState = SaveLoadState.CorruptDefaults;
                _backupPending = true;
            }
            else
            {
                Debug.LogWarning("[SaveSystem] Kayıt dosyası yok, varsayılanlarla başlanıyor.");
                _data = CreateDefaults();
                LoadState = SaveLoadState.NewDefaults;
            }
        }

        private void SetLoaded(SaveData data, SaveLoadState state)
        {
            _data = data;
            LoadState = state;

            if (data.saveVersion > config.saveVersion)
            {
                Debug.LogWarning($"[SaveSystem] Kayıt sürümü ({data.saveVersion}) koddakinden ({config.saveVersion}) yeni. " +
                                 "Yükleniyor; ilk yazmadan önce yedek alınacak.");
                LoadState = SaveLoadState.LoadedNewerVersion;
                _backupPending = true;
            }

            // --- Eski sürümlerden göç ---

            // v1 kayıtlarında stok alanı yoktur: oyuncuya bir kez başlangıç stoğu verilir.
            // Boş listeye değil sürüme bakılır; stoğu bitmiş v2 kaydı da boş liste taşıyabilir.
            if (data.saveVersion < StockIntroducedVersion)
            {
                Debug.Log($"[SaveSystem] Kayıt sürüm {data.saveVersion}; başlangıç stoğu ekleniyor.");
                data.stock = CreateStartingStock();
            }

            if (FindLevel(data.currentLevelId) == null)
            {
                Debug.LogWarning($"[SaveSystem] Kayıttaki seviye '{data.currentLevelId}' bulunamadı, " +
                                 $"başlangıç seviyesine dönülüyor: '{config.startingLevel.id}'.");
                data.currentLevelId = config.startingLevel.id;
            }
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Kayıt okunamadı ({path}): {e.Message}");
                return false;
            }

            return IsValid(data);
        }

        private static bool IsValid(SaveData data)
        {
            if (data == null || data.saveVersion <= 0 || data.gold < 0 || data.gems < 0 || data.shopRating < 0)
            {
                return false;
            }

            if (data.upgrades == null)
            {
                data.upgrades = new System.Collections.Generic.List<UpgradeLevelEntry>();
            }

            foreach (UpgradeLevelEntry entry in data.upgrades)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.level < 0)
                {
                    return false;
                }
            }

            SanitizeKitchen(data);
            return true;
        }

        // Mutfak alanlarındaki hatalı satırlar tüm kaydı bozuk saymak yerine ayıklanır.
        private static void SanitizeKitchen(SaveData data)
        {
            data.stock ??= new List<StockEntry>();
            data.ovens ??= new List<OvenSaveEntry>();
            data.boardOrders ??= new List<CustomOrder>();
            data.pendingOrderRefreshUtcTicks ??= new List<long>();

            data.stock.RemoveAll(e => e == null || string.IsNullOrEmpty(e.productId));
            foreach (StockEntry entry in data.stock)
            {
                entry.amount = Mathf.Max(0, entry.amount);
            }

            data.boardOrders.RemoveAll(o => o == null || string.IsNullOrEmpty(o.id));
        }

        private SaveData CreateDefaults()
        {
            return new SaveData
            {
                saveVersion = config.saveVersion,
                gold = config.startingGold,
                gems = config.startingGems,
                shopRating = config.startingShopRating,
                currentLevelId = config.startingLevel.id,
                stock = CreateStartingStock()
            };
        }

        // KitchenSettings.startingStock'tan yeni bir stok listesi üretir.
        private List<StockEntry> CreateStartingStock()
        {
            var stock = new List<StockEntry>();
            if (config.kitchen == null || config.kitchen.startingStock == null)
            {
                Debug.LogWarning("[SaveSystem] GameConfig.kitchen atanmamış; başlangıç stoğu verilmedi.");
                return stock;
            }

            foreach (StockAmount item in config.kitchen.startingStock)
            {
                if (item == null || item.product == null || string.IsNullOrEmpty(item.product.id) || item.amount <= 0)
                {
                    continue;
                }

                StockEntry existing = stock.Find(e => e.productId == item.product.id);
                if (existing != null)
                {
                    existing.amount += item.amount;
                }
                else
                {
                    stock.Add(new StockEntry { productId = item.product.id, amount = item.amount });
                }
            }

            return stock;
        }

        private void ApplyToEconomy()
        {
            EconomyManager economy = EconomyManager.Instance;
            if (economy == null)
            {
                Debug.LogWarning("[SaveSystem] EconomyManager bulunamadı, kayıt uygulanamadı.");
                return;
            }

            economy.Set(CurrencyType.Gold, _data.gold);
            economy.Set(CurrencyType.Gem, _data.gems);
            economy.Set(CurrencyType.ShopRating, _data.shopRating);
        }

        private LevelData FindLevel(string id)
        {
            if (string.IsNullOrEmpty(id) || config.levels == null)
            {
                return null;
            }

            foreach (LevelData level in config.levels)
            {
                if (level != null && level.id == id)
                {
                    return level;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ Geliştirmeler

        /// <summary>Geliştirmenin kayıtlı seviyesi; hiç alınmadıysa 0.</summary>
        public int GetUpgradeLevel(string upgradeId)
        {
            UpgradeLevelEntry entry = FindUpgrade(upgradeId);
            return entry != null ? entry.level : 0;
        }

        /// <summary>Geliştirme seviyesini ayarlar (diske yazmak için Save çağrılmalı).</summary>
        public void SetUpgradeLevel(string upgradeId, int level)
        {
            if (_data == null || string.IsNullOrEmpty(upgradeId))
            {
                return;
            }

            UpgradeLevelEntry entry = FindUpgrade(upgradeId);
            if (entry == null)
            {
                entry = new UpgradeLevelEntry { id = upgradeId };
                _data.upgrades.Add(entry);
            }

            entry.level = Mathf.Max(0, level);
        }

        private UpgradeLevelEntry FindUpgrade(string upgradeId)
        {
            return _data?.upgrades.Find(e => e.id == upgradeId);
        }

        // ------------------------------------------------------------------ Stok

        /// <summary>Ürünün kayıtlı stoğu; kayıt yüklenmediyse veya ürün yoksa 0. Erişim için StockManager kullanılmalı.</summary>
        public int GetStock(string productId)
        {
            StockEntry entry = FindStock(productId);
            return entry != null ? entry.amount : 0;
        }

        /// <summary>Ürünün stoğunu ayarlar (diske yazmak için Save çağrılmalı). Kayıt yüklenmediyse false.</summary>
        public bool SetStock(string productId, int amount)
        {
            if (_data == null || string.IsNullOrEmpty(productId))
            {
                return false;
            }

            StockEntry entry = FindStock(productId);
            if (entry == null)
            {
                entry = new StockEntry { productId = productId };
                _data.stock.Add(entry);
            }

            entry.amount = Mathf.Max(0, amount);
            return true;
        }

        private StockEntry FindStock(string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                return null;
            }

            return _data?.stock.Find(e => e.productId == productId);
        }

        // ------------------------------------------------------------------ Fırınlar ve saat

        /// <summary>Kayıt yüklendi mi (yüklenmeden okunan değerler varsayılandır, yazılanlar kaybolur).</summary>
        public bool IsLoaded => LoadState != SaveLoadState.NotLoaded && _data != null;

        /// <summary>Son görülen UTC zaman (DateTime.Ticks); hiç görülmediyse 0. Yalnızca ileri gider.</summary>
        public long LastSeenUtcTicks => _data != null ? _data.lastSeenUtcTicks : 0L;

        /// <summary>
        /// Son görülen zamanı ileri alır (geri gitmez). Bellekte güncellenir; diske Save ile yazılır.
        /// Cihaz saati geri alınırsa süreler bu değerden geriye düşmez.
        /// </summary>
        public void AdvanceLastSeen(long utcTicks)
        {
            if (_data != null && utcTicks > _data.lastSeenUtcTicks)
            {
                _data.lastSeenUtcTicks = utcTicks;
            }
        }

        /// <summary>Fırının kayıtlı durumu. Kayıt yoksa veya fırın boşsa false (recipeId null).</summary>
        public bool TryGetOven(int ovenIndex, out string recipeId, out long startUtcTicks)
        {
            recipeId = null;
            startUtcTicks = 0L;
            if (_data == null || ovenIndex < 0 || ovenIndex >= _data.ovens.Count)
            {
                return false;
            }

            OvenSaveEntry entry = _data.ovens[ovenIndex];
            if (entry == null || string.IsNullOrEmpty(entry.recipeId))
            {
                return false;
            }

            recipeId = entry.recipeId;
            startUtcTicks = entry.startUtcTicks;
            return true;
        }

        /// <summary>
        /// Fırının durumunu ayarlar; recipeId boşsa fırın boşalır. Liste gerekirse uzatılır.
        /// Diske yazmak için Save çağrılmalı. Kayıt yüklenmediyse veya indeks negatifse false.
        /// </summary>
        public bool SetOven(int ovenIndex, string recipeId, long startUtcTicks)
        {
            if (_data == null || ovenIndex < 0)
            {
                return false;
            }

            while (_data.ovens.Count <= ovenIndex)
            {
                _data.ovens.Add(new OvenSaveEntry());
            }

            bool empty = string.IsNullOrEmpty(recipeId);
            _data.ovens[ovenIndex] = new OvenSaveEntry
            {
                recipeId = empty ? null : recipeId,
                startUtcTicks = empty ? 0L : startUtcTicks
            };
            return true;
        }

        // ------------------------------------------------------------------ Yazma

        /// <summary>
        /// Güncel durumu diske yazar. Yükleme yapılmadıysa yazmaz. Başarılıysa true döner; hata oyunu çökertmez.
        /// </summary>
        public bool Save()
        {
            if (LoadState == SaveLoadState.NotLoaded || _data == null)
            {
                Debug.LogWarning("[SaveSystem] Kayıt henüz yüklenmedi; dosyaya yazılmadı.");
                return false;
            }

            CollectFromEconomy();
            AdvanceLastSeen(GameClock.UtcNow.Ticks);
            _data.saveVersion = config.saveVersion;

            string mainPath = GetPath(config.saveFileName);
            string tempPath = GetPath(config.saveTempFileName);

            try
            {
                if (_backupPending && File.Exists(mainPath))
                {
                    File.Copy(mainPath, GetPath(config.saveBackupFileName), true);
                }

                _backupPending = false;

                WriteFully(tempPath, JsonUtility.ToJson(_data, true));
                ReplaceMain(tempPath, mainPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Kayıt yazılamadı: {e.Message}");
                return false;
            }
        }

        private void CollectFromEconomy()
        {
            EconomyManager economy = EconomyManager.Instance;
            if (economy == null)
            {
                return;
            }

            _data.gold = economy.Get(CurrencyType.Gold);
            _data.gems = economy.Get(CurrencyType.Gem);
            _data.shopRating = economy.Get(CurrencyType.ShopRating);
        }

        // Geçici dosyaya yazar ve diske boşaltır; yarıda kalırsa ana dosya etkilenmez.
        private static void WriteFully(string path, string json)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        // Geçici dosyayı asıl dosyanın yerine koyar.
        private static void ReplaceMain(string tempPath, string mainPath)
        {
            if (!File.Exists(mainPath))
            {
                File.Move(tempPath, mainPath);
                return;
            }

            try
            {
                File.Replace(tempPath, mainPath, null);
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is IOException)
            {
                // Replace desteklenmiyorsa veya başarısızsa kopyala ve geçiciyi sil.
                // Kopyalama yarıda kalırsa geçici dosya durduğu için açılışta oradan kurtarılır.
                File.Copy(tempPath, mainPath, true);
                File.Delete(tempPath);
            }
        }

        // ------------------------------------------------------------------ Silme

        /// <summary>Kayıt dosyalarını siler ve oyun içi değerleri varsayılana döndürür.</summary>
        public void DeleteSave()
        {
            // Silinmeden önceki stoklu ürünler de "değişti" sayılır (yeni değerleri 0 olabilir).
            var changedProducts = new HashSet<string>();
            if (_data != null)
            {
                _data.stock.ForEach(e => changedProducts.Add(e.productId));
            }

            DeleteFiles(config);
            _data = CreateDefaults();
            LoadState = SaveLoadState.NewDefaults;
            _backupPending = false;
            ApplyToEconomy();

            _data.stock.ForEach(e => changedProducts.Add(e.productId));
            foreach (string productId in changedProducts)
            {
                GameEvents.RaiseStockChanged(productId, GetStock(productId));
            }
        }

        /// <summary>Ana, geçici ve yedek kayıt dosyalarını siler. Hata oyunu çökertmez.</summary>
        public static bool DeleteFiles(GameConfig gameConfig)
        {
            try
            {
                File.Delete(GetPath(gameConfig.saveFileName));
                File.Delete(GetPath(gameConfig.saveTempFileName));
                File.Delete(GetPath(gameConfig.saveBackupFileName));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Kayıt silinemedi: {e.Message}");
                return false;
            }
        }

        /// <summary>persistentDataPath altındaki kayıt dosyasının tam yolu.</summary>
        public static string GetPath(string fileName) =>
            Path.Combine(Application.persistentDataPath, fileName);

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
