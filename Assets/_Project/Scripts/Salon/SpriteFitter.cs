using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Görsel (SpriteRenderer) nesnesinde durur ve sprite'ı sabit bir kutuya sığdırır; yalnızca bu nesnenin ölçeğini değiştirir.
    /// Kök nesne, collider'lar ve bırakma noktaları etkilenmez.
    /// Gerçek görsel oranı korunarak kutunun içine sığar; yer tutucu kutu ise eskisi gibi kutuyu tam doldurur.
    /// boxSize 0 ise ilk kullanımda mevcut görünümden (yer tutucu sprite × ölçek) alınır; böylece eski prefab'lar da çalışır.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFitter : MonoBehaviour
    {
        [Tooltip("Görselin sığacağı kutu (birim, ebeveyne göre). 0 ise ilk kullanımda mevcut görünümden alınır.")]
        [SerializeField] private Vector2 boxSize;

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

        /// <summary>
        /// Hedefe sprite ve renk verir, sonra kutuya sığdırır. Hedefte SpriteFitter yoksa eklenir (kutu mevcut görünümden alınır).
        /// preserveAspect: gerçek görsel için true (oran korunur), yer tutucu kutu için false (kutuyu doldurur).
        /// </summary>
        public static void Apply(SpriteRenderer target, Sprite sprite, Color color, bool preserveAspect)
        {
            if (target == null)
            {
                return;
            }

            SpriteFitter fitter = target.GetComponent<SpriteFitter>();
            if (fitter == null)
            {
                fitter = target.gameObject.AddComponent<SpriteFitter>();
            }

            fitter.Set(sprite, color, preserveAspect);
        }

        public void Set(Sprite sprite, Color color, bool preserveAspect)
        {
            // Kutu, sprite değişmeden önceki görünümden alınmalı.
            CaptureBoxIfNeeded();

            Renderer.sprite = sprite;
            Renderer.color = color;
            Fit(preserveAspect);
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
        }

        private void Fit(bool preserveAspect)
        {
            Sprite sprite = Renderer.sprite;
            if (sprite == null)
            {
                return;
            }

            Vector2 size = sprite.bounds.size;
            if (size.x <= Mathf.Epsilon || size.y <= Mathf.Epsilon)
            {
                return;
            }

            float scaleX = boxSize.x / size.x;
            float scaleY = boxSize.y / size.y;
            if (preserveAspect)
            {
                float uniform = Mathf.Min(scaleX, scaleY);
                scaleX = uniform;
                scaleY = uniform;
            }

            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}
