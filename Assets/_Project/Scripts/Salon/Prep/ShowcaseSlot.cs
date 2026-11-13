using CutiePie.Core.Data;
using TMPro;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Vitrindeki tek pasta yeri (servis adımı 3): tabağa tek dilim verir.
    /// Stoktan yalnızca dilim tepsideki tabağa oturunca düşülür; ıskalanan dilim stoğu değiştirmez.
    /// Kalan dilim sayısı rakamla gösterilir. Stok 0'da yuva sönük görünür, sürüklenmez ve sayının yerine
    /// "Tükendi" etiketi (yerelleştirilmiş, salon.sold_out) çıkar. Sayılar ortak stoğun Changed olayıyla yenilenir.
    /// </summary>
    public class ShowcaseSlot : ShelfSlot
    {
        [SerializeField] private TMP_Text countText;

        [Tooltip("Stok 0 iken görünen \"Tükendi\" etiketi (LocalizedText'li TMP nesnesi).")]
        [SerializeField] private GameObject soldOutLabel;

        private IShowcaseStock stock;

        protected override ServingItemKind ItemKind => ServingItemKind.CakeSlice;

        protected override bool CanTake => Product != null && stock != null && stock.GetSlices(Product) > 0;

        /// <summary>Sürüklemeden tepsideki tabağa dilim koyar (otomasyon girişi).</summary>
        public bool TryPlaceSlice(Tray tray) => TryPlaceOn(tray);

        public override void Bind(ProductData product, IShowcaseStock showcaseStock)
        {
            if (stock != null)
            {
                stock.Changed -= OnStockChanged;
            }

            stock = showcaseStock;
            if (stock != null)
            {
                stock.Changed += OnStockChanged;
            }

            base.Bind(product, showcaseStock);
        }

        protected override bool Commit(ProductData product) => stock != null && stock.TryTakeSlice(product);

        protected override void RefreshState()
        {
            base.RefreshState();

            bool soldOut = Product != null && !CanTake;
            if (soldOutLabel != null && soldOutLabel.activeSelf != soldOut)
            {
                soldOutLabel.SetActive(soldOut);
            }

            if (countText == null)
            {
                return;
            }

            // Tükendi etiketi varsa sayı gizlenir; etiket yoksa (eski prefab) sönük "0" gösterilir.
            countText.enabled = Product != null && (!soldOut || soldOutLabel == null);
            if (!countText.enabled)
            {
                return;
            }

            countText.text = (stock != null ? stock.GetSlices(Product) : 0).ToString();
            Color color = countText.color;
            color.a = soldOut ? SlotEmptyAlpha : 1f;
            countText.color = color;
        }

        private void OnStockChanged(ProductData product)
        {
            if (product == Product)
            {
                RefreshState();
            }
        }

        private void OnDestroy()
        {
            if (stock != null)
            {
                stock.Changed -= OnStockChanged;
            }
        }
    }
}
