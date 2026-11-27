using System;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Mutfağın giriş ekranı: Pasta Dolabı (G22). Kendi Canvas'ı olan prefab'dır (Prefabs/UI/CakeCabinetPanel.prefab).
    /// Kafede satılan tatlıları ve her birinden salonda kalan dilimi gösterir; oyuncu bir tatlı seçip "… yap"a basınca
    /// RecipeChosen(tarif id'si) yayınlanır (tezgâh ekranına geçişi dinleyen taraf yapar).
    /// Tarifler varsayılan olarak GameConfig.kitchen.recipes'ten gelir (SetRecipes ile değiştirilebilir).
    /// Stok yalnızca GameManager.Instance.Stock'tan okunur; kartlar GameEvents.StockChanged ile canlı güncellenir.
    /// Kilit bilgisi projede henüz yoktur: LockLevelProvider atanmazsa tüm tarifler açık sayılır.
    /// </summary>
    public class CakeCabinetPanel : MonoBehaviour
    {
        private const string KeyPick = "kitchen.cabinet.pick";
        private const string KeyMake = "kitchen.cabinet.make";

        [SerializeField] private CakeCabinetSettings settings;

        [Tooltip("Gösterilip gizlenen tam ekran kutu (Screen: zemin + tasarım alanı).")]
        [SerializeField] private GameObject content;

        [Tooltip("Sahne açılınca panel görünsün mü.")]
        [SerializeField] private bool showOnStart = true;

        [Header("Kartlar")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Kartların eklendiği kaydırma içeriği (GridLayoutGroup, 2 raf).")]
        [SerializeField] private RectTransform cardContainer;

        [Tooltip("Prefabs/UI/Kitchen/CakeCabinetCard.prefab")]
        [SerializeField] private CakeCabinetCard cardPrefab;

        [Header("Yap butonu")]
        [SerializeField] private Button makeButton;
        [SerializeField] private LocalizedText makeLabel;

        /// <summary>"… yap"a basıldı: seçilen tarifin id'si.</summary>
        public event Action<string> RecipeChosen;

        private readonly List<CakeCabinetCard> cards = new List<CakeCabinetCard>();
        private IReadOnlyList<RecipeData> recipeOverride;
        private Func<RecipeData, int> lockLevelProvider;
        private CakeCabinetCard selected;
        private bool cardsBuilt;
        private bool started;

        public bool IsVisible => content != null && content.activeSelf;

        /// <summary>Seçili tarifin id'si; seçim yoksa null.</summary>
        public string SelectedRecipeId => selected != null && selected.Recipe != null ? selected.Recipe.id : null;

        /// <summary>
        /// Tarifin açılacağı seviye: 0 açık, 0'dan büyükse kilitli ("Seviye {0}'te açılır"). null: hepsi açık.
        /// Atanınca kartlar yenilenir; seçili tarif kilitlenirse seçim düşer.
        /// </summary>
        public Func<RecipeData, int> LockLevelProvider
        {
            get => lockLevelProvider;
            set
            {
                lockLevelProvider = value;
                RefreshIfReady();
            }
        }

        private static GameManager Game => GameManager.Instance;

        // ------------------------------------------------------------------ Yaşam döngüsü

        private void Awake()
        {
            if (settings == null || content == null || scrollRect == null || cardContainer == null || cardPrefab == null ||
                makeButton == null || makeLabel == null)
            {
                Debug.LogError("[CakeCabinetPanel] Referanslar eksik; 'Cutie Pie/UI/Pasta Dolabı Oluştur' menüsünü çalıştırın.", this);
            }

            if (makeButton != null)
            {
                makeButton.onClick.AddListener(HandleMakeClicked);
            }
        }

        private void Start()
        {
            started = true;
            if (showOnStart)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void OnEnable()
        {
            GameEvents.StockChanged += HandleStockChanged;

            // İlk yenileme Start'ta yapılır (sahne yüklenirken GameManager henüz Awake olmamış olabilir);
            // sonradan yeniden etkinleşince kapalıyken kaçırılan stok değişimleri burada telafi edilir.
            if (started && IsVisible)
            {
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            GameEvents.StockChanged -= HandleStockChanged;
        }

        private void OnDestroy()
        {
            if (makeButton != null)
            {
                makeButton.onClick.RemoveListener(HandleMakeClicked);
            }

            foreach (CakeCabinetCard card in cards)
            {
                if (card != null)
                {
                    card.Clicked -= HandleCardClicked;
                }
            }
        }

        // ------------------------------------------------------------------ Dış API

        /// <summary>Paneli gösterir ve kartları güncel stokla yeniler. Önceki seçim (hâlâ geçerliyse) korunur.</summary>
        public void Show()
        {
            if (content == null)
            {
                return;
            }

            content.SetActive(true);
            RefreshAll();
        }

        public void Hide()
        {
            if (content != null)
            {
                content.SetActive(false);
            }
        }

        /// <summary>
        /// Gösterilecek tarifleri değiştirir (null: GameConfig.kitchen.recipes). Kartlar yeniden üretilir, seçim düşer.
        /// </summary>
        public void SetRecipes(IReadOnlyList<RecipeData> recipes)
        {
            recipeOverride = recipes;
            ClearCards();
            RefreshIfReady();
        }

        public void ClearSelection()
        {
            Select(null);
        }

        // ------------------------------------------------------------------ Kartlar

        private void RefreshIfReady()
        {
            if (started && IsVisible)
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            if (!EnsureBuilt())
            {
                UpdateMakeButton();
                return;
            }

            foreach (CakeCabinetCard card in cards)
            {
                RefreshCard(card);
            }

            // Seçili tarif bu arada kilitlendiyse seçim düşer.
            if (selected != null && selected.IsLocked)
            {
                selected = null;
            }

            UpdateMakeButton();
        }

        private bool EnsureBuilt()
        {
            if (cardsBuilt)
            {
                return true;
            }

            IReadOnlyList<RecipeData> recipes = recipeOverride ?? ConfigRecipes();
            if (recipes == null || cardPrefab == null || cardContainer == null)
            {
                // Bir sonraki Show/OnEnable'da yeniden denenir.
                Debug.LogError("[CakeCabinetPanel] Tarif listesi (GameConfig.kitchen.recipes) veya kart referansları yok; dolap kurulamadı.", this);
                return false;
            }

            var seen = new HashSet<string>();
            foreach (RecipeData recipe in recipes)
            {
                // Ürünü ya da id'si olmayan tarifin stoğu okunamaz; aynı tarif iki kez gösterilmez.
                if (recipe == null || recipe.product == null || string.IsNullOrEmpty(recipe.id) ||
                    string.IsNullOrEmpty(recipe.product.id) || !seen.Add(recipe.id))
                {
                    continue;
                }

                CakeCabinetCard card = Instantiate(cardPrefab, cardContainer, false);
                card.name = $"CakeCabinetCard_{recipe.id}";
                card.Bind(recipe);
                card.Clicked += HandleCardClicked;
                cards.Add(card);
            }

            cardsBuilt = true;

            if (scrollRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);
                scrollRect.StopMovement();
                scrollRect.horizontalNormalizedPosition = 0f;
            }

            return true;
        }

        private void ClearCards()
        {
            foreach (CakeCabinetCard card in cards)
            {
                if (card != null)
                {
                    card.Clicked -= HandleCardClicked;

                    // Destroy kare sonunda işler; kapatılan kart aynı karedeki yerleşim hesabına girmez.
                    card.gameObject.SetActive(false);
                    Destroy(card.gameObject);
                }
            }

            cards.Clear();
            selected = null;
            cardsBuilt = false;
        }

        private void RefreshCard(CakeCabinetCard card)
        {
            if (card == null || card.Recipe == null)
            {
                return;
            }

            card.SetState(GetStock(card.ProductId), GetLockLevel(card.Recipe));
            card.SetSelected(card == selected);
        }

        private int GetLockLevel(RecipeData recipe)
        {
            if (lockLevelProvider == null || recipe == null)
            {
                return 0;
            }

            return Mathf.Max(0, lockLevelProvider(recipe));
        }

        private static IReadOnlyList<RecipeData> ConfigRecipes() =>
            Game != null && Game.Config != null && Game.Config.kitchen != null ? Game.Config.kitchen.recipes : null;

        private static int GetStock(string productId) =>
            Game != null && Game.Stock != null && !string.IsNullOrEmpty(productId) ? Game.Stock.Get(productId) : 0;

        // ------------------------------------------------------------------ Seçim ve buton

        private void HandleCardClicked(CakeCabinetCard card)
        {
            if (card != null && !card.IsLocked)
            {
                Select(card);
            }
        }

        private void Select(CakeCabinetCard card)
        {
            selected = card;
            foreach (CakeCabinetCard c in cards)
            {
                if (c != null)
                {
                    c.SetSelected(c == selected);
                }
            }

            UpdateMakeButton();
        }

        private void UpdateMakeButton()
        {
            bool hasSelection = selected != null && selected.Recipe != null;
            KitchenUIUtil.SetInteractable(makeButton, hasSelection);

            if (makeLabel == null)
            {
                return;
            }

            if (hasSelection)
            {
                // Ad, metin her yenilendiğinde (dil değişimi dahil) güncel dilden çözülür.
                makeLabel.SetKey(KeyMake, new LocalizedKeyArg(selected.Recipe.nameKey));
            }
            else
            {
                makeLabel.SetKey(KeyPick);
            }
        }

        private void HandleMakeClicked()
        {
            string recipeId = SelectedRecipeId;
            if (recipeId != null && !selected.IsLocked)
            {
                RecipeChosen?.Invoke(recipeId);
            }
        }

        // ------------------------------------------------------------------ Olaylar

        // Kapalıyken gelen değişimler yok sayılır; Show/OnEnable gerçek stokla yeniler.
        private void HandleStockChanged(string productId, int amount)
        {
            if (!IsVisible || !cardsBuilt || string.IsNullOrEmpty(productId))
            {
                return;
            }

            foreach (CakeCabinetCard card in cards)
            {
                if (card != null && card.ProductId == productId)
                {
                    card.SetState(amount, GetLockLevel(card.Recipe));
                    card.SetSelected(card == selected);
                }
            }
        }

        /// <summary>
        /// Biçim argümanı olarak yerelleştirilmiş metin: string.Format her çağrıldığında anahtarı güncel dilden çözer.
        /// Böylece LocalizedText dil değişince yenilendiğinde "{0} yap" içindeki tarif adı da yeni dilde yazılır.
        /// </summary>
        private sealed class LocalizedKeyArg
        {
            private readonly string key;

            public LocalizedKeyArg(string key) => this.key = key;

            public override string ToString()
            {
                LocalizationService service = Game != null ? Game.Localization : null;
                return service != null ? service.Get(key) : key ?? string.Empty;
            }
        }
    }
}
