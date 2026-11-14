using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Topla'da dilim simgelerini fırın kartından stok kalemine kavisli uçurur.
    /// Simgeler prefab'daki kapalı şablondan üretilip havuzda tutulur; animasyon gerçek zamanla akar (Time.unscaledDeltaTime).
    /// Panel kapanırsa (OnDisable) yarıdaki uçuşlar hemen bitirilir ve bitiş geri çağrıları yine çalışır.
    /// </summary>
    public class SliceFlyAnimator : MonoBehaviour
    {
        [SerializeField] private OvenPanelSettings settings;

        [Tooltip("Uçan simgelerin çizildiği tam ekran katman (panelin en üstünde).")]
        [SerializeField] private RectTransform layer;

        [Tooltip("Kapalı şablon simge; sprite'ı ürün simgesi yoksa kullanılan yer tutucudur.")]
        [SerializeField] private Image iconTemplate;

        // Uçuş sonunda simgenin küçüldüğü ölçek (inişte kalem büyüyerek karşılar).
        private const float EndScale = 0.7f;

        private readonly Stack<Image> pool = new Stack<Image>();
        private readonly List<Flight> flights = new List<Flight>();

        private sealed class Flight
        {
            public readonly List<Image> Icons = new List<Image>();
            public Action Finished;
            public Coroutine Routine;
        }

        private void Awake()
        {
            if (layer == null)
            {
                layer = (RectTransform)transform;
            }

            if (iconTemplate != null)
            {
                iconTemplate.gameObject.SetActive(false);
            }
        }

        private void OnDisable() => CancelAll();

        /// <summary>
        /// count simgeyi from'dan to'ya uçurur. landed(i): i. simge indi (0'dan başlar); finished: hepsi indi ya da iptal edildi.
        /// sprite null ise şablondaki yer tutucu, placeholderColor rengiyle kullanılır.
        /// Çalışamıyorsa (kapalı, referans eksik) simgeler anında inmiş sayılır.
        /// </summary>
        public void Play(RectTransform from, RectTransform to, Sprite sprite, int count, Action<int> landed, Action finished)
        {
            if (!isActiveAndEnabled || settings == null || iconTemplate == null || from == null || to == null || count <= 0)
            {
                for (int i = 0; i < count; i++)
                {
                    landed?.Invoke(i);
                }

                finished?.Invoke();
                return;
            }

            var flight = new Flight { Finished = finished };
            for (int i = 0; i < count; i++)
            {
                Image icon = Rent();
                KitchenUIUtil.SetIcon(icon, sprite, iconTemplate.sprite, settings.iconPlaceholderColor);
                icon.rectTransform.sizeDelta = Vector2.one * settings.flyIconSize;
                icon.gameObject.SetActive(false);
                flight.Icons.Add(icon);
            }

            flights.Add(flight);
            flight.Routine = StartCoroutine(FlyRoutine(flight, from, to, landed));
        }

        /// <summary>Tüm uçuşları hemen bitirir; her uçuşun finished geri çağrısı çalışır.</summary>
        public void CancelAll()
        {
            // Geri çağrılar yeni uçuş başlatabilir; kopya üzerinden dönülür.
            Flight[] active = flights.ToArray();
            flights.Clear();
            foreach (Flight flight in active)
            {
                if (flight.Routine != null)
                {
                    StopCoroutine(flight.Routine);
                }

                ReturnAll(flight);
                flight.Finished?.Invoke();
            }
        }

        private IEnumerator FlyRoutine(Flight flight, RectTransform from, RectTransform to, Action<int> landed)
        {
            int count = flight.Icons.Count;
            var done = new bool[count];
            int landedCount = 0;

            // Çıkış noktası ilk karede sabitlenir; hedef her karede okunur (yerleşim değişse de doğru yere iner).
            Vector3 start = layer.InverseTransformPoint(from.position);
            float elapsed = 0f;

            while (landedCount < count)
            {
                Vector3 end = to != null ? layer.InverseTransformPoint(to.position) : start;
                Vector3 control = (start + end) * 0.5f + Vector3.up * settings.flyArcHeight;

                for (int i = 0; i < count; i++)
                {
                    if (done[i])
                    {
                        continue;
                    }

                    float t = (elapsed - i * settings.flyStagger) / settings.flyDuration;
                    Image icon = flight.Icons[i];
                    if (t < 0f)
                    {
                        continue;
                    }

                    if (t >= 1f)
                    {
                        done[i] = true;
                        landedCount++;
                        icon.gameObject.SetActive(false);
                        landed?.Invoke(i);
                        continue;
                    }

                    // Yumuşak hızlanıp yavaşlayan ikinci derece Bezier kavisi.
                    float eased = t * t * (3f - 2f * t);
                    Vector3 a = Vector3.Lerp(start, control, eased);
                    Vector3 b = Vector3.Lerp(control, end, eased);
                    icon.rectTransform.localPosition = Vector3.Lerp(a, b, eased);
                    icon.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, EndScale, eased);
                    if (!icon.gameObject.activeSelf)
                    {
                        icon.gameObject.SetActive(true);
                    }
                }

                if (landedCount < count)
                {
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                }
            }

            flights.Remove(flight);
            ReturnAll(flight);
            flight.Finished?.Invoke();
        }

        private Image Rent()
        {
            Image icon = pool.Count > 0 ? pool.Pop() : null;
            if (icon == null)
            {
                icon = Instantiate(iconTemplate, layer, false);
                icon.name = "FlyingSlice";
                icon.raycastTarget = false;
            }

            icon.transform.SetAsLastSibling();
            return icon;
        }

        private void ReturnAll(Flight flight)
        {
            foreach (Image icon in flight.Icons)
            {
                if (icon != null)
                {
                    icon.gameObject.SetActive(false);
                    pool.Push(icon);
                }
            }

            flight.Icons.Clear();
        }
    }
}
