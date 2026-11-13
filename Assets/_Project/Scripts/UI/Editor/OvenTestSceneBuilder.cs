using CutiePie.Core;
using CutiePie.Core.Data;
using CutiePie.UI.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: fırın ekranını Kitchen.unity'ye dokunmadan denemek için Scenes/Test/OvenTest.unity'yi baştan kurar
    /// ("Cutie Pie/Test/Fırın Test Sahnesini Kur", tekrar çalıştırılabilir; var olan test sahnesinin üzerine yazar).
    /// Önce fırın prefab'larını yeniden kurar, sonra sahneye kamera, EventSystem, Boot'taki yöneticilerin kopyası
    /// (GameManager, SaveSystem, EconomyManager + OvenPanelTestHarness) ve OvenPanel örneğini koyar.
    /// Sahne Build Settings'te değildir; Play'e basınca Boot atlanır, kaydı harness yükler.
    /// </summary>
    public static class OvenTestSceneBuilder
    {
        private const string MenuPath = "Cutie Pie/Test/Fırın Test Sahnesini Kur";
        private const string ScenePath = "Assets/_Project/Scenes/Test/OvenTest.unity";
        private const string GameConfigPath = "Assets/_Project/Data/GameConfig.asset";
        private const string MainCameraTag = "MainCamera";

        // Kamera yalnızca boş ekranı temizler; ekranı OvenPanel'in kendi Canvas'ı çizer.
        private const float CameraSize = 5.4f;
        private const float CameraZ = -10f;

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Fırın Test Sahnesi", "Önce Play modundan çıkın.", "Tamam");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Fırın Test Sahnesi", $"{GameConfigPath} bulunamadı.", "Tamam");
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sorulur; vazgeçerse hiçbir şey yapılmaz.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject panelPrefab = OvenPanelUIBuilder.BuildPrefabs();
            OvenPanelSettings ovenSettings = OvenPanelUIBuilder.GetOrCreateOvenSettings();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(ovenSettings.backgroundColor);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, scene);
            OvenPanel panel = panelInstance.GetComponent<OvenPanel>();

            CreateManagers(config, panel);

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[OvenTestSceneBuilder] Sahne kaydedilemedi: {ScenePath}");
                return;
            }

            Selection.activeGameObject = panelInstance;
            Debug.Log($"[OvenTestSceneBuilder] Test sahnesi kuruldu: {ScenePath}. Play'e basın; fırın menüleri (Cutie Pie/Fırın) de çalışır.");
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

        // Boot'taki Managers objesinin kopyası; üçü de aynı GameConfig'i kullanır.
        private static void CreateManagers(GameConfig config, OvenPanel panel)
        {
            var go = new GameObject("TestManagers");
            SetConfig(go.AddComponent<GameManager>(), config);
            SetConfig(go.AddComponent<SaveSystem>(), config);
            SetConfig(go.AddComponent<EconomyManager>(), config);

            OvenPanelTestHarness harness = go.AddComponent<OvenPanelTestHarness>();
            var so = new SerializedObject(harness);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetConfig(Component component, GameConfig config)
        {
            var so = new SerializedObject(component);
            SerializedProperty property = so.FindProperty("config");
            if (property == null)
            {
                Debug.LogError($"[OvenTestSceneBuilder] {component.GetType().Name} bileşeninde 'config' alanı yok.");
                return;
            }

            property.objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
