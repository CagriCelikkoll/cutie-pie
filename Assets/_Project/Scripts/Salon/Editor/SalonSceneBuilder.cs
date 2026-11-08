using System.Collections.Generic;
using System.IO;
using CutiePie.Core.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

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

        // Giriş (sağ üst) ve kapı kutusu.
        private static readonly Vector3 EntrancePosition = new Vector3(8.8f, 4.4f, 0f);
        private static readonly Vector2 DoorSize = new Vector2(1.2f, 1.4f);
        private static readonly Color DoorColor = new Color(0.55f, 0.4f, 0.3f, 1f);

        // Hazırlama alanı yer tutucuları (sol taraf, x ≈ -9.1…-2.3). G3 servisi için şimdilik işlevsiz.
        private static readonly PrepBox[] PrepBoxes =
        {
            new PrepBox(BoxCakeShowcase, new Vector2(-5.7f, 3.6f), new Vector2(6.8f, 2.6f), new Color(0.98f, 0.85f, 0.88f, 1f)),
            new PrepBox(BoxDrinks, new Vector2(-5.7f, 0.7f), new Vector2(6.8f, 2.4f), new Color(0.8f, 0.9f, 0.98f, 1f)),
            new PrepBox(BoxTrayStack, new Vector2(-8.6f, -3.0f), new Vector2(1f, 3.4f), new Color(0.85f, 0.75f, 0.6f, 1f)),
            new PrepBox(BoxPlateStack, new Vector2(-7.4f, -3.0f), new Vector2(1f, 3.4f), new Color(0.95f, 0.95f, 0.95f, 1f)),
            new PrepBox(BoxPrepCounter, new Vector2(-5.1f, -3.0f), new Vector2(3.2f, 3.4f), new Color(0.9f, 0.8f, 0.65f, 1f)),
            new PrepBox(BoxTrashBin, new Vector2(-2.8f, -3.0f), new Vector2(1f, 3.4f), new Color(0.6f, 0.65f, 0.6f, 1f)),
        };

        private const string BoxCakeShowcase = "CakeShowcase";
        private const string BoxDrinks = "Drinks";
        private const string BoxTrayStack = "TrayStack";
        private const string BoxPlateStack = "PlateStack";
        private const string BoxPrepCounter = "PrepCounter";
        private const string BoxTrashBin = "TrashBin";

        // Kutunun görseli; kök ölçeksiz kalır ki altına konan parçalar ezilmesin.
        private const string PrepVisualName = "Visual";

        // Tepsi prefab'ı yerleşimi (tepsi merkezine göre, birim).
        private const string TrayPrefabPath = PrefabFolder + "/Tray.prefab";
        private static readonly Vector2 TraySize = new Vector2(2.4f, 1.4f);
        private static readonly Vector2 TrayPlateSlotOffset = new Vector2(-0.45f, 0f);
        private static readonly Vector2 TrayDrinkSlotOffset = new Vector2(0.75f, 0.15f);

        // Tezgâhta tepsi yerlerinin dizildiği dikey aralık (tezgâh merkezine göre). Üst kısım fişe kalır.
        private const float CounterSlotAreaTopY = 0.5f;
        private const float CounterSlotAreaBottomY = -1.7f;

        private readonly struct PrepBox
        {
            public readonly string Name;
            public readonly Vector2 Position;
            public readonly Vector2 Size;
            public readonly Color Color;

            public PrepBox(string name, Vector2 position, Vector2 size, Color color)
            {
                Name = name;
                Position = position;
                Size = size;
                Color = color;
            }
        }

        // Sorting Layer (ortak dosya) yerine sortingOrder; değerler çalışma zamanıyla ortak (SalonSortOrder).
        public const int OrderFloor = SalonSortOrder.Floor;
        public const int OrderDoor = SalonSortOrder.Door;
        public const int OrderTable = SalonSortOrder.Table;
        public const int OrderTableLock = SalonSortOrder.TableLock;
        public const int OrderCustomer = SalonSortOrder.Customer;
        public const int OrderOverlay = SalonSortOrder.Overlay;
        public const int OrderOverlayContent = SalonSortOrder.OverlayContent;

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

        [MenuItem(MenuRoot + "3 - Giriş, Spawner ve Hazırlık Alanını Kur")]
        private static void BuildSpawnerAndPrepArea()
        {
            if (!IsSalonSceneOpen())
            {
                return;
            }

            Transform root = GetOrCreateSceneObject("SalonRoot", null);
            Transform tablesTf = root.Find("Tables");
            TableManager manager = tablesTf != null ? tablesTf.GetComponent<TableManager>() : null;
            var customerPrefab = AssetDatabase.LoadAssetAtPath<Customer>(CustomerPrefabPath);
            if (manager == null || customerPrefab == null)
            {
                EditorUtility.DisplayDialog("Salon Kurulumu", "Önce 1. ve 2. menü adımlarını çalıştırın (Tables ve Customer.prefab gerekli).", "Tamam");
                return;
            }

            if (root.Find("Entrance") != null || root.Find("PrepArea") != null || root.Find("CustomerSpawner") != null)
            {
                if (!EditorUtility.DisplayDialog("Salon Kurulumu", "Entrance / PrepArea / CustomerSpawner zaten var. Silinip yeniden kurulsun mu?", "Evet", "Vazgeç"))
                {
                    return;
                }

                foreach (string name in new[] { "Entrance", "PrepArea", "CustomerSpawner", "Customers" })
                {
                    Transform old = root.Find(name);
                    if (old != null)
                    {
                        Undo.DestroyObjectImmediate(old.gameObject);
                    }
                }
            }

            Sprite square = GetOrCreatePlaceholderSprite(SquareSpritePath, false);
            SalonSettings settings = GetOrCreateSettings();
            EnsureDefaultCustomerType(settings);

            // Hazırlama alanı yer tutucuları (ölçeksiz kök + ölçekli Visual).
            Transform prepArea = GetOrCreateSceneObject("PrepArea", root);
            foreach (PrepBox box in PrepBoxes)
            {
                CreatePrepBox(prepArea, box, square);
            }

            // Giriş noktası ve kapı kutusu.
            Transform entrance = GetOrCreateSceneObject("Entrance", root);
            entrance.localPosition = EntrancePosition;
            CreateSpriteChild(entrance, "Door", square, DoorSize, DoorColor, OrderDoor);

            Transform customers = GetOrCreateSceneObject("Customers", root);

            Transform spawnerTf = GetOrCreateSceneObject("CustomerSpawner", root);
            CustomerSpawner spawner = Undo.AddComponent<CustomerSpawner>(spawnerTf.gameObject);
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("settings").objectReferenceValue = settings;
            spawnerSo.FindProperty("tableManager").objectReferenceValue = manager;
            spawnerSo.FindProperty("customerPrefab").objectReferenceValue = customerPrefab;
            spawnerSo.FindProperty("entrance").objectReferenceValue = entrance;
            spawnerSo.FindProperty("customerContainer").objectReferenceValue = customers;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            // Başlatıcıya spawner bağlanır.
            Transform bootstrapTf = root.Find("SalonBootstrap");
            SalonBootstrap bootstrap = bootstrapTf != null ? bootstrapTf.GetComponent<SalonBootstrap>() : null;
            if (bootstrap != null)
            {
                var bootstrapSo = new SerializedObject(bootstrap);
                bootstrapSo.FindProperty("customerSpawner").objectReferenceValue = spawner;
                bootstrapSo.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[SalonSceneBuilder] SalonBootstrap bulunamadı; 1. adımı yeniden çalıştırın.");
            }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = spawnerTf.gameObject;
            Debug.Log("[SalonSceneBuilder] Giriş, spawner ve hazırlık alanı kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        [MenuItem(MenuRoot + "4 - Hazırlama Alanını Kur")]
        private static void BuildPrepArea()
        {
            if (!IsSalonSceneOpen())
            {
                return;
            }

            Transform root = GetOrCreateSceneObject("SalonRoot", null);
            Transform prepArea = root.Find("PrepArea");
            if (prepArea == null)
            {
                EditorUtility.DisplayDialog("Hazırlama Alanı", "Önce 3. menü adımını çalıştırın (PrepArea gerekli).", "Tamam");
                return;
            }

            // Fare ve dokunma girdisi: EventSystem + kamerada 2D fizik raycaster'ı.
            EnsureEventSystem();
            EnsurePhysics2DRaycaster();

            Sprite square = GetOrCreatePlaceholderSprite(SquareSpritePath, false);
            SalonSettings settings = GetOrCreateSettings();

            // Bırakma hedeflerinin listesi PrepArea üzerinde durur.
            DropTargetRegistry registry = GetOrAddComponent<DropTargetRegistry>(prepArea.gameObject);

            // Eski düzendeki (kökü ölçekli) kutular yeni düzene çevrilir.
            foreach (PrepBox box in PrepBoxes)
            {
                GetOrConvertPrepBox(prepArea, box, square);
            }

            Tray trayPrefab = BuildTrayPrefab(square, settings);
            PrepCounter counter = SetupPrepCounter(prepArea, registry, settings);
            SetupTrayStack(prepArea, settings, registry, trayPrefab, counter);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = prepArea.gameObject;
            Debug.Log("[SalonSceneBuilder] Hazırlama alanı kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        private static PrepCounter SetupPrepCounter(Transform prepArea, DropTargetRegistry registry, SalonSettings settings)
        {
            Transform counterTf = prepArea.Find(BoxPrepCounter);
            PrepCounter counter = GetOrAddComponent<PrepCounter>(counterTf.gameObject);

            // Tepsi yerleri her kurulumda yeniden üretilir (sayı SalonSettings'ten).
            DestroyChildrenExcept(counterTf, PrepVisualName);
            int count = Mathf.Max(1, settings.prepTraySlotCount);
            var slots = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                // Yerler dikey aralığa eşit dağılır; ilk yer en üstte.
                float t = (i + 0.5f) / count;
                float y = Mathf.Lerp(CounterSlotAreaTopY, CounterSlotAreaBottomY, t);
                slots[i] = CreatePoint(counterTf, $"TraySlot_{i}", new Vector2(0f, y));
            }

            var so = new SerializedObject(counter);
            so.FindProperty("registry").objectReferenceValue = registry;
            SerializedProperty list = so.FindProperty("traySlots");
            list.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            so.ApplyModifiedProperties();
            return counter;
        }

        private static void SetupTrayStack(Transform prepArea, SalonSettings settings, DropTargetRegistry registry, Tray trayPrefab, PrepCounter counter)
        {
            Transform stackTf = prepArea.Find(BoxTrayStack);
            TrayStack stack = GetOrAddComponent<TrayStack>(stackTf.gameObject);
            DragItem drag = GetOrAddComponent<DragItem>(stackTf.gameObject);
            EnsureBoxCollider(stackTf, FindPrepBox(BoxTrayStack).Size);

            var stackSo = new SerializedObject(stack);
            stackSo.FindProperty("settings").objectReferenceValue = settings;
            stackSo.FindProperty("trayPrefab").objectReferenceValue = trayPrefab;
            stackSo.FindProperty("counter").objectReferenceValue = counter;
            stackSo.ApplyModifiedProperties();

            AssignDragItem(drag, settings, registry);
        }

        private static void AssignDragItem(DragItem drag, SalonSettings settings, DropTargetRegistry registry)
        {
            var so = new SerializedObject(drag);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("registry").objectReferenceValue = registry;
            so.ApplyModifiedProperties();
        }

        /// <summary>Tepsi prefab'ını her seferinde yeniden üretir (aynı yol → GUID ve sahne bağlantıları korunur).</summary>
        private static Tray BuildTrayPrefab(Sprite square, SalonSettings settings)
        {
            EnsureFolder(PrefabFolder);

            var root = new GameObject("Tray");
            ServingItem item = root.AddComponent<ServingItem>(); // SortingGroup RequireComponent ile gelir
            root.GetComponent<SortingGroup>().sortingOrder = SalonSortOrder.PrepItem;
            Tray tray = root.AddComponent<Tray>();

            SpriteRenderer body = CreateSpriteChild(root.transform, "Body", square, TraySize, settings.trayColor, 0);
            Transform plateSlot = CreatePoint(root.transform, "PlateSlot", TrayPlateSlotOffset);
            Transform drinkSlot = CreatePoint(root.transform, "DrinkSlot", TrayDrinkSlotOffset);

            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("kind").intValue = (int)ServingItemKind.Tray;
            itemSo.FindProperty("visual").objectReferenceValue = body;
            itemSo.ApplyModifiedPropertiesWithoutUndo();

            var traySo = new SerializedObject(tray);
            traySo.FindProperty("plateSlot").objectReferenceValue = plateSlot;
            traySo.FindProperty("drinkSlot").objectReferenceValue = drinkSlot;
            traySo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TrayPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Tray>();
        }

        // --- Hazırlama alanı kutuları ---

        private static PrepBox FindPrepBox(string name)
        {
            foreach (PrepBox box in PrepBoxes)
            {
                if (box.Name == name)
                {
                    return box;
                }
            }

            throw new System.ArgumentException($"Bilinmeyen hazırlama kutusu: {name}");
        }

        /// <summary>Ölçeksiz kök + ölçekli "Visual" sprite olarak bir hazırlama kutusu kurar.</summary>
        private static Transform CreatePrepBox(Transform prepArea, PrepBox box, Sprite square)
        {
            var go = new GameObject(box.Name);
            Undo.RegisterCreatedObjectUndo(go, "Hazırlama Alanı");
            go.transform.SetParent(prepArea, false);
            go.transform.localPosition = box.Position;
            CreateSpriteChild(go.transform, PrepVisualName, square, box.Size, box.Color, OrderFloor);
            return go.transform;
        }

        /// <summary>Kutu yeni düzendeyse onu döner; eski düzendeyse (kökte SpriteRenderer) yerine yenisini kurar.</summary>
        private static Transform GetOrConvertPrepBox(Transform prepArea, PrepBox box, Sprite square)
        {
            Transform existing = prepArea.Find(box.Name);
            if (existing != null && existing.Find(PrepVisualName) != null)
            {
                return existing;
            }

            int siblingIndex = existing != null ? existing.GetSiblingIndex() : -1;
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            Transform created = CreatePrepBox(prepArea, box, square);
            if (siblingIndex >= 0)
            {
                created.SetSiblingIndex(siblingIndex);
            }

            return created;
        }

        private static void DestroyChildrenExcept(Transform parent, string keepName)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name != keepName)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        private static Transform CreatePoint(Transform parent, string name, Vector2 localPosition)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Hazırlama Alanı");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static void EnsureBoxCollider(Transform target, Vector2 size)
        {
            BoxCollider2D col = GetOrAddComponent<BoxCollider2D>(target.gameObject);
            Undo.RecordObject(col, "Collider");
            col.size = size;
            col.offset = Vector2.zero;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
        }

        private static void EnsurePhysics2DRaycaster()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[SalonSceneBuilder] MainCamera etiketli kamera bulunamadı; Physics2DRaycaster eklenmedi.");
                return;
            }

            if (cam.GetComponent<Physics2DRaycaster>() == null)
            {
                Undo.AddComponent<Physics2DRaycaster>(cam.gameObject);
            }
        }

        private static void EnsureDefaultCustomerType(SalonSettings settings)
        {
            if (settings.customerTypes.Count > 0)
            {
                return;
            }

            var defaultType = AssetDatabase.LoadAssetAtPath<CustomerTypeData>(DefaultCustomerTypePath);
            if (defaultType != null)
            {
                Undo.RecordObject(settings, "Salon Ayarları");
                settings.customerTypes.Add(defaultType);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
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
