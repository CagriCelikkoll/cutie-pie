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
    /// Yalnızca editörde: Tezgâh ekranını Kitchen.unity'ye dokunmadan denemek için Scenes/Test/CounterTest.unity'yi baştan kurar
    /// ("Cutie Pie/Test/Tezgâh Test Sahnesini Kur", tekrar çalıştırılabilir; var olan test sahnesinin üzerine yazar).
    /// Önce tezgâh prefab'larını yerinde günceller, sonra sahneye kamera, EventSystem, Boot'taki yöneticilerin kopyası
    /// (GameManager, SaveSystem, EconomyManager + CounterTestHarness) ve CounterPanel örneğini koyar.
    /// Panel kendi Canvas'ıyla doğrudan sahne köküne InstantiatePrefab ile konur (sonradan SetParent yapılmaz).
    /// Sahne Build Settings'te değildir; Play'e basınca Boot atlanır, kaydı harness yükler.
    /// </summary>
    public static class CounterTestSceneBuilder
    {
        private const string MenuPath = "Cutie Pie/Test/Tezgâh Test Sahnesini Kur";
        private const string ScenePath = "Assets/_Project/Scenes/Test/CounterTest.unity";
        private const string GameConfigPath = "Assets/_Project/Data/GameConfig.asset";
        private const string MainCameraTag = "MainCamera";

        // Kamera yalnızca boş ekranı temizler; ekranı panelin kendi Canvas'ı çizer.
        private const float CameraSize = 5.4f;
        private const float CameraZ = -10f;

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Tezgâh Test Sahnesi", "Önce Play modundan çıkın.", "Tamam");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Tezgâh Test Sahnesi", $"{GameConfigPath} bulunamadı.", "Tamam");
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sorulur; vazgeçerse hiçbir şey yapılmaz.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject panelPrefab = CounterUIBuilder.BuildPrefabs();
            CounterSettings settings = CounterUIBuilder.GetOrCreateSettings();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(settings.backgroundColor);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, scene);
            CounterPanel panel = panelInstance.GetComponent<CounterPanel>();

            CreateManagers(config, panel);

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[CounterTestSceneBuilder] Sahne kaydedilemedi: {ScenePath}");
                return;
            }

            Selection.activeGameObject = panelInstance;
            Debug.Log($"[CounterTestSceneBuilder] Test sahnesi kuruldu: {ScenePath}. Play'e basın; sol alttaki butonlarla tarif seçin.");
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
        private static void CreateManagers(GameConfig config, CounterPanel panel)
        {
            var go = new GameObject("TestManagers");
            SetConfig(go.AddComponent<GameManager>(), config);
            SetConfig(go.AddComponent<SaveSystem>(), config);
            SetConfig(go.AddComponent<EconomyManager>(), config);

            CounterTestHarness harness = go.AddComponent<CounterTestHarness>();
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
                Debug.LogError($"[CounterTestSceneBuilder] {component.GetType().Name} bileşeninde 'config' alanı yok.");
                return;
            }

            property.objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
