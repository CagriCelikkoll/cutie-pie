using CutiePie.Ads;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using CutiePie.Kitchen;
using CutiePie.Upgrades;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutiePie.Core
{
    /// <summary>
    /// Oyun akışı ve sahne geçişleri. Boot sahnesinde durur, sahne değişse de yaşar.
    /// Ortak servislerin (ör. reklam) tek erişim noktasıdır.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;

        public GameConfig Config => config;

        /// <summary>Reklam servisi. Kullanım: GameManager.Instance.Ads.ShowRewarded(...).</summary>
        public IAdsService Ads { get; private set; }

        /// <summary>Yerelleştirme servisi. Kullanım: GameManager.Instance.Localization.Get("anahtar").</summary>
        public LocalizationService Localization { get; private set; }

        /// <summary>Geliştirme servisi. Kullanım: GameManager.Instance.Upgrades.TryPurchase("id").</summary>
        public UpgradeManager Upgrades { get; private set; }

        /// <summary>Ortak pasta stoğu. Kullanım: GameManager.Instance.Stock.TryTake("id", 1).</summary>
        public StockManager Stock { get; private set; }

        /// <summary>Fırın servisi. Kullanım: GameManager.Instance.Ovens.StartBake(0, "recipeId").</summary>
        public IOvenService Ovens { get; private set; }

        /// <summary>Özel sipariş servisi. Kullanım: GameManager.Instance.Orders.Board.</summary>
        public ICustomOrderService Orders { get; private set; }

        // Ovens'ın somut hâli: Initialize/Tick yalnızca buradan çağrılır, dışarıya IOvenService açılır.
        private OvenService _ovenService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Boot ikinci kez yüklenirse kopya yöneticiler yok edilir.
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // TODO (yayın): AdMobAdsService hazır olunca burada seçilecek.
            Ads = new FakeAdsService(this, config.fakeAdDurationSeconds);

            // Dil şimdilik kaydedilmez; her açılışta varsayılan dil kullanılır.
            Localization = new LocalizationService(config.languages, config.defaultLanguage);

            Upgrades = new UpgradeManager(config.upgrades);

            Stock = new StockManager();

            _ovenService = new OvenService(config.kitchen, Stock, Ads);
            Ovens = _ovenService;

            // G11: boş gövde. Mantık G14'te gelecek.
            Orders = new CustomOrderService();
        }

        private void Start()
        {
            // Boot açılınca önce kayıt yüklenir, sonra ilk oyun sahnesine geçilir.
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.LoadAndApply();

                    // Kayıt yüklendikten sonra: açılışta hazır olan fırınlar için OvenReady burada yayınlanır.
                    _ovenService.Initialize();
                }
                else
                {
                    Debug.LogError("[GameManager] SaveSystem bulunamadı; kayıt yüklenmedi.");
                }

                // Editörde Play başka bir sahnede basıldıysa doğrudan o sahne açılır (build'de her zaman null).
                string editorStartScene = EditorPlayStartScene.Consume();
                LoadScene(string.IsNullOrEmpty(editorStartScene) ? config.firstSceneName : editorStartScene);
            }
        }

        // Fırın süreleri UTC zaman damgasıyla hesaplanır (timeScale'den etkilenmez); burada yalnızca hazır olma kontrol edilir.
        private void Update() => _ovenService?.Tick();

        /// <summary>Açılış ekranına (pastane) döner. Ör. round sonundaki "Pastaneye dön" butonu.</summary>
        public void GoToShop() => LoadScene(config.shopSceneName);

        /// <summary>Salon'a geçer. Ör. açılış ekranındaki Kasa butonu.</summary>
        public void GoToSalon() => LoadScene(config.salonSceneName);

        /// <summary>Adı verilen sahneyi yükler (Build Settings'te olmalı).</summary>
        public void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameManager] '{sceneName}' sahnesi Build Settings'te yok.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
