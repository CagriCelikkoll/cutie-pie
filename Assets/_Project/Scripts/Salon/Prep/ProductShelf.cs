using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Vitrin veya içecek rafı: sabit sayıda yuva (sayfa başına SalonSettings.shelfSlotsPerPage; builder o kadar yuva kurar)
    /// ve sol/sağ ok. Yalnızca LevelData.menu'deki bu kategorideki ürünler, menü sırasıyla gösterilir.
    /// Ürün sayısı yuva sayısını aşmazsa tek sayfadır ve oklar gizlenir.
    /// </summary>
    public class ProductShelf : MonoBehaviour
    {
        [SerializeField] private ProductCategory category;
        [SerializeField] private ShelfSlot[] slots = new ShelfSlot[0];
        [SerializeField] private PageArrow prevArrow;
        [SerializeField] private PageArrow nextArrow;

        private readonly List<ProductData> products = new List<ProductData>();
        private IShowcaseStock stock;
        private int page;

        public int PageCount => slots.Length == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt(products.Count / (float)slots.Length));

        public int CurrentPage => page;

        /// <summary>Round başında çağrılır. stock: vitrin için dilim stoğu, içecek rafında null.</summary>
        public void Setup(LevelData level, IShowcaseStock showcaseStock)
        {
            stock = showcaseStock;
            products.Clear();

            if (level != null && level.menu != null)
            {
                foreach (ProductData product in level.menu)
                {
                    if (product != null && product.category == category && !products.Contains(product))
                    {
                        products.Add(product);
                    }
                }
            }

            page = 0;
            ShowPage();
        }

        public void NextPage() => ChangePage(1);

        public void PrevPage() => ChangePage(-1);

        private void ChangePage(int delta)
        {
            int count = PageCount;
            if (count <= 1)
            {
                return;
            }

            // Uçlarda başa/sona sarar.
            page = (page + delta + count) % count;
            ShowPage();
        }

        private void ShowPage()
        {
            int perPage = slots.Length;
            for (int i = 0; i < perPage; i++)
            {
                int index = page * perPage + i;
                if (slots[i] != null)
                {
                    slots[i].Bind(index < products.Count ? products[index] : null, stock);
                }
            }

            bool paged = PageCount > 1;
            if (prevArrow != null)
            {
                prevArrow.gameObject.SetActive(paged);
            }

            if (nextArrow != null)
            {
                nextArrow.gameObject.SetActive(paged);
            }
        }
    }
}
