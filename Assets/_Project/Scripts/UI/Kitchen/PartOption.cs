using System;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Pasta yapım ekranının altındaki tek seçenek (Prefabs/UI/Kitchen/PartOption.prefab): taban, krema rengi, süs ya da kutu.
    /// Görsel: parçanın sprite'ı varsa o, yoksa yer tutucu şekil parçanın rengiyle (kutu için kare, diğerleri için daire).
    /// Taban/krema/kutuda dokununca Clicked yayınlanır; süs adımında aynı karttaki DecorationDragSource sürüklemeyi başlatır.
    /// </summary>
    public class PartOption : MonoBehaviour
    {
        [SerializeField] private OrderUISettings settings;
        [SerializeField] private Button button;

        [Tooltip("Seçili olunca rengi değişen çerçeve.")]
        [SerializeField] private Image frame;

        [Tooltip("Parçanın görseli; prefab'daki sprite daire yer tutucudur.")]
        [SerializeField] private Image swatch;

        [Tooltip("Kutu parçalarının yer tutucu şekli (yuvarlak köşeli kare).")]
        [SerializeField] private Sprite boxPlaceholder;

        [SerializeField] private LocalizedText nameText;

        /// <summary>Karta dokunuldu.</summary>
        public event Action<PartOption> Clicked;

        private Sprite circlePlaceholder;
        private bool defaultsCaptured;

        /// <summary>Kartın gösterdiği parça.</summary>
        public CakePartData Part { get; private set; }

        /// <summary>Kart kullanılabilir mi (ör. süs sınırı dolunca süsler kapanır).</summary>
        public bool IsAvailable => button == null || button.interactable;

        private void Awake()
        {
            CaptureDefaults();
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        /// <summary>Kartı parçaya bağlar: görsel ve ad.</summary>
        public void Bind(CakePartData part)
        {
            CaptureDefaults();
            Part = part;
            if (part == null)
            {
                return;
            }

            Sprite placeholder = part.type == CakePartType.Box && boxPlaceholder != null ? boxPlaceholder : circlePlaceholder;
            KitchenUIUtil.SetIcon(swatch, part.sprite, placeholder, part.color);
            KitchenUIUtil.SetKey(nameText, part.nameKey);
        }

        public void SetSelected(bool selected)
        {
            if (frame != null && settings != null)
            {
                frame.color = selected ? settings.optionSelectedFrameColor : settings.optionFrameColor;
            }
        }

        public void SetAvailable(bool available) => KitchenUIUtil.SetInteractable(button, available);

        private void HandleClick()
        {
            if (Part != null)
            {
                Clicked?.Invoke(this);
            }
        }

        private void CaptureDefaults()
        {
            if (defaultsCaptured)
            {
                return;
            }

            defaultsCaptured = true;
            if (swatch != null)
            {
                circlePlaceholder = swatch.sprite;
            }
        }
    }
}
