using CutiePie.Core;
using CutiePie.Salon;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Round başında vitrinde hiç pasta yoksa açılan uyarı (G16): "Mutfakta pasta pişir".
    /// SalonEvents.NoCakeStockAtRoundStart ile açılır; açıkken Time.timeScale = 0 olur (round süresi ve sabır durur).
    /// Butonlar: "Mutfağa git" (GameManager'da GoToKitchen olmadığı için şimdilik pastaneye döner) ve
    /// "Yine de aç" (round yalnızca içecek siparişleriyle başlar).
    /// Zaman ölçeği panel kapanınca, bileşen kapanınca/yok olunca ve aktif sahne değişince MUTLAKA 1'e döner.
    /// Panel zamana bağlı animasyon kullanmaz; buton geçişleri uGUI'de zaman ölçeğinden bağımsızdır.
    /// </summary>
    public class NoStockPanel : MonoBehaviour
    {
        // Panelin kapanınca geri yüklediği zaman ölçeği (oyunun normal hızı; denge değeri değildir).
        private const float NormalTimeScale = 1f;

        [Tooltip("Açılıp kapanan içerik (perde + kart). Bu bileşen her zaman açık olan kök nesnede durur.")]
        [SerializeField] private GameObject content;

        [SerializeField] private Button goToKitchenButton;
        [SerializeField] private Button continueButton;

        // Zamanı bu panel mi durdurdu? Yalnızca kendi durdurduğunu geri açar.
        private bool pausedTime;

        public bool IsOpen => content != null && content.activeSelf;

        private void Awake()
        {
            if (content == null || goToKitchenButton == null || continueButton == null)
            {
                Debug.LogError("[NoStockPanel] Referanslar eksik; 'Cutie Pie/UI/Stok Uyarı Panelini Kur' menüsünü çalıştırın.", this);
            }

            if (goToKitchenButton != null)
            {
                goToKitchenButton.onClick.AddListener(HandleGoToKitchen);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinue);
            }

            SetContentActive(false);
        }

        private void OnEnable()
        {
            SalonEvents.NoCakeStockAtRoundStart += Show;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDisable()
        {
            SalonEvents.NoCakeStockAtRoundStart -= Show;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            Hide();
        }

        private void OnDestroy()
        {
            if (goToKitchenButton != null)
            {
                goToKitchenButton.onClick.RemoveListener(HandleGoToKitchen);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinue);
            }

            ResumeTime();
        }

        private void Show()
        {
            if (content == null)
            {
                return;
            }

            SetContentActive(true);
            if (!pausedTime)
            {
                Time.timeScale = 0f;
                pausedTime = true;
            }
        }

        private void Hide()
        {
            SetContentActive(false);
            ResumeTime();
        }

        private void ResumeTime()
        {
            if (!pausedTime)
            {
                return;
            }

            pausedTime = false;
            Time.timeScale = NormalTimeScale;
        }

        private void HandleActiveSceneChanged(Scene previous, Scene next) => Hide();

        private void HandleContinue()
        {
            if (!IsOpen)
            {
                return;
            }

            Hide();
            SalonEvents.RaiseNoStockContinueRequested();
        }

        private void HandleGoToKitchen()
        {
            if (!IsOpen)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[NoStockPanel] GameManager yok; sahne değiştirilemedi.", this);
                return;
            }

            Hide();

            // TODO (altyapı): GameManager.GoToKitchen eklenince buraya bağlanacak. Şimdilik pastaneye döner.
            GameManager.Instance.GoToShop();
        }

        private void SetContentActive(bool active)
        {
            if (content != null && content.activeSelf != active)
            {
                content.SetActive(active);
            }
        }
    }
}
