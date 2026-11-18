using System;
using System.Collections;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Özel sipariş sonucu (pasta yapım ekranının üstünde açılan kart). Yalnızca gösterir; ödül Complete ile zaten verilmiştir.
    /// Benzerlik yüzdesi, altın ve bahşiş 0'dan sayarak artar, ardından 0–3 yıldız sırayla dolar (StarRow).
    /// "Panoya dön" her an basılabilir. Animasyon gerçek zamanlıdır (Time.timeScale'den etkilenmez).
    /// </summary>
    public class OrderResultView : MonoBehaviour
    {
        private const string KeyPercent = "order.result.percent";

        [SerializeField] private OrderUISettings settings;

        [Tooltip("Ortak palet ve yıldız animasyonu ayarları (round sonu ekranıyla aynı).")]
        [SerializeField] private RoundEndSettings roundEndSettings;

        [Tooltip("Gösterilip gizlenen içerik (perde + kart).")]
        [SerializeField] private GameObject content;

        [SerializeField] private LocalizedText percentText;
        [SerializeField] private StarRow starRow;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private Button continueButton;

        /// <summary>"Panoya dön"e basıldı.</summary>
        public event Action ContinueClicked;

        private Coroutine countRoutine;

        public bool IsOpen => content != null && content.activeSelf;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinue);
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinue);
            }
        }

        /// <summary>Sonucu gösterir ve animasyonu baştan başlatır.</summary>
        public void Show(CustomOrderResult result)
        {
            if (content == null || result == null)
            {
                return;
            }

            StopAnimation();
            content.SetActive(true);
            SetValues(0, 0, 0);
            if (starRow != null && roundEndSettings != null)
            {
                starRow.ResetStars(roundEndSettings);
            }

            countRoutine = StartCoroutine(Animate(result));
        }

        public void Hide()
        {
            StopAnimation();
            if (content != null)
            {
                content.SetActive(false);
            }
        }

        private IEnumerator Animate(CustomOrderResult result)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(result.similarity) * 100f);
            float duration = settings != null ? settings.countUpDuration : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetValues(Mathf.RoundToInt(percent * t), Mathf.RoundToInt(result.gold * t), Mathf.RoundToInt(result.tip * t));
                yield return null;
            }

            SetValues(percent, result.gold, result.tip);

            if (starRow != null && roundEndSettings != null)
            {
                yield return starRow.Fill(result.stars, roundEndSettings);
            }

            countRoutine = null;
        }

        private void SetValues(int percent, int gold, int tip)
        {
            KitchenUIUtil.SetKey(percentText, KeyPercent, percent);
            if (goldText != null)
            {
                goldText.text = $"+{gold}";
            }

            if (tipText != null)
            {
                tipText.text = $"+{tip}";
            }
        }

        private void StopAnimation()
        {
            if (countRoutine != null)
            {
                StopCoroutine(countRoutine);
                countRoutine = null;
            }
        }

        private void HandleContinue() => ContinueClicked?.Invoke();
    }
}
