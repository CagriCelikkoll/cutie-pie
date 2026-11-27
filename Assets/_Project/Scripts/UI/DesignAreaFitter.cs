using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.UI
{
    /// <summary>
    /// Sabit boyutlu bir tasarım alanını (varsayılan 1920x1080) üst kutuya sığdırır ve ortalar ("Fit In Parent" gibi),
    /// ama AspectRatioFitter'dan farklı olarak içeriği de ölçekler: kutu designSize boyutunda kalır, localScale
    /// min(üstGenişlik / tasarımGenişliği, üstYükseklik / tasarımYüksekliği) olur. Böylece alt objeler, fontlar ve
    /// yerleşim ızgaraları oranı bozulmadan birlikte küçülür / büyür; tasarım alanının dışı hiçbir ekranda kesilmez.
    /// Anchor, pivot, konum, boyut ve ölçek bu bileşen tarafından sürülür (Inspector'da kilitli görünür).
    /// Editörde de çalışır: Game görünümü Play'e basmadan doğru görünür.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DesignAreaFitter : UIBehaviour
    {
        private const DrivenTransformProperties DrivenProperties =
            DrivenTransformProperties.Anchors | DrivenTransformProperties.Pivot | DrivenTransformProperties.AnchoredPosition |
            DrivenTransformProperties.SizeDelta | DrivenTransformProperties.Scale;

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        [Tooltip("Tasarım alanının referans boyutu (içerik bu boyuta göre yerleştirilir).")]
        [SerializeField] private Vector2 designSize = new Vector2(1920f, 1080f);

        private RectTransform rectTransformCache;
        private DrivenRectTransformTracker tracker;
        private Vector2 lastParentSize = new Vector2(-1f, -1f);
        private bool applying;

        public Vector2 DesignSize
        {
            get => designSize;
            set
            {
                designSize = Sanitize(value);
                Apply();
            }
        }

        private RectTransform SelfRect =>
            rectTransformCache != null ? rectTransformCache : rectTransformCache = (RectTransform)transform;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        protected override void OnDisable()
        {
            tracker.Clear();
            lastParentSize = new Vector2(-1f, -1f);
            base.OnDisable();
        }

        // Kendi kutusu değişince (ör. biri sizeDelta'yı elle değiştirdi) yeniden eşitlenir.
        protected override void OnRectTransformDimensionsChange() => Apply();

        // Başka bir üst kutunun altına taşınınca yeni üst kutuya göre yeniden hesaplanır.
        protected override void OnTransformParentChanged() => Apply();

        protected override void OnCanvasHierarchyChanged() => Apply();

        // Yedek denetim: üst kutunun boyutu her durumda bildirim göndermeyebilir (ör. sabit boyutlu çocuğa
        // ekran yönü değişimi). Boyut aynıysa hiçbir şey yapılmaz.
        private void LateUpdate()
        {
            RectTransform parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.size != lastParentSize)
            {
                Apply();
            }
        }

#if UNITY_EDITOR
        // OnValidate içinde RectTransform değiştirmek uyarı verir; bir sonraki LateUpdate uygular.
        protected override void OnValidate()
        {
            base.OnValidate();
            designSize = Sanitize(designSize);
            lastParentSize = new Vector2(-1f, -1f);
        }
#endif

        private void Apply()
        {
            if (applying || !isActiveAndEnabled)
            {
                return;
            }

            RectTransform parent = transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            applying = true;
            try
            {
                RectTransform tf = SelfRect;
                tracker.Clear();
                tracker.Add(this, tf, DrivenProperties);

                SetIfDifferent(tf.anchorMin, Center, v => tf.anchorMin = v);
                SetIfDifferent(tf.anchorMax, Center, v => tf.anchorMax = v);
                SetIfDifferent(tf.pivot, Center, v => tf.pivot = v);
                SetIfDifferent(tf.anchoredPosition, Vector2.zero, v => tf.anchoredPosition = v);
                SetIfDifferent(tf.sizeDelta, designSize, v => tf.sizeDelta = v);

                Vector2 parentSize = parent.rect.size;
                lastParentSize = parentSize;

                // Üst kutu henüz boyutlanmadıysa (ör. prefab düzenleme sahnesi, 0 boyutlu kök Canvas) ölçek korunur.
                if (parentSize.x <= 0f || parentSize.y <= 0f)
                {
                    return;
                }

                float scale = Mathf.Min(parentSize.x / designSize.x, parentSize.y / designSize.y);
                var desired = new Vector3(scale, scale, 1f);
                if (tf.localScale != desired)
                {
                    tf.localScale = desired;
                }
            }
            finally
            {
                applying = false;
            }
        }

        private static void SetIfDifferent(Vector2 current, Vector2 desired, System.Action<Vector2> apply)
        {
            if (current != desired)
            {
                apply(desired);
            }
        }

        private static Vector2 Sanitize(Vector2 size) => new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    }
}
