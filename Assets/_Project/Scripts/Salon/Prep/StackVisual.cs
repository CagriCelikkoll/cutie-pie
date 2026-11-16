using UnityEngine;
using UnityEngine.Rendering;

namespace CutiePie.Salon
{
    /// <summary>
    /// Tepsi / tabak yığınının görünümü: SalonSettings'teki görselden birkaç kopyayı kaydırarak üst üste çizer.
    /// Yalnızca görseldir; sürükleme ve collider yığın nesnesinde (TrayStack / PlateStack) kalır.
    /// Görsel atanmamışsa hiçbir şey çizmez; yığının renkli yer tutucu kutusu görünmeye devam eder.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class StackVisual : MonoBehaviour
    {
        /// <summary>Yığında hangi görselin dizileceği.</summary>
        public enum StackKind
        {
            Tray = 0,
            Plate = 1
        }

        [SerializeField] private SalonSettings settings;
        [SerializeField] private StackKind kind;

        [Tooltip("Tek bir kopyanın sığacağı kutu (birim).")]
        [SerializeField] private Vector2 itemBoxSize = Vector2.one;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            Sprite sprite = GetSprite();
            if (sprite == null)
            {
                return;
            }

            int count = Mathf.Max(1, settings.stackVisualCount);
            Vector2 step = settings.stackVisualStep;

            // Yığın kendi merkezinde durur; en üstteki kopya en son çizilir.
            Vector2 start = -step * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Copy_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = start + step * i;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = i;

                SpriteFitter fitter = go.AddComponent<SpriteFitter>();
                fitter.BoxSize = itemBoxSize;
                fitter.Set(sprite, Color.white, true);
            }
        }

        private Sprite GetSprite()
        {
            if (settings == null)
            {
                return null;
            }

            return kind == StackKind.Tray ? settings.traySprite : settings.plateSprite;
        }
    }
}
