using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Upgrades.Editor
{
    /// <summary>Yalnızca editörde ve Play sırasında: geliştirme seviyelerini sıfırlar.</summary>
    public static class UpgradeMenu
    {
        private const string ResetMenu = "Cutie Pie/Test: Geliştirmeleri Sıfırla";

        // Satın alma olmadığı için altın geri verilmez ve UpgradePurchased yayınlanmaz.
        [MenuItem(ResetMenu)]
        private static void ResetUpgrades()
        {
            foreach (UpgradeData upgrade in GameManager.Instance.Config.upgrades)
            {
                SaveSystem.Instance.SetUpgradeLevel(upgrade.id, 0);
            }

            SaveSystem.Instance.Save();
            Debug.Log("[UpgradeMenu] Geliştirmeler sıfırlandı.");
        }

        [MenuItem(ResetMenu, true)]
        private static bool ResetUpgradesValidate() =>
            EditorApplication.isPlaying && GameManager.Instance != null && SaveSystem.Instance != null &&
            SaveSystem.Instance.LoadState != SaveLoadState.NotLoaded;
    }
}
