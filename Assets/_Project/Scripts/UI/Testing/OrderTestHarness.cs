using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.Kitchen;
using UnityEngine;

namespace CutiePie.UI.Testing
{
    /// <summary>
    /// Yalnızca OrderTest sahnesi için (Scenes/Test/OrderTest.unity, Build Settings dışı; Boot atlanır).
    /// Boot'un yaptığını taklit eder: kaydı yükler, sipariş ve fırın servislerini başlatır (GameManager bunu yalnızca Boot'ta yapar).
    /// Gerçek kayıt dosyası (save.json) kullanılır; sıfırlamak için "Cutie Pie/Kaydı Sil".
    /// Ekrandaki butonlarla ilk fiş tamamlanır, yenilemeler bitirilir ve Time.timeScale denenir.
    /// Geliştirici aracıdır; metinleri yerelleştirilmez.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class OrderTestHarness : MonoBehaviour
    {
        [SerializeField] private OrderBoard board;

        private string lastMessage = "-";
        private bool showTools;

        // Yalnızca test: Initialize ve hata ayıklama metotları arayüzde yok, somut servis üzerinden çağrılır (OrderMenu gibi).
        private static CustomOrderService Service =>
            GameManager.Instance != null ? GameManager.Instance.Orders as CustomOrderService : null;

        private void Start()
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null || GameManager.Instance == null)
            {
                Debug.LogError("[OrderTest] GameManager/SaveSystem yok; sahneyi 'Cutie Pie/Test/Sipariş Test Sahnesini Kur' ile yeniden kurun.", this);
                return;
            }

            // Boot'tan gelindiyse (kayıt yüklü) hiçbir şey yapılmaz.
            if (!save.IsLoaded)
            {
                save.LoadAndApply();
                Service?.Initialize();

                // Fırın ekranı da sahnede (yerleşim çakışması denensin diye); fırın servisi de başlatılır (OvenPanelTestHarness gibi).
                if (GameManager.Instance.Ovens is OvenService ovens)
                {
                    ovens.Initialize();
                }
            }

            if (board != null)
            {
                board.OrderSelected += HandleOrderSelected;
            }
        }

        private void OnDestroy()
        {
            if (board != null)
            {
                board.OrderSelected -= HandleOrderSelected;
            }

            // Test sırasında durdurulan zaman bir sonraki Play'e taşınmasın.
            Time.timeScale = 1f;
        }

        private void HandleOrderSelected(CustomOrder order)
        {
            lastMessage = $"Seçilen fiş: {Short(order.id)}";
            Debug.Log($"[OrderTest] OrderSelected: {order.id} (yapım ekranı açıldı).");
        }

        private void OnGUI()
        {
            CustomOrderService service = Service;
            if (service == null)
            {
                return;
            }

            // Üst şerit (TopBar) yüksekliğinde, fırın alanını örtmesin diye kısa tutulur; tıklanınca açılır.
            showTools = GUI.Toggle(new Rect(10f, 10f, 160f, 24f), showTools, " Test araçları", GUI.skin.button);
            if (!showTools)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 40f, 360f, 280f), GUI.skin.box);
            GUILayout.Label($"Fiş: {service.Board.Count}   Bekleyen yenileme: {service.PendingRefreshCount}");
            GUILayout.Label($"Sonraki yenileme: {service.GetSecondsUntilNextRefresh():0.0} sn");
            GUILayout.Label($"Altın: {(EconomyManager.Instance != null ? EconomyManager.Instance.Get(CurrencyType.Gold) : 0)}   timeScale: {Time.timeScale}");
            GUILayout.Label(lastMessage);

            if (GUILayout.Button("İlk fişi mükemmel tamamla") && service.Board.Count > 0)
            {
                CustomOrder first = service.Board[0];

                // Hedefin derin kopyası; fişin kendisi değiştirilmez.
                CakeBuild copy = JsonUtility.FromJson<CakeBuild>(JsonUtility.ToJson(first.target));
                CustomOrderResult result = service.Complete(first.id, copy);
                lastMessage = result != null
                    ? $"Tamamlandı: %{result.similarity * 100f:0}, {result.stars} yıldız, +{result.gold} +{result.tip}"
                    : "Tamamlanamadı.";
            }

#if UNITY_EDITOR
            if (GUILayout.Button("Yenilemeleri hemen bitir"))
            {
                service.FinishRefreshesForDebug();
            }
#endif

            if (GUILayout.Button(Time.timeScale > 0f ? "Zamanı durdur (timeScale 0)" : "Zamanı başlat (timeScale 1)"))
            {
                Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
            }

            GUILayout.EndArea();
        }

        private static string Short(string id) => string.IsNullOrEmpty(id) || id.Length <= 8 ? id : id.Substring(0, 8);
    }
}
