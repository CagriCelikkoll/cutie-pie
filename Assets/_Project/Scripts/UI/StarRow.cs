using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Round sonu ekranındaki üç yıldız. Hepsi boş başlar; Fill ile kazanılan kadarı sırayla büyüyüp renklenerek dolar.
    /// Animasyon gerçek zamanlıdır (Time.timeScale'den etkilenmez).
    /// </summary>
    public class StarRow : MonoBehaviour
    {
        [Tooltip("Soldan sağa yıldız görselleri.")]
        [SerializeField] private Image[] stars = new Image[0];

        public int Count => stars.Length;

        /// <summary>Tüm yıldızları boş görünüme döndürür.</summary>
        public void ResetStars(RoundEndSettings settings)
        {
            foreach (Image star in stars)
            {
                if (star != null)
                {
                    star.color = settings.starEmptyColor;
                    star.rectTransform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>İlk count yıldızı tek tek doldurur. count 0 ise hiçbir yıldız dolmaz.</summary>
        public IEnumerator Fill(int count, RoundEndSettings settings)
        {
            int target = Mathf.Clamp(count, 0, stars.Length);
            if (target == 0)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(settings.firstStarDelay);

            for (int i = 0; i < target; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSecondsRealtime(settings.starInterval);
                }

                if (stars[i] != null)
                {
                    yield return FillOne(stars[i], settings);
                }
            }
        }

        // Yıldız popScale'e büyür, sonra 1'e iner; bu sırada boş renkten dolu renge geçer.
        private static IEnumerator FillOne(Image star, RoundEndSettings settings)
        {
            RectTransform tf = star.rectTransform;
            float elapsed = 0f;
            while (elapsed < settings.starFillDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settings.starFillDuration);

                // 0→0,5: büyü, 0,5→1: yerine otur.
                float pop = t < 0.5f ? t * 2f : (1f - t) * 2f;
                tf.localScale = Vector3.one * Mathf.Lerp(1f, settings.starPopScale, pop);
                star.color = Color.Lerp(settings.starEmptyColor, settings.starFilledColor, t);
                yield return null;
            }

            tf.localScale = Vector3.one;
            star.color = settings.starFilledColor;
        }
    }
}
