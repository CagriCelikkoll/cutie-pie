using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.Core.Shop
{
    /// <summary>
    /// Shop'taki Mutfak butonunun üstündeki "N pasta hazır" rozeti. Değer GameManager.Instance.Ovens.ReadyCount'tan gelir.
    /// Açılışta toplu sorgulanır (OvenReady açılışta Boot'ta yayınlandığı için Shop onu kaçırır); Shop açıkken
    /// GameEvents.OvenReady ve BatchCollected ile yenilenir. Hazır fırın yoksa hap gizlenir.
    /// Bu bileşen her zaman aktif kalan tutucudadır; yalnızca görsel çocuk (pill) açılıp kapanır.
    /// Hap güvenli alanın (çentik) dışına taşarsa ekranın içine doğru kaydırılır.
    /// </summary>
    public class OvenReadyBadge : MonoBehaviour
    {
        [SerializeField] private OvenReadyBadgeSettings settings;

        [Tooltip("Açılıp kapanan hap: beyaz kenar Image'ı, HorizontalLayoutGroup ve ContentSizeFitter taşır.")]
        [SerializeField] private RectTransform pill;

        [SerializeField] private Image borderImage;

        [Tooltip("Yeşil iç zemin; layout dışıdır, hapa kenar kalınlığı kadar içeriden yayılır.")]
        [SerializeField] private Image backgroundImage;

        [SerializeField] private HorizontalLayoutGroup layoutGroup;

        [SerializeField] private TMP_Text label;

        [SerializeField] private LocalizedText localizedLabel;

        private Vector2 _basePosition;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _clampDirty;
        private LocalizationService _subscribedLocalization;

        /// <summary>Sayıya göre yerelleştirme anahtarı: 1 ise oneKey, değilse otherKey.</summary>
        public static string KeyFor(int count, string oneKey, string otherKey) => count == 1 ? oneKey : otherKey;

        private void Awake() => ApplyStyle();

        private void OnEnable()
        {
            GameEvents.OvenReady += HandleOvenReady;
            GameEvents.BatchCollected += HandleBatchCollected;

            // Dil değişince yazı genişliği değişir; güvenli alan kontrolü yeniden yapılır.
            if (GameManager.Instance != null)
            {
                _subscribedLocalization = GameManager.Instance.Localization;
                _subscribedLocalization.LanguageChanged += MarkClampDirty;
            }

            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OvenReady -= HandleOvenReady;
            GameEvents.BatchCollected -= HandleBatchCollected;

            if (_subscribedLocalization != null)
            {
                _subscribedLocalization.LanguageChanged -= MarkClampDirty;
                _subscribedLocalization = null;
            }
        }

        private void HandleOvenReady(int ovenIndex) => Refresh();

        private void HandleBatchCollected(string productId, int slices) => Refresh();

        private void MarkClampDirty() => _clampDirty = true;

        /// <summary>ReadyCount'u sorgular, hapı gösterir/gizler ve metni günceller.</summary>
        public void Refresh()
        {
            if (pill == null)
            {
                return;
            }

            int count = GameManager.Instance != null && GameManager.Instance.Ovens != null
                ? GameManager.Instance.Ovens.ReadyCount
                : 0;

            bool visible = count > 0 && settings != null;
            pill.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            // Hap önce açılır: LocalizedText ilk açılışta Awake/OnEnable ile metni bu anahtarla doldurur.
            localizedLabel.SetKey(KeyFor(count, settings.oneKey, settings.otherKey), count);
            _clampDirty = true;
        }

        private void LateUpdate()
        {
            if (pill == null || !pill.gameObject.activeSelf)
            {
                return;
            }

            // Cihaz döndürülünce (Landscape Left/Right) çentik diğer yana geçer; güvenli alan değişimi izlenir.
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != _lastSafeArea || screenSize != _lastScreenSize)
            {
                _lastSafeArea = Screen.safeArea;
                _lastScreenSize = screenSize;
                _clampDirty = true;
            }

            if (_clampDirty)
            {
                _clampDirty = false;
                ClampToSafeArea();
            }
        }

        // Görünüm ve yerleşim değerlerini ayarlardan uygular (sahnede değer tutulmaz).
        private void ApplyStyle()
        {
            if (settings == null)
            {
                Debug.LogWarning("[OvenReadyBadge] Ayarlar (OvenReadyBadgeSettings) atanmamış; rozet gösterilmeyecek.");
                return;
            }

            if (pill == null || borderImage == null || backgroundImage == null || layoutGroup == null || label == null ||
                localizedLabel == null)
            {
                Debug.LogWarning("[OvenReadyBadge] Sahne bağlantıları eksik; rozet gösterilmeyecek.");
                settings = null;
                return;
            }

            // Hap butonun sağ üst köşesine bağlanır; sağ kenarı butonun içinde kalır, ortası üst kenarda durur.
            pill.anchorMin = Vector2.one;
            pill.anchorMax = Vector2.one;
            pill.pivot = new Vector2(1f, 0.5f);
            _basePosition = new Vector2(-settings.insetFromButtonRight, settings.offsetFromButtonTop);
            pill.anchoredPosition = _basePosition;
            pill.sizeDelta = new Vector2(pill.sizeDelta.x, settings.height);

            // Kenar kalınlığı + yazı boşluğu kadar iç boşluk; genişliği ContentSizeFitter yazıya göre verir.
            int padding = Mathf.RoundToInt(settings.borderWidth + settings.horizontalPadding);
            layoutGroup.padding = new RectOffset(padding, padding, 0, 0);

            borderImage.color = settings.borderColor;
            backgroundImage.color = settings.backgroundColor;

            RectTransform background = backgroundImage.rectTransform;
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = new Vector2(settings.borderWidth, settings.borderWidth);
            background.offsetMax = new Vector2(-settings.borderWidth, -settings.borderWidth);

            MakePill(borderImage, settings.height);
            MakePill(backgroundImage, settings.height - 2f * settings.borderWidth);

            if (settings.font != null)
            {
                label.font = settings.font;
            }

            label.fontSize = settings.fontSize;
            label.enableAutoSizing = false;
            label.fontStyle = settings.bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = settings.textColor;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            borderImage.raycastTarget = false;
            backgroundImage.raycastTarget = false;
        }

        // Sliced sprite'ın köşe yarıçapını yüksekliğin yarısına eşitler (tam yuvarlak uçlu hap).
        private static void MakePill(Image image, float height)
        {
            if (image.sprite == null || height <= 0f)
            {
                return;
            }

            float borderPixels = image.sprite.border.x;
            float pixelsPerUnit = image.pixelsPerUnit;
            if (borderPixels > 0f && pixelsPerUnit > 0f)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = borderPixels / (height * 0.5f) / pixelsPerUnit;
            }
        }

        // Hap güvenli alan dışına taşıyorsa (ör. 20:9 çentik) ekranın içine doğru kaydırır.
        private void ClampToSafeArea()
        {
            pill.anchoredPosition = _basePosition;
            LayoutRebuilder.ForceRebuildLayoutImmediate(pill);

            Canvas canvas = pill.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Canvas root = canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            float scale = root.scaleFactor > 0f ? root.scaleFactor : 1f;

            var corners = new Vector3[4];
            pill.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);

            Rect safe = Screen.safeArea;
            float margin = settings.safeAreaMargin * scale;
            Vector2 shift = Vector2.zero;

            if (max.x > safe.xMax - margin)
            {
                shift.x = safe.xMax - margin - max.x;
            }
            else if (min.x < safe.xMin + margin)
            {
                shift.x = safe.xMin + margin - min.x;
            }

            if (max.y > safe.yMax - margin)
            {
                shift.y = safe.yMax - margin - max.y;
            }
            else if (min.y < safe.yMin + margin)
            {
                shift.y = safe.yMin + margin - min.y;
            }

            if (shift != Vector2.zero)
            {
                pill.anchoredPosition = _basePosition + shift / scale;
            }
        }
    }
}
