using System;
using System.Collections.Generic;
using CutiePie.Ads;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Kitchen;
using CutiePie.Upgrades;
using UnityEngine;

namespace CutiePie.UI
{
    /// <summary>
    /// Mutfak fırın ekranı (G19). Kendi Canvas'ı olan prefab'dır (Prefabs/UI/Kitchen/OvenPanel.prefab).
    /// - Üstte stok şeridi: her pastanın kalan dilimi.
    /// - Her fırın bir kart: Boş → Pişir (tarif seçici), Pişiyor → geri sayım + ilerleme + reklamla bitir,
    ///   Hazır → parlayan Topla (dilimler stok şeridine uçar), Kilitli → soluk, Geliştirmeyle aç.
    /// Kart sayısı KitchenSettings.maxOvenCount'tur; açık fırın sayısının (Ovens.Count) ötesindekiler kilitli görünür.
    /// Fırın servisine yalnızca GameManager.Instance.Ovens üzerinden, reklama GameManager.Instance.Ads üzerinden erişilir.
    /// Olaylarla yenilenir (BakeStarted, OvenReady, BatchCollected, StockChanged, UpgradePurchased: extra_oven);
    /// geri sayım her karede gerçek zamanla güncellenir, Time.timeScale'den etkilenmez.
    /// </summary>
    public class OvenPanel : MonoBehaviour
    {
        [SerializeField] private OvenPanelSettings settings;

        [Tooltip("Gösterilip gizlenen içerik (zemin, stok şeridi, fırınlar, seçici, uçuş katmanı).")]
        [SerializeField] private GameObject content;

        [Tooltip("Sahne açılınca panel görünsün mü.")]
        [SerializeField] private bool showOnStart = true;

        [Header("Fırınlar")]
        [Tooltip("Kartların eklendiği kaydırma içeriği (HorizontalLayoutGroup).")]
        [SerializeField] private RectTransform ovenContainer;

        [Tooltip("Prefabs/UI/Kitchen/OvenCard.prefab")]
        [SerializeField] private OvenCard ovenCardPrefab;

        [Header("Parçalar")]
        [SerializeField] private StockSummary stockSummary;
        [SerializeField] private RecipePicker recipePicker;
        [SerializeField] private SliceFlyAnimator flyAnimator;

        /// <summary>
        /// Kilitli fırında "Geliştirmeyle aç"a basıldı. Mutfak sahnesi bunu geliştirme mağazasına (UpgradesPanel.Open) bağlar.
        /// </summary>
        public event Action UnlockRequested;

        private readonly List<OvenCard> cards = new List<OvenCard>();
        private bool cardsBuilt;
        private bool started;

        // Reklamla hızlandırılan fırın; reklam oynamıyorsa -1. Aynı anda tek reklam oynar.
        private int adOvenIndex = -1;

        public bool IsVisible => content != null && content.activeSelf;

        private static GameManager Game => GameManager.Instance;

        private static KitchenSettings Kitchen => Game != null && Game.Config != null ? Game.Config.kitchen : null;

        // ------------------------------------------------------------------ Yaşam döngüsü

