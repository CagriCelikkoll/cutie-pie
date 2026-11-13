using System.Collections.Generic;
using CutiePie.Core.Data;
using CutiePie.Core.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: G3a hazırlama alanını (tepsi → tabak → dilim → bardak, çöp, fiş) kurar.
    /// "4 - Hazırlama Alanını Kur" tekrar çalıştırılabilir: prefab'ları yeniden üretir, üretilmiş sahne parçalarını yeniler.
    /// </summary>
    public static partial class SalonSceneBuilder
    {
        // Kutunun görseli; kök ölçeksiz kalır ki altına konan parçalar ezilmesin.
        private const string PrepVisualName = "Visual";
        private const string OrderTicketName = "OrderTicket";
        private const string ReadyTrayStationName = "ReadyTrayStation";

        // Prefab yolları.
        private const string TrayPrefabPath = PrefabFolder + "/Tray.prefab";
        private const string PlatePrefabPath = PrefabFolder + "/Plate.prefab";
        private const string CakeSlicePrefabPath = PrefabFolder + "/CakeSlice.prefab";
        private const string DrinkCupPrefabPath = PrefabFolder + "/DrinkCup.prefab";
        private const string ShowcaseSlotPrefabPath = PrefabFolder + "/ShowcaseSlot.prefab";
        private const string DrinkMachineSlotPrefabPath = PrefabFolder + "/DrinkMachineSlot.prefab";
        private const string OrderTicketPrefabPath = PrefabFolder + "/OrderTicket.prefab";

        // Tepsi (tepsi merkezine göre, birim).
        private static readonly Vector2 TraySize = new Vector2(2.4f, 1.4f);
        private static readonly Vector2 TrayPlateSlotOffset = new Vector2(-0.45f, 0f);
        private static readonly Vector2 TrayDrinkSlotOffset = new Vector2(0.75f, 0.15f);

        // Tabak, dilim, bardak.
        private static readonly Vector2 PlateSize = new Vector2(1.05f, 1.05f);
        private static readonly Vector2 PlateAttachOffset = new Vector2(0f, 0.05f);
        private static readonly Vector2 SliceSize = new Vector2(0.55f, 0.45f);
        private static readonly Vector2 CupSize = new Vector2(0.45f, 0.65f);

        // Tezgâhta tepsi yerlerinin dizildiği dikey aralık (tezgâh merkezine göre). Üst kısım fişe kalır.
        private const float CounterSlotAreaTopY = 0.5f;
        private const float CounterSlotAreaBottomY = -1.7f;

        // Raf (vitrin / içecek): yuvalar okların arasındaki genişliğe eşit dağılır.
        private const float ShelfArrowMargin = 0.8f;
        private const float ShelfArrowInset = 0.4f;
        private static readonly Vector2 ShelfSlotColliderSize = new Vector2(1.2f, 1.9f);
        private const float ShelfIconSize = 1f;
        private const float ShelfIconOffsetY = 0.2f;
        private const float ShelfCountOffsetY = -0.65f;
        private const float ShelfCountFontSize = 4.5f;
        private static readonly Vector2 ShelfCountRectSize = new Vector2(1.2f, 0.5f);
        private static readonly Color ShelfCountColor = new Color(0.3f, 0.2f, 0.15f, 1f);

        // Vitrin yuvası "Tükendi" etiketi (G16): sayının yerinde, otomatik küçülen tek satır.
        private const string SoldOutKey = "salon.sold_out";
        private const string SoldOutFontPath = "Assets/_Project/Art/Fonts/Nunito-Bold SDF.asset";
        private const float SoldOutFontSize = 3.2f;
        private const float SoldOutMinFontSize = 1.5f;
        private static readonly Color SoldOutColor = new Color(0.85f, 0.3f, 0.35f, 1f);
        private static readonly Vector2 ArrowVisualSize = new Vector2(0.35f, 0.9f);
        private static readonly Vector2 ArrowColliderSize = new Vector2(0.7f, 1.6f);
        private static readonly Color ArrowColor = new Color(0.45f, 0.35f, 0.4f, 1f);

        // Fiş: tezgâhın üst kısmında (tezgâh merkezine göre).
        private static readonly Vector2 TicketOffsetOnCounter = new Vector2(0f, 1.15f);
        private static readonly Vector2 TicketSize = new Vector2(2.8f, 0.9f);
        private static readonly Color TicketPaperColor = new Color(1f, 0.98f, 0.92f, 1f);
        private const float TicketMarkerX = -1f;
        private const float TicketMarkerSize = 0.55f;
        private const float TicketCakeLineX = -0.05f;
        private const float TicketDrinkLineX = 0.95f;
        private const float TicketIconSize = 0.5f;
        private const float TicketTickSize = 0.28f;
        private static readonly Vector2 TicketTickOffset = new Vector2(0.3f, 0.22f);

        // Müşteri dokunma alanı gövdenin biraz üstüne kayar (balonu da kapsasın).
        private const float CustomerTapOffsetY = 0.3f;

        // İkonu olmayan menü ürünlerine verilecek ayırt edici yer tutucu renkler (sırayla).
        private static readonly Color[] CakePlaceholderPalette =
        {
            new Color(0.45f, 0.28f, 0.18f, 1f), // kahverengi
            new Color(0.95f, 0.5f, 0.6f, 1f),   // pembe
            new Color(0.95f, 0.85f, 0.55f, 1f), // krem
            new Color(0.6f, 0.4f, 0.75f, 1f),   // mor
        };

        private static readonly Color[] DrinkPlaceholderPalette =
        {
            new Color(0.98f, 0.92f, 0.35f, 1f), // sarı
            new Color(0.55f, 0.75f, 0.35f, 1f), // yeşil
            new Color(0.95f, 0.6f, 0.25f, 1f),  // turuncu
            new Color(0.45f, 0.7f, 0.95f, 1f),  // mavi
        };

        // Test: sayfalama seviyesi (yalnızca geliştirme için).
        private const string TestFolder = DataFolder + "/Test";
        private const string TestIconFolder = TestFolder + "/Icons";
        private const string TestLevelPath = TestFolder + "/Level_Test_Paging.asset";
        private const int TestProductPrice = 10;

        private static readonly Color32[] TestCakeColors =
        {
            new Color32(230, 80, 80, 255),
            new Color32(120, 200, 120, 255),
            new Color32(90, 120, 230, 255),
            new Color32(240, 170, 60, 255),
        };

        private static readonly Color32[] TestDrinkColors =
        {
            new Color32(60, 200, 200, 255),
            new Color32(200, 90, 200, 255),
            new Color32(150, 150, 150, 255),
        };

        [MenuItem(MenuRoot + "4 - Hazırlama Alanını Kur")]
        private static void BuildPrepArea()
        {
            if (!IsSalonSceneOpen())
            {
                return;
            }

            Transform root = GetOrCreateSceneObject("SalonRoot", null);
            Transform prepArea = root.Find("PrepArea");
            if (prepArea == null || AssetDatabase.LoadAssetAtPath<GameObject>(CustomerPrefabPath) == null)
            {
                EditorUtility.DisplayDialog("Hazırlama Alanı", "Önce 1–3. menü adımlarını çalıştırın (PrepArea ve Customer.prefab gerekli).", "Tamam");
                return;
            }

            // Fare ve dokunma girdisi: EventSystem + kamerada 2D fizik raycaster'ı.
            EnsureEventSystem();
            EnsurePhysics2DRaycaster();

            Sprite square = GetOrCreatePlaceholderSprite(SquareSpritePath, false);
            Sprite circle = GetOrCreatePlaceholderSprite(CircleSpritePath, true);
            SalonSettings settings = GetOrCreateSettings();
            EnsureProductPlaceholderColors(settings);

            // Bırakma hedeflerinin listesi ve ortak stok adaptörü PrepArea üzerinde durur.
            // G16: silinen LocalShowcaseStock'tan kalan "Missing Script" bileşeni temizlenir.
            int removedMissing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prepArea.gameObject);
            if (removedMissing > 0)
            {
                Debug.Log($"[SalonSceneBuilder] PrepArea'dan {removedMissing} eksik betik bileşeni (eski LocalShowcaseStock) kaldırıldı.");
            }

            DropTargetRegistry registry = GetOrAddComponent<DropTargetRegistry>(prepArea.gameObject);
            SharedShowcaseStock stock = GetOrAddComponent<SharedShowcaseStock>(prepArea.gameObject);

            // Eski düzendeki (kökü ölçekli) kutular yeni düzene çevrilir.
            foreach (PrepBox box in PrepBoxes)
            {
                GetOrConvertPrepBox(prepArea, box, square);
            }

            // Prefab'lar (her seferinde yeniden üretilir; aynı yol → GUID korunur).
            EnsureFolder(PrefabFolder);
            ServingItem platePrefab = BuildServingItemPrefab(PlatePrefabPath, "Plate", ServingItemKind.Plate, circle, PlateSize, settings.plateColor, true);
            ServingItem slicePrefab = BuildServingItemPrefab(CakeSlicePrefabPath, "CakeSlice", ServingItemKind.CakeSlice, square, SliceSize, settings.cakePlaceholderColor, false);
            ServingItem cupPrefab = BuildServingItemPrefab(DrinkCupPrefabPath, "DrinkCup", ServingItemKind.Drink, square, CupSize, settings.drinkPlaceholderColor, false);
            Tray trayPrefab = BuildTrayPrefab(square, settings);
            ShowcaseSlot showcaseSlotPrefab = BuildShelfSlotPrefab<ShowcaseSlot>(ShowcaseSlotPrefabPath, "ShowcaseSlot", square, settings, slicePrefab, settings.cakePlaceholderColor, true);
            DrinkMachineSlot drinkSlotPrefab = BuildShelfSlotPrefab<DrinkMachineSlot>(DrinkMachineSlotPrefabPath, "DrinkMachineSlot", square, settings, cupPrefab, settings.drinkPlaceholderColor, false);
            OrderTicket ticketPrefab = BuildOrderTicketPrefab(square, circle, settings);

            // Sahne parçaları.
            PrepCounter counter = SetupPrepCounter(prepArea, registry, settings);
            TrayStack trayStack = SetupTrayStack(prepArea, settings, registry, trayPrefab, counter);
            PlateStack plateStack = SetupPlateStack(prepArea, settings, registry, platePrefab, counter);
            ReadyTrayStation readyTrayStation = SetupReadyTrayStation(prepArea, settings, counter, trayStack, plateStack);
            ProductShelf cakeShelf = SetupShelf(prepArea, BoxCakeShowcase, ProductCategory.Cake, showcaseSlotPrefab, settings, registry, square);
            ProductShelf drinkShelf = SetupShelf(prepArea, BoxDrinks, ProductCategory.Drink, drinkSlotPrefab, settings, registry, square);
            SetupTrashBin(prepArea, registry);
            SetupOrderTicket(prepArea, ticketPrefab, counter);

            EnsureCustomerTapCollider(settings);
            WireSalonRoundPrep(root, stock, cakeShelf, drinkShelf, readyTrayStation);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = prepArea.gameObject;
            Debug.Log("[SalonSceneBuilder] Hazırlama alanı kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        [MenuItem(MenuRoot + "5 - Test: Sayfalama Seviyesi Oluştur")]
        private static void BuildPagingTestLevel()
        {
            var baseLevel = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
            if (baseLevel == null)
            {
                EditorUtility.DisplayDialog("Test Seviyesi", $"{LevelPath} bulunamadı.", "Tamam");
                return;
            }

            EnsureFolder(TestIconFolder);

            // Menü: Level_01'in ürünleri (ilk sayfa) + renkli ikonlu test ürünleri → her rafta 4'ten fazla ürün.
            var menu = new List<ProductData>(baseLevel.menu);
            for (int i = 0; i < TestCakeColors.Length; i++)
            {
                menu.Add(GetOrCreateTestProduct($"test_cake_{i + 1}", ProductCategory.Cake, TestCakeColors[i]));
            }

            for (int i = 0; i < TestDrinkColors.Length; i++)
            {
                menu.Add(GetOrCreateTestProduct($"test_drink_{i + 1}", ProductCategory.Drink, TestDrinkColors[i]));
            }

            var level = AssetDatabase.LoadAssetAtPath<LevelData>(TestLevelPath);
            if (level == null)
            {
                level = Object.Instantiate(baseLevel);
                AssetDatabase.CreateAsset(level, TestLevelPath);
            }

            level.id = "test_paging";
            level.menu = menu;
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();

            Selection.activeObject = level;
            EditorGUIUtility.PingObject(level);
            // Salon seviyesi artık SaveSystem'deki mevcut seviyeden gelir (RoundManager); bu varlık kendiliğinden kullanılmaz.
            Debug.Log("[SalonSceneBuilder] Test seviyesi hazır (Level_Test_Paging).");
        }

        // --- Sahne parçaları ---

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
            SetObjectArray(so.FindProperty("traySlots"), slots);
            so.ApplyModifiedProperties();
            return counter;
        }

        private static TrayStack SetupTrayStack(Transform prepArea, SalonSettings settings, DropTargetRegistry registry, Tray trayPrefab, PrepCounter counter)
        {
            Transform stackTf = prepArea.Find(BoxTrayStack);
            TrayStack stack = GetOrAddComponent<TrayStack>(stackTf.gameObject);
            EnsureBoxCollider(stackTf, FindPrepBox(BoxTrayStack).Size);

            var so = new SerializedObject(stack);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("registry").objectReferenceValue = registry;
            so.FindProperty("trayPrefab").objectReferenceValue = trayPrefab;
            so.FindProperty("counter").objectReferenceValue = counter;
            so.ApplyModifiedProperties();

            AssignDragItem(GetOrAddComponent<DragItem>(stackTf.gameObject), settings, registry);
            return stack;
        }

        private static PlateStack SetupPlateStack(Transform prepArea, SalonSettings settings, DropTargetRegistry registry, ServingItem platePrefab, PrepCounter counter)
        {
            Transform stackTf = prepArea.Find(BoxPlateStack);
            PlateStack stack = GetOrAddComponent<PlateStack>(stackTf.gameObject);
            EnsureBoxCollider(stackTf, FindPrepBox(BoxPlateStack).Size);

            var so = new SerializedObject(stack);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("platePrefab").objectReferenceValue = platePrefab;
            so.FindProperty("counter").objectReferenceValue = counter;
            so.ApplyModifiedProperties();

            AssignDragItem(GetOrAddComponent<DragItem>(stackTf.gameObject), settings, registry);
            return stack;
        }

        /// <summary>Görseli olmayan istasyon nesnesi (PrepArea/ReadyTrayStation); tekrar çalıştırılınca yalnızca referanslar güncellenir.</summary>
        private static ReadyTrayStation SetupReadyTrayStation(Transform prepArea, SalonSettings settings, PrepCounter counter,
            TrayStack trayStack, PlateStack plateStack)
        {
            Transform stationTf = prepArea.Find(ReadyTrayStationName);
            if (stationTf == null)
            {
                stationTf = CreatePoint(prepArea, ReadyTrayStationName, Vector2.zero);
            }

            ReadyTrayStation station = GetOrAddComponent<ReadyTrayStation>(stationTf.gameObject);

            var so = new SerializedObject(station);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("counter").objectReferenceValue = counter;
            so.FindProperty("trayStack").objectReferenceValue = trayStack;
            so.FindProperty("plateStack").objectReferenceValue = plateStack;
            so.ApplyModifiedProperties();
            return station;
        }

        private static ProductShelf SetupShelf(Transform prepArea, string boxName, ProductCategory category, ShelfSlot slotPrefab,
            SalonSettings settings, DropTargetRegistry registry, Sprite square)
        {
            Transform shelfTf = prepArea.Find(boxName);
            PrepBox box = FindPrepBox(boxName);
            ProductShelf shelf = GetOrAddComponent<ProductShelf>(shelfTf.gameObject);

            // Yuvalar ve oklar her kurulumda yeniden üretilir (yuva sayısı SalonSettings.shelfSlotsPerPage).
            DestroyChildrenExcept(shelfTf, PrepVisualName);
            int count = Mathf.Max(1, settings.shelfSlotsPerPage);
            float usableWidth = box.Size.x - 2f * ShelfArrowMargin;
            float spacing = usableWidth / count;

            var slots = new ShelfSlot[count];
            for (int i = 0; i < count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, shelfTf);
                Undo.RegisterCreatedObjectUndo(go, "Raf Yuvası");
                go.name = $"Slot_{i}";
                go.transform.localPosition = new Vector3(-usableWidth * 0.5f + spacing * (i + 0.5f), 0f, 0f);
                slots[i] = go.GetComponent<ShelfSlot>();

                // Registry sahnede olduğu için prefab'a yazılamaz; örnekte atanır.
                AssignDragItem(go.GetComponent<DragItem>(), settings, registry);
            }

            float arrowX = box.Size.x * 0.5f - ShelfArrowInset;
            PageArrow prev = CreatePageArrow(shelfTf, "PrevArrow", -arrowX, -1, shelf, square);
            PageArrow next = CreatePageArrow(shelfTf, "NextArrow", arrowX, 1, shelf, square);

            var so = new SerializedObject(shelf);
            so.FindProperty("category").intValue = (int)category;
            SetObjectArray(so.FindProperty("slots"), slots);
            so.FindProperty("prevArrow").objectReferenceValue = prev;
            so.FindProperty("nextArrow").objectReferenceValue = next;
            so.ApplyModifiedProperties();
            return shelf;
        }

        private static PageArrow CreatePageArrow(Transform parent, string name, float x, int direction, ProductShelf shelf, Sprite square)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Sayfa Oku");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);

            CreateSpriteChild(go.transform, PrepVisualName, square, ArrowVisualSize, ArrowColor, SalonSortOrder.PrepFixture);
            go.AddComponent<BoxCollider2D>().size = ArrowColliderSize;
            PageArrow arrow = go.AddComponent<PageArrow>();

            var so = new SerializedObject(arrow);
            so.FindProperty("shelf").objectReferenceValue = shelf;
            so.FindProperty("direction").intValue = direction;
            so.ApplyModifiedPropertiesWithoutUndo();
            return arrow;
        }

        private static void SetupTrashBin(Transform prepArea, DropTargetRegistry registry)
        {
            Transform trashTf = prepArea.Find(BoxTrashBin);
            TrashBin trash = GetOrAddComponent<TrashBin>(trashTf.gameObject);
            SetObjectField(trash, "registry", registry);
        }

        private static void SetupOrderTicket(Transform prepArea, OrderTicket ticketPrefab, PrepCounter counter)
        {
            Transform old = prepArea.Find(OrderTicketName);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(ticketPrefab.gameObject, prepArea);
            Undo.RegisterCreatedObjectUndo(go, "Sipariş Fişi");
            go.name = OrderTicketName;
            go.transform.localPosition = (Vector2)counter.transform.localPosition + TicketOffsetOnCounter;

            SetObjectField(go.GetComponent<OrderTicket>(), "counter", counter);
        }

        private static void WireSalonRoundPrep(Transform root, SharedShowcaseStock stock, ProductShelf cakeShelf, ProductShelf drinkShelf,
            ReadyTrayStation readyTrayStation)
        {
            var so = new SerializedObject(GetOrCreateSalonRound(root));
            so.FindProperty("showcaseStock").objectReferenceValue = stock;
            so.FindProperty("cakeShelf").objectReferenceValue = cakeShelf;
            so.FindProperty("drinkShelf").objectReferenceValue = drinkShelf;
            so.FindProperty("readyTrayStation").objectReferenceValue = readyTrayStation;
            so.ApplyModifiedProperties();
        }

        // --- Prefab'lar ---

        private static ServingItem BuildServingItemPrefab(string path, string name, ServingItemKind kind, Sprite sprite, Vector2 size, Color color, bool withAttachPoint)
        {
            var root = new GameObject(name);
            ServingItem item = root.AddComponent<ServingItem>(); // SortingGroup RequireComponent ile gelir
            root.GetComponent<SortingGroup>().sortingOrder = SalonSortOrder.PrepItem;

            SpriteRenderer visual = CreateSpriteChild(root.transform, PrepVisualName, sprite, size, color, 0);
            Transform attach = withAttachPoint ? CreateChildTransform(root.transform, "AttachPoint", PlateAttachOffset) : null;

            var so = new SerializedObject(item);
            so.FindProperty("kind").intValue = (int)kind;
            so.FindProperty("visual").objectReferenceValue = visual;
            so.FindProperty("attachPoint").objectReferenceValue = attach;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, path).GetComponent<ServingItem>();
        }

        private static Tray BuildTrayPrefab(Sprite square, SalonSettings settings)
        {
            var root = new GameObject("Tray");
            ServingItem item = root.AddComponent<ServingItem>(); // SortingGroup RequireComponent ile gelir
            root.GetComponent<SortingGroup>().sortingOrder = SalonSortOrder.PrepItem;
            Tray tray = root.AddComponent<Tray>(); // DragItem RequireComponent ile gelir
            root.AddComponent<BoxCollider2D>().size = TraySize;

            SpriteRenderer body = CreateSpriteChild(root.transform, "Body", square, TraySize, settings.trayColor, 0);
            Transform plateSlot = CreateChildTransform(root.transform, "PlateSlot", TrayPlateSlotOffset);
            Transform drinkSlot = CreateChildTransform(root.transform, "DrinkSlot", TrayDrinkSlotOffset);

            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("kind").intValue = (int)ServingItemKind.Tray;
            itemSo.FindProperty("visual").objectReferenceValue = body;
            itemSo.ApplyModifiedPropertiesWithoutUndo();

            var traySo = new SerializedObject(tray);
            traySo.FindProperty("plateSlot").objectReferenceValue = plateSlot;
            traySo.FindProperty("drinkSlot").objectReferenceValue = drinkSlot;
            traySo.ApplyModifiedPropertiesWithoutUndo();

            // Registry çalışma zamanında Tray.Init ile verilir.
            var dragSo = new SerializedObject(root.GetComponent<DragItem>());
            dragSo.FindProperty("settings").objectReferenceValue = settings;
            dragSo.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, TrayPrefabPath).GetComponent<Tray>();
        }

        private static T BuildShelfSlotPrefab<T>(string path, string name, Sprite square, SalonSettings settings, ServingItem itemPrefab,
            Color iconColor, bool withCount) where T : ShelfSlot
        {
            var root = new GameObject(name);
            T slot = root.AddComponent<T>();
            DragItem drag = root.GetComponent<DragItem>();
            if (drag == null)
            {
                drag = root.AddComponent<DragItem>();
            }

            root.AddComponent<BoxCollider2D>().size = ShelfSlotColliderSize;

            SpriteRenderer icon = CreateSpriteChild(root.transform, "Icon", square, new Vector2(ShelfIconSize, ShelfIconSize), iconColor, SalonSortOrder.PrepFixture);
            icon.transform.localPosition = new Vector3(0f, ShelfIconOffsetY, 0f);

            var so = new SerializedObject(slot);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
            so.FindProperty("icon").objectReferenceValue = icon;

            if (withCount)
            {
                // Yalnızca rakam basılır (oyuncuya görünen kelime yok → yerelleştirme gerekmez).
                var textGo = new GameObject("Count", typeof(RectTransform));
                textGo.transform.SetParent(root.transform, false);
                textGo.transform.localPosition = new Vector3(0f, ShelfCountOffsetY, 0f);

                TextMeshPro text = textGo.AddComponent<TextMeshPro>();
                text.font = TMP_Settings.defaultFontAsset;
                text.text = "0";
                text.fontSize = ShelfCountFontSize;
                text.alignment = TextAlignmentOptions.Center;
                text.color = ShelfCountColor;
                text.sortingOrder = SalonSortOrder.PrepFixtureOverlay;
                text.rectTransform.sizeDelta = ShelfCountRectSize;

                so.FindProperty("countText").objectReferenceValue = text;
                so.FindProperty("soldOutLabel").objectReferenceValue = BuildSoldOutLabel(root.transform);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var dragSo = new SerializedObject(drag);
            dragSo.FindProperty("settings").objectReferenceValue = settings;
            dragSo.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, path).GetComponent<T>();
        }

        /// <summary>
        /// Vitrin yuvasında sayının yerinde duran "Tükendi" etiketi (G16). Metin LocalizedText ile salon.sold_out anahtarından gelir;
        /// Türkçe karakterler için Nunito fontu kullanılır. Başta kapalıdır, ShowcaseSlot stok 0 olunca açar.
        /// </summary>
        private static GameObject BuildSoldOutLabel(Transform slotRoot)
        {
            var labelGo = new GameObject("SoldOut", typeof(RectTransform));
            labelGo.transform.SetParent(slotRoot, false);
            labelGo.transform.localPosition = new Vector3(0f, ShelfCountOffsetY, 0f);

            TextMeshPro text = labelGo.AddComponent<TextMeshPro>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SoldOutFontPath);
            if (font == null)
            {
                Debug.LogWarning($"[SalonSceneBuilder] '{SoldOutFontPath}' bulunamadı; TMP varsayılan fontu kullanılıyor.");
                font = TMP_Settings.defaultFontAsset;
            }

            text.font = font;
            text.text = SoldOutKey; // Editörde anahtar görünür; oyunda LocalizedText gerçek metni yazar.
            text.enableAutoSizing = true;
            text.fontSizeMin = SoldOutMinFontSize;
            text.fontSizeMax = SoldOutFontSize;
            text.fontSize = SoldOutFontSize;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
            text.color = SoldOutColor;
            text.sortingOrder = SalonSortOrder.PrepFixtureOverlay;
            text.rectTransform.sizeDelta = ShelfCountRectSize;

            LocalizedText localized = labelGo.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.FindProperty("key").stringValue = SoldOutKey;
            so.ApplyModifiedPropertiesWithoutUndo();

            labelGo.SetActive(false);
            return labelGo;
        }

        private static OrderTicket BuildOrderTicketPrefab(Sprite square, Sprite circle, SalonSettings settings)
        {
            var root = new GameObject(OrderTicketName);
            root.AddComponent<SortingGroup>().sortingOrder = SalonSortOrder.PrepFixture;
            OrderTicket ticket = root.AddComponent<OrderTicket>();

            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            CreateSpriteChild(content.transform, "Paper", square, TicketSize, TicketPaperColor, 0);
            SpriteRenderer marker = CreateSpriteChild(content.transform, "CustomerMarker", circle, new Vector2(TicketMarkerSize, TicketMarkerSize), Color.white, 1);
            marker.transform.localPosition = new Vector3(TicketMarkerX, 0f, 0f);

            TicketLine cakeLine = BuildTicketLine(content.transform, "CakeLine", TicketCakeLineX, square, circle, settings.cakePlaceholderColor, settings);
            TicketLine drinkLine = BuildTicketLine(content.transform, "DrinkLine", TicketDrinkLineX, square, circle, settings.drinkPlaceholderColor, settings);

            var so = new SerializedObject(ticket);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("customerMarker").objectReferenceValue = marker;
            so.FindProperty("cakeLine").objectReferenceValue = cakeLine;
            so.FindProperty("drinkLine").objectReferenceValue = drinkLine;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, OrderTicketPrefabPath).GetComponent<OrderTicket>();
        }

        private static TicketLine BuildTicketLine(Transform parent, string name, float x, Sprite square, Sprite circle, Color iconColor, SalonSettings settings)
        {
            Transform lineTf = CreateChildTransform(parent, name, new Vector2(x, 0f));
            TicketLine line = lineTf.gameObject.AddComponent<TicketLine>();

            SpriteRenderer icon = CreateSpriteChild(lineTf, "Icon", square, new Vector2(TicketIconSize, TicketIconSize), iconColor, 1);
            SpriteRenderer tick = CreateSpriteChild(lineTf, "Tick", circle, new Vector2(TicketTickSize, TicketTickSize), settings.ticketTickColor, 2);
            tick.transform.localPosition = TicketTickOffset;
            tick.enabled = false;

            var so = new SerializedObject(line);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("tick").objectReferenceValue = tick;
            so.ApplyModifiedPropertiesWithoutUndo();
            return line;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // --- Müşteri prefab'ı ---

        /// <summary>Mevcut Customer.prefab'a dokunma collider'ını ekler/günceller.</summary>
        private static void EnsureCustomerTapCollider(SalonSettings settings)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
            try
            {
                ApplyCustomerTapCollider(contents, settings);
                PrefabUtility.SaveAsPrefabAsset(contents, CustomerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ApplyCustomerTapCollider(GameObject customerRoot, SalonSettings settings)
        {
            BoxCollider2D col = customerRoot.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = customerRoot.AddComponent<BoxCollider2D>();
            }

            col.size = settings.customerTapSize;
            col.offset = new Vector2(0f, CustomerTapOffsetY);
        }

        // --- Veri ---

        /// <summary>Level_01 menüsünde ikonu olmayan ürünlere ayırt edici yer tutucu renk verir (zaten verilmişse dokunmaz).</summary>
        private static void EnsureProductPlaceholderColors(SalonSettings settings)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
            if (level == null || level.menu == null)
            {
                return;
            }

            int cakeIndex = 0;
            int drinkIndex = 0;
            bool changed = false;
            foreach (ProductData product in level.menu)
            {
                if (product == null || product.icon != null || HasPlaceholderColor(settings, product))
                {
                    continue;
                }

                Color color = product.category == ProductCategory.Drink
                    ? DrinkPlaceholderPalette[drinkIndex++ % DrinkPlaceholderPalette.Length]
                    : CakePlaceholderPalette[cakeIndex++ % CakePlaceholderPalette.Length];

                if (!changed)
                {
                    Undo.RecordObject(settings, "Ürün Yer Tutucu Renkleri");
                    changed = true;
                }

                settings.productPlaceholderColors.Add(new ProductPlaceholderColor { product = product, color = color });
            }

            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        private static bool HasPlaceholderColor(SalonSettings settings, ProductData product)
        {
            foreach (ProductPlaceholderColor entry in settings.productPlaceholderColors)
            {
                if (entry.product == product)
                {
                    return true;
                }
            }

            return false;
        }

        private static ProductData GetOrCreateTestProduct(string id, ProductCategory category, Color32 color)
        {
            string path = $"{TestFolder}/Product_{id}.asset";
            var product = AssetDatabase.LoadAssetAtPath<ProductData>(path);
            if (product == null)
            {
                product = ScriptableObject.CreateInstance<ProductData>();
                AssetDatabase.CreateAsset(product, path);
            }

            product.id = id;
            product.nameKey = $"product.{id}.name";
            product.price = TestProductPrice;
            product.category = category;
            product.icon = GetOrCreateSprite($"{TestIconFolder}/Icon_{id}.png", false, color);
            EditorUtility.SetDirty(product);
            return product;
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

        // --- Yardımcılar ---

        private static void AssignDragItem(DragItem drag, SalonSettings settings, DropTargetRegistry registry)
        {
            var so = new SerializedObject(drag);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("registry").objectReferenceValue = registry;
            so.ApplyModifiedProperties();
        }

        private static void SetObjectField(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetObjectArray(SerializedProperty list, IReadOnlyList<Object> values)
        {
            list.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
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

        /// <summary>Sahnede, geri alınabilir boş nokta.</summary>
        private static Transform CreatePoint(Transform parent, string name, Vector2 localPosition)
        {
            Transform tf = CreateChildTransform(parent, name, localPosition);
            Undo.RegisterCreatedObjectUndo(tf.gameObject, "Hazırlama Alanı");
            return tf;
        }

        /// <summary>Prefab kurarken kullanılan boş alt nesne (Undo kaydı yok).</summary>
        private static Transform CreateChildTransform(Transform parent, string name, Vector2 localPosition)
        {
            var go = new GameObject(name);
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
    }
}
