using System;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Tezgâh tepsisindeki tek malzeme: görsel, ad, miktar. Sürüklenebilir (uGUI EventSystem; Input System paketi gerekmez).
    /// Kendisi hiçbir şey taşımaz: sürükleme olaylarını yayınlar, CounterPanel sürüklenen görseli yürütür ve kaseye düşüp
    /// düşmediğine karar verir. Kök GridLayoutGroup'a bağlı olduğu için yükselme ve parlama iç gövdede (body) yapılır.
    /// Durumlar: normal, sıradaki (parlar + hafifçe yukarı kalkık), sürükleniyor (görsel soluk), eklendi (✓ + soluk; sürüklenmez).
    /// </summary>
    public class IngredientCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CounterSettings settings;

        [Tooltip("Yükselen / parlayan iç gövde (zemin, görsel, yazılar).")]
        [SerializeField] private RectTransform body;

        [SerializeField] private Image background;

        [Tooltip("Malzeme görseli; IngredientData.icon boşsa malzeme renginde yer tutucu kutu kalır.")]
        [SerializeField] private Image icon;

        [SerializeField] private LocalizedText nameText;
        [SerializeField] private LocalizedText amountText;
        [SerializeField] private GameObject checkBadge;

        [Tooltip("Sıradaki malzemenin altın çerçevesi (nesne açık, bileşen kapalı kaydedilir).")]
        [SerializeField] private Image glow;

        public event Action<IngredientCard, PointerEventData> DragStarted;
        public event Action<IngredientCard, PointerEventData> DragMoved;
        public event Action<IngredientCard, PointerEventData> DragEnded;

        private IngredientEntry entry;
        private Sprite placeholderSprite;
        private TMP_Text nameLabel;
        private TMP_Text amountLabel;
        private bool defaultsCaptured;
        private bool suggested;
        private bool dragging;
        private float lift;

        public IngredientEntry Entry => entry;

        public IngredientData Ingredient => entry != null ? entry.ingredient : null;

        public bool IsAdded { get; private set; }

        /// <summary>Sürüklenen görselin kopyalanacağı ve geri döneceği görsel.</summary>
        public Image Icon => icon;

        private void Awake() => CaptureDefaults();

        private void OnDisable()
        {
            // Panel kapanırken yarıdaki sürükleme unutulur.
            dragging = false;
            ApplyLook();
        }

        // Sıradaki kart yükselir ve nabız gibi parlar; diğerleri yerine iner. Gerçek zamanla akar.
        private void Update()
        {
            if (settings == null || body == null)
            {
                return;
            }

            float target = suggested ? settings.suggestedLift : 0f;
            lift = Mathf.Lerp(lift, target, 1f - Mathf.Exp(-settings.liftSpeed * Time.unscaledDeltaTime));
            if (Mathf.Abs(lift - target) < 0.05f)
            {
                lift = target;
            }

            body.anchoredPosition = new Vector2(0f, lift);

            if (glow != null && glow.enabled)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * settings.suggestedPulsesPerSecond * Mathf.PI * 2f);
                Color color = settings.goldColor;
                color.a = Mathf.Lerp(settings.glowMinAlpha, settings.glowMaxAlpha, wave);
                glow.color = color;
            }
        }

        public void Bind(IngredientEntry data)
        {
            CaptureDefaults();
            entry = data;
            IsAdded = false;
            suggested = false;
            dragging = false;
            lift = 0f;

            IngredientData ingredient = Ingredient;
            if (ingredient != null)
            {
                KitchenUIUtil.SetKey(nameText, ingredient.nameKey);
            }

            KitchenUIUtil.SetKey(amountText, entry != null ? entry.amountKey : string.Empty);
            ApplyLook();
        }

        public void SetSuggested(bool value)
        {
            suggested = value && !IsAdded;
            ApplyLook();
        }

        public void MarkAdded()
        {
            IsAdded = true;
            suggested = false;
            dragging = false;
            ApplyLook();
        }

        /// <summary>Sürükleme sürerken kartta soluk bir görsel kalır.</summary>
        public void SetDragging(bool value)
        {
            dragging = value;
            ApplyLook();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !IsAdded && Ingredient != null)
            {
                DragStarted?.Invoke(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData) => DragMoved?.Invoke(this, eventData);

        public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke(this, eventData);

        private void ApplyLook()
        {
            if (settings == null)
            {
                return;
            }

            IngredientData ingredient = Ingredient;
            Sprite sprite = ingredient != null ? ingredient.icon : null;
            Color tint = ingredient != null ? ingredient.bowlTint : Color.white;
            KitchenUIUtil.SetIcon(icon, sprite, placeholderSprite, tint);

            float alpha = IsAdded ? settings.addedAlpha : dragging ? settings.draggingAlpha : 1f;
            if (icon != null)
            {
                Color color = icon.color;
                color.a *= alpha;
                icon.color = color;
            }

            float textAlpha = IsAdded ? settings.addedAlpha : 1f;
            SetAlpha(nameLabel, textAlpha);
            SetAlpha(amountLabel, textAlpha);

            if (background != null)
            {
                background.color = IsAdded ? settings.cardAddedColor : settings.cardColor;
            }

            if (glow != null)
            {
                glow.enabled = suggested;
            }

            KitchenUIUtil.SetActive(checkBadge, IsAdded);
        }

        private static void SetAlpha(TMP_Text text, float alpha)
        {
            if (text != null)
            {
                Color color = text.color;
                color.a = alpha;
                text.color = color;
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

            nameLabel = nameText != null ? nameText.GetComponent<TMP_Text>() : null;
            amountLabel = amountText != null ? amountText.GetComponent<TMP_Text>() : null;
        }
    }
}
