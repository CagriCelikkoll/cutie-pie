using CutiePie.Core.Data;
using UnityEngine;

namespace CutiePie.Core.Testing
{
    /// <summary>
    /// Yalnızca RoundTest sahnesi için: ekrandaki butonlarla sahte müşteri olayları gönderir ve round durumunu gösterir.
    /// Oyuncuya görünmeyen geliştirici aracıdır; metinleri bu yüzden yerelleştirilmez.
    /// </summary>
    public class RoundTestHarness : MonoBehaviour
    {
        // Sahte müşterilerde sıra ve masa önemli değil.
        private const int FakeCustomerId = 0;
        private const int NoTable = -1;

        [SerializeField] private RoundManager roundManager;
        [SerializeField] private LevelData testLevel;
        [Min(0)] [SerializeField] private int fakePayment;
        [Min(0)] [SerializeField] private int fakeTip;

        private static readonly CustomerInfo FakeCustomer = new CustomerInfo(FakeCustomerId, null, NoTable);

        private RoundResult _lastResult;

        private void OnEnable() => GameEvents.RoundEnded += HandleRoundEnded;

        private void OnDisable() => GameEvents.RoundEnded -= HandleRoundEnded;

        private void HandleRoundEnded(RoundResult result)
        {
            _lastResult = result;
            Debug.Log($"[RoundTest] Bitti: {Describe(result)}");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Round Başlat"))
            {
                _lastResult = null;
                roundManager.StartRound(testLevel);
            }

            if (GUILayout.Button($"Doğru Teslim (ödeme {fakePayment}, bahşiş {fakeTip})"))
            {
                GameEvents.RaiseCustomerServed(FakeCustomer, fakePayment, fakeTip);
            }

            if (GUILayout.Button("Yanlış Sipariş"))
            {
                GameEvents.RaiseWrongOrderDelivered(FakeCustomer);
            }

            if (GUILayout.Button("Kızgın Gitti (kaçırma)"))
            {
                GameEvents.RaiseCustomerLeftAngry(FakeCustomer);
            }

            GUILayout.Label($"Sürüyor: {roundManager.IsRunning}  Kalan: {roundManager.RemainingSeconds:F1}");
            GUILayout.Label($"Seri: {roundManager.CurrentStreak}  Çarpan: x{roundManager.CurrentMultiplier}");

            if (EconomyManager.Instance != null)
            {
                GUILayout.Label($"Altın: {EconomyManager.Instance.Get(CurrencyType.Gold)}");
            }

            if (_lastResult != null)
            {
                GUILayout.Label($"Sonuç: {Describe(_lastResult)}");
            }
        }

        private static string Describe(RoundResult result) =>
            $"altın {result.goldEarned}, bahşiş {result.tipsEarned}, kombo farkı {result.comboBonusEarned}, " +
            $"en yüksek seri {result.highestCombo}, yıldız {result.stars}, kaçırma {result.misses}, " +
            $"tamamlandı {result.completed}";
    }
}
