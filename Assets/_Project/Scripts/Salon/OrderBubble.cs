using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Müşterinin üstündeki sipariş balonu: pasta ve içecek ikonu. Metin yoktur.
    /// Ürünün ikonu yoksa yer tutucu renkli kare gösterilir.
    /// </summary>
    public class OrderBubble : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer cakeIcon;
        [SerializeField] private SpriteRenderer drinkIcon;

        // İkon yokken kullanılacak, prefab'taki yer tutucu sprite'lar.
        private Sprite cakePlaceholder;
        private Sprite drinkPlaceholder;

        private void Awake()
        {
            cakePlaceholder = cakeIcon != null ? cakeIcon.sprite : null;
            drinkPlaceholder = drinkIcon != null ? drinkIcon.sprite : null;
        }

        public void Show(CustomerOrder order, SalonSettings settings)
        {
            gameObject.SetActive(true);
            ApplyIcon(cakeIcon, order?.Cake, cakePlaceholder, settings != null ? settings.cakePlaceholderColor : Color.white);
            ApplyIcon(drinkIcon, order?.Drink, drinkPlaceholder, settings != null ? settings.drinkPlaceholderColor : Color.white);
        }

        public void Hide() => gameObject.SetActive(false);

        private static void ApplyIcon(SpriteRenderer target, ProductData product, Sprite placeholder, Color placeholderColor)
        {
            if (target == null)
            {
                return;
            }

            target.enabled = product != null;
            if (product == null)
            {
                return;
            }

            if (product.icon != null)
            {
                target.sprite = product.icon;
                target.color = Color.white;
            }
            else
            {
                target.sprite = placeholder;
                target.color = placeholderColor;
            }
        }
    }
}
