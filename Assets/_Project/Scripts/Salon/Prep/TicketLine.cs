using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>Fişte tek satır: ürün ikonu + tamamlandı tiki. Metin yoktur.</summary>
    public class TicketLine : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private SpriteRenderer tick;

        private Sprite placeholderSprite;
        private bool placeholderCached;

        /// <summary>Satırı ürünle gösterir; ürün yoksa satır gizlenir. Tik kapalı başlar.</summary>
        public void Show(ProductData product, SalonSettings settings)
        {
            gameObject.SetActive(product != null);
            if (product == null)
            {
                return;
            }

            if (icon != null)
            {
                if (!placeholderCached)
                {
                    placeholderSprite = icon.sprite;
                    placeholderCached = true;
                }

                if (product.icon != null)
                {
                    SpriteFitter.Apply(icon, product.icon, Color.white, true);
                }
                else
                {
                    Color color = settings != null ? settings.GetProductPlaceholderColor(product) : Color.white;
                    SpriteFitter.Apply(icon, placeholderSprite, color, false);
                }
            }

            if (tick != null && settings != null)
            {
                tick.color = settings.ticketTickColor;
            }

            SetTicked(false);
        }

        public void SetTicked(bool ticked)
        {
            if (tick != null)
            {
                tick.enabled = ticked;
            }
        }
    }
}