        private void Awake()
        {
            if (settings == null || content == null || ovenContainer == null || ovenCardPrefab == null ||
                stockSummary == null || recipePicker == null || flyAnimator == null)
            {
                Debug.LogError("[OvenPanel] Referanslar eksik; 'Cutie Pie/UI/Fırın Panelini Kur' menüsünü çalıştırın.", this);
            }

            if (recipePicker != null)
            {
                recipePicker.RecipeChosen += HandleRecipeChosen;
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

        private void OnDestroy()
        {
            if (recipePicker != null)
            {
                recipePicker.RecipeChosen -= HandleRecipeChosen;
            }

            foreach (OvenCard card in cards)
            {
                UnbindCard(card);
            }
        }

        private void OnEnable()
        {
            GameEvents.BakeStarted += HandleBakeStarted;
            GameEvents.OvenReady += HandleOvenReady;
            GameEvents.BatchCollected += HandleBatchCollected;
            GameEvents.StockChanged += HandleStockChanged;
            GameEvents.UpgradePurchased += HandleUpgradePurchased;

            // İlk açılışta yenileme Start'ta yapılır (sahne yüklenirken GameManager henüz Awake olmamış olabilir);
            // sonradan yeniden etkinleşince kaçırılan olaylar burada telafi edilir.
            if (started && IsVisible)
            {
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            GameEvents.BakeStarted -= HandleBakeStarted;
            GameEvents.OvenReady -= HandleOvenReady;
            GameEvents.BatchCollected -= HandleBatchCollected;
            GameEvents.StockChanged -= HandleStockChanged;
            GameEvents.UpgradePurchased -= HandleUpgradePurchased;
        }

        // Kart durumu her karede servisle karşılaştırılır: olay kaçsa da (ör. test menüsüyle seviye değişimi,
        // olayı olmayan saat kaydırma) görünüm bir kare içinde düzelir. Geri sayım da buradan akar.
        private void Update()
        {
            if (!IsVisible || !cardsBuilt)
            {
                return;
            }

            IOvenService ovens = Game != null ? Game.Ovens : null;
            IAdsService ads = Game != null ? Game.Ads : null;
            bool adAvailable = ads != null && ads.IsRewardedReady && adOvenIndex < 0;

            for (int i = 0; i < cards.Count; i++)
            {
                OvenCard card = cards[i];
                if (card == null)
                {
                    continue;
                }

                if (NeedsRefresh(card, ovens))
                {
                    RefreshCard(card);
                }

                if (card.CurrentView == OvenCard.View.Baking && card.Recipe != null && ovens != null)
                {
                    card.SetCountdown(ovens.GetRemainingSeconds(i), card.Recipe.bakeSeconds);
                    card.SetAdState(adOvenIndex == i, adAvailable);
                }
            }

            // Seçicinin açıldığı fırın bu arada doldu ya da kilitlendiyse seçici kapanır.
            if (recipePicker != null && recipePicker.IsOpen &&
                (recipePicker.OvenIndex < 0 || recipePicker.OvenIndex >= cards.Count ||
                 cards[recipePicker.OvenIndex].CurrentView != OvenCard.View.Empty))
            {
                recipePicker.Close();
            }
        }

        // ------------------------------------------------------------------ Göster / gizle

        /// <summary>Paneli gösterir ve tüm kartları/stok şeridini güncel değerlerle yeniler.</summary>
        public void Show()
        {
            if (content == null)
            {
                return;
            }

            content.SetActive(true);
            if (recipePicker != null)
            {
                recipePicker.Close();
            }

            RefreshAll();
        }

        /// <summary>Paneli gizler; seçici kapanır, yarıdaki uçuşlar bitirilir.</summary>
        public void Hide()
        {
            if (recipePicker != null)
            {
                recipePicker.Close();
            }

            // Uçuşların bitiş geri çağrıları stok kalemlerini gerçek stoğa eşitler.
            if (flyAnimator != null)
            {
                flyAnimator.CancelAll();
            }

            if (content != null)
            {
                content.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Yenileme

        private void RefreshAll()
        {
            if (!EnsureBuilt())
            {
                return;
            }

            foreach (OvenCard card in cards)
            {
                RefreshCard(card);
            }

            if (stockSummary != null)
            {
                stockSummary.ResetAll(GetStock);
            }
        }

        private bool EnsureBuilt()
        {
            KitchenSettings kitchen = Kitchen;
            if (kitchen == null || Game.Ovens == null)
            {
                // Bir sonraki Show/OnEnable'da yeniden denenir.
                Debug.LogError("[OvenPanel] GameManager, GameConfig.kitchen veya fırın servisi yok; fırın ekranı kurulamadı.", this);
                return false;
            }

            if (stockSummary != null)
            {
                stockSummary.EnsureEntries(kitchen);
            }

            if (cardsBuilt)
            {
                return true;
            }

            if (ovenCardPrefab == null || ovenContainer == null)
            {
                return false;
            }

            int cardCount = Mathf.Max(kitchen.maxOvenCount, Game.Ovens.Count);
            for (int i = 0; i < cardCount; i++)
            {
                OvenCard card = Instantiate(ovenCardPrefab, ovenContainer, false);
                card.name = $"OvenCard_{i + 1}";
                card.Bind(i);
                card.BakeClicked += HandleBakeClicked;
                card.AdClicked += HandleAdClicked;
                card.CollectClicked += HandleCollectClicked;
                card.UnlockClicked += HandleUnlockClicked;
                cards.Add(card);
            }

            cardsBuilt = true;
            return true;
        }

        private void RefreshCard(int ovenIndex)
        {
            if (ovenIndex >= 0 && ovenIndex < cards.Count)
            {
                RefreshCard(cards[ovenIndex]);
            }
        }

        private void RefreshCard(OvenCard card)
        {
            IOvenService ovens = Game != null ? Game.Ovens : null;
            if (card == null || ovens == null)
            {
                return;
            }

            int i = card.Index;
            OvenCard.View view = DesiredView(i, ovens, out RecipeData recipe);
            switch (view)
            {
                case OvenCard.View.Locked:
                    card.ShowLocked();
                    break;
                case OvenCard.View.Baking:
                    card.ShowBaking(recipe);
                    card.SetCountdown(ovens.GetRemainingSeconds(i), recipe.bakeSeconds);
                    break;
                case OvenCard.View.Ready:
                    card.ShowReady(recipe);
                    break;
                default:
                    card.ShowEmpty();
                    break;
            }
        }

        // Her karede çağrılır: tarif aranmaz (bellek ayırmasın diye), yalnızca durum ve tarif id'si karşılaştırılır.
        private static bool NeedsRefresh(OvenCard card, IOvenService ovens)
        {
            if (card == null || ovens == null)
            {
                return false;
            }

            if (card.Index >= ovens.Count)
            {
                return card.CurrentView != OvenCard.View.Locked;
            }

            OvenState state = ovens.GetState(card.Index);
            if (state == OvenState.Empty)
            {
                return card.CurrentView != OvenCard.View.Empty;
            }

            OvenCard.View expected = state == OvenState.Ready ? OvenCard.View.Ready : OvenCard.View.Baking;
            return card.CurrentView != expected || card.Recipe == null || card.Recipe.id != ovens.GetRecipeId(card.Index);
        }

        // Servisin durumundan kartın görünümü. Tarifi bilinmeyen dolu fırın boş sayılır (OvenService de öyle döner).
        private static OvenCard.View DesiredView(int ovenIndex, IOvenService ovens, out RecipeData recipe)
        {
            recipe = null;
            if (ovenIndex >= ovens.Count)
            {
                return OvenCard.View.Locked;
            }

            OvenState state = ovens.GetState(ovenIndex);
            if (state == OvenState.Empty)
            {
                return OvenCard.View.Empty;
            }

            recipe = Kitchen != null ? Kitchen.FindRecipe(ovens.GetRecipeId(ovenIndex)) : null;
            if (recipe == null)
            {
                return OvenCard.View.Empty;
            }

            return state == OvenState.Ready ? OvenCard.View.Ready : OvenCard.View.Baking;
        }

        private static int GetStock(string productId) =>
            Game != null && Game.Stock != null ? Game.Stock.Get(productId) : 0;

        // ------------------------------------------------------------------ Kart butonları

        private void HandleBakeClicked(OvenCard card)
        {
            if (recipePicker != null && card.CurrentView == OvenCard.View.Empty)
            {
                recipePicker.Open(card.Index, Kitchen);
            }
        }

        private void HandleRecipeChosen(int ovenIndex, RecipeData recipe)
        {
            IOvenService ovens = Game != null ? Game.Ovens : null;
            if (ovens == null || recipe == null)
            {
                return;
            }

            // Başarılıysa BakeStarted kartı yeniler; başarısızsa kart güncel durumu gösterir.
            if (!ovens.StartBake(ovenIndex, recipe.id))
            {
                Debug.LogWarning($"[OvenPanel] Fırın {ovenIndex}: '{recipe.id}' pişirilemedi.", this);
                RefreshCard(ovenIndex);
            }
        }

        private void HandleAdClicked(OvenCard card)
        {
            IOvenService ovens = Game != null ? Game.Ovens : null;
            IAdsService ads = Game != null ? Game.Ads : null;
            if (ovens == null || ads == null || !ads.IsRewardedReady || adOvenIndex >= 0 ||
                card.CurrentView != OvenCard.View.Baking)
            {
                return;
            }

            int ovenIndex = card.Index;
            adOvenIndex = ovenIndex;
            card.SetAdState(true, false);
            ovens.SpeedUpWithAd(ovenIndex, success => HandleAdFinished(ovenIndex, success));
        }

        private void HandleAdFinished(int ovenIndex, bool success)
        {
            // Reklam bitene kadar sahne değiştiyse panel yok edilmiştir.
            if (this == null || adOvenIndex != ovenIndex)
            {
                return;
            }

            adOvenIndex = -1;

            // Başarılıysa OvenReady zaten yayınlandı; başarısızsa kart pişmeye devam eder. İkisinde de güncel durum gösterilir.
            RefreshCard(ovenIndex);
        }

        private void HandleCollectClicked(OvenCard card)
        {
            IOvenService ovens = Game != null ? Game.Ovens : null;
            RecipeData recipe = card.Recipe;
            if (ovens == null || recipe == null || recipe.product == null || card.CurrentView != OvenCard.View.Ready)
            {
                return;
            }

            string productId = recipe.product.id;
            int slices = recipe.slicesPerBatch;
            StockEntry entry = null;
            bool animate = stockSummary != null && flyAnimator != null && stockSummary.TryGetEntry(productId, out entry);

            // Collect içinde StockChanged yayınlanır; kalem sayıyı atlamasın diye uçuş önceden bildirilir.
            if (animate)
            {
                entry.BeginFlight();
            }

            if (!ovens.Collect(card.Index))
            {
                if (animate)
                {
                    entry.EndFlight(GetStock(productId));
                }

                RefreshCard(card);
                return;
            }

            if (!animate)
            {
                return;
            }

            // Dilimler simgelere bölünür; kalan dilimler ilk simgelere birer fazla düşer.
            int iconCount = Mathf.Clamp(slices, 1, settings != null ? settings.maxFlyIcons : 1);
            int perIcon = slices / iconCount;
            int remainder = slices % iconCount;
            Sprite sprite = recipe.product.icon != null ? recipe.product.icon : recipe.icon;

            flyAnimator.Play(card.FlyOrigin, entry.FlyTarget, sprite, iconCount,
                i => entry.Land(perIcon + (i < remainder ? 1 : 0)),
                () => entry.EndFlight(GetStock(productId)));
        }

        private void HandleUnlockClicked(OvenCard card) => UnlockRequested?.Invoke();

        // ------------------------------------------------------------------ Olaylar

        private void HandleBakeStarted(int ovenIndex, string recipeId) => RefreshIfVisible(ovenIndex);

        private void HandleOvenReady(int ovenIndex) => RefreshIfVisible(ovenIndex);

        // Topla'yı bu panel değil de başka biri (ör. test menüsü) yaptıysa kart da buradan yenilenir.
        private void HandleBatchCollected(string productId, int slices)
        {
            if (!IsVisible)
            {
                return;
            }

            foreach (OvenCard card in cards)
            {
                if (NeedsRefresh(card, Game != null ? Game.Ovens : null))
                {
                    RefreshCard(card);
                }
            }
        }

        private void HandleStockChanged(string productId, int amount)
        {
            if (IsVisible && stockSummary != null)
            {
                stockSummary.SetAmount(productId, amount);
            }
        }

        // Ek fırın alınınca açık fırın sayısı artar: kilitli kartlar yenilenir.
        private void HandleUpgradePurchased(UpgradeData upgrade, int newLevel)
        {
            if (IsVisible && upgrade != null && upgrade.id == UpgradeIds.ExtraOven)
            {
                foreach (OvenCard card in cards)
                {
                    RefreshCard(card);
                }
            }
        }

        private void RefreshIfVisible(int ovenIndex)
        {
            if (IsVisible && cardsBuilt)
            {
                RefreshCard(ovenIndex);
            }
        }

        private void UnbindCard(OvenCard card)
        {
            if (card == null)
            {
                return;
            }

            card.BakeClicked -= HandleBakeClicked;
            card.AdClicked -= HandleAdClicked;
            card.CollectClicked -= HandleCollectClicked;
            card.UnlockClicked -= HandleUnlockClicked;
        }
    }
}
