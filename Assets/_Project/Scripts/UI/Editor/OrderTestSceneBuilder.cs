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
    /// Yalnızca editörde: sipariş ekranını Kitchen.unity'ye dokunmadan denemek için Scenes/Test/OrderTest.unity'yi baştan kurar
    /// ("Cutie Pie/Test/Sipariş Test Sahnesini Kur", tekrar çalıştırılabilir; var olan test sahnesinin üzerine yazar).
    /// Önce sipariş ve fırın prefab'larını yeniden kurar, sonra sahneye kamera, EventSystem, Kitchen.unity yerleşim kılavuzu
    /// (üst şerit + sol/sağ yer tutucu), Boot'taki yöneticilerin kopyası (GameManager, SaveSystem, EconomyManager + OrderTestHarness),
    /// OvenPanel ve OrderBoard örneklerini koyar (Mutfak'taki gibi ikisi aynı anda açık).
    /// Sahne Build Settings'te değildir; Play'e basınca Boot atlanır, kaydı ve panoyu harness başlatır.
    /// </summary>
    public static class OrderTestSceneBuilder
    {
        private const string MenuPath = "Cutie Pie/Test/Sipariş Test Sahnesini Kur";
        private const string ScenePath = "Assets/_Project/Scenes/Test/OrderTest.unity";
        private const string GameConfigPath = "Assets/_Project/Data/GameConfig.asset";
        private const string MainCameraTag = "MainCamera";

        // Kamera yalnızca boş ekranı temizler; ekranları prefab'ların kendi Canvas'ları çizer.
        private const float CameraSize = 5.4f;
        private const float CameraZ = -10f;

        // Kılavuz katmanı: Kitchen.unity'deki KitchenCanvas'ın (sıralama 0) yer tutucularının kopyası, aynı anchor ve renklerle.
        // Prefab'lar bunların tam üstüne oturmalı; kılavuzun kenarı görünürse ya da iki prefab üst üste binerse yerleşim bozuktur.
        private const int GuideSortingOrder = 0;
        private static readonly Color GuideBackgroundColor = new Color(1f, 0.94f, 0.9f, 1f);
        private static readonly Color GuideTopBarColor = new Color(0.95f, 0.55f, 0.35f, 0.35f);
        private static readonly Color GuideOvenSlotColor = new Color(0.8f, 0.9f, 1f, 0.45f);
        private static readonly Color GuideBoardSlotColor = new Color(0.85f, 1f, 0.85f, 0.45f);
        private static readonly Vector2 TopBarAnchorMin = new Vector2(0f, 0.88f);
        private static readonly Vector2 TopBarAnchorMax = Vector2.one;

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Sipariş Test Sahnesi", "Önce Play modundan çıkın.", "Tamam");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Sipariş Test Sahnesi", $"{GameConfigPath} bulunamadı.", "Tamam");
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sorulur; vazgeçerse hiçbir şey yapılmaz.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject boardPrefab = OrderUIBuilder.BuildPrefabs();
            GameObject ovenPrefab = OvenPanelUIBuilder.BuildPrefabs();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(GuideBackgroundColor);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            CreateLayoutGuide();

            // Mutfak'taki gibi ikisi aynı anda açık: fırın ekranı sol alanda, pano sağ alanda.
            PrefabUtility.InstantiatePrefab(ovenPrefab, scene);
            var boardInstance = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, scene);
            OrderBoard board = boardInstance.GetComponent<OrderBoard>();

            CreateManagers(config, board);

            UIBuilderKit.EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[OrderTestSceneBuilder] Sahne kaydedilemedi: {ScenePath}");
                return;
            }

            Selection.activeGameObject = boardInstance;
            Debug.Log($"[OrderTestSceneBuilder] Test sahnesi kuruldu: {ScenePath}. Play'e basın; sipariş menüleri (Cutie Pie/Sipariş) de çalışır.");
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

        private static void CreateLayoutGuide()
        {
            var go = new GameObject("KitchenLayoutGuide", typeof(RectTransform));
            UIBuilderKit.SetupCanvas(go, GuideSortingOrder);
            CreateGuideRect(go.transform, "TopBar", TopBarAnchorMin, TopBarAnchorMax, GuideTopBarColor);
            CreateGuideRect(go.transform, "OvenPanelSlot", OvenPanelUIBuilder.AreaAnchorMin, OvenPanelUIBuilder.AreaAnchorMax,
                GuideOvenSlotColor);
            CreateGuideRect(go.transform, "OrderBoardSlot", OrderUIBuilder.AreaAnchorMin, OrderUIBuilder.AreaAnchorMax,
                GuideBoardSlotColor);
        }

        private static void CreateGuideRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform tf = UIBuilderKit.CreateStretched(parent, name);
            tf.anchorMin = anchorMin;
            tf.anchorMax = anchorMax;
            tf.offsetMin = tf.offsetMax = Vector2.zero;
            UnityEngine.UI.Image image = tf.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        // Boot'taki Managers objesinin kopyası; üçü de aynı GameConfig'i kullanır.
        private static void CreateManagers(GameConfig config, OrderBoard board)
        {
            var go = new GameObject("TestManagers");
            SetConfig(go.AddComponent<GameManager>(), config);
            SetConfig(go.AddComponent<SaveSystem>(), config);
            SetConfig(go.AddComponent<EconomyManager>(), config);

            OrderTestHarness harness = go.AddComponent<OrderTestHarness>();
            var so = new SerializedObject(harness);
            so.FindProperty("board").objectReferenceValue = board;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetConfig(Component component, GameConfig config)
        {
            var so = new SerializedObject(component);
            SerializedProperty property = so.FindProperty("config");
            if (property == null)
            {
                Debug.LogError($"[OrderTestSceneBuilder] {component.GetType().Name} bileşeninde 'config' alanı yok.");
                return;
            }

            property.objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
