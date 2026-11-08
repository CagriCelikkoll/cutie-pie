using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core.Localization
{
    /// <summary>
    /// Anahtarla oyuncuya görünen metni getirir. Erişim: GameManager.Instance.Localization.
    /// Dil şimdilik kaydedilmez; açılışta GameConfig.defaultLanguage kullanılır.
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
            SetLanguage(defaultLanguage);
        }

        /// <summary>Etkin dili değiştirir. Bulunamazsa uyarı verir, mevcut dil kalır.</summary>
        public bool SetLanguage(string code)
        {
            LanguageFile language = Array.Find(_languages, l => l != null && l.code == code);
            if (language == null || language.file == null)
            {
                Debug.LogWarning($"[Localization] '{code}' dili bulunamadı.");
                return false;
            }

            LocalizationTable parsed;
            try
            {
                parsed = JsonUtility.FromJson<LocalizationTable>(language.file.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Localization] '{code}' dil dosyası okunamadı: {e.Message}");
                return false;
            }

            _table.Clear();
            _reportedMissing.Clear();
            if (parsed?.entries != null)
            {
                foreach (LocalizationEntry entry in parsed.entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                    {
                        _table[entry.key] = entry.value;
                    }
                }
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
