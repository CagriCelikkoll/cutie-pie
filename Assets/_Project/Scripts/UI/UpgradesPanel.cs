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
    /// Kartlar yatay kaydırılır; sığmayınca kaydırma kendiliğinden devreye girer.
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

        public void Open()
        {
            if (content == null)
            {
                return;
            }

            content.SetActive(true);
            EnsureCards();
            Subscribe();
            RefreshAll();
            ScrollToStart();
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
