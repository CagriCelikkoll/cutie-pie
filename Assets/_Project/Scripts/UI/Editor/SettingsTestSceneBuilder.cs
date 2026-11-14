using CutiePie.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: Ayarlar panelini Shop.unity'ye dokunmadan denemek için Scenes/Test/SettingsTest.unity'yi baştan kurar
    /// ("Cutie Pie/Test/Ayarlar Test Sahnesini Kur", tekrar çalıştırılabilir; var olan test sahnesinin üzerine yazar).
    /// Önce paneli yeniden kurar, sonra sahneye kamera, EventSystem, SettingsPanel örneği, Open'a bağlı "Ayarlar" butonu
    /// ve BootInEditorMarker koyar. Sahne Build Settings'te değildir; işaret sayesinde Play'e basınca önce Boot açılır
    /// (gerçek GameManager ve Localization), ardından bu sahne yüklenir. Sahnede Boot yöneticisi bulunmaz.
    /// </summary>
    public static class SettingsTestSceneBuilder
    {
        private const string MenuPath = "Cutie Pie/Test/Ayarlar Test Sahnesini Kur";
        private const string ScenePath = "Assets/_Project/Scenes/Test/SettingsTest.unity";
        private const string MainCameraTag = "MainCamera";

        // Kamera yalnızca boş ekranı temizler; ekranı Canvas'lar çizer.
        private const float CameraSize = 5.4f;
        private const float CameraZ = -10f;

        // Paneli açan buton: panelin (20) altındaki Canvas'ta, ekranın ortasında.
        private const int OpenCanvasSortingOrder = 0;
        private static readonly Vector2 OpenButtonSize = new Vector2(480f, 130f);
        private const float OpenButtonFontSize = 52f;
        private const string KeyOpen = "settings.title";

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Ayarlar Test Sahnesi", "Önce Play modundan çıkın.", "Tamam");
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sorulur; vazgeçerse hiçbir şey yapılmaz.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject panelPrefab = SettingsUIBuilder.BuildPrefab();
            RoundEndSettings settings = UIBuilderKit.GetOrCreateSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(settings.cardColor);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("BootInEditorMarker", typeof(BootInEditorMarker));

            var panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, scene);
            SettingsPanel panel = panelInstance.GetComponent<SettingsPanel>();

            CreateOpenButton(panel, settings, rounded);

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[SettingsTestSceneBuilder] Sahne kaydedilemedi: {ScenePath}");
                return;
            }

            Selection.activeGameObject = panelInstance;
            Debug.Log($"[SettingsTestSceneBuilder] Test sahnesi kuruldu: {ScenePath}. Play'e basın, ortadaki butonla paneli açın.");
        }

        private static void CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = MainCameraTag;
            go.transform.position = new Vector3(0f, 0f, CameraZ);

            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
        }

        // Shop'taki Ayarlar butonunun yerine: onClick sahnede kalıcı olarak SettingsPanel.Open'a bağlanır.
        private static void CreateOpenButton(SettingsPanel panel, RoundEndSettings settings, Sprite rounded)
        {
            var root = new GameObject("TestCanvas", typeof(RectTransform));
            UIBuilderKit.SetupCanvas(root, OpenCanvasSortingOrder);

            Button button = UIBuilderKit.CreateButton(root.transform, "OpenSettingsButton", KeyOpen, Vector2.zero,
                OpenButtonSize, OpenButtonFontSize, settings.upgradesButtonColor, rounded, settings);
            UnityEventTools.AddPersistentListener(button.onClick, panel.Open);
        }
    }
}
