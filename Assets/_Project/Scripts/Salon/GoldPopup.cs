using TMPro;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Teslimde müşterinin üstünde yükselip solan "+N" altın yazısı (dünya uzayı, Canvas yok).
    /// Yalnızca "+" ve rakam basar; oyuncuya görünen kelime olmadığı için yerelleştirme gerekmez.
    /// Süresi bitince kendini yok eder. Altını EconomyManager ekler; bu yalnızca görseldir.
    /// </summary>
    public class GoldPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro text;

        [Tooltip("Yazının yanındaki altın ikonu.")]
        [SerializeField] private SpriteRenderer icon;

        private float duration;
        private float riseDistance;
        private float elapsed;
        private Vector3 startPosition;
        private Color textColor;
        private Color iconColor;
        private bool playing;

        /// <summary>Üretildikten hemen sonra çağrılır: miktarı ve rengi yazar, animasyonu başlatır.</summary>
        public void Play(int amount, Color color, SalonSettings settings)
        {
            duration = settings.popupDuration;
            riseDistance = settings.popupRiseDistance;
            startPosition = transform.position;
            elapsed = 0f;

            textColor = color;
            if (text != null)
            {
                text.text = "+" + amount;
                text.color = textColor;
            }

            if (icon != null)
            {
                iconColor = icon.color;
            }

            playing = true;
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            transform.position = startPosition + Vector3.up * (riseDistance * t);

            // İlk yarıda tam görünür, ikinci yarıda solar.
            float alpha = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
            if (text != null)
            {
                text.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * alpha);
            }

            if (icon != null)
            {
                icon.color = new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * alpha);
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
