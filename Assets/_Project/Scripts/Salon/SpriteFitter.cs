using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>Sprite kutuya sığdırıldıktan sonra görselin kutu içindeki yeri.</summary>
    public enum FitAlignment
    {
        /// <summary>Konuma dokunulmaz (yalnızca ölçek değişir).</summary>
        KeepPosition = 0,

        /// <summary>Görselin dolu alanı kutunun ortasına gelir.</summary>
        Center = 1,

        /// <summary>Görselin tabanı kutunun tabanına oturur, yatayda ortalanır (ayakta duran müşteri, masa ayakları).</summary>
        Bottom = 2
    }

    /// <summary>
    /// Görsel (SpriteRenderer) nesnesinde durur ve sprite'ı sabit bir kutuya sığdırır; yalnızca bu nesnenin ölçeğini
    /// (ve hizalama seçiliyse konumunu) değiştirir. Kök nesne, collider'lar ve bırakma noktaları etkilenmez.
    /// Gerçek görsel oranı korunarak kutunun içine sığar; yer tutucu kutu ise eskisi gibi kutuyu tam doldurur.
    /// boxSize 0 ise ilk kullanımda mevcut görünümden (yer tutucu sprite × ölçek) alınır; böylece eski prefab'lar da çalışır.
    /// Durum görselleri (ör. müşteri modları) aynı referans sprite'ın ölçeğiyle çizilerek boyları zıplamaz.
    /// 9-slice (SetSliced): yükseklik kutuya oturur, kenarlar oranı korur, yalnızca orta kısım yatayda uzar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFitter : MonoBehaviour
    {
        [Tooltip("Görselin sığacağı kutu (birim, ebeveyne göre). 0 ise ilk kullanımda mevcut görünümden alınır.")]
        [SerializeField] private Vector2 boxSize;

        [Tooltip("Hizalama KeepPosition değilse kutunun merkezi (ebeveyne göre).")]
        [SerializeField] private Vector2 boxCenter;

        [SerializeField] private FitAlignment alignment = FitAlignment.KeepPosition;

        private SpriteRenderer spriteRenderer;

        public SpriteRenderer Renderer
        {
            get
            {
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponent<SpriteRenderer>();
                }

                return spriteRenderer;
            }
        }

        public Vector2 BoxSize
        {
            get => boxSize;
            set => boxSize = value;
        }

        /// <summary>Hedefteki SpriteFitter; yoksa eklenir (kutu ilk kullanımda mevcut görünümden alınır).</summary>
        public static SpriteFitter GetOrAdd(SpriteRenderer target)
        {
            SpriteFitter fitter = target.GetComponent<SpriteFitter>();
            return fitter != null ? fitter : target.gameObject.AddComponent<SpriteFitter>();
        }

        /// <summary>
        /// Hedefe sprite ve renk verir, sonra kutuya sığdırır. Hedefte SpriteFitter yoksa eklenir.
        /// preserveAspect: gerçek görsel için true (oran korunur), yer tutucu kutu için false (kutuyu doldurur).
        /// </summary>
        public static void Apply(SpriteRenderer target, Sprite sprite, Color color, bool preserveAspect)
        {
            if (target != null)
            {
                GetOrAdd(target).Set(sprite, color, preserveAspect);
            }
        }

        /// <summary>Kutuyu ve hizalamayı ayarlar (ör. SalonSettings'ten çalışma zamanında).</summary>
        public void Configure(Vector2 size, Vector2 center, FitAlignment fitAlignment)
        {
            boxSize = size;
            boxCenter = center;
            alignment = fitAlignment;
        }

        /// <summary>
        /// Sprite ve rengi verir, kutuya sığdırır. scaleReference verilirse ölçek o sprite'a göre hesaplanır
        /// (aynı karakterin / nesnenin durum görselleri aynı boyda görünsün diye).
        /// </summary>
        public void Set(Sprite sprite, Color color, bool preserveAspect, Sprite scaleReference = null)
        {
            // Kutu, sprite değişmeden önceki görünümden alınmalı.
            CaptureBoxIfNeeded();

            Renderer.drawMode = SpriteDrawMode.Simple;
            Renderer.sprite = sprite;
            Renderer.color = color;

            if (sprite == null)
            {
                return;
            }

            Sprite reference = scaleReference != null ? scaleReference : sprite;
            if (!TryGetScale(reference, preserveAspect, out Vector2 scale))
            {
                return;
            }

            transform.localScale = new Vector3(scale.x, scale.y, 1f);
            Align(sprite.bounds, scale);
        }

        /// <summary>
        /// 9-slice çizim: görsel yükseklikte kutuya oturur (kenarlar oranı korur), orta kısım kutu genişliğine uzar.
        /// Sprite'ta border yoksa görsel yatayda bütün olarak uzar. Sprite Full Rect mesh olmalıdır.
        /// </summary>
        public void SetSliced(Sprite sprite, Color color)
        {
            CaptureBoxIfNeeded();

            Renderer.sprite = sprite;
            Renderer.color = color;
            if (sprite == null)
            {
                Renderer.drawMode = SpriteDrawMode.Simple;
                return;
            }

            Vector2 native = sprite.bounds.size;
            if (native.x <= Mathf.Epsilon || native.y <= Mathf.Epsilon || boxSize.y <= Mathf.Epsilon)
            {
                return;
            }

            float scale = boxSize.y / native.y;
            Renderer.drawMode = SpriteDrawMode.Sliced;
            Renderer.size = new Vector2(Mathf.Max(native.x, boxSize.x / scale), native.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            // Uzatılmış görselin sınırları: pivot'un genişliğe oranı korunur.
            Vector2 nativeMin = sprite.bounds.min;
            float pivotRatioX = native.x > 0f ? -nativeMin.x / native.x : 0.5f;
            var size = Renderer.size;
            var center = new Vector3(size.x * (0.5f - pivotRatioX), sprite.bounds.center.y, 0f);
            Align(new Bounds(center, new Vector3(size.x, size.y, 0f)), new Vector2(scale, scale));
        }

        private bool TryGetScale(Sprite reference, bool preserveAspect, out Vector2 scale)
        {
            scale = Vector2.one;
            Vector2 size = reference.bounds.size;
            if (size.x <= Mathf.Epsilon || size.y <= Mathf.Epsilon)
            {
                return false;
            }

            float scaleX = boxSize.x / size.x;
            float scaleY = boxSize.y / size.y;
            if (preserveAspect)
            {
                float uniform = Mathf.Min(scaleX, scaleY);
                scaleX = uniform;
                scaleY = uniform;
            }

            scale = new Vector2(scaleX, scaleY);
            return true;
        }

        // bounds: sprite'ın kendi (ölçeksiz) sınırları; pivot ne olursa olsun görsel kutuya göre konumlanır.
        private void Align(Bounds bounds, Vector2 scale)
        {
            if (alignment == FitAlignment.KeepPosition)
            {
                return;
            }

            float x = boxCenter.x - bounds.center.x * scale.x;
            float y = alignment == FitAlignment.Bottom
                ? boxCenter.y - boxSize.y * 0.5f - bounds.min.y * scale.y
                : boxCenter.y - bounds.center.y * scale.y;

            transform.localPosition = new Vector3(x, y, transform.localPosition.z);
        }

        private void CaptureBoxIfNeeded()
        {
            if (boxSize.x > 0f && boxSize.y > 0f)
            {
                return;
            }

            Sprite current = Renderer.sprite;
            Vector2 spriteSize = current != null ? (Vector2)current.bounds.size : Vector2.one;
            Vector3 scale = transform.localScale;
            boxSize = new Vector2(spriteSize.x * Mathf.Abs(scale.x), spriteSize.y * Mathf.Abs(scale.y));
            boxCenter = transform.localPosition;
        }
    }
}
