using System;
using System.Collections.Generic;
using CutiePie.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Boş fırının "Pişir" butonuyla açılan tarif seçici (perde + kart). Tarifler yatay kaydırılır.
    /// Tarife dokununca RecipeChosen(fırın indeksi, tarif) yayınlanır ve seçici kapanır; pişirmeyi OvenPanel başlatır.
    /// OvenPanel prefab'ının içinde durur.
    /// </summary>
    public class RecipePicker : MonoBehaviour
    {
        [Tooltip("Açılıp kapanan içerik (perde + kart).")]
        [SerializeField] private GameObject content;

        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Tariflerin eklendiği kaydırma içeriği (HorizontalLayoutGroup).")]
        [SerializeField] private RectTransform optionContainer;

        [Tooltip("Prefabs/UI/Kitchen/RecipeOption.prefab")]
        [SerializeField] private RecipeOption optionPrefab;

        /// <summary>Tarif seçildi: (fırın indeksi, tarif).</summary>
        public event Action<int, RecipeData> RecipeChosen;

        private readonly List<RecipeOption> options = new List<RecipeOption>();
        private bool optionsBuilt;

        public bool IsOpen => content != null && content.activeSelf;

        /// <summary>Seçicinin açıldığı fırın; kapalıyken -1.</summary>
        public int OvenIndex { get; private set; } = -1;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            foreach (RecipeOption option in options)
            {
                if (option != null)
                {
                    option.Clicked -= HandleOptionClicked;
                }
            }
        }

        /// <summary>Seçiciyi verilen fırın için açar; tarif kartları ilk açılışta üretilir.</summary>
        public void Open(int ovenIndex, KitchenSettings kitchen)
        {
            if (content == null)
            {
                return;
            }

            EnsureOptions(kitchen);
            OvenIndex = ovenIndex;
            content.SetActive(true);

            if (scrollRect != null && optionContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(optionContainer);
                scrollRect.StopMovement();
                scrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        public void Close()
        {
            OvenIndex = -1;
            if (content != null)
            {
                content.SetActive(false);
            }
        }

        private void EnsureOptions(KitchenSettings kitchen)
        {
            if (optionsBuilt)
            {
                return;
            }

            if (kitchen == null || optionPrefab == null || optionContainer == null)
            {
                // Bir sonraki açılışta yeniden denenir.
                Debug.LogError("[RecipePicker] KitchenSettings yok veya tarif kartı referansları eksik; tarifler üretilemedi.", this);
                return;
            }

            if (kitchen.recipes != null)
            {
                foreach (RecipeData recipe in kitchen.recipes)
                {
                    // Ürünü olmayan tarif fırına konamaz (OvenService reddeder); listede gösterilmez.
                    if (recipe == null || recipe.product == null)
                    {
                        continue;
                    }

                    RecipeOption option = Instantiate(optionPrefab, optionContainer, false);
                    option.name = $"RecipeOption_{recipe.id}";
                    option.Bind(recipe);
                    option.Clicked += HandleOptionClicked;
                    options.Add(option);
                }
            }

            optionsBuilt = true;
        }

        private void HandleOptionClicked(RecipeData recipe)
        {
            int ovenIndex = OvenIndex;
            Close();
            if (ovenIndex >= 0)
            {
                RecipeChosen?.Invoke(ovenIndex, recipe);
            }
        }
    }
}
