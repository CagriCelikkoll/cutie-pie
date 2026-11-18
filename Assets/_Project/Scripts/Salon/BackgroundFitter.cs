using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon arka planı: SalonSettings.backgroundSprite'ı kameranın görüş alanını tamamen kaplayacak şekilde
    /// ölçekler ve kameranın ortasına koyar. Ekran oranı görselden genişse (ör. 20:9 telefon) üst/alt kırpılır,
    /// darsa yanlar kırpılır; boşluk kalmaz. Ekran boyutu veya kamera değişince yeniden sığdırılır.
    /// Görsel atanmamışsa hiçbir şey çizilmez.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private SalonSettings settings;

        [Tooltip("Boşsa MainCamera kullanılır.")]
        [SerializeField] private Camera targetCamera;

        private SpriteRenderer spriteRenderer;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private float lastOrthoSize;
        private Vector3 lastCameraPosition;

        private void Start()
        {
            Fit();
        }

        private void LateUpdate()
        {
            Camera cam = GetCamera();
            if (cam == null)
            {
                return;
            }

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight
                || !Mathf.Approximately(cam.orthographicSize, lastOrthoSize) || cam.transform.position != lastCameraPosition)
            {
                Fit();
            }
        }

        /// <summary>Görseli atar ve kamerayı kaplayacak şekilde sığdırır (kurucu editörde de çağırır).</summary>
        public void Fit()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Sprite sprite = settings != null ? settings.backgroundSprite : null;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.sortingOrder = SalonSortOrder.Background;
            spriteRenderer.enabled = sprite != null;

            Camera cam = GetCamera();
            if (sprite == null || cam == null || !cam.orthographic)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastOrthoSize = cam.orthographicSize;
            lastCameraPosition = cam.transform.position;

            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * cam.aspect;
            Vector2 size = sprite.bounds.size;
            if (size.x <= Mathf.Epsilon || size.y <= Mathf.Epsilon)
            {
                return;
            }

            // Kaplama: iki oranın büyüğü (görsel ekrandan taşabilir, boşluk kalmaz).
            float scale = Mathf.Max(viewWidth / size.x, viewHeight / size.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 camPos = cam.transform.position;
            Vector3 offset = sprite.bounds.center * scale;
            transform.position = new Vector3(camPos.x - offset.x, camPos.y - offset.y, transform.position.z);
        }

        private Camera GetCamera() => targetCamera != null ? targetCamera : Camera.main;
    }
}
