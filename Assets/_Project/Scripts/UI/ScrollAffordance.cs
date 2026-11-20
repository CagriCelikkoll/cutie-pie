using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Yatay ScrollRect'in kaydırılabildiğini belli eder: sol/sağ ok butonları, kenar solmaları ve fare tekerleği.
    /// ScrollRect ile aynı nesnede durur. Sığmayan öğe yoksa oklar ve solmalar gizlenir; uçlarda o yöndeki ok pasif olur,
    /// o kenardaki solma kapanır. İnce kaydırma çubuğu ScrollRect'in kendi Scrollbar'ıdır (AutoHide).
    /// Tekerlek bu bileşende işlenir (ScrollRect'in scrollSensitivity'si 0 tutulur): her tık bir adım kaydırır,
    /// aşağı/sağ ileri gider. Durum her karede yeniden okunur; öğe sayısı veya ekran boyutu değişince kendiliğinden güncellenir.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollAffordance : MonoBehaviour, IScrollHandler, IBeginDragHandler
    {
        // Uçta sayılmak için kalan kaydırma payı (referans piksel).
        private const float EdgeEpsilon = 1f;

        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject leftFade;
        [SerializeField] private GameObject rightFade;

        [Tooltip("Ok veya tekerlek tıkı başına kayma (piksel); genelde bir öğe genişliği + aralık.")]
        [SerializeField] private float step = 410f;

        [Tooltip("Ok/tekerlek kaymasının süresi (sn, zaman ölçeğinden bağımsız).")]
        [SerializeField] private float animationDuration = 0.25f;

        private bool animating;
        private float animationTime;
        private float fromOffset;
        private float toOffset;

        private void Awake()
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
            }

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(ScrollPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(ScrollNext);
            }
        }

        private void OnDestroy()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(ScrollPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(ScrollNext);
            }
        }

        // Durum OnEnable'da değil LateUpdate'te güncellenir: etkinleşme sırasında çocukların etkinliği değiştirilemez,
        // LateUpdate ise aynı karede çizimden önce çalışır.
        private void OnDisable() => animating = false;

        private void Update()
        {
            if (!animating)
            {
                return;
            }

            animationTime += Time.unscaledDeltaTime;
            float t = animationDuration > 0f ? Mathf.Clamp01(animationTime / animationDuration) : 1f;
            SetOffset(Mathf.Lerp(fromOffset, toOffset, Mathf.SmoothStep(0f, 1f, t)));
            if (t >= 1f)
            {
                animating = false;
            }
        }

        private void LateUpdate() => UpdateState();

        public void ScrollPrevious() => ScrollBy(-step);

        public void ScrollNext() => ScrollBy(step);

        /// <summary>İçeriği pixels kadar (pozitif: sağdaki öğelere doğru) yumuşakça kaydırır; uçlarda durur.</summary>
        public void ScrollBy(float pixels)
        {
            float hidden = HiddenWidth;
            if (scrollRect == null || hidden <= EdgeEpsilon)
            {
                return;
            }

            // Kayma sürerken yeni adım, varılacak noktadan devam eder; art arda tıklamalar birikir.
            float start = animating ? toOffset : CurrentOffset;
            fromOffset = CurrentOffset;
            toOffset = Mathf.Clamp(start + pixels, 0f, hidden);
            animationTime = 0f;
            animating = true;
            scrollRect.StopMovement();
        }

        public void OnScroll(PointerEventData eventData)
        {
            Vector2 delta = eventData.scrollDelta;

            // Dikey tekerlekte aşağı (negatif y) ileri; yatay tekerlek/dokunmatik yüzeyde sağ (pozitif x) ileri.
            float amount = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? delta.x : -delta.y;
            if (Mathf.Approximately(amount, 0f))
            {
                return;
            }

            ScrollBy(Mathf.Sign(amount) * step);
        }

        // Kullanıcı sürüklemeye başlarsa kayma bırakılır.
        public void OnBeginDrag(PointerEventData eventData) => animating = false;

        // ------------------------------------------------------------------ Durum

        private float ViewportWidth
        {
            get
            {
                RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
                return viewport.rect.width;
            }
        }

        // İçeriğin görünen alana sığmayan genişliği.
        private float HiddenWidth =>
            scrollRect != null && scrollRect.content != null
                ? Mathf.Max(0f, scrollRect.content.rect.width - ViewportWidth)
                : 0f;

        private float CurrentOffset => scrollRect.horizontalNormalizedPosition * HiddenWidth;

        private void SetOffset(float offset)
        {
            float hidden = HiddenWidth;
            if (hidden <= EdgeEpsilon)
            {
                return;
            }

            scrollRect.StopMovement();
            scrollRect.horizontalNormalizedPosition = offset / hidden;
        }

        private void UpdateState()
        {
            if (scrollRect == null)
            {
                return;
            }

            float hidden = HiddenWidth;
            bool scrollable = hidden > EdgeEpsilon;
            float offset = scrollable ? CurrentOffset : 0f;
            bool atStart = !scrollable || offset <= EdgeEpsilon;
            bool atEnd = !scrollable || offset >= hidden - EdgeEpsilon;

            SetButton(previousButton, scrollable, !atStart);
            SetButton(nextButton, scrollable, !atEnd);
            SetActive(leftFade, !atStart);
            SetActive(rightFade, !atEnd);
        }

        private static void SetButton(Button button, bool visible, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            SetActive(button.gameObject, visible);
            if (button.interactable != interactable)
            {
                button.interactable = interactable;
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }
    }
}
