using TMPro;
using UnityEngine;

namespace CutiePie.Core.Localization
{
    /// <summary>
    /// Bağlı TMP metnini yerelleştirme anahtarından doldurur; dil değişince yeniler.
    /// Inspector'dan key atanır ya da kodla SetKey çağrılır.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _text;
        private object[] _args;
        private LocalizationService _subscribed;

        public string Key => key;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            LocalizationService service = Service;
            if (service != null)
            {
                service.LanguageChanged += Refresh;
                _subscribed = service;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_subscribed != null)
            {
                _subscribed.LanguageChanged -= Refresh;
                _subscribed = null;
            }
        }

        /// <summary>Anahtarı (ve isteğe bağlı biçim argümanlarını) değiştirir ve metni yeniler.</summary>
        public void SetKey(string newKey, params object[] args)
        {
            key = newKey;
            _args = args;
            Refresh();
        }

        private static LocalizationService Service =>
            GameManager.Instance != null ? GameManager.Instance.Localization : null;

        private void Refresh()
        {
            LocalizationService service = Service;
            if (service == null || _text == null)
            {
                return;
            }

            _text.text = _args != null && _args.Length > 0 ? service.Get(key, _args) : service.Get(key);
        }
    }
}
