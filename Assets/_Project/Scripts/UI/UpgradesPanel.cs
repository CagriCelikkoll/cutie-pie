using System.Collections.Generic;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Geliştirme mağazası. Kendi Canvas'ı olan ayrı prefab'dır (Prefabs/UI/UpgradesPanel.prefab):
    /// round sonu ekranı onu iç içe kullanır, başka bir sahne de prefab'ı bırakıp Open() çağırabilir.
    /// İlk açılışta GameConfig.upgrades listesindeki her geliştirme için bir kart üretir.
    /// Açıkken altın değişince veya satın alma olunca tüm kartlar yenilenir; mevcut altını CurrencyDisplay gösterir.
    /// Kartlar yatay kaydırılır; sığmayınca kaydırma kendiliğinden devreye girer (oklar, solma ve tekerlek: ScrollAffordance).
    /// Open() ilk karttan başlar; Open(id) o geliştirmenin kartına kaydırıp vurgular (ör. fırın kilidinden açılınca).
    /// </summary>
    public class UpgradesPanel : MonoBehaviour
    {
        [Tooltip("Açılıp kapanan içerik (perde + kart).")]
        [SerializeField] private GameObject content;

        [SerializeField] private Button closeButton;

        [Header("Kartlar")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Kartların eklendiği kaydırma içeriği (HorizontalLayoutGroup).")]
        [SerializeField] private RectTransform cardContainer;

        [Tooltip("Prefabs/UI/UpgradeCard.prefab")]
        [SerializeField] private UpgradeCard cardPrefab;

        private readonly List<UpgradeCard> cards = new List<UpgradeCard>();
        private bool cardsBuilt;
        private bool subscribed;

        public bool IsOpen => content != null && content.activeSelf;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            Unsubscribe();
        }

        /// <summary>Paneli açar ve ilk karttan başlatır.</summary>
        public void Open() => OpenInternal(null);

        /// <summary>
        /// Paneli açar, verilen geliştirmenin kartını görünür alanın ortasına kaydırır (uçlarda sınırlanır) ve kısaca vurgular.
        /// id boşsa Open() gibidir; bulunamazsa uyarı yazar ve yine Open() gibi ilk karttan başlar.
        /// </summary>
        public void Open(string focusUpgradeId) => OpenInternal(focusUpgradeId);

        private void OpenInternal(string focusUpgradeId)
        {
            if (content == null)
            {
                return;
            }

            content.SetActive(true);
            EnsureCards();
            Subscribe();
            RefreshAll();

            UpgradeCard focus = FindCard(focusUpgradeId);
            if (focus == null)
            {
                ScrollToStart();
                return;
            }

            ScrollToCard(focus);
            focus.PlayFocusHighlight();
        }

        public void Close()
        {
            Unsubscribe();
            if (content != null)
            {
                content.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Kartlar

        private void EnsureCards()
        {
            if (cardsBuilt)
            {
                return;
            }

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null || cardPrefab == null || cardContainer == null)
            {
                // Bir sonraki açılışta yeniden denenir.
                Debug.LogError("[UpgradesPanel] GameManager/GameConfig yok veya kart referansları eksik; kartlar üretilemedi.", this);
                return;
            }

            if (config.upgrades != null)
            {
                foreach (UpgradeData upgrade in config.upgrades)
                {
                    if (upgrade == null)
                    {
                        continue;
                    }

                    UpgradeCard card = Instantiate(cardPrefab, cardContainer, false);
                    card.name = $"UpgradeCard_{upgrade.id}";
                    card.Bind(upgrade);
                    cards.Add(card);
                }
            }

            cardsBuilt = true;
        }

        private void RefreshAll()
        {
            foreach (UpgradeCard card in cards)
            {
                if (card != null)
                {
                    card.Refresh();
                }
            }
        }

        // Her açılışta ilk karttan başlanır; yerleşim önce hesaplanır ki konum doğru olsun.
        private void ScrollToStart()
        {
            if (scrollRect == null || cardContainer == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);
            scrollRect.StopMovement();
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        private UpgradeCard FindCard(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return null;
            }

            foreach (UpgradeCard card in cards)
            {
                if (card != null && card.Data != null && card.Data.id == upgradeId)
                {
                    return card;
                }
            }

            Debug.LogWarning($"[UpgradesPanel] '{upgradeId}' id'li geliştirme kartı bulunamadı; ilk karttan açılıyor.", this);
            return null;
        }

        // Kartı görünür alanın ortasına getirir; içerik sığıyorsa veya kart uçtaysa konum uca sınırlanır.
        private void ScrollToCard(UpgradeCard card)
        {
            if (scrollRect == null || cardContainer == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);
            scrollRect.StopMovement();

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            float hidden = cardContainer.rect.width - viewport.rect.width;
            if (hidden <= 0f)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(cardContainer, card.transform);
            float centerFromLeft = bounds.center.x - cardContainer.rect.xMin;
            float offset = Mathf.Clamp(centerFromLeft - viewport.rect.width * 0.5f, 0f, hidden);
            scrollRect.horizontalNormalizedPosition = offset / hidden;
        }

        // ------------------------------------------------------------------ Olaylar

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            subscribed = true;
            GameEvents.CurrencyChanged += HandleCurrencyChanged;
            GameEvents.UpgradePurchased += HandleUpgradePurchased;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            subscribed = false;
            GameEvents.CurrencyChanged -= HandleCurrencyChanged;
            GameEvents.UpgradePurchased -= HandleUpgradePurchased;
        }

        // TryPurchase'ta altın seviyeden önce düşer; iki olay da yenilediği için son görünüm doğrudur.
        private void HandleCurrencyChanged(CurrencyType currency, int newAmount)
        {
            if (currency == CurrencyType.Gold)
            {
                RefreshAll();
            }
        }

        private void HandleUpgradePurchased(UpgradeData upgrade, int newLevel) => RefreshAll();
    }
}
