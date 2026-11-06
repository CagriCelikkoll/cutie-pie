using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Core.Editor
{
    /// <summary>Yalnızca editörde: kayıt silme ve test menüleri.</summary>
    public static class SaveMenu
    {
        // Test menüsünün eklediği altın; menü etiketiyle birlikte değiştirilmeli.
        private const int TestGoldAmount = 100;

        [MenuItem("Cutie Pie/Kaydı Sil")]
        private static void DeleteSave()
        {
            if (EditorApplication.isPlaying && SaveSystem.Instance != null)
            {
                // Play sırasında: dosyalar silinir ve değerler varsayılana döner (çıkışta eski değer yazılmaz).
                SaveSystem.Instance.DeleteSave();
                Debug.Log("[SaveMenu] Kayıt silindi, değerler varsayılana döndü.");
                return;
            }

            GameConfig config = FindGameConfig();
            if (config == null)
            {
                Debug.LogWarning("[SaveMenu] GameConfig bulunamadı; kayıt silinemedi.");
                return;
            }

            if (SaveSystem.DeleteFiles(config))
            {
                Debug.Log($"[SaveMenu] Kayıt dosyaları silindi: {Application.persistentDataPath}");
            }
        }

        [MenuItem("Cutie Pie/Test: 100 Altın Ekle")]
        private static void AddTestGold()
        {
            EconomyManager.Instance.Add(CurrencyType.Gold, TestGoldAmount);
        }

        [MenuItem("Cutie Pie/Test: 100 Altın Ekle", true)]
        private static bool AddTestGoldValidate() => EditorApplication.isPlaying && EconomyManager.Instance != null;

        private static GameConfig FindGameConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameConfig));
            if (guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
