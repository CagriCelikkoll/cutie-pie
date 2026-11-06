using CutiePie.Ads;
using CutiePie.Core.Data;
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
        }

        private void Start()
        {
            // Boot açılınca önce kayıt yüklenir, sonra ilk oyun sahnesine geçilir.
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.LoadAndApply();
                }
                else
                {
                    Debug.LogError("[GameManager] SaveSystem bulunamadı; kayıt yüklenmedi.");
                }

                LoadScene(config.firstSceneName);
            }
        }

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
