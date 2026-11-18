using System;
using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using CutiePie.Kitchen;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Özel sipariş pasta yapım ekranı (G20, Prefabs/UI/Kitchen/CakeBuilder.prefab). OrderBoard.prefab'ın içinde iç içe durur,
    /// kendi alt Canvas'ı vardır (sıralama 6) ve açılınca tüm ekranı kaplar.
    /// Yerleşim: solda fiş (hedef pasta hep görünür), ortada oyuncunun pastası, altta adımın seçenekleri, iki yanda Geri / İleri.
    /// Adımlar: 1 Taban → 2 Krema rengi → 3 Süs (süsler pastaya sürüklenir, en fazla KitchenSettings.maxDecorations,
    /// konulmuş süse dokununca geri alınır) → 4 Kutu → Onayla.
    /// - Seçeneğe dokununca pastada görünür; İleri seçim yoksa kapalıdır, süs adımında hep açıktır. Son adımda İleri yerine Onayla.
    /// - Her adımda Geri; ilk adımda Geri panoya döner. Taslak (seçimler) fiş başına saklanır: onaylanana ya da fiş panodan
    ///   kalkana kadar korunur.
    /// - Onayla: ICustomOrderService.Complete hemen çağrılır (altın verilir), sonuç kartı o sonuçla oynar, "Panoya dön" ekranı kapatır.
    /// - Yapılan fiş panodan başka yolla kalkarsa (Kaydı Sil, test menüsü) ekran kapanır.
    /// Süs koordinatı CakeBuild sözleşmesine uyar (CakeView.TryGetCakePosition). Animasyonlar gerçek zamanlıdır.
    /// </summary>
    public class CakeBuilder : MonoBehaviour
    {
        /// <summary>Yapım adımları (sırası ekrandaki sıradır).</summary>
        public enum Step
        {
            Base = 0,
            Cream = 1,
            Decoration = 2,
            Box = 3
        }

        private const int StepCount = 4;

        private const string KeyStepCounter = "order.builder.step_counter";
        private const string KeyStepBase = "order.step.base";
        private const string KeyStepCream = "order.step.cream";
        private const string KeyStepDecoration = "order.step.decor";
        private const string KeyStepBox = "order.step.box";
        private const string KeyDecorationCount = "order.decor.count";
        private const string KeyNext = "order.builder.next";
        private const string KeyConfirm = "order.builder.confirm";

        [SerializeField] private OrderUISettings settings;

        [Tooltip("Gösterilip gizlenen içerik (zemin ve tüm ekran).")]
        [SerializeField] private GameObject content;

        [Header("Başlık")]
        [SerializeField] private LocalizedText stepCounterText;
        [SerializeField] private LocalizedText stepTitleText;

        [Header("Fiş (hedef)")]
        [SerializeField] private CakeView ticketCake;
        [SerializeField] private TMP_Text ticketRewardText;

        [Header("Oyuncunun pastası")]
        [SerializeField] private CakeView buildCake;

        [Tooltip("Süs adımında görünen sayaç ve ipucu.")]
        [SerializeField] private GameObject decorationInfo;

        [SerializeField] private LocalizedText decorationCountText;

        [Header("Seçenekler")]
        [Tooltip("Seçenek kartlarının eklendiği kap (HorizontalLayoutGroup).")]
        [SerializeField] private RectTransform optionContainer;

        [Tooltip("Prefabs/UI/Kitchen/PartOption.prefab (PartOption + DecorationDragSource).")]
        [SerializeField] private PartOption optionPrefab;

        [Header("Butonlar")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Image nextButtonImage;
        [SerializeField] private LocalizedText nextLabel;

        [Header("Sürükleme")]
        [Tooltip("Sürüklenen süsün çizildiği en üst katman (tıklama almaz).")]
        [SerializeField] private RectTransform dragLayer;

        [SerializeField] private Image dragGhost;

        [Header("Sonuç")]
        [SerializeField] private OrderResultView resultView;

        /// <summary>Ekran kapandı (panoya dönüldü).</summary>
        public event Action Closed;

        private readonly Dictionary<string, CakeBuild> drafts = new Dictionary<string, CakeBuild>();
        private readonly List<PartOption> options = new List<PartOption>();
        private readonly List<CakePartData> partsBuffer = new List<CakePartData>();

        private CustomOrder order;
        private CakeBuild draft;
        private Step step;

        // Onayla'ya basıldı: Complete fişi panodan kaldırır; bu arada gelen pano olayı ekranı kapatmasın.
        private bool completing;

        // O an süs sürükleyen kart ve parmak/fare kimliği; aynı anda tek sürükleme.
        private DecorationDragSource activeDrag;
        private int activePointerId;

        public bool IsOpen => content != null && content.activeSelf;

        public Step CurrentStep => step;

        private static GameManager Game => GameManager.Instance;

        private static KitchenSettings Kitchen => Game != null && Game.Config != null ? Game.Config.kitchen : null;

        private static ICustomOrderService Orders => Game != null ? Game.Orders : null;

        // ------------------------------------------------------------------ Yaşam döngüsü

        private void Awake()
        {
            if (settings == null || content == null || ticketCake == null || buildCake == null || optionContainer == null ||
                optionPrefab == null || backButton == null || nextButton == null || dragLayer == null || dragGhost == null ||
                resultView == null)
            {
                Debug.LogError("[CakeBuilder] Referanslar eksik; 'Cutie Pie/UI/Sipariş Ekranını Kur' menüsünü çalıştırın.", this);
            }

            AddListener(backButton, HandleBack);
            AddListener(nextButton, HandleNext);

            if (buildCake != null)
            {
                buildCake.DecorationTapped += HandleDecorationTapped;
            }

            if (resultView != null)
            {
                resultView.ContinueClicked += HandleResultContinue;
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            RemoveListener(backButton, HandleBack);
            RemoveListener(nextButton, HandleNext);

            if (buildCake != null)
            {
                buildCake.DecorationTapped -= HandleDecorationTapped;
            }

            if (resultView != null)
            {
                resultView.ContinueClicked -= HandleResultContinue;
            }

            foreach (PartOption option in options)
            {
                UnbindOption(option);
            }
        }

        private void OnEnable() => GameEvents.CustomOrderBoardChanged += HandleBoardChanged;

        private void OnDisable()
        {
            GameEvents.CustomOrderBoardChanged -= HandleBoardChanged;
            CancelDrag();
        }

        // ------------------------------------------------------------------ Aç / kapat

        /// <summary>Fişin yapım ekranını açar; fişin taslağı varsa seçimler korunur. İlk adımdan başlar.</summary>
        public void Open(CustomOrder target)
        {
            KitchenSettings kitchen = Kitchen;
            if (target == null || content == null || kitchen == null)
            {
                return;
            }

            order = target;
            completing = false;
            if (!drafts.TryGetValue(target.id, out draft))
            {
                draft = new CakeBuild();
                drafts.Add(target.id, draft);
            }

            content.SetActive(true);
            if (resultView != null)
            {
                resultView.Hide();
            }

            if (ticketCake != null)
            {
                ticketCake.Render(target.target, kitchen);
            }

            if (ticketRewardText != null)
            {
                ticketRewardText.text = Mathf.Max(0, target.baseReward).ToString();
            }

            ShowStep(Step.Base);
        }

        /// <summary>Ekranı kapatır (taslak korunur) ve panoya döner.</summary>
        public void Close()
        {
            bool wasOpen = IsOpen;
            HideImmediate();
            if (wasOpen)
            {
                Closed?.Invoke();
            }
        }

        private void HideImmediate()
        {
            CancelDrag();
            if (resultView != null)
            {
                resultView.Hide();
            }

            if (content != null)
            {
                content.SetActive(false);
            }

            order = null;
            draft = null;
            completing = false;
        }

        // ------------------------------------------------------------------ Adımlar

        private void ShowStep(Step newStep)
        {
            CancelDrag();
            step = newStep;

            KitchenUIUtil.SetKey(stepCounterText, KeyStepCounter, (int)step + 1, StepCount);
            KitchenUIUtil.SetKey(stepTitleText, StepTitleKey(step));

            bool decorating = step == Step.Decoration;
            KitchenUIUtil.SetActive(decorationInfo, decorating);
            if (buildCake != null)
            {
                buildCake.SetInteractive(decorating);
            }

            BuildOptions();
            RefreshAll();
        }

        private void BuildOptions()
        {
            partsBuffer.Clear();
            KitchenSettings kitchen = Kitchen;
            if (kitchen != null && kitchen.cakeParts != null)
            {
                CakePartType type = PartType(step);
                foreach (CakePartData part in kitchen.cakeParts)
                {
                    if (part != null && part.type == type && !string.IsNullOrEmpty(part.id))
                    {
                        partsBuffer.Add(part);
                    }
                }
            }

            EnsureOptions(partsBuffer.Count);
            for (int i = 0; i < options.Count; i++)
            {
                PartOption option = options[i];
                bool used = i < partsBuffer.Count;
                KitchenUIUtil.SetActive(option, used);
                if (used)
                {
                    option.Bind(partsBuffer[i]);
                }
            }
        }

        private void EnsureOptions(int count)
        {
            if (optionPrefab == null || optionContainer == null)
            {
                return;
            }

            while (options.Count < count)
            {
                PartOption option = Instantiate(optionPrefab, optionContainer, false);
                option.name = $"PartOption_{options.Count + 1}";
                option.Clicked += HandleOptionClicked;

                DecorationDragSource drag = option.GetComponent<DecorationDragSource>();
                if (drag != null)
                {
                    drag.DragStarted += HandleDragStarted;
                    drag.DragMoved += HandleDragMoved;
                    drag.DragEnded += HandleDragEnded;
                }

                options.Add(option);
            }
        }

        // Pasta, seçili çerçeveler, süs sayacı ve butonlar taslaktan yenilenir.
        private void RefreshAll()
        {
            if (draft == null)
            {
                return;
            }

            KitchenSettings kitchen = Kitchen;
            if (buildCake != null)
            {
                buildCake.Render(draft, kitchen);
            }

            bool decorating = step == Step.Decoration;
            int maxDecorations = MaxDecorations;
            bool canAddDecoration = draft.decorations.Count < maxDecorations;
            string selectedId = SelectedId(step);

            foreach (PartOption option in options)
            {
                if (option == null || !option.gameObject.activeSelf || option.Part == null)
                {
                    continue;
                }

                option.SetSelected(!decorating && option.Part.id == selectedId);
                option.SetAvailable(!completing && (!decorating || canAddDecoration));
            }

            if (decorating)
            {
                KitchenUIUtil.SetKey(decorationCountText, KeyDecorationCount, draft.decorations.Count, maxDecorations);
            }

            bool last = step == Step.Box;
            KitchenUIUtil.SetKey(nextLabel, last ? KeyConfirm : KeyNext);
            if (nextButtonImage != null && settings != null)
            {
                nextButtonImage.color = last ? settings.confirmButtonColor : settings.nextButtonColor;
            }

            KitchenUIUtil.SetInteractable(nextButton, !completing && (decorating || !string.IsNullOrEmpty(selectedId)));
            KitchenUIUtil.SetInteractable(backButton, !completing);
        }

        private int MaxDecorations => Kitchen != null ? Mathf.Max(0, Kitchen.maxDecorations) : 0;

        private string SelectedId(Step s)
        {
            switch (s)
            {
                case Step.Base:
                    return draft.baseId;
                case Step.Cream:
                    return draft.creamId;
                case Step.Box:
                    return draft.boxId;
                default:
                    return null;
            }
        }

        private static CakePartType PartType(Step s)
        {
            switch (s)
            {
                case Step.Cream:
                    return CakePartType.Cream;
                case Step.Decoration:
                    return CakePartType.Decoration;
                case Step.Box:
                    return CakePartType.Box;
                default:
                    return CakePartType.Base;
            }
        }

        private static string StepTitleKey(Step s)
        {
            switch (s)
            {
                case Step.Cream:
                    return KeyStepCream;
                case Step.Decoration:
                    return KeyStepDecoration;
                case Step.Box:
                    return KeyStepBox;
                default:
                    return KeyStepBase;
            }
        }

        // ------------------------------------------------------------------ Seçim ve süsler

        private void HandleOptionClicked(PartOption option)
        {
            if (draft == null || completing || option == null || option.Part == null)
            {
                return;
            }

            // Süsler dokunmayla değil sürüklemeyle konur.
            switch (step)
            {
                case Step.Base:
                    draft.baseId = option.Part.id;
                    break;
                case Step.Cream:
                    draft.creamId = option.Part.id;
                    break;
                case Step.Box:
                    draft.boxId = option.Part.id;
                    break;
                default:
                    return;
            }

            RefreshAll();
        }

        private void HandleDecorationTapped(int index)
        {
            if (draft == null || completing || step != Step.Decoration || index < 0 || index >= draft.decorations.Count)
            {
                return;
            }

            draft.decorations.RemoveAt(index);
            RefreshAll();
        }

        private void HandleDragStarted(DecorationDragSource source, PointerEventData eventData)
        {
            if (activeDrag != null || draft == null || completing || step != Step.Decoration ||
                draft.decorations.Count >= MaxDecorations || dragGhost == null || buildCake == null)
            {
                return;
            }

            activeDrag = source;
            activePointerId = eventData.pointerId;

            // Sürüklenen görsel pastadaki süsle aynı görünür, aynı boyda (biraz büyük) ve yarı saydamdır.
            buildCake.ApplyDecorationLook(dragGhost, source.Option.Part);
            Color color = dragGhost.color;
            color.a *= settings != null ? settings.dragGhostAlpha : 1f;
            dragGhost.color = color;
            dragGhost.raycastTarget = false;
            dragGhost.rectTransform.sizeDelta = GhostSize();
            dragGhost.transform.SetAsLastSibling();
            dragGhost.gameObject.SetActive(true);

            MoveGhost(eventData);
        }

        private void HandleDragMoved(DecorationDragSource source, PointerEventData eventData)
        {
            if (IsActiveDrag(source, eventData))
            {
                MoveGhost(eventData);
            }
        }

        private void HandleDragEnded(DecorationDragSource source, PointerEventData eventData)
        {
            if (!IsActiveDrag(source, eventData))
            {
                return;
            }

            CakePartData part = source.Option != null ? source.Option.Part : null;
            Camera camera = eventData.pressEventCamera;
            CancelDrag();

            // Pasta yüzeyinin (ya da yarım süs boyu yakınının) dışına bırakılan süs konmaz.
            if (part == null || draft == null || completing || step != Step.Decoration ||
                draft.decorations.Count >= MaxDecorations ||
                !buildCake.TryGetCakePosition(eventData.position, camera, out Vector2 position))
            {
                return;
            }

            draft.decorations.Add(new DecorationPlacement { decorationId = part.id, position = position });
            RefreshAll();
        }

        private bool IsActiveDrag(DecorationDragSource source, PointerEventData eventData) =>
            activeDrag != null && activeDrag == source && eventData.pointerId == activePointerId;

        private void MoveGhost(PointerEventData eventData)
        {
            if (dragLayer != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(dragLayer, eventData.position,
                    eventData.pressEventCamera, out Vector3 world))
            {
                dragGhost.transform.position = world;
            }
        }

        // Pastadaki süs boyu (yüzey genişliği × oran), sürükleme katmanının ölçeğine çevrilmiş.
        private Vector2 GhostSize()
        {
            RectTransform surface = buildCake.Surface;
            if (surface == null || dragLayer == null)
            {
                return Vector2.zero;
            }

            float layerScale = Mathf.Approximately(dragLayer.lossyScale.x, 0f) ? 1f : dragLayer.lossyScale.x;
            float side = surface.rect.width * surface.lossyScale.x / layerScale * buildCake.DecorationSizeRatio;
            side *= settings != null ? settings.dragGhostScale : 1f;
            return new Vector2(side, side);
        }

        private void CancelDrag()
        {
            activeDrag = null;
            if (dragGhost != null)
            {
                dragGhost.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Butonlar

        private void HandleBack()
        {
            if (completing || draft == null)
            {
                return;
            }

            if (step == Step.Base)
            {
                Close();
            }
            else
            {
                ShowStep(step - 1);
            }
        }

        private void HandleNext()
        {
            if (completing || draft == null)
            {
                return;
            }

            if (step == Step.Box)
            {
                Confirm();
            }
            else
            {
                ShowStep(step + 1);
            }
        }

        private void Confirm()
        {
            ICustomOrderService orders = Orders;
            if (orders == null || order == null || string.IsNullOrEmpty(draft.boxId))
            {
                return;
            }

            completing = true;
            RefreshAll();

            // Taslağın kopyası gönderilir; servis kayda yazarken taslakla aynı listeyi paylaşmasın.
            string orderId = order.id;
            CakeBuild build = JsonUtility.FromJson<CakeBuild>(JsonUtility.ToJson(draft));
            CustomOrderResult result = orders.Complete(orderId, build);
            drafts.Remove(orderId);

            if (result == null)
            {
                Debug.LogWarning($"[CakeBuilder] Fiş tamamlanamadı (panoda yok): {orderId}", this);
                Close();
                return;
            }

            if (resultView != null)
            {
                resultView.Show(result);
            }
            else
            {
                Close();
            }
        }

        private void HandleResultContinue() => Close();

        // ------------------------------------------------------------------ Pano olayı

        // Panodan kalkan fişlerin taslakları silinir; yapılan fiş kalktıysa (onaylama dışında) ekran kapanır.
        private void HandleBoardChanged()
        {
            ICustomOrderService orders = Orders;
            if (orders == null)
            {
                return;
            }

            IReadOnlyList<CustomOrder> board = orders.Board;
            var stale = new List<string>();
            foreach (string id in drafts.Keys)
            {
                if (!Contains(board, id))
                {
                    stale.Add(id);
                }
            }

            foreach (string id in stale)
            {
                drafts.Remove(id);
            }

            if (IsOpen && !completing && order != null && !Contains(board, order.id))
            {
                Close();
            }
        }

        private static bool Contains(IReadOnlyList<CustomOrder> board, string orderId)
        {
            foreach (CustomOrder item in board)
            {
                if (item != null && item.id == orderId)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------ Yardımcılar

        private void UnbindOption(PartOption option)
        {
            if (option == null)
            {
                return;
            }

            option.Clicked -= HandleOptionClicked;
            DecorationDragSource drag = option.GetComponent<DecorationDragSource>();
            if (drag != null)
            {
                drag.DragStarted -= HandleDragStarted;
                drag.DragMoved -= HandleDragMoved;
                drag.DragEnded -= HandleDragEnded;
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
