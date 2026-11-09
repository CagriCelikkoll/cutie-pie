using System.Collections;
using CutiePie.Ads;
using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Round sonu ekranı. GameEvents.RoundEnded gelince salonun üstünde açılır, RoundStarted gelince kapanır.
    /// Kazanılan altın ve bahşiş sayarak çıkar, en yüksek kombo yazılır, yıldızlar tek tek dolar;
    /// round 3 kaçırmayla erken bittiyse uyarı satırı görünür.
    /// Butonlar: Reklam izle 2x (bir kez, kazanç 0 ise gizli), Tekrar oyna, Pastaneye dön, Geliştirmeler.
    /// Reklam oynarken tüm butonlar kapalıdır.
    /// </summary>
    public class RoundEndPanel : MonoBehaviour
    {
        // Yerelleştirme anahtarları; metinler Localization/tr.json ve en.json'dadır.
        private const string KeyTitleCompleted = "round_end.title_completed";
        private const string KeyTitleEarly = "round_end.title_early";
        private const string KeyEarlyReason = "round_end.early_reason";
        private const string KeyComboValue = "round_end.combo_value";
        private const string KeyComboNone = "round_end.combo_none";
        private const string KeyWatchAd = "round_end.watch_ad";
        private const string KeyAdPlaying = "round_end.ad_playing";
        private const string KeyAdClaimed = "round_end.ad_claimed";

        [SerializeField] private RoundEndSettings settings;

        [Tooltip("Salon sahnesindeki RoundManager (Core prefab'ı); Tekrar oyna bunu kullanır.")]
        [SerializeField] private RoundManager roundManager;

        [Header("Görünüm")]
        [Tooltip("Açılıp kapanan içerik (perde + kart). Bu bileşen her zaman açık olan kök nesnede durur.")]
        [SerializeField] private GameObject content;

        [SerializeField] private LocalizedText title;

        [Tooltip("Round erken bittiyse görünen satır.")]
        [SerializeField] private GameObject earlyEndRow;

        [SerializeField] private LocalizedText earlyEndText;
        [SerializeField] private TMP_Text goldValue;
        [SerializeField] private TMP_Text tipsValue;
        [SerializeField] private LocalizedText comboValue;
        [SerializeField] private StarRow starRow;

        [Header("Butonlar")]
        [SerializeField] private Button adButton;
        [SerializeField] private LocalizedText adButtonLabel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private UpgradesPanel upgradesPanel;

        private RoundResult result;
        private int shownGold;
        private bool adPlaying;
        private bool bonusClaimed;

        // Her açılış/kapanışta artar; önceki açılıştan gelen geç reklam dönüşleri yok sayılır.
        private int showVersion;

        private Coroutine revealRoutine;
        private Coroutine goldRoutine;

        // Prefab'daki yeri; reklam butonu gizliyken Tekrar oyna aynı satırda ortaya alınır.
        private RectTransform playAgainRect;
        private Vector2 playAgainHomePosition;

        public bool IsOpen => content != null && content.activeSelf;

        private void Awake()
        {
            if (settings == null || content == null || starRow == null || adButton == null ||
                playAgainButton == null || shopButton == null || upgradesButton == null)
            {
                Debug.LogError("[RoundEndPanel] Referanslar eksik; 'Cutie Pie/UI/Round Sonu Panelini Kur' menüsünü çalıştırın.", this);
            }

            if (playAgainButton != null)
            {
                playAgainRect = (RectTransform)playAgainButton.transform;
                playAgainHomePosition = playAgainRect.anchoredPosition;
            }

            AddListener(adButton, HandleAdClicked);
            AddListener(playAgainButton, HandlePlayAgainClicked);
            AddListener(shopButton, HandleShopClicked);
            AddListener(upgradesButton, HandleUpgradesClicked);

            Hide();
        }

        private void OnDestroy()
        {
            RemoveListener(adButton, HandleAdClicked);
            RemoveListener(playAgainButton, HandlePlayAgainClicked);
            RemoveListener(shopButton, HandleShopClicked);
            RemoveListener(upgradesButton, HandleUpgradesClicked);
        }

        private void OnEnable()
        {
            GameEvents.RoundEnded += Show;
            GameEvents.RoundStarted += HandleRoundStarted;
        }

        private void OnDisable()
        {
            GameEvents.RoundEnded -= Show;
            GameEvents.RoundStarted -= HandleRoundStarted;
        }

        private void Update()
        {
            // Reklamın hazır olma durumu değişebilir (ör. önceki reklam bitti); buton görünümü güncel tutulur.
            if (IsOpen)
            {
                RefreshAdButton();
            }
        }

        // ------------------------------------------------------------------ Açma / kapama

        private void Show(RoundResult roundResult)
        {
            if (roundResult == null || settings == null || content == null || starRow == null || adButton == null)
            {
                return;
            }

            showVersion++;
            result = roundResult;
            adPlaying = false;
            bonusClaimed = false;
            StopRoutines();

            content.SetActive(true);
            if (upgradesPanel != null)
            {
                upgradesPanel.Close();
            }

            SetKey(title, result.completed ? KeyTitleCompleted : KeyTitleEarly);
            if (earlyEndRow != null)
            {
                earlyEndRow.SetActive(!result.completed);
            }

            if (!result.completed)
            {
                SetKey(earlyEndText, KeyEarlyReason, result.misses);
            }

            if (result.highestCombo > 0)
            {
                SetKey(comboValue, KeyComboValue, result.highestCombo);
            }
            else
            {
                SetKey(comboValue, KeyComboNone);
            }
            SetGold(0);
            SetTips(0);
            starRow.ResetStars(settings);

            // Kazanç yoksa ikiye katlanacak bir şey de yok: buton gizlenir.
            bool hasAd = result.goldEarned > 0;
            adButton.gameObject.SetActive(hasAd);
            if (playAgainRect != null)
            {
                playAgainRect.anchoredPosition = hasAd ? playAgainHomePosition : new Vector2(0f, playAgainHomePosition.y);
            }
            SetKey(adButtonLabel, KeyWatchAd);
            SetMainButtonsInteractable(true);
            RefreshAdButton();

            revealRoutine = StartCoroutine(Reveal());
        }

        private void HandleRoundStarted(LevelData level) => Hide();

        private void Hide()
        {
            showVersion++;
            adPlaying = false;
            StopRoutines();

            if (upgradesPanel != null)
            {
                upgradesPanel.Close();
            }

            if (content != null)
            {
                content.SetActive(false);
            }
        }

        // Önce altın ve bahşiş sayarak çıkar, sonra yıldızlar dolar.
        private IEnumerator Reveal()
        {
            goldRoutine = StartCoroutine(CountGold(0, result.goldEarned, settings.countUpDuration));

            float elapsed = 0f;
            while (elapsed < settings.countUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settings.countUpDuration);
                SetTips(Mathf.RoundToInt(Mathf.Lerp(0, result.tipsEarned, t)));
                yield return null;
            }

            SetTips(result.tipsEarned);
            yield return starRow.Fill(result.stars, settings);
            revealRoutine = null;
        }

        private IEnumerator CountGold(int from, int to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetGold(Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration))));
                yield return null;
            }

            SetGold(to);
            goldRoutine = null;
        }

        private void StopRoutines()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }

            if (goldRoutine != null)
            {
                StopCoroutine(goldRoutine);
                goldRoutine = null;
            }
        }

        // ------------------------------------------------------------------ Butonlar

        private void HandleAdClicked()
        {
            IAdsService ads = GameManager.Instance != null ? GameManager.Instance.Ads : null;
            if (adPlaying || bonusClaimed || result == null || ads == null || !ads.IsRewardedReady)
            {
                return;
            }

            adPlaying = true;
            SetMainButtonsInteractable(false);
            SetKey(adButtonLabel, KeyAdPlaying);
            RefreshAdButton();

            int version = showVersion;
            ads.ShowRewarded(success => HandleAdFinished(version, success));
        }

        private void HandleAdFinished(int version, bool success)
        {
            // Reklam bitene kadar sahne değiştiyse panel yok edilmiştir; yeni round açıldıysa sürüm değişmiştir.
            if (this == null || version != showVersion || !adPlaying)
            {
                return;
            }

            adPlaying = false;
            if (success && !bonusClaimed)
            {
                GrantBonus();
            }
            else
            {
                SetKey(adButtonLabel, KeyWatchAd);
            }

            SetMainButtonsInteractable(true);
            RefreshAdButton();
        }

        // Round kazancının tamamı bir kez daha eklenir ve hemen kaydedilir
        // (SaveSystem RoundEnded'da, bonustan önce kaydetmiştir).
        private void GrantBonus()
        {
            EconomyManager economy = EconomyManager.Instance;
            if (economy == null)
            {
                Debug.LogError("[RoundEndPanel] EconomyManager yok, 2x ödül verilemedi.", this);
                SetKey(adButtonLabel, KeyWatchAd);
                return;
            }

            bonusClaimed = true;
            economy.Add(CurrencyType.Gold, result.goldEarned);

            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Save();
            }
            else
            {
                Debug.LogWarning("[RoundEndPanel] SaveSystem yok; 2x ödül bir sonraki kayda kadar diske yazılmadı.", this);
            }

            SetKey(adButtonLabel, KeyAdClaimed);

            if (goldRoutine != null)
            {
                StopCoroutine(goldRoutine);
            }

            goldRoutine = StartCoroutine(CountGold(shownGold, result.goldEarned * 2, settings.bonusCountUpDuration));
        }

        private void HandlePlayAgainClicked()
        {
            if (adPlaying)
            {
                return;
            }

            if (roundManager == null)
            {
                Debug.LogError("[RoundEndPanel] RoundManager atanmamış; yeni round başlatılamadı.", this);
                return;
            }

            // RoundStarted gelince panel kendini kapatır; tezgâh, fiş, masalar ve müşteriler SalonRound'da sıfırlanır.
            roundManager.StartRound();
        }

        private void HandleShopClicked()
        {
            if (adPlaying)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[RoundEndPanel] GameManager yok; pastaneye dönülemedi.", this);
                return;
            }

            GameManager.Instance.GoToShop();
        }

        private void HandleUpgradesClicked()
        {
            if (!adPlaying && upgradesPanel != null)
            {
                upgradesPanel.Open();
            }
        }

        // ------------------------------------------------------------------ Yardımcılar

        private void SetMainButtonsInteractable(bool interactable)
        {
            SetInteractable(playAgainButton, interactable);
            SetInteractable(shopButton, interactable);
            SetInteractable(upgradesButton, interactable);
        }

        // Ödül alındıysa, reklam oynuyorsa veya reklam hazır değilse buton pasif görünür.
        private void RefreshAdButton()
        {
            if (adButton == null || !adButton.gameObject.activeSelf)
            {
                return;
            }

            IAdsService ads = GameManager.Instance != null ? GameManager.Instance.Ads : null;
            bool ready = ads != null && ads.IsRewardedReady;
            SetInteractable(adButton, ready && !adPlaying && !bonusClaimed);
        }

        private void SetGold(int value)
        {
            shownGold = value;
            if (goldValue != null)
            {
                goldValue.SetText("{0}", value);
            }
        }

        private void SetTips(int value)
        {
            if (tipsValue != null)
            {
                tipsValue.SetText("{0}", value);
            }
        }

        private static void SetKey(LocalizedText text, string key, params object[] args)
        {
            if (text != null)
            {
                text.SetKey(key, args);
            }
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null && button.interactable != interactable)
            {
                button.interactable = interactable;
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
