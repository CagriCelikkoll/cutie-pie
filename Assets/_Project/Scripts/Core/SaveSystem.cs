using System;
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

            // Eski sürümlerden göç adımları ileride burada yapılacak (data.saveVersion < config.saveVersion).

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

            return true;
        }

        private SaveData CreateDefaults()
        {
            return new SaveData
            {
                saveVersion = config.saveVersion,
                gold = config.startingGold,
                gems = config.startingGems,
                shopRating = config.startingShopRating,
                currentLevelId = config.startingLevel.id
            };
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
            DeleteFiles(config);
            _data = CreateDefaults();
            LoadState = SaveLoadState.NewDefaults;
            _backupPending = false;
            ApplyToEconomy();
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
