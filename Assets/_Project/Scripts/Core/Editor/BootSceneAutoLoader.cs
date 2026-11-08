using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutiePie.Core.Editor
{
    /// <summary>
    /// Yalnızca editörde: hangi sahne açık olursa olsun Play'e basınca önce Boot (Build Settings'teki 0. sahne) yüklenir,
    /// böylece kalıcı yöneticiler hazır olur. Boot'tan sonra Play'e basılan sahne açılır (ör. Salon'da basılırsa doğrudan Salon);
    /// Boot'ta basılırsa normal akış (GameConfig.firstSceneName) çalışır. "Cutie Pie" menüsünden kapatılabilir.
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
                    RememberStartScene();
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

        // Play'e basılan sahneyi Boot'a bildirir; Boot'un kendisi veya Build Settings dışı sahneler için normal akış.
        private static void RememberStartScene()
        {
            EditorPlayStartScene.Set(null);
            if (!Enabled || EditorBuildSettings.scenes.Length == 0)
            {
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            string bootPath = EditorBuildSettings.scenes[0].path;
            if (string.IsNullOrEmpty(active.path) || active.path == bootPath)
            {
                return;
            }

            bool inBuild = Array.Exists(EditorBuildSettings.scenes, s => s.enabled && s.path == active.path);
            if (!inBuild)
            {
                Debug.LogWarning($"[BootSceneAutoLoader] '{active.name}' Build Settings'te değil; normal akış çalışacak.");
                return;
            }

            EditorPlayStartScene.Set(active.name);
        }
    }
}
