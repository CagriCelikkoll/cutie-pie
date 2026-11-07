using System.Collections.Generic;
using System.IO;
using CutiePie.Core.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: Salon sahnesinin G2 yerleşimini (yer tutucu sprite'lar, prefab'lar, masalar, kamera) kurar.
    /// "Cutie Pie/Salon" menüsünden çalıştırılır; sahneyi kaydetmek kullanıcıya bırakılır.
    /// </summary>
    public static class SalonSceneBuilder
    {
        private const string MenuRoot = "Cutie Pie/Salon/";
        private const string SceneName = "Salon";

        private const string PlaceholderFolder = "Assets/_Project/Art/Salon/Placeholders";
        private const string SquareSpritePath = PlaceholderFolder + "/Square.png";
        private const string CircleSpritePath = PlaceholderFolder + "/Circle.png";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Salon";
        private const string TablePrefabPath = PrefabFolder + "/Table.prefab";
        private const string CustomerPrefabPath = PrefabFolder + "/Customer.prefab";
        private const string DataFolder = "Assets/_Project/Data/Salon";
        private const string SettingsPath = DataFolder + "/SalonSettings.asset";
        private const string LevelPath = "Assets/_Project/Data/Levels/Level_01.asset";
        private const string DefaultCustomerTypePath = "Assets/_Project/Data/Customers/CustomerType_Default.asset";

        // Yer tutucu doku boyutu; piksel/birim de aynı olduğundan sprite 1x1 birimdir.
        private const int PlaceholderTextureSize = 32;

        // Yerleşim: 1920x1080 referans, orto boyutu 5.4 → 19.2 x 10.8 birim görünür alan.
        private const float CameraOrthoSize = 5.4f;
        private static readonly float[] TableColumnX = { 0.6f, 2.9f, 5.2f, 7.5f };
        private static readonly float[] TableRowY = { 1.2f, -2.6f }; // 0: üst sıra, 1: alt sıra
        private static readonly Vector2 TableSize = new Vector2(1.8f, 1.1f);
        private const float SeatPointOffsetY = 0.95f;
        private static readonly Color LockColor = new Color(0.15f, 0.15f, 0.2f, 0.65f);
        private const float LockScale = 0.9f;

        // Müşteri prefab'ı yerleşimi (müşteri merkezine göre, birim).
        private static readonly Vector2 CustomerBodySize = new Vector2(0.8f, 1f);
        private const float PatienceBarOffsetY = 0.7f;
        private static readonly Vector2 PatienceBarSize = new Vector2(1f, 0.15f);
        private const float BubbleOffsetY = 1.25f;
        private static readonly Vector2 BubbleSize = new Vector2(1.3f, 0.7f);
        private const float BubbleIconOffsetX = 0.32f;
        private const float BubbleIconSize = 0.45f;
        private static readonly Color BubbleColor = Color.white;
        private static readonly Color PatienceFillDefaultColor = new Color(0.35f, 0.8f, 0.4f, 1f);

        // Sorting Layer (ortak dosya) yerine sortingOrder: zemin 0, masa 10, müşteri 20, balon/bar 30.
        public const int OrderTable = 10;
        public const int OrderTableLock = 11;
        public const int OrderCustomer = 20;
        public const int OrderOverlay = 30;
        public const int OrderOverlayContent = 31;

        [MenuItem(MenuRoot + "1 - Masaları Kur")]
        private static void BuildTables()
        {
            if (!IsSalonSceneOpen())
            {
                return;
            }

            Sprite square = GetOrCreatePlaceholderSprite(SquareSpritePath, false);
            SalonSettings settings = GetOrCreateSettings();
            GameObject tablePrefab = GetOrCreateTablePrefab(square, settings);

            Transform root = GetOrCreateSceneObject("SalonRoot", null);

            Transform existing = root.Find("Tables");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Masaları Kur", "Sahnede 'Tables' zaten var. Silinip yeniden kurulsun mu?", "Evet", "Vazgeç"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            // Masalar: üst sıra soldan sağa 0-3, alt sıra 4-7.
            var tablesGo = new GameObject("Tables");
            Undo.RegisterCreatedObjectUndo(tablesGo, "Masaları Kur");
            tablesGo.transform.SetParent(root, false);
            TableManager manager = tablesGo.AddComponent<TableManager>();

            var tables = new List<Table>();
            for (int row = 0; row < TableRowY.Length; row++)
            {
                for (int col = 0; col < TableColumnX.Length; col++)
                {
                    int index = row * TableColumnX.Length + col;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, tablesGo.transform);
                    instance.name = $"Table_{index}";
                    instance.transform.localPosition = new Vector3(TableColumnX[col], TableRowY[row], 0f);

                    Table table = instance.GetComponent<Table>();
                    var tableSo = new SerializedObject(table);
                    tableSo.FindProperty("index").intValue = index;
                    tableSo.ApplyModifiedPropertiesWithoutUndo();
                    tables.Add(table);
                }
            }

            var managerSo = new SerializedObject(manager);
            managerSo.FindProperty("settings").objectReferenceValue = settings;
            SerializedProperty list = managerSo.FindProperty("tables");
            list.arraySize = tables.Count;
            for (int i = 0; i < tables.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = tables[i];
            }
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            // Geçici başlatıcı.
            Transform bootstrapTf = GetOrCreateSceneObject("SalonBootstrap", root);
            SalonBootstrap bootstrap = bootstrapTf.GetComponent<SalonBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = Undo.AddComponent<SalonBootstrap>(bootstrapTf.gameObject);
            }

            var bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("level").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
            bootstrapSo.FindProperty("tableManager").objectReferenceValue = manager;
            bootstrapSo.ApplyModifiedProperties();

            SetupCamera();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = tablesGo;
            Debug.Log("[SalonSceneBuilder] 8 masa kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        [MenuItem(MenuRoot + "2 - Müşteri Prefab'ını Kur")]
        private static void BuildCustomerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CustomerPrefabPath) != null &&
                !EditorUtility.DisplayDialog("Müşteri Prefab'ı", "Customer.prefab zaten var. Üzerine yazılsın mı?", "Evet", "Vazgeç"))
            {
                return;
            }

            Sprite square = GetOrCreatePlaceholderSprite(SquareSpritePath, false);
            Sprite circle = GetOrCreatePlaceholderSprite(CircleSpritePath, true);
            SalonSettings settings = GetOrCreateSettings();
            EnsureFolder(PrefabFolder);

            var root = new GameObject("Customer");
            Customer customer = root.AddComponent<Customer>();

            SpriteRenderer body = CreateSpriteChild(root.transform, "Body", circle, CustomerBodySize, Color.white, OrderCustomer);

            // Sabır barı: Bg + soldan sabitli FillPivot/Fill.
            var barGo = new GameObject("PatienceBar");
            barGo.transform.SetParent(root.transform, false);
            barGo.transform.localPosition = new Vector3(0f, PatienceBarOffsetY, 0f);
            PatienceBar bar = barGo.AddComponent<PatienceBar>();

            SpriteRenderer barBg = CreateSpriteChild(barGo.transform, "Bg", square, PatienceBarSize, settings.patienceBackgroundColor, OrderOverlay);

            var fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(barGo.transform, false);
            fillPivot.localPosition = new Vector3(-PatienceBarSize.x * 0.5f, 0f, 0f);

            SpriteRenderer fill = CreateSpriteChild(fillPivot, "Fill", square, PatienceBarSize, PatienceFillDefaultColor, OrderOverlayContent);
            fill.transform.localPosition = new Vector3(PatienceBarSize.x * 0.5f, 0f, 0f);

            var barSo = new SerializedObject(bar);
            barSo.FindProperty("background").objectReferenceValue = barBg;
            barSo.FindProperty("fillPivot").objectReferenceValue = fillPivot;
            barSo.FindProperty("fill").objectReferenceValue = fill;
            barSo.ApplyModifiedPropertiesWithoutUndo();

            // Sipariş balonu: balon kutusu + pasta ve içecek ikonu.
            var bubbleGo = new GameObject("OrderBubble");
            bubbleGo.transform.SetParent(root.transform, false);
            bubbleGo.transform.localPosition = new Vector3(0f, BubbleOffsetY, 0f);
            OrderBubble bubble = bubbleGo.AddComponent<OrderBubble>();

            CreateSpriteChild(bubbleGo.transform, "Bubble", square, BubbleSize, BubbleColor, OrderOverlay);
            var iconSize = new Vector2(BubbleIconSize, BubbleIconSize);
            SpriteRenderer cakeIcon = CreateSpriteChild(bubbleGo.transform, "CakeIcon", square, iconSize, settings.cakePlaceholderColor, OrderOverlayContent);
            cakeIcon.transform.localPosition = new Vector3(-BubbleIconOffsetX, 0f, 0f);
            SpriteRenderer drinkIcon = CreateSpriteChild(bubbleGo.transform, "DrinkIcon", square, iconSize, settings.drinkPlaceholderColor, OrderOverlayContent);
            drinkIcon.transform.localPosition = new Vector3(BubbleIconOffsetX, 0f, 0f);

            var bubbleSo = new SerializedObject(bubble);
            bubbleSo.FindProperty("cakeIcon").objectReferenceValue = cakeIcon;
            bubbleSo.FindProperty("drinkIcon").objectReferenceValue = drinkIcon;
            bubbleSo.ApplyModifiedPropertiesWithoutUndo();

            var customerSo = new SerializedObject(customer);
            customerSo.FindProperty("body").objectReferenceValue = body;
            customerSo.FindProperty("orderBubble").objectReferenceValue = bubble;
            customerSo.FindProperty("patienceBar").objectReferenceValue = bar;
            customerSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefabPath);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("[SalonSceneBuilder] Customer.prefab oluşturuldu.");
        }

        // --- Sahne ---

        private static bool IsSalonSceneOpen()
        {
            if (EditorSceneManager.GetActiveScene().name == SceneName)
            {
                return true;
            }

            EditorUtility.DisplayDialog("Salon Kurulumu", "Önce Salon.unity sahnesini açın.", "Tamam");
            return false;
        }

        private static Transform GetOrCreateSceneObject(string name, Transform parent)
        {
            if (parent != null)
            {
                Transform child = parent.Find(name);
                if (child != null)
                {
                    return child;
                }
            }
            else
            {
                foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (go.name == name)
                    {
                        return go.transform;
                    }
                }
            }

            var created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Salon Kurulumu");
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[SalonSceneBuilder] MainCamera etiketli kamera bulunamadı; kamera ayarlanmadı.");
                return;
            }

            Undo.RecordObject(cam, "Salon Kamerası");
            cam.orthographic = true;
            cam.orthographicSize = CameraOrthoSize;
        }

        // --- Asset'ler ---

        private static GameObject GetOrCreateTablePrefab(Sprite square, SalonSettings settings)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(PrefabFolder);

            var root = new GameObject("Table");
            Table table = root.AddComponent<Table>();

            SpriteRenderer body = CreateSpriteChild(root.transform, "Body", square, TableSize, settings.tableEmptyColor, OrderTable);
            SpriteRenderer lockRenderer = CreateSpriteChild(root.transform, "Lock", square, TableSize * LockScale, LockColor, OrderTableLock);

            var seat = new GameObject("SeatPoint").transform;
            seat.SetParent(root.transform, false);
            seat.localPosition = new Vector3(0f, SeatPointOffsetY, 0f);

            var so = new SerializedObject(table);
            so.FindProperty("seatPoint").objectReferenceValue = seat;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("lockVisual").objectReferenceValue = lockRenderer.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TablePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static SpriteRenderer CreateSpriteChild(Transform parent, string name, Sprite sprite, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private static SalonSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SalonSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder(DataFolder);
            settings = ScriptableObject.CreateInstance<SalonSettings>();

            var defaultType = AssetDatabase.LoadAssetAtPath<CustomerTypeData>(DefaultCustomerTypePath);
            if (defaultType != null)
            {
                settings.customerTypes.Add(defaultType);
            }

            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>Beyaz, 1x1 birimlik kare veya daire yer tutucu sprite'ı oluşturur (yoksa).</summary>
        private static Sprite GetOrCreatePlaceholderSprite(string path, bool circle)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));

            int size = PlaceholderTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Daire: piksel merkezi yarıçapın dışındaysa saydam.
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    bool inside = !circle || dx * dx + dy * dy <= radius * radius;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
                }
            }

            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
