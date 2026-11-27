using System;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Pasta Dolabı'ndaki tek tatlı: görsel, ad, dilim kutucukları ve durum yazısı.
    /// Durumlar: Normal ("{0} dilim"), Az kaldı (eşik ve altı), Bitti (0, görsel soluk),
    /// Kilitli (görsel soluk + kilit, "{0}. seviyede açılır"; seçilemez).
    /// Kartın tamamı butondur; dokununca Clicked yayınlanır, seçimi CakeCabinetPanel yönetir.
    /// </summary>
    public class CakeCabinetCard : MonoBehaviour
    {
        public enum State
        {
            Normal,
            Low,
            Out,
            Locked
        }

        private const string KeySlices = "kitchen.cabinet.slices";
        private const string KeyLow = "kitchen.cabinet.low";
        private const string KeyOut = "kitchen.cabinet.out";
        private const string KeyLocked = "kitchen.cabinet.locked";

        [SerializeField] private CakeCabinetSettings settings;
        [SerializeField] private Button button;
        [SerializeField] private Image background;

        [Tooltip("Pasta görseli; RecipeData.icon (yoksa ürünün simgesi) boşsa yer tutucu kutu kalır.")]
        [SerializeField] private Image cakeImage;

        [SerializeField] private Image lockIcon;
        [SerializeField] private LocalizedText nameText;
        [SerializeField] private LocalizedText statusText;

        [Tooltip("Soldan sağa dilim kutucukları (CakeCabinetSettings.sliceTileCount kadar).")]
        [SerializeField] private Image[] sliceTiles;

        [Tooltip("Seçilince açılan altın çerçeve (nesne açık, bileşen kapalı kaydedilir).")]
        [SerializeField] private Image selectionFrame;

        [Tooltip("Seçilince görünen yeşil ✓ rozeti.")]
        [SerializeField] private GameObject checkBadge;

        /// <summary>Karta dokunuldu (kilitliyse de yayınlanır; panel yok sayar).</summary>
        public event Action<CakeCabinetCard> Clicked;

        private RecipeData recipe;
        private TMP_Text statusLabel;
        private Sprite placeholderSprite;
        private bool hasCakeSprite;
        private bool defaultsCaptured;

        public RecipeData Recipe => recipe;

        public string ProductId => recipe != null && recipe.product != null ? recipe.product.id : null;

        public State CurrentState { get; private set; } = State.Normal;

        public bool IsLocked => CurrentState == State.Locked;

        public bool IsSelected { get; private set; }

        private void Awake()
        {
            CaptureDefaults();
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Bind(RecipeData data)
        {
            CaptureDefaults();
            recipe = data;
            if (recipe == null)
            {
                return;
            }

            Sprite sprite = recipe.icon != null ? recipe.icon : recipe.product != null ? recipe.product.icon : null;
            hasCakeSprite = sprite != null;
            if (settings != null)
            {
                KitchenUIUtil.SetIcon(cakeImage, sprite, placeholderSprite, settings.cakePlaceholderColor);
            }

            KitchenUIUtil.SetKey(nameText, recipe.nameKey);
        }

        /// <summary>Dilim sayısını ve kilidi uygular. lockLevel 0: açık; 0'dan büyükse açılacağı seviye.</summary>
        public void SetState(int slices, int lockLevel)
        {
            if (settings == null)
            {
                return;
            }

            slices = Mathf.Max(0, slices);
            State state = lockLevel > 0 ? State.Locked
                : slices == 0 ? State.Out
                : slices <= settings.lowStockThreshold ? State.Low
                : State.Normal;
            CurrentState = state;

            bool locked = state == State.Locked;
            ApplyTiles(slices, locked);
            ApplyStatus(state, slices, lockLevel);

            if (background != null)
            {
                background.color = locked ? settings.lockedColor : settings.cardColor;
            }

            ApplyCakeAlpha(locked || state == State.Out);
            KitchenUIUtil.SetActive(lockIcon, locked);

            if (locked)
            {
                SetSelected(false);
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected && !IsLocked;
            if (selectionFrame != null)
            {
                selectionFrame.enabled = IsSelected;
            }

            KitchenUIUtil.SetActive(checkBadge, IsSelected);
        }

        // Kutucuklar: ilk 'slices' tanesi dolu. Kilitliyken dilim satırı gizlenir.
        private void ApplyTiles(int slices, bool locked)
        {
            if (sliceTiles == null)
            {
                return;
            }

            int count = Mathf.Min(settings.sliceTileCount, sliceTiles.Length);
            for (int i = 0; i < sliceTiles.Length; i++)
            {
                Image tile = sliceTiles[i];
                if (tile == null)
                {
                    continue;
                }

                bool visible = !locked && i < count;
                KitchenUIUtil.SetActive(tile, visible);
                if (visible)
                {
                    tile.color = i < slices ? settings.sliceFilledColor : settings.sliceEmptyColor;
                }
            }
        }

        private void ApplyStatus(State state, int slices, int lockLevel)
        {
            switch (state)
            {
                case State.Locked:
                    KitchenUIUtil.SetKey(statusText, KeyLocked, lockLevel);
                    SetStatusColor(settings.textColor);
                    break;
                case State.Out:
                    KitchenUIUtil.SetKey(statusText, KeyOut);
                    SetStatusColor(settings.outOfStockColor);
                    break;
                case State.Low:
                    KitchenUIUtil.SetKey(statusText, KeyLow);
                    SetStatusColor(settings.lowStockColor);
                    break;
                default:
                    KitchenUIUtil.SetKey(statusText, KeySlices, slices);
                    SetStatusColor(settings.textColor);
                    break;
            }
        }

        private void SetStatusColor(Color color)
        {
            if (statusLabel != null)
            {
                statusLabel.color = color;
            }
        }

        // Gerçek sprite beyaz, yer tutucu kendi rengiyle çizilir; soluklukta yalnızca saydamlık değişir.
        private void ApplyCakeAlpha(bool dimmed)
        {
            if (cakeImage == null)
            {
                return;
            }

            Color color = hasCakeSprite ? Color.white : settings.cakePlaceholderColor;
            color.a *= dimmed ? settings.dimmedAlpha : 1f;
            cakeImage.color = color;
        }

        private void HandleClicked() => Clicked?.Invoke(this);

        private void CaptureDefaults()
        {
            if (defaultsCaptured)
            {
                return;
            }

            defaultsCaptured = true;
            if (cakeImage != null)
            {
                placeholderSprite = cakeImage.sprite;
            }

            if (statusText != null)
            {
                statusLabel = statusText.GetComponent<TMP_Text>();
            }
        }
    }
}
