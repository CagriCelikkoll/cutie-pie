using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using CutiePie.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Geliştirme mağazasındaki tek kart: simge, ad, kısa açıklama, seviye (ör. 1/3), fiyat ve Satın al butonu.
    /// Tüm değerler GameManager.Instance.Upgrades'ten okunur; satın alma TryPurchase ile yapılır.
    /// Altın yetmiyorsa buton pasiftir; en üst seviyede buton "Tam" yazar ve fiyat gizlenir.
    /// Yenileme UpgradesPanel'den gelir (CurrencyChanged / UpgradePurchased).
    /// </summary>
    public class UpgradeCard : MonoBehaviour
    {
        // Yerelleştirme anahtarları; metinler Localization/tr.json ve en.json'dadır.
        private const string KeyLevel = "upgrades.level";
        private const string KeyBuy = "upgrades.buy";
        private const string KeyMaxed = "upgrades.maxed";

        // UpgradeData'da açıklama alanı yok; anahtar id'den kuralla üretilir: upgrade.<id>.desc
        private const string DescriptionKeyFormat = "upgrade.{0}.desc";

        [SerializeField] private RoundEndSettings settings;

        [Tooltip("Geliştirmenin simgesi; UpgradeData.icon boşsa prefab'daki yer tutucu kutu kalır.")]
        [SerializeField] private Image icon;

        [SerializeField] private LocalizedText nameText;
        [SerializeField] private LocalizedText descriptionText;
        [SerializeField] private LocalizedText levelText;

        [Tooltip("Altın simgesi + fiyat; en üst seviyede gizlenir.")]
        [SerializeField] private GameObject priceRow;

        [SerializeField] private TMP_Text priceValue;
        [SerializeField] private Button buyButton;
        [SerializeField] private LocalizedText buyLabel;

        private UpgradeData data;

        // Prefab'daki yer tutucu görünüm ve buton tonları; Bind ve Refresh bunlara geri döner.
        private bool defaultsCaptured;
        private Sprite placeholderSprite;
        private Image.Type placeholderType;
        private Color placeholderColor;
        private ColorBlock buyColors;
        private Image buyImage;

        public UpgradeData Data => data;

        private void Awake()
        {
            CaptureDefaults();
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(HandleBuyClicked);
            }
        }

        private void OnDestroy()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
            }
        }

        /// <summary>Kartı bir geliştirmeye bağlar ve hemen yeniler.</summary>
        public void Bind(UpgradeData upgrade)
        {
            CaptureDefaults();
            data = upgrade;
            if (data == null)
            {
                return;
            }

            SetIcon(data.icon);
            SetKey(nameText, data.nameKey);
            SetKey(descriptionText, string.Format(DescriptionKeyFormat, data.id));
            Refresh();
        }

        /// <summary>Seviye, fiyat ve buton durumunu güncel değerlerle yeniler.</summary>
        public void Refresh()
        {
            UpgradeManager upgrades = Upgrades;
            if (data == null || upgrades == null)
            {
                SetBuyInteractable(false);
                return;
            }

            int level = upgrades.GetLevel(data.id);
            int maxLevel = upgrades.GetMaxLevel(data.id);
            int price = upgrades.GetNextPrice(data.id);
            bool maxed = price == UpgradeManager.NoPrice;

            SetKey(levelText, KeyLevel, level, maxLevel);

            if (priceRow != null)
            {
                priceRow.SetActive(!maxed);
            }

            if (!maxed && priceValue != null)
            {
                priceValue.SetText("{0}", price);
            }

            SetKey(buyLabel, maxed ? KeyMaxed : KeyBuy);
            ApplyBuyLook(maxed);
            SetBuyInteractable(!maxed && upgrades.CanPurchase(data.id));
        }

        private void HandleBuyClicked()
        {
            UpgradeManager upgrades = Upgrades;
            if (data == null || upgrades == null)
            {
                return;
            }

            // Başarılı satın almada panel olaylarla tüm kartları yeniler; başarısızsa yalnızca bu kart güncellenir.
            if (!upgrades.TryPurchase(data.id))
            {
                Refresh();
            }
        }

        private static UpgradeManager Upgrades =>
            GameManager.Instance != null ? GameManager.Instance.Upgrades : null;

        // ------------------------------------------------------------------ Görünüm

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
                placeholderType = icon.type;
                placeholderColor = icon.color;
            }

            if (buyButton != null)
            {
                buyColors = buyButton.colors;
                buyImage = buyButton.targetGraphic as Image;
            }
        }

        private void SetIcon(Sprite sprite)
        {
            if (icon == null)
            {
                return;
            }

            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.color = Color.white;
            }
            else
            {
                icon.sprite = placeholderSprite;
                icon.type = placeholderType;
                icon.preserveAspect = false;
                icon.color = placeholderColor;
            }
        }

        // "Tam" butonu pasiftir ama soluk görünmez; altın yetmeyen "Satın al" butonu soluk görünür.
        private void ApplyBuyLook(bool maxed)
        {
            if (buyButton == null)
            {
                return;
            }

            if (buyImage != null && settings != null)
            {
                buyImage.color = maxed ? settings.maxedButtonColor : settings.buyButtonColor;
            }

            ColorBlock colors = buyColors;
            if (maxed)
            {
                colors.disabledColor = Color.white;
            }

            buyButton.colors = colors;
        }

        private void SetBuyInteractable(bool interactable)
        {
            if (buyButton != null && buyButton.interactable != interactable)
            {
                buyButton.interactable = interactable;
            }
        }

        private static void SetKey(LocalizedText text, string key, params object[] args)
        {
            if (text != null)
            {
                text.SetKey(key, args);
            }
        }
    }
}
