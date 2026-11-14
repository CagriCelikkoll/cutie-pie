using System;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Tarif seçicideki tek tarif: simge, ad, pişme süresi ve partideki dilim sayısı.
    /// Kartın tamamı butondur; dokununca Clicked yayınlanır.
    /// </summary>
    public class RecipeOption : MonoBehaviour
    {
        private const string KeySlices = "kitchen.recipe.slices";

        [SerializeField] private OvenPanelSettings settings;
        [SerializeField] private Button button;

        [Tooltip("Tarifin simgesi; RecipeData.icon (yoksa ürünün simgesi) boşsa yer tutucu kutu kalır.")]
        [SerializeField] private Image icon;

        [SerializeField] private LocalizedText nameText;
        [SerializeField] private LocalizedText durationText;
        [SerializeField] private LocalizedText slicesText;

        /// <summary>Bu tarife dokunuldu.</summary>
        public event Action<RecipeData> Clicked;

        private RecipeData data;
        private Sprite placeholderSprite;
        private bool defaultsCaptured;

        public RecipeData Data => data;

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

        public void Bind(RecipeData recipe)
        {
            CaptureDefaults();
            data = recipe;
            if (data == null)
            {
                return;
            }

            Sprite sprite = data.icon != null ? data.icon : data.product != null ? data.product.icon : null;
            if (settings != null)
            {
                KitchenUIUtil.SetIcon(icon, sprite, placeholderSprite, settings.iconPlaceholderColor);
            }

            KitchenUIUtil.SetKey(nameText, data.nameKey);
            KitchenUIUtil.SetDuration(durationText, data.bakeSeconds);
            KitchenUIUtil.SetKey(slicesText, KeySlices, data.slicesPerBatch);
        }

        private void HandleClicked()
        {
            if (data != null)
            {
                Clicked?.Invoke(data);
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
    }
}
