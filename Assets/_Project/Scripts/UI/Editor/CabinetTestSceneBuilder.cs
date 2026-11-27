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
    /// Yalnızca editörde: Pasta Dolabı'nı Kitchen.unity'ye dokunmadan denemek için Scenes/Test/CabinetTest.unity'yi baştan kurar
    /// ("Cutie Pie/Test/Pasta Dolabı Test Sahnesini Kur", tekrar çalıştırılabilir; var olan test sahnesinin üzerine yazar).
    /// Önce dolap prefab'larını yerinde günceller, sonra sahneye kamera, EventSystem, Boot'taki yöneticilerin kopyası
    /// (GameManager, SaveSystem, EconomyManager + CabinetTestHarness) ve CakeCabinetPanel örneğini koyar.
    /// Panel kendi Canvas'ıyla doğrudan sahne köküne InstantiatePrefab ile konur (sonradan SetParent yapılmaz).
    /// Sahne Build Settings'te değildir; Play'e basınca Boot atlanır, kaydı harness yükler.
    /// </summary>
    public static class CabinetTestSceneBuilder
    {
        private const string MenuPath = "Cutie Pie/Test/Pasta Dolabı Test Sahnesini Kur";
        private const string ScenePath = "Assets/_Project/Scenes/Test/CabinetTest.unity";
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
                EditorUtility.DisplayDialog("Pasta Dolabı Test Sahnesi", "Önce Play modundan çıkın.", "Tamam");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Pasta Dolabı Test Sahnesi", $"{GameConfigPath} bulunamadı.", "Tamam");
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sorulur; vazgeçerse hiçbir şey yapılmaz.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject panelPrefab = CakeCabinetUIBuilder.BuildPrefabs();
            CakeCabinetSettings settings = CakeCabinetUIBuilder.GetOrCreateSettings();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(settings.backgroundColor);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, scene);
            CakeCabinetPanel panel = panelInstance.GetComponent<CakeCabinetPanel>();

            CreateManagers(config, panel);

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[CabinetTestSceneBuilder] Sahne kaydedilemedi: {ScenePath}");
                return;
            }

            Selection.activeGameObject = panelInstance;
            Debug.Log($"[CabinetTestSceneBuilder] Test sahnesi kuruldu: {ScenePath}. Play'e basın; stok sol üstteki butonlarla değiştirilir.");
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
        private static void CreateManagers(GameConfig config, CakeCabinetPanel panel)
        {
            var go = new GameObject("TestManagers");
            SetConfig(go.AddComponent<GameManager>(), config);
            SetConfig(go.AddComponent<SaveSystem>(), config);
            SetConfig(go.AddComponent<EconomyManager>(), config);

            CabinetTestHarness harness = go.AddComponent<CabinetTestHarness>();
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
                Debug.LogError($"[CabinetTestSceneBuilder] {component.GetType().Name} bileşeninde 'config' alanı yok.");
                return;
            }

            property.objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
