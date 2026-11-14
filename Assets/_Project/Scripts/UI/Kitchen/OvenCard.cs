using System;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Fırın ekranındaki tek fırın kartı. Yalnızca görünümdür; servis çağrılarını OvenPanel yapar.
    /// Görünümler:
    /// - Boş: "Pişir" (tarif seçici açılır).
    /// - Pişiyor: tarif adı, dk:sn geri sayım, ilerleme çubuğu, "Reklam izle, hemen bitir".
    /// - Hazır: parlayan, nabız atan "Topla".
    /// - Kilitli: soluk kart, "Geliştirmeyle aç".
    /// Parlama gerçek zamanla (Time.unscaledTime) akar.
    /// </summary>
    public class OvenCard : MonoBehaviour
    {
        /// <summary>Kartın gösterdiği durum.</summary>
        public enum View
        {
            None = 0,
            Locked = 1,
            Empty = 2,
            Baking = 3,
            Ready = 4
        }

        private const string KeyName = "kitchen.oven.name";
        private const string KeyEmpty = "kitchen.oven.empty";
        private const string KeyReady = "kitchen.oven.ready";
        private const string KeyLocked = "kitchen.oven.locked";
        private const string KeySpeedUp = "kitchen.oven.speed_up";
        private const string KeyAdPlaying = "kitchen.oven.ad_playing";

        [SerializeField] private OvenPanelSettings settings;

        [Tooltip("Kartın zemini; rengi duruma göre değişir.")]
        [SerializeField] private Image background;

        [Tooltip("Kilitli görünümde saydamlığı düşürülür.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private LocalizedText titleText;
        [SerializeField] private LocalizedText statusText;

        [Tooltip("Tarifin simgesi; boş ve kilitli fırında gizlenir. Dilimler buradan uçar.")]
        [SerializeField] private Image icon;

        [Header("Pişiyor")]
        [Tooltip("Geri sayım + ilerleme çubuğu.")]
        [SerializeField] private GameObject bakingGroup;

        [SerializeField] private TMP_Text countdownText;

        [Tooltip("İlerleme çubuğunun dolgusu; genişliği anchorMax.x ile ayarlanır.")]
        [SerializeField] private RectTransform progressFill;

        [Header("Butonlar")]
        [SerializeField] private Button bakeButton;
        [SerializeField] private Button adButton;
        [SerializeField] private LocalizedText adLabel;
        [SerializeField] private Button collectButton;

        [Tooltip("Topla butonunun arkasındaki parlama.")]
        [SerializeField] private Image collectGlow;

        [SerializeField] private Button unlockButton;

        /// <summary>Boş fırında "Pişir"e basıldı.</summary>
        public event Action<OvenCard> BakeClicked;

        /// <summary>Pişen fırında reklam butonuna basıldı.</summary>
        public event Action<OvenCard> AdClicked;

        /// <summary>Hazır fırında "Topla"ya basıldı.</summary>
        public event Action<OvenCard> CollectClicked;

        /// <summary>Kilitli fırında "Geliştirmeyle aç"a basıldı.</summary>
        public event Action<OvenCard> UnlockClicked;

        // Prefab'daki yer tutucu simge (tarif simgesi yoksa kullanılır).
        private Sprite placeholderSprite;
        private bool defaultsCaptured;

        private int shownCountdown = -1;
        private bool adPlaying;
        private RecipeData recipe;

        public int Index { get; private set; }

        public View CurrentView { get; private set; } = View.None;

        /// <summary>Kartın gösterdiği tarif (pişiyor/hazır); yoksa null.</summary>
        public RecipeData Recipe => recipe;

        /// <summary>Uçan dilimlerin çıkış noktası.</summary>
        public RectTransform FlyOrigin => icon != null ? icon.rectTransform : (RectTransform)transform;

        private void Awake()
        {
            CaptureDefaults();
            AddListener(bakeButton, HandleBake);
            AddListener(adButton, HandleAd);
            AddListener(collectButton, HandleCollect);
            AddListener(unlockButton, HandleUnlock);
        }

        private void OnDestroy()
        {
            RemoveListener(bakeButton, HandleBake);
            RemoveListener(adButton, HandleAd);
            RemoveListener(collectButton, HandleCollect);
            RemoveListener(unlockButton, HandleUnlock);
        }

        private void Update()
        {
            if (CurrentView == View.Ready)
            {
                AnimateGlow();
            }
        }

        /// <summary>Kartı fırın indeksine bağlar (başlık "Fırın 1" gibi 1'den başlar).</summary>
        public void Bind(int ovenIndex)
        {
            CaptureDefaults();
            Index = ovenIndex;
            KitchenUIUtil.SetKey(titleText, KeyName, ovenIndex + 1);
        }

        // ------------------------------------------------------------------ Görünümler

        public void ShowLocked()
        {
            SetView(View.Locked, null);
            KitchenUIUtil.SetKey(statusText, KeyLocked);
        }

        public void ShowEmpty()
        {
            SetView(View.Empty, null);
            KitchenUIUtil.SetKey(statusText, KeyEmpty);
        }

        public void ShowBaking(RecipeData bakingRecipe)
        {
            SetView(View.Baking, bakingRecipe);
            KitchenUIUtil.SetKey(statusText, bakingRecipe != null ? bakingRecipe.nameKey : KeyEmpty);
        }

        public void ShowReady(RecipeData readyRecipe)
        {
            SetView(View.Ready, readyRecipe);
            KitchenUIUtil.SetKey(statusText, KeyReady);
        }

        /// <summary>Geri sayımı ve ilerleme çubuğunu günceller (yalnızca Pişiyor görünümünde anlamlı).</summary>
        public void SetCountdown(float remainingSeconds, int totalSeconds)
        {
            int seconds = KitchenUIUtil.ToCountdownSeconds(remainingSeconds);
            if (countdownText != null && seconds != shownCountdown)
            {
                shownCountdown = seconds;
                countdownText.text = KitchenUIUtil.FormatCountdown(seconds);
            }

            if (progressFill != null)
            {
                float progress = totalSeconds > 0 ? 1f - Mathf.Clamp01(remainingSeconds / totalSeconds) : 1f;
                Vector2 max = progressFill.anchorMax;
                if (!Mathf.Approximately(max.x, progress))
                {
                    progressFill.anchorMax = new Vector2(progress, max.y);
                }
            }
        }

        /// <summary>
        /// Reklam butonunun durumu. playing: bu fırın için reklam oynuyor (etiket değişir);
        /// interactable: basılabilir mi (reklam hazır ve başka reklam oynamıyor).
        /// </summary>
        public void SetAdState(bool playing, bool interactable)
        {
            if (playing != adPlaying)
            {
                adPlaying = playing;
                KitchenUIUtil.SetKey(adLabel, playing ? KeyAdPlaying : KeySpeedUp);
            }

            KitchenUIUtil.SetInteractable(adButton, interactable && !playing);
        }

        // ------------------------------------------------------------------ İç işler

        private void SetView(View view, RecipeData viewRecipe)
        {
            CaptureDefaults();
            CurrentView = view;
            recipe = viewRecipe;
            shownCountdown = -1;

            bool hasRecipe = view == View.Baking || view == View.Ready;
            KitchenUIUtil.SetActive(icon, hasRecipe);
            if (hasRecipe && settings != null)
            {
                Sprite sprite = viewRecipe != null ? viewRecipe.icon : null;
                if (sprite == null && viewRecipe != null && viewRecipe.product != null)
                {
                    sprite = viewRecipe.product.icon;
                }

                KitchenUIUtil.SetIcon(icon, sprite, placeholderSprite, settings.iconPlaceholderColor);
            }

            KitchenUIUtil.SetActive(bakingGroup, view == View.Baking);
            KitchenUIUtil.SetActive(bakeButton, view == View.Empty);
            KitchenUIUtil.SetActive(adButton, view == View.Baking);
            KitchenUIUtil.SetActive(collectButton, view == View.Ready);
            KitchenUIUtil.SetActive(collectGlow, view == View.Ready);
            KitchenUIUtil.SetActive(unlockButton, view == View.Locked);

            if (view != View.Ready && collectButton != null)
            {
                collectButton.transform.localScale = Vector3.one;
            }

            if (canvasGroup != null && settings != null)
            {
                canvasGroup.alpha = view == View.Locked ? settings.lockedAlpha : 1f;
            }

            if (background != null && settings != null)
            {
                background.color = BackgroundColor(view);
            }

            if (view == View.Baking)
            {
                adPlaying = false;
                KitchenUIUtil.SetKey(adLabel, KeySpeedUp);
            }
        }

        private Color BackgroundColor(View view)
        {
            switch (view)
            {
                case View.Locked:
                    return settings.lockedCardColor;
                case View.Baking:
                    return settings.bakingCardColor;
                case View.Ready:
                    return settings.readyCardColor;
                default:
                    return settings.emptyCardColor;
            }
        }

        // Parlama ve Topla butonu aynı nabızla atar: 0..1 arası sinüs.
        private void AnimateGlow()
        {
            if (settings == null)
            {
                return;
            }

            float wave = (Mathf.Sin(Time.unscaledTime * settings.glowPulsesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f;

            if (collectGlow != null)
            {
                Color color = settings.glowColor;
                color.a = Mathf.Lerp(settings.glowMinAlpha, settings.glowMaxAlpha, wave);
                collectGlow.color = color;
            }

            if (collectButton != null)
            {
                collectButton.transform.localScale = Vector3.one * Mathf.Lerp(1f, settings.collectPulseScale, wave);
            }
        }

        private void CaptureDefaults()
        {
            if (defaultsCaptured)
            {
                return;
            }

            defaultsCaptured = true;
            if (icon != null)
            {
                placeholderSprite = icon.sprite;
            }
        }

        private void HandleBake() => BakeClicked?.Invoke(this);

        private void HandleAd() => AdClicked?.Invoke(this);

        private void HandleCollect() => CollectClicked?.Invoke(this);

        private void HandleUnlock() => UnlockClicked?.Invoke(this);

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
