using UnityEditor;
using UnityEngine;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde, Play modunda: UpgradesPanel.Open(id) odaklanmasını elle denemek için test menüsü.
    /// Sahnedeki (kapalı olsa da) UpgradesPanel'i bulur ve Garson geliştirmesine odaklanarak açar.
    /// Play modunda değilken menü pasiftir.
    /// </summary>
    internal static class UpgradesPanelTestMenu
    {
        private const string FocusWaiterMenuPath = "Cutie Pie/Test/Geliştirmeler: Garson'a odakla";
        private const string WaiterUpgradeId = "waiter";

        [MenuItem(FocusWaiterMenuPath)]
        private static void FocusWaiter()
        {
            UpgradesPanel[] panels = Object.FindObjectsOfType<UpgradesPanel>(true);
            if (panels.Length == 0)
            {
                Debug.LogWarning("[UpgradesPanelTest] Sahnede UpgradesPanel yok (Shop veya Salon sahnesinde deneyin).");
                return;
            }

            if (panels.Length > 1)
            {
                Debug.LogWarning($"[UpgradesPanelTest] Sahnede {panels.Length} UpgradesPanel var; ilki açılıyor.", panels[0]);
            }

            panels[0].Open(WaiterUpgradeId);
            Debug.Log($"[UpgradesPanelTest] Open(\"{WaiterUpgradeId}\") çağrıldı.", panels[0]);
        }

        [MenuItem(FocusWaiterMenuPath, true)]
        private static bool FocusWaiterValidate() => Application.isPlaying;
    }
}
