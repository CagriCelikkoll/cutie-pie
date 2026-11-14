using CutiePie.Core;
using CutiePie.Kitchen;
using CutiePie.Upgrades;
using UnityEngine;

namespace CutiePie.UI.Testing
{
    /// <summary>
    /// Yalnızca OvenTest sahnesi için (Scenes/Test/OvenTest.unity, Build Settings dışı; Boot atlanır).
    /// Boot'un yaptığını taklit eder: kaydı yükler ve fırın servisini başlatır (GameManager bunu yalnızca Boot'ta yapar).
    /// Gerçek kayıt dosyası (save.json) kullanılır; sıfırlamak için "Cutie Pie/Kaydı Sil".
    /// Ekrandaki butonlarla ek fırın seviyesi ve Time.timeScale denenir. Geliştirici aracıdır; metinleri yerelleştirilmez.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class OvenPanelTestHarness : MonoBehaviour
    {
        [SerializeField] private OvenPanel panel;

        private bool panelHidden;

        private void Start()
        {
            SaveSystem save = SaveSystem.Instance;
            if (save == null || GameManager.Instance == null)
            {
                Debug.LogError("[OvenTest] GameManager/SaveSystem yok; sahneyi 'Cutie Pie/Test/Fırın Test Sahnesini Kur' ile yeniden kurun.", this);
                return;
            }

            // Boot'tan gelindiyse (kayıt yüklü) hiçbir şey yapılmaz.
            if (!save.IsLoaded)
            {
                save.LoadAndApply();

                // Yalnızca test: Initialize arayüzde yok, somut servis üzerinden çağrılır (OvenMenu gibi).
                if (GameManager.Instance.Ovens is OvenService service)
                {
                    service.Initialize();
                }
            }

            if (panel != null)
            {
                panel.UnlockRequested += HandleUnlockRequested;
            }
        }

        private void OnDestroy()
        {
            if (panel != null)
            {
                panel.UnlockRequested -= HandleUnlockRequested;
            }

            // Test sırasında durdurulan zaman bir sonraki Play'e taşınmasın.
            Time.timeScale = 1f;
        }

        private void HandleUnlockRequested() =>
            Debug.Log("[OvenTest] UnlockRequested: Kitchen.unity'de burası UpgradesPanel.Open()'a bağlanacak.");

        private void OnGUI()
        {
            SaveSystem save = SaveSystem.Instance;
            GameManager game = GameManager.Instance;
            if (save == null || game == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 10f, 330f, 260f), GUI.skin.box);
            int level = save.GetUpgradeLevel(UpgradeIds.ExtraOven);
            GUILayout.Label($"Açık fırın: {game.Ovens.Count}  (extra_oven seviyesi {level})");
            GUILayout.Label($"Reklam hazır: {game.Ads.IsRewardedReady}   timeScale: {Time.timeScale}");

            if (GUILayout.Button("Ek fırın +1"))
            {
                SetExtraOvenLevel(level + 1);
            }

            if (GUILayout.Button("Ek fırın -1") && level > 0)
            {
                SetExtraOvenLevel(level - 1);
            }

            if (GUILayout.Button(Time.timeScale > 0f ? "Zamanı durdur (timeScale 0)" : "Zamanı başlat (timeScale 1)"))
            {
                Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
            }

            if (panel != null && GUILayout.Button(panelHidden ? "Paneli göster" : "Paneli gizle"))
            {
                panelHidden = !panelHidden;
                if (panelHidden)
                {
                    panel.Hide();
                }
                else
                {
                    panel.Show();
                }
            }

            GUILayout.EndArea();
        }

        // Geliştirme asset'i (G15) olmadan fırın sayısını denemek için seviye doğrudan kayda yazılır; olay yayınlanmaz,
        // panel değişikliği kare başı kontrolle yakalar.
        private static void SetExtraOvenLevel(int level)
        {
            SaveSystem.Instance.SetUpgradeLevel(UpgradeIds.ExtraOven, Mathf.Max(0, level));
            SaveSystem.Instance.Save();
        }
    }
}
