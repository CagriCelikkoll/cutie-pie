using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Müşterinin üstündeki dünya uzayı sabır barı (Canvas yok). Dolgu soldan sabitli olarak kısalır.
    /// </summary>
    public class PatienceBar : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;

        [Tooltip("Sol kenardaki pivot; X ölçeği dolum oranıdır.")]
        [SerializeField] private Transform fillPivot;

        [SerializeField] private SpriteRenderer fill;

        private SalonSettings settings;

        public void Initialize(SalonSettings salonSettings)
        {
            settings = salonSettings;
            if (background != null && settings != null)
            {
                background.color = settings.patienceBackgroundColor;
            }
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Dolum oranını (0-1) ve rengini günceller.</summary>
        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            if (fillPivot != null)
            {
                Vector3 scale = fillPivot.localScale;
                scale.x = ratio;
                fillPivot.localScale = scale;
            }

            if (fill != null && settings != null)
            {
                fill.color = settings.GetPatienceColor(ratio);
            }
        }
    }
}
