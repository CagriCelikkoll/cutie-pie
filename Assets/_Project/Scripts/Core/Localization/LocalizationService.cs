using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core.Localization
{
    /// <summary>
    /// Anahtarla oyuncuya görünen metni getirir. Erişim: GameManager.Instance.Localization.
    /// Oyuncunun seçtiği dil (SetLanguage) kayda yazılır. Açılışta GameManager ApplyStartupLanguage ile dili seçer:
    /// kayıtlı dil → cihaz dili (LanguageFile.systemLanguage) → GameConfig.deviceFallbackLanguage → GameConfig.defaultLanguage.
    /// </summary>
    public class LocalizationService
    {
        private readonly LanguageFile[] _languages;
        private readonly Dictionary<string, string> _table = new Dictionary<string, string>();
        private readonly HashSet<string> _reportedMissing = new HashSet<string>();

        /// <summary>Dil değişince yayınlanır; LocalizedText'ler kendini yeniler.</summary>
        public event Action LanguageChanged;

        /// <summary>Etkin dil kodu.</summary>
        public string CurrentLanguage { get; private set; }

        public LocalizationService(LanguageFile[] languages, string defaultLanguage)
        {
            _languages = languages ?? Array.Empty<LanguageFile>();
            // Kayıt henüz yüklenmedi; geçici dil uygulanır, kaydedilmez. Asıl dil ApplyStartupLanguage ile gelir.
            ApplyLanguage(defaultLanguage);
        }

        /// <summary>
        /// Oyuncunun dil seçimi: dili uygular ve kayda yazar. Bulunamazsa uyarı verir, mevcut dil kalır, kaydedilmez.
        /// </summary>
        public bool SetLanguage(string code)
        {
            if (!ApplyLanguage(code))
            {
                return false;
            }

            // Kayıt yoksa (ör. Boot'suz test sahnesi) seçim yalnızca bu oturumda geçerlidir.
            SaveSystem save = SaveSystem.Instance;
            if (save != null)
            {
                save.SetLanguageCode(code);
            }

            return true;
        }

        /// <summary>
        /// Açılış dilini seçip uygular; kaydetmez (cihaz diline göre seçilen dil kayda yazılmaz).
        /// Kayıt yüklendikten sonra GameManager çağırır.
        /// </summary>
        public void ApplyStartupLanguage(string savedCode, SystemLanguage systemLanguage, string deviceFallbackLanguage,
            string defaultLanguage)
        {
            string code = ResolveStartupLanguage(_languages, savedCode, systemLanguage, deviceFallbackLanguage,
                defaultLanguage);
            if (code != null && code != CurrentLanguage)
            {
                ApplyLanguage(code);
            }
        }

        /// <summary>
        /// Açılış dili kuralı: kayıtlı dil → systemLanguage'ı cihaz diliyle eşleşen dil → deviceFallbackLanguage →
        /// defaultLanguage. Yalnızca listede olan ve dosyası atanmış diller seçilir; hiçbiri yoksa null.
        /// </summary>
        public static string ResolveStartupLanguage(LanguageFile[] languages, string savedCode,
            SystemLanguage systemLanguage, string deviceFallbackLanguage, string defaultLanguage)
        {
            if (languages == null)
            {
                return null;
            }

            if (IsAvailable(languages, savedCode))
            {
                return savedCode;
            }

            if (systemLanguage != SystemLanguage.Unknown)
            {
                LanguageFile device = Array.Find(languages,
                    l => l != null && l.HasFiles && l.systemLanguage == systemLanguage);
                if (device != null && !string.IsNullOrEmpty(device.code))
                {
                    return device.code;
                }
            }

            if (IsAvailable(languages, deviceFallbackLanguage))
            {
                return deviceFallbackLanguage;
            }

            return IsAvailable(languages, defaultLanguage) ? defaultLanguage : null;
        }

        private static bool IsAvailable(LanguageFile[] languages, string code) =>
            !string.IsNullOrEmpty(code) && Array.Exists(languages, l => l != null && l.HasFiles && l.code == code);

        // Dili uygular ve LanguageChanged yayınlar; kayda dokunmaz.
        private bool ApplyLanguage(string code)
        {
            LanguageFile language = Array.Find(_languages, l => l != null && l.code == code);
            if (language == null || !language.HasFiles)
            {
                Debug.LogWarning($"[Localization] '{code}' dili bulunamadı.");
                return false;
            }

            // Önce tüm dosyalar okunur; biri bozuksa dil değiştirilmez, mevcut dil kalır.
            var merged = new Dictionary<string, string>();
            var sources = new Dictionary<string, string>();
            foreach (TextAsset file in language.files)
            {
                if (file == null)
                {
                    continue;
                }

                LocalizationTable parsed;
                try
                {
                    parsed = JsonUtility.FromJson<LocalizationTable>(file.text);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Localization] '{code}' dil dosyası okunamadı ({file.name}): {e.Message}");
                    return false;
                }

                if (parsed?.entries == null)
                {
                    continue;
                }

                foreach (LocalizationEntry entry in parsed.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.key))
                    {
                        continue;
                    }

                    // Bir anahtar yalnızca bir dosyada olmalı; çakışmada ilk dosyadaki değer kalır.
                    if (sources.TryGetValue(entry.key, out string firstFile))
                    {
                        Debug.LogWarning($"[Localization] '{code}' dilinde '{entry.key}' anahtarı iki dosyada var " +
                                         $"({firstFile}, {file.name}); {firstFile} kullanılıyor.");
                        continue;
                    }

                    merged[entry.key] = entry.value;
                    sources[entry.key] = file.name;
                }
            }

            _table.Clear();
            _reportedMissing.Clear();
            foreach (KeyValuePair<string, string> pair in merged)
            {
                _table[pair.Key] = pair.Value;
            }

            CurrentLanguage = code;
            LanguageChanged?.Invoke();
            return true;
        }

        /// <summary>Anahtarın metni; yoksa anahtarın kendisi döner ve bir kez uyarı yazılır.</summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_table.TryGetValue(key, out string value))
            {
                return value;
            }

            if (_reportedMissing.Add(key))
            {
                Debug.LogWarning($"[Localization] '{CurrentLanguage}' dilinde '{key}' anahtarı yok.");
            }

            return key;
        }

        /// <summary>Anahtarın metnini string.Format ile biçimlendirir (ör. "Kazanç: {0}").</summary>
        public string Get(string key, params object[] args)
        {
            string format = Get(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[Localization] '{key}' metni biçimlendirilemedi.");
                return format;
            }
        }
    }

    /// <summary>Dil dosyasının JSON modeli (JsonUtility sözlük desteklemediği için liste).</summary>
    [Serializable]
    public class LocalizationTable
    {
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }

    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }
}
