using System.Collections;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Stok şeridindeki tek pasta: simge, ad ve kalan dilim sayısı.
    /// Topla animasyonu sürerken sayı StockChanged ile değil, inen dilimlerle artar; son dilim inince gerçek stoğa eşitlenir.
    /// </summary>
    public class StockEntry : MonoBehaviour
    {
        [SerializeField] private OvenPanelSettings settings;

        [Tooltip("Ürünün simgesi; dilimler buraya iner ve iniş anında büyüyüp küçülür.")]
        [SerializeField] private Image icon;

        [SerializeField] private LocalizedText nameText;
        [SerializeField] private TMP_Text amountText;

        private ProductData product;
        private Sprite placeholderSprite;
        private bool defaultsCaptured;

        private int shownAmount = -1;

        // Bu ürüne doğru uçan parti sayısı; 0 değilse StockChanged sayıyı değiştirmez.
        private int pendingFlights;

        private Coroutine popRoutine;

        public ProductData Product => product;

        /// <summary>Dilimlerin indiği nokta.</summary>
        public RectTransform FlyTarget => icon != null ? icon.rectTransform : (RectTransform)transform;

        private void Awake() => CaptureDefaults();

        private void OnDisable()
        {
            // Kapanırken yarıda kalan büyüme sıfırlanır.
            popRoutine = null;
            if (icon != null)
            {
                icon.rectTransform.localScale = Vector3.one;
            }
        }

        public void Bind(ProductData boundProduct)
        {
            CaptureDefaults();
            product = boundProduct;
            if (product == null)
            {
                return;
            }

            if (settings != null)
            {
                KitchenUIUtil.SetIcon(icon, product.icon, placeholderSprite, settings.iconPlaceholderColor);
            }

            KitchenUIUtil.SetKey(nameText, product.nameKey);
        }

        /// <summary>Stok değişti (StockChanged); uçuş sürüyorsa yok sayılır.</summary>
        public void SetAmount(int amount)
        {
            if (pendingFlights == 0)
            {
                Show(amount);
            }
        }

        /// <summary>Bir parti bu kaleme uçmaya başlayacak; Collect'ten önce çağrılır ki StockChanged sayıyı atlatmasın.</summary>
        public void BeginFlight() => pendingFlights++;

        /// <summary>Bir dilim simgesi indi: sayı amount kadar artar, simge büyüyüp küçülür.</summary>
        public void Land(int amount)
        {
            Show(Mathf.Max(0, shownAmount) + amount);
            Pop();
        }

        /// <summary>Parti bitti (ya da iptal edildi); son parti bitince sayı gerçek stoğa eşitlenir.</summary>
        public void EndFlight(int actualAmount)
        {
            pendingFlights = Mathf.Max(0, pendingFlights - 1);
            if (pendingFlights == 0)
            {
                Show(actualAmount);
            }
        }

        /// <summary>Tüm uçuşları unutur ve gerçek stoğu gösterir (panel gizlenince/yenilenince).</summary>
        public void ResetFlights(int actualAmount)
        {
            pendingFlights = 0;
            Show(actualAmount);
        }

        private void Show(int amount)
        {
            if (amount == shownAmount)
            {
                return;
            }

            shownAmount = amount;
            if (amountText != null)
            {
                amountText.SetText("{0}", amount);
            }
        }

        private void Pop()
        {
            if (icon == null || settings == null || !isActiveAndEnabled)
            {
                return;
            }

            if (popRoutine != null)
            {
                StopCoroutine(popRoutine);
            }

            popRoutine = StartCoroutine(PopRoutine());
        }

        // Büyüyüp yerine oturur: ilk yarıda landPopScale'e çıkar, ikinci yarıda 1'e iner.
        private IEnumerator PopRoutine()
        {
            Transform target = icon.rectTransform;
            float duration = settings.landPopDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = 1f - Mathf.Abs(t * 2f - 1f);
                target.localScale = Vector3.one * Mathf.Lerp(1f, settings.landPopScale, wave);
                yield return null;
            }

            target.localScale = Vector3.one;
            popRoutine = null;
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
