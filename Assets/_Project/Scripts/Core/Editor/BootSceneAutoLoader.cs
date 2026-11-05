using UnityEditor;
using UnityEditor.SceneManagement;

namespace CutiePie.Core.Editor
{
    /// <summary>
    /// Yalnızca editörde: hangi sahne açık olursa olsun Play'e basınca önce Boot (Build Settings'teki 0. sahne) yüklenir.
    /// Böylece kalıcı yöneticiler her zaman hazır olur. "Cutie Pie" menüsünden kapatılabilir.
    /// </summary>
    [InitializeOnLoad]
    public static class BootSceneAutoLoader
    {
        private const string MenuPath = "Cutie Pie/Play'i Boot'tan Başlat";
        private const string PrefKey = "CutiePie.BootSceneAutoLoader.Enabled";

        static BootSceneAutoLoader()
        {
            // Build Settings değiştikten sonra da doğru sahneyi göstermek için her editör yüklemesinde uygulanır.
            EditorApplication.delayCall += Apply;
            // Play'e geçmeden hemen önce de uygulanır (delayCall henüz çalışmamış olabilir).
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    Apply();
                }
            };
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            if (!Enabled || EditorBuildSettings.scenes.Length == 0)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            string bootPath = EditorBuildSettings.scenes[0].path;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(bootPath);
        }
    }
}
