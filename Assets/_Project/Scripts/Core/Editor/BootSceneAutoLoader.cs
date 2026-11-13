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
    /// Boot'ta basılırsa normal akış (GameConfig.firstSceneName) çalışır. Build Settings dışı sahneler (ör. RoundTest)
    /// Boot'a uğramadan doğrudan açılır; ancak sahnede BootInEditorMarker varsa önce Boot, sonra o sahne açılır.
    /// "Cutie Pie" menüsünden kapatılabilir.
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

        // Play'e basılan sahneyi Boot'a bildirir; Boot'un kendisinde normal akış, Build Settings dışı sahnede Boot atlanır.
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
            if (!inBuild && HasBootMarker(active))
            {
                // İşaretli test sahnesi: önce Boot, sonra bu sahne (adla yüklenemediği için yoluyla).
                Debug.Log($"[BootSceneAutoLoader] '{active.name}' BootInEditorMarker içeriyor; Boot'tan sonra açılacak.");
                EditorPlayStartScene.Set(active.path);
                return;
            }

            if (!inBuild)
            {
                // Test sahneleri gibi Build Settings dışı sahneler Boot'a uğramadan doğrudan açılır.
                EditorSceneManager.playModeStartScene = null;
                Debug.Log($"[BootSceneAutoLoader] '{active.name}' Build Settings'te değil; Boot atlanıyor.");
                return;
            }

            EditorPlayStartScene.Set(active.name);
        }

        // Sahnede (pasif nesneler dahil) BootInEditorMarker var mı.
        private static bool HasBootMarker(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<BootInEditorMarker>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
