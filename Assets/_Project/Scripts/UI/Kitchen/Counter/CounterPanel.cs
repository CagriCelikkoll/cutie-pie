using System;
using System.Collections;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Mutfak Tezgâh ekranı (G23): seçilen tarifin adım adım yapıldığı ekran. Kendi Canvas'ı olan prefab'dır
    /// (Prefabs/UI/Kitchen/CounterPanel.prefab). Tarif dışarıdan gelir: Begin(recipeId).
    /// Adım 1 (Malzemeler): tepsideki malzemeler herhangi bir sırayla kaseye sürüklenir; aşçı sıradaki malzemeyi önerir,
    /// o kart parlar. Kase dışına bırakılan malzeme yerine döner. Yanlış malzeme yoktur (tepside yalnızca bu tarifinkiler var).
    /// Adım 2 (Karıştır): hepsi eklenince başlar; kasede parmakla daire çizilir. %100'de "Hamur hazır!" görünür,
    /// 3. adım (Fırına koy) aktif olur ve BatterReady(recipeId) yayınlanır; panel açık kalır (G24 buradan devam eder).
    /// Geri butonu onay sorar; "Evet" tarifi iptal eder (ceza yok), Cancelled(recipeId) yayınlanır ve panel kapanır.
    /// Tarifin adı ve ikonu GameConfig.kitchen'dan (salt okunur), malzemeler RecipeStepsLibrary'den gelir.
    /// </summary>
    public class CounterPanel : MonoBehaviour
    {
        private enum Stage
        {
            Idle,
            Ingredients,
            Mixing,
            Ready
        }

        // Adım çubuğundaki sıra (0 tabanlı).
        private const int StepIngredients = 0;
        private const int StepMix = 1;
        private const int StepBake = 2;

        private const string KeyProgressIngredients = "kitchen.counter.progress.ingredients";
        private const string KeyProgressMix = "kitchen.counter.progress.mix";
        private const string KeyHintMix = "kitchen.counter.hint.mix";
        private const string KeyHintReady = "kitchen.counter.hint.ready";
        private const string KeyBarStep = "kitchen.counter.bar.step";
        private const string KeyBarDoIngredients = "kitchen.counter.bar.do.ingredients";
        private const string KeyBarDoMix = "kitchen.counter.bar.do.mix";
        private const string KeyBarDoBake = "kitchen.counter.bar.do.bake";
        private const string KeyBarNext = "kitchen.counter.bar.next";

        private static readonly string[] StepNameKeys =
        {
            "kitchen.counter.step.ingredients",
            "kitchen.counter.step.mix",
            "kitchen.counter.step.bake",
            "kitchen.counter.step.decorate",
            "kitchen.counter.step.slice"
        };

        [SerializeField] private CounterSettings settings;
        [SerializeField] private RecipeStepsLibrary library;

        [Tooltip("Gösterilip gizlenen tam ekran kutu (Screen: zemin + tasarım alanı).")]
        [SerializeField] private GameObject screen;

        [Header("Üst bar")]
        [SerializeField] private Button backButton;
        [SerializeField] private LocalizedText titleText;

        [Tooltip("Pastanın küçük ikonu; RecipeData.icon (yoksa ürünün simgesi) boşsa yer tutucu kalır.")]
        [SerializeField] private Image titleIcon;

        [Header("Adımlar ve aşçı")]
        [SerializeField] private StepTracker stepTracker;
        [SerializeField] private LocalizedText chefBubbleText;

        [Header("Tepsi ve kase")]
        [Tooltip("Malzeme kartlarının eklendiği ızgara (GridLayoutGroup).")]
        [SerializeField] private RectTransform trayContainer;

        [Tooltip("Prefabs/UI/Kitchen/IngredientCard.prefab")]
        [SerializeField] private IngredientCard cardPrefab;

        [SerializeField] private MixingBowl bowl;

        [Tooltip("\"Hamur hazır!\" yazısı (kasenin üstünde).")]
        [SerializeField] private GameObject readyBanner;

        [Header("Sürükleme")]
        [Tooltip("Sürüklenen görselin çizildiği tam alan katman (tasarım alanının en üstünde).")]
        [SerializeField] private RectTransform dragLayer;

        [Tooltip("Kapalı kaydedilen sürükleme görseli.")]
        [SerializeField] private Image dragGhost;

        [Header("İpucu çubuğu")]
        [SerializeField] private LocalizedText hintStepText;
        [SerializeField] private LocalizedText hintDoText;
        [SerializeField] private LocalizedText hintNextText;

        [Tooltip("İlerleme dolgusu; genişliği anchorMax.x ile (0–1) sürülür.")]
        [SerializeField] private RectTransform hintProgressFill;

        [Header("Bırakma onayı")]
        [SerializeField] private GameObject leaveDialog;
        [SerializeField] private Button leaveYesButton;
        [SerializeField] private Button leaveNoButton;

        /// <summary>Karıştırma bitti, hamur hazır: tarif id'si. Panel açık kalır.</summary>
        public event Action<string> BatterReady;

        /// <summary>Oyuncu tarifi fırından önce bıraktı (ceza yok): tarif id'si. Panel kapanır.</summary>
        public event Action<string> Cancelled;

        private readonly List<IngredientCard> cards = new List<IngredientCard>();
        private Stage stage = Stage.Idle;
        private string recipeId;
        private RecipeStepsData steps;
        private Sprite titlePlaceholder;
        private Color titlePlaceholderColor;
        private IngredientCard draggedCard;
        private Coroutine returnRoutine;

        public bool IsVisible => screen != null && screen.activeSelf;

        /// <summary>Tezgâhtaki tarif; Begin'den önce ve iptalden sonra null.</summary>
        public string RecipeId => recipeId;

        public bool IsBatterReady => stage == Stage.Ready;

        private static GameManager Game => GameManager.Instance;

        // ------------------------------------------------------------------ Yaşam döngüsü

        private void Awake()
        {
            if (settings == null || library == null || screen == null || backButton == null || stepTracker == null ||
                trayContainer == null || cardPrefab == null || bowl == null || dragLayer == null || dragGhost == null ||
                leaveDialog == null || leaveYesButton == null || leaveNoButton == null)
            {
                Debug.LogError("[CounterPanel] Referanslar eksik; 'Cutie Pie/UI/Tezgâh Oluştur' menüsünü çalıştırın.", this);
            }

            if (titleIcon != null)
            {
                titlePlaceholder = titleIcon.sprite;
                titlePlaceholderColor = titleIcon.color;
            }

            AddListener(backButton, HandleBackClicked);
            AddListener(leaveYesButton, HandleLeaveYes);
            AddListener(leaveNoButton, HandleLeaveNo);

            if (bowl != null)
            {
                bowl.MixProgressChanged += HandleMixProgress;
                bowl.MixCompleted += HandleMixCompleted;
            }

            // Tarif gelene kadar ekran kapalı.
            HideDragGhost();
            KitchenUIUtil.SetActive(leaveDialog, false);
            KitchenUIUtil.SetActive(readyBanner, false);
            KitchenUIUtil.SetActive(screen, false);
        }

        private void OnDestroy()
        {
            RemoveListener(backButton, HandleBackClicked);
            RemoveListener(leaveYesButton, HandleLeaveYes);
            RemoveListener(leaveNoButton, HandleLeaveNo);

            if (bowl != null)
            {
                bowl.MixProgressChanged -= HandleMixProgress;
                bowl.MixCompleted -= HandleMixCompleted;
            }

            foreach (IngredientCard card in cards)
            {
                UnbindCard(card);
            }
        }

        // ------------------------------------------------------------------ Dış API

        /// <summary>
        /// Tezgâhı verilen tarifle baştan açar (adım 1). Tarifin adımları RecipeStepsLibrary'de yoksa açılmaz ve false döner.
        /// Önceki tarif varsa sessizce bırakılır (Cancelled yayınlanmaz).
        /// </summary>
        public bool Begin(string newRecipeId)
        {
            RecipeStepsData data = library != null ? library.Find(newRecipeId) : null;
            if (data == null || data.ingredients == null || data.ingredients.Length == 0)
            {
                Debug.LogError($"[CounterPanel] '{newRecipeId}' için tezgâh adımları yok (Data/Tezgah/RecipeStepsLibrary).", this);
                return false;
            }

            CancelDrag();
            ClearCards();

            recipeId = newRecipeId;
            steps = data;
            ApplyTitle();
            BuildCards();

            if (bowl != null)
            {
                bowl.ResetBowl(cards.Count, data.mixTurns, data.batterStartColor, data.batterReadyColor);
            }

            KitchenUIUtil.SetActive(readyBanner, false);
            KitchenUIUtil.SetActive(leaveDialog, false);
            KitchenUIUtil.SetActive(screen, true);

            stage = Stage.Ingredients;
            RefreshAll();
            return true;
        }

        /// <summary>Paneli gizler; tarif durumu korunur (Show ile kaldığı yerden görünür).</summary>
        public void Hide()
        {
            CancelDrag();
            KitchenUIUtil.SetActive(leaveDialog, false);
            KitchenUIUtil.SetActive(screen, false);
        }

        /// <summary>Gizlenmiş paneli kaldığı yerden gösterir; tarif yoksa bir şey yapmaz.</summary>
        public void Show()
        {
            if (stage == Stage.Idle)
            {
                return;
            }

            KitchenUIUtil.SetActive(screen, true);
            RefreshAll();
        }

        /// <summary>Test aracı: kalan malzemelerin hepsini kaseye ekler (adım 2 başlar).</summary>
        public void AutoCompleteIngredients()
        {
            if (stage != Stage.Ingredients)
            {
                return;
            }

            CancelDrag();
            foreach (IngredientCard card in cards.ToArray())
            {
                if (card != null && !card.IsAdded && stage == Stage.Ingredients)
                {
                    AddToBowl(card);
                }
            }
        }

        /// <summary>Test aracı: karıştırmayı hemen bitirir (BatterReady yayınlanır).</summary>
        public void AutoCompleteMixing()
        {
            if (stage == Stage.Mixing && bowl != null)
            {
                bowl.CompleteMixing();
            }
        }

        // ------------------------------------------------------------------ Kurulum

        private void ApplyTitle()
        {
            KitchenSettings kitchen = Game != null && Game.Config != null ? Game.Config.kitchen : null;
            RecipeData recipe = kitchen != null ? kitchen.FindRecipe(recipeId) : null;
            if (recipe == null)
            {
                Debug.LogWarning($"[CounterPanel] GameConfig.kitchen'da '{recipeId}' tarifi yok; başlıkta id görünür.", this);
            }

            KitchenUIUtil.SetKey(titleText, recipe != null ? recipe.nameKey : recipeId);

            Sprite sprite = recipe == null ? null : recipe.icon != null ? recipe.icon : recipe.product != null ? recipe.product.icon : null;
            KitchenUIUtil.SetIcon(titleIcon, sprite, titlePlaceholder, titlePlaceholderColor);
        }

        private void BuildCards()
        {
            if (cardPrefab == null || trayContainer == null)
            {
                return;
            }

            foreach (IngredientEntry entry in steps.ingredients)
            {
                if (entry == null || entry.ingredient == null)
                {
                    continue;
                }

                IngredientCard card = Instantiate(cardPrefab, trayContainer, false);
                card.name = $"IngredientCard_{entry.ingredient.id}";
                card.Bind(entry);
                card.DragStarted += HandleDragStarted;
                card.DragMoved += HandleDragMoved;
                card.DragEnded += HandleDragEnded;
                cards.Add(card);
            }
        }

        private void ClearCards()
        {
            foreach (IngredientCard card in cards)
            {
                if (card != null)
                {
                    UnbindCard(card);

                    // Destroy kare sonunda işler; kapatılan kart aynı karedeki yerleşim hesabına girmez.
                    card.gameObject.SetActive(false);
                    Destroy(card.gameObject);
                }
            }

            cards.Clear();
        }

        private void UnbindCard(IngredientCard card)
        {
            if (card == null)
            {
                return;
            }

            card.DragStarted -= HandleDragStarted;
            card.DragMoved -= HandleDragMoved;
            card.DragEnded -= HandleDragEnded;
        }

        // ------------------------------------------------------------------ Adım 1: sürükle-bırak

        private void HandleDragStarted(IngredientCard card, PointerEventData eventData)
        {
            if (stage != Stage.Ingredients || card.IsAdded || draggedCard != null || IsDialogOpen)
            {
                return;
            }

            StopReturn();
            draggedCard = card;
            card.SetDragging(true);

            if (dragGhost != null && card.Icon != null)
            {
                Image source = card.Icon;
                dragGhost.sprite = source.sprite;
                dragGhost.type = source.type;
                dragGhost.preserveAspect = source.preserveAspect;
                dragGhost.color = new Color(source.color.r, source.color.g, source.color.b, 1f);
                dragGhost.rectTransform.sizeDelta = Vector2.one * (settings != null ? settings.dragIconSize : 110f);
                dragGhost.gameObject.SetActive(true);
                dragGhost.transform.SetAsLastSibling();
            }

            MoveGhost(eventData);
        }

        private void HandleDragMoved(IngredientCard card, PointerEventData eventData)
        {
            if (card != draggedCard)
            {
                return;
            }

            MoveGhost(eventData);
            if (bowl != null)
            {
                bowl.SetHover(bowl.ContainsScreenPoint(eventData.position, eventData.pressEventCamera));
            }
        }

        private void HandleDragEnded(IngredientCard card, PointerEventData eventData)
        {
            if (card != draggedCard)
            {
                return;
            }

            draggedCard = null;
            if (bowl != null)
            {
                bowl.SetHover(false);
            }

            if (stage == Stage.Ingredients && bowl != null && bowl.ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
            {
                HideDragGhost();
                AddToBowl(card);
                return;
            }

            // Kase dışında bırakıldı: görsel karta geri süzülür.
            returnRoutine = StartCoroutine(ReturnGhost(card));
        }

        private void MoveGhost(PointerEventData eventData)
        {
            if (dragGhost == null || dragLayer == null || !dragGhost.gameObject.activeSelf)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventData.pressEventCamera,
                    out Vector2 local))
            {
                dragGhost.rectTransform.anchoredPosition = local;
            }
        }

        private IEnumerator ReturnGhost(IngredientCard card)
        {
            RectTransform ghost = dragGhost != null ? dragGhost.rectTransform : null;
            if (ghost != null && card != null && card.Icon != null && dragLayer != null)
            {
                Vector2 from = ghost.anchoredPosition;
                Vector2 to = dragLayer.InverseTransformPoint(card.Icon.rectTransform.position);
                float duration = settings != null ? settings.returnDuration : 0.25f;
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    ghost.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                    yield return null;
                }
            }

            returnRoutine = null;
            HideDragGhost();
            if (card != null)
            {
                card.SetDragging(false);
            }
        }

        private void AddToBowl(IngredientCard card)
        {
            card.MarkAdded();
            if (bowl != null)
            {
                Image icon = card.Icon;
                bowl.AddIngredient(card.Ingredient, icon != null ? icon.sprite : null, icon != null ? icon.color : Color.white,
                    icon != null ? icon.type : Image.Type.Simple);
            }

            if (AddedCount >= cards.Count)
            {
                StartMixing();
            }
            else
            {
                RefreshAll();
            }
        }

        // Yarıdaki sürükleme / geri dönüş hemen bitirilir (panel kapanırken, yeni tarifte, onay açılınca).
        private void CancelDrag()
        {
            StopReturn();
            if (draggedCard != null)
            {
                draggedCard.SetDragging(false);
                draggedCard = null;
            }

            foreach (IngredientCard card in cards)
            {
                if (card != null && !card.IsAdded)
                {
                    card.SetDragging(false);
                }
            }

            if (bowl != null)
            {
                bowl.SetHover(false);
            }

            HideDragGhost();
        }

        private void StopReturn()
        {
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }

        private void HideDragGhost()
        {
            if (dragGhost != null)
            {
                dragGhost.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Adım 2: karıştırma

        private void StartMixing()
        {
            stage = Stage.Mixing;
            if (bowl != null)
            {
                bowl.BeginMixing();
            }

            RefreshAll();
        }

        private void HandleMixProgress(float progress)
        {
            if (stage == Stage.Mixing)
            {
                RefreshAll();
            }
        }

        private void HandleMixCompleted()
        {
            if (stage != Stage.Mixing)
            {
                return;
            }

            stage = Stage.Ready;
            KitchenUIUtil.SetActive(readyBanner, true);
            RefreshAll();
            BatterReady?.Invoke(recipeId);
        }

        // ------------------------------------------------------------------ Bırakma onayı

        private bool IsDialogOpen => leaveDialog != null && leaveDialog.activeSelf;

        private void HandleBackClicked()
        {
            if (stage == Stage.Idle)
            {
                return;
            }

            CancelDrag();
            KitchenUIUtil.SetActive(leaveDialog, true);
        }

        private void HandleLeaveNo() => KitchenUIUtil.SetActive(leaveDialog, false);

        private void HandleLeaveYes()
        {
            string cancelledId = recipeId;
            KitchenUIUtil.SetActive(leaveDialog, false);
            CancelDrag();
            ClearCards();
            stage = Stage.Idle;
            recipeId = null;
            steps = null;
            KitchenUIUtil.SetActive(readyBanner, false);
            KitchenUIUtil.SetActive(screen, false);
            Cancelled?.Invoke(cancelledId);
        }

        // ------------------------------------------------------------------ Görünüm

        private int AddedCount
        {
            get
            {
                int added = 0;
                foreach (IngredientCard card in cards)
                {
                    if (card != null && card.IsAdded)
                    {
                        added++;
                    }
                }

                return added;
            }
        }

        // Sıradaki öneri: listede henüz eklenmemiş ilk malzeme.
        private IngredientCard NextCard
        {
            get
            {
                foreach (IngredientCard card in cards)
                {
                    if (card != null && !card.IsAdded)
                    {
                        return card;
                    }
                }

                return null;
            }
        }

        private void RefreshAll()
        {
            int total = cards.Count;
            int added = AddedCount;
            IngredientCard next = stage == Stage.Ingredients ? NextCard : null;

            foreach (IngredientCard card in cards)
            {
                if (card != null)
                {
                    card.SetSuggested(card == next);
                }
            }

            int mixPercent = bowl != null ? Mathf.RoundToInt(bowl.MixProgress * 100f) : 0;
            switch (stage)
            {
                case Stage.Ingredients:
                    ShowSteps(StepIngredients, KeyProgressIngredients, added, total);
                    KitchenUIUtil.SetKey(chefBubbleText, next != null && next.Ingredient != null ? next.Ingredient.hintKey : string.Empty);
                    ShowHintBar(StepIngredients, KeyBarDoIngredients, total > 0 ? (float)added / total : 0f);
                    break;
                case Stage.Mixing:
                    ShowSteps(StepMix, KeyProgressMix, mixPercent);
                    KitchenUIUtil.SetKey(chefBubbleText, KeyHintMix);
                    ShowHintBar(StepMix, KeyBarDoMix, bowl != null ? bowl.MixProgress : 0f);
                    break;
                case Stage.Ready:
                    ShowSteps(StepBake, null);
                    KitchenUIUtil.SetKey(chefBubbleText, KeyHintReady);
                    ShowHintBar(StepBake, KeyBarDoBake, 0f);
                    break;
            }
        }

        private void ShowSteps(int activeIndex, string subKey, params object[] args)
        {
            if (stepTracker != null)
            {
                stepTracker.Show(activeIndex, subKey, args);
            }
        }

        // "Adım 2 · Kasede parmağınla daire çiz · Sıradaki: Fırına koy" + ilerleme çubuğu.
        private void ShowHintBar(int stepIndex, string doKey, float progress)
        {
            KitchenUIUtil.SetKey(hintStepText, KeyBarStep, stepIndex + 1);
            KitchenUIUtil.SetKey(hintDoText, doKey);

            int nextIndex = stepIndex + 1;
            bool hasNext = nextIndex < StepNameKeys.Length;
            KitchenUIUtil.SetActive(hintNextText, hasNext);
            if (hasNext)
            {
                KitchenUIUtil.SetKey(hintNextText, KeyBarNext, new LocalizedKeyArg(StepNameKeys[nextIndex]));
            }

            if (hintProgressFill != null)
            {
                hintProgressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), hintProgressFill.anchorMax.y);
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
