using CutiePie.Core;
using UnityEngine;

namespace CutiePie.UI.Testing
{
    /// <summary>
    /// Yalnızca CounterTest sahnesi için (Scenes/Test/CounterTest.unity, Build Settings dışı; Boot atlanır).
    /// Kaydı yükler (üst bardaki altın için); OnGUI butonlarıyla tarif seçilip CounterPanel.Begin çağrılır,
    /// adım 1 ve 2 otomatik bitirilebilir. BatterReady ve Cancelled gelince ekrana yazar.
    /// Geliştirici aracıdır; metinleri yerelleştirilmez.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CounterTestHarness : MonoBehaviour
    {
        private const string ChocolateId = "cake_chocolate";
        private const string StrawberryId = "cake_strawberry";

        [SerializeField] private CounterPanel panel;

        private string lastEvent;
        private GUIStyle eventStyle;

        private void Start()
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null || GameManager.Instance == null)
            {
                Debug.LogError("[CounterTest] GameManager/SaveSystem yok; sahneyi 'Cutie Pie/Test/Tezgâh Test Sahnesini Kur' ile yeniden kurun.", this);
                return;
            }

            // Boot'tan gelindiyse (kayıt yüklü) yeniden yüklenmez.
            if (!save.IsLoaded)
            {
                save.LoadAndApply();
            }

            if (panel != null)
            {
                panel.BatterReady += HandleBatterReady;
                panel.Cancelled += HandleCancelled;
            }
        }

        private void OnDestroy()
        {
            if (panel != null)
            {
                panel.BatterReady -= HandleBatterReady;
                panel.Cancelled -= HandleCancelled;
            }
        }

        private void HandleBatterReady(string recipeId)
        {
            lastEvent = $"BatterReady: {recipeId}";
            Debug.Log($"[CounterTest] {lastEvent}");
        }

        private void HandleCancelled(string recipeId)
        {
            lastEvent = $"Cancelled: {recipeId}";
            Debug.Log($"[CounterTest] {lastEvent}");
        }

        private void OnGUI()
        {
            if (panel == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, Screen.height - 230f, 300f, 220f), GUI.skin.box);
            GUILayout.Label($"Tarif: {panel.RecipeId ?? "-"}   Hamur hazır: {panel.IsBatterReady}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Çikolatalı"))
            {
                Begin(ChocolateId);
            }

            if (GUILayout.Button("Çilekli"))
            {
                Begin(StrawberryId);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Adım 1'i otomatik bitir"))
            {
                panel.AutoCompleteIngredients();
            }

            if (GUILayout.Button("Adım 2'yi otomatik bitir"))
            {
                panel.AutoCompleteMixing();
            }

            if (GUILayout.Button(panel.IsVisible ? "Paneli gizle" : "Paneli göster"))
            {
                if (panel.IsVisible)
                {
                    panel.Hide();
                }
                else
                {
                    panel.Show();
                }
            }

            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(lastEvent))
            {
                if (eventStyle == null)
                {
                    eventStyle = new GUIStyle(GUI.skin.box) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
                }

                GUI.Box(new Rect(Screen.width - 470f, Screen.height - 70f, 460f, 60f), lastEvent, eventStyle);
            }
        }

        private void Begin(string recipeId)
        {
            lastEvent = panel.Begin(recipeId) ? null : $"Begin başarısız: {recipeId}";
        }
    }
}
