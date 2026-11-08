using UnityEngine;
using UnityEngine.EventSystems;

namespace CutiePie.Salon
{
    /// <summary>
    /// Fare ve dokunma ile sürükleme (uGUI EventSystem + Physics2DRaycaster; Input System paketi gerekmez).
    /// Tutulabilir nesnede (collider'lı) durur; taşınacak parçayı aynı GameObject'teki IDragSource verir.
    /// Bırakınca DropTargetRegistry'den en yakın uygun hedef aranır: snapDistance içindeyse hedef parçayı alır,
    /// değilse parça dragReturnSpeed ile başlangıç konumuna döner ve kaynağa haber verilir.
    /// Aynı anda yalnızca bir sürükleme olur; çoklu dokunmada ikinci parmak yok sayılır.
    /// </summary>
    public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private SalonSettings settings;
        [SerializeField] private DropTargetRegistry registry;

        // Tüm sürüklenebilirler için ortak kilit: o an sürükleyen bileşen ve parmak/fare kimliği.
        private static DragItem activeDrag;
        private static int activePointerId;

        private IDragSource source;
        private ServingItem dragged;
        private ServingItem returning;
        private Vector3 startPosition;
        private Vector3 baseScale;
        private int baseSortingOrder;

        /// <summary>Herhangi bir parça şu an parmak/fare altında mı?</summary>
        public static bool IsAnyDragActive => activeDrag != null;

        /// <summary>Bu kaynaktan çıkan parça sürükleniyor veya geri dönüyor.</summary>
        public bool IsBusy => dragged != null || returning != null;

        /// <summary>Çalışma zamanında üretilen sürüklenebilirler (ör. tepsi) için bağımlılıkları verir.</summary>
        public void Init(SalonSettings salonSettings, DropTargetRegistry dropTargets)
        {
            settings = salonSettings;
            registry = dropTargets;
        }

        private void Awake()
        {
            source = GetComponent<IDragSource>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (activeDrag != null || IsBusy || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (source == null || settings == null || registry == null)
            {
                Debug.LogWarning("[DragItem] IDragSource, SalonSettings veya DropTargetRegistry eksik.", this);
                return;
            }

            ServingItem item = source.BeginDrag();
            if (item == null)
            {
                return;
            }

            activeDrag = this;
            activePointerId = eventData.pointerId;
            dragged = item;

            Transform tf = item.transform;
            startPosition = tf.position;
            baseScale = tf.localScale;
            baseSortingOrder = item.SortingGroup.sortingOrder;

            // "Elde tutuluyor" hissi: biraz büyür ve her şeyin üstüne çıkar.
            tf.localScale = baseScale * settings.dragItemScale;
            item.SortingGroup.sortingOrder = SalonSortOrder.Dragged;

            FollowPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsOwnPointer(eventData))
            {
                FollowPointer(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsOwnPointer(eventData))
            {
                return;
            }

            FollowPointer(eventData);

            ServingItem item = dragged;
            dragged = null;
            activeDrag = null;
            RestoreLook(item);

            IDropTarget target = registry.FindBestTarget(item, item.transform.position, settings.snapDistance);
            if (target != null)
            {
                target.Accept(item);
                source.OnDropped(item, target);
            }
            else
            {
                returning = item;
            }
        }

        /// <summary>
        /// Hedefe kabul edilmiş ama yerinde kalması gereken parçayı başlangıç konumuna geri gönderir
        /// (ör. çöp tepsinin içeriğini siler, tepsi tezgâhtaki yerine döner). Bitince kaynağın OnReturned'ı çağrılır.
        /// </summary>
        public void StartReturn(ServingItem item)
        {
            if (item == null || dragged != null)
            {
                return;
            }

            returning = item;
        }

        private void Update()
        {
            if (returning == null)
            {
                return;
            }

            Transform tf = returning.transform;
            tf.position = Vector3.MoveTowards(tf.position, startPosition, settings.dragReturnSpeed * Time.deltaTime);
            if ((tf.position - startPosition).sqrMagnitude > Mathf.Epsilon)
            {
                return;
            }

            ServingItem item = returning;
            returning = null;
            source.OnReturned(item);
        }

        private void OnDisable()
        {
            // Sürükleme/dönüş yarıda kaldıysa parça hemen yerine konur, kilit bırakılır.
            if (activeDrag == this)
            {
                activeDrag = null;
            }

            ServingItem item = dragged != null ? dragged : returning;
            bool wasDragging = dragged != null;
            dragged = null;
            returning = null;

            if (item == null)
            {
                return;
            }

            if (wasDragging)
            {
                RestoreLook(item);
            }

            item.transform.position = startPosition;
            if (source is Object sourceObject && sourceObject != null)
            {
                source.OnReturned(item);
            }
        }

        private bool IsOwnPointer(PointerEventData eventData) =>
            activeDrag == this && dragged != null && eventData.pointerId == activePointerId;

        private void RestoreLook(ServingItem item)
        {
            if (item == null)
            {
                return;
            }

            item.transform.localScale = baseScale;
            item.SortingGroup.sortingOrder = baseSortingOrder;
        }

        private void FollowPointer(PointerEventData eventData)
        {
            if (dragged == null)
            {
                return;
            }

            Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            Transform tf = dragged.transform;
            Vector3 screen = eventData.position;
            screen.z = tf.position.z - cam.transform.position.z;
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = tf.position.z;
            tf.position = world;
        }

        // Domain reload kapalıyken önceki Play oturumundan kalan kilidi temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeDrag = null;
            activePointerId = 0;
        }
    }
}
