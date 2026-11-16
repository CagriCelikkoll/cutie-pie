using System;
using CutiePie.Core;
using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Ayarlar paneli. Kendi Canvas'ı olan ayrı prefab'dır (Prefabs/UI/SettingsPanel.prefab):
    /// bir sahne prefab'ı bırakıp bir butonu Open()'a bağlar (ör. Shop'taki Ayarlar butonu).
    /// Dil butonları GameManager.Instance.Localization.SetLanguage çağırır; etkin dilin butonu vurgulanır.
    /// Vurgu LanguageChanged ile yenilenir (dil başka bir yerden değişse de doğru kalır). Dil burada kaydedilmez.
    /// Köşede sürüm numarası (Application.version) yazar. Ses satırı AudioManager gelince eklenecek.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        // Yerelleştirme anahtarı; metin Localization/tr.json ve en.json'dadır ("Sürüm {0}").
        private const string KeyVersion = "settings.version";

        /// <summary>Tek dil butonu; kod GameConfig.languages'teki koddur (ör. tr, en).</summary>
        [Serializable]
        private class LanguageOption
        {
            [Tooltip("GameConfig.languages'teki dil kodu (ör. tr).")]
            public string code;

            public Button button;

            [Tooltip("Rengi seçili / seçili değil durumuna göre değişen buton zemini.")]
            public Image background;

            [Tooltip("Yalnızca etkin dilde görünen çerçeve (butonun arkasında).")]
            public GameObject selectedFrame;
        }

        [SerializeField] private RoundEndSettings settings;

        [Tooltip("Açılıp kapanan içerik (perde + kart).")]
        [SerializeField] private GameObject content;

        [SerializeField] private Button closeButton;

        [Header("Dil")]
        [SerializeField] private LanguageOption[] languages = Array.Empty<LanguageOption>();

        [Header("Sürüm")]
        [Tooltip("\"Sürüm {0}\" metni; {0} yerine Application.version yazılır.")]
        [SerializeField] private LocalizedText versionText;

        // Butonlara eklenen dinleyiciler; OnDestroy'da aynı nesnelerle kaldırılır.
        private UnityAction[] languageHandlers = Array.Empty<UnityAction>();
        private LocalizationService subscribed;

        public bool IsOpen => content != null && content.activeSelf;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            languageHandlers = new UnityAction[languages.Length];
            for (int i = 0; i < languages.Length; i++)
            {
                LanguageOption option = languages[i];
                if (option == null || option.button == null)
                {
                    continue;
                }

                string code = option.code;
                languageHandlers[i] = () => SelectLanguage(code);
                option.button.onClick.AddListener(languageHandlers[i]);
            }

            // Metin, içerik ilk açıldığında LocalizedText.OnEnable ile yazılır; dil değişince kendiliğinden yenilenir.
            if (versionText != null)
            {
                versionText.SetKey(KeyVersion, Application.version);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            for (int i = 0; i < languages.Length && i < languageHandlers.Length; i++)
            {
                if (languages[i] != null && languages[i].button != null && languageHandlers[i] != null)
                {
                    languages[i].button.onClick.RemoveListener(languageHandlers[i]);
                }
            }

            Unsubscribe();
        }

        public void Open()
        {
            if (content == null)
            {
                return;
            }

            content.SetActive(true);
            Subscribe();
            RefreshLanguageButtons();
        }

        public void Close()
        {
            Unsubscribe();
            if (content != null)
            {
                content.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Dil

        private static LocalizationService Service =>
            GameManager.Instance != null ? GameManager.Instance.Localization : null;

        private void SelectLanguage(string code)
        {
            LocalizationService service = Service;
            if (service == null)
            {
                Debug.LogError("[SettingsPanel] GameManager/Localization yok; dil değiştirilemedi.", this);
                return;
            }

            if (code == service.CurrentLanguage)
            {
                return;
            }

            // Başarılı olursa LanguageChanged vurguyu yeniler; olmazsa mevcut dil ve vurgu kalır.
            service.SetLanguage(code);
        }

        private void RefreshLanguageButtons()
        {
            string current = Service?.CurrentLanguage;
            foreach (LanguageOption option in languages)
            {
                if (option == null)
                {
                    continue;
                }

                bool selected = !string.IsNullOrEmpty(current) && option.code == current;
                if (option.background != null && settings != null)
                {
                    option.background.color = selected ? settings.languageSelectedColor : settings.languageIdleColor;
                }

                if (option.selectedFrame != null)
                {
                    option.selectedFrame.SetActive(selected);
                }
            }
        }

        // ------------------------------------------------------------------ Olaylar

        private void Subscribe()
        {
            if (subscribed != null)
            {
                return;
            }

            subscribed = Service;
            if (subscribed == null)
            {
                Debug.LogError("[SettingsPanel] GameManager/Localization yok; dil butonları çalışmayacak.", this);
                return;
            }

            subscribed.LanguageChanged += RefreshLanguageButtons;
        }

        private void Unsubscribe()
        {
            if (subscribed == null)
            {
                return;
            }

            subscribed.LanguageChanged -= RefreshLanguageButtons;
            subscribed = null;
        }
    }
}
