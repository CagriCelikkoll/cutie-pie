using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: Salon görsellerini verilere atar ve arka planı kurar.
    /// - Ürünler (Data/Products): Items'taki dosya adı ürün kimliğine (ProductData.id) eşitse yalnızca icon alanı yazılır.
    /// - SalonSettings: tepsi, tabak, arka plan, masa, müşteri modları, vitrin, içecek tezgâhı ve çöp görselleri.
    ///   Beyaz arka planlı görsellerin temizlenmiş "_cut" kopyaları kullanılır (Beyaz Arka Planı Temizle menüsü üretir).
    /// Görseli bulunmayan alanlara dokunulmaz; tekrar çalıştırılabilir.
    /// </summary>
    public static partial class SalonSceneBuilder
    {
        private const string ArtFolder = "Assets/_Project/Art/Salon";
        private const string ItemArtFolder = ArtFolder + "/Items";
        private const string CustomerArtFolder = ArtFolder + "/Customers";
        private const string BackgroundArtPath = ArtFolder + "/Background/salon_background.png";
        private const string ProductDataFolder = "Assets/_Project/Data/Products";
        private const string TrayArtName = "tray";
        private const string PlateArtName = "plate";
        private const string BackgroundObjectName = "Background";

        [MenuItem(MenuRoot + "6 - Görselleri Verilere Ata")]
        private static void AssignItemArt()
        {
            int assigned = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ProductData), new[] { ProductDataFolder }))
            {
                var product = AssetDatabase.LoadAssetAtPath<ProductData>(AssetDatabase.GUIDToAssetPath(guid));
                if (product == null || string.IsNullOrEmpty(product.id))
                {
                    continue;
                }

                Sprite sprite = LoadSpriteAt(ItemPath(product.id));
                if (sprite == null || product.icon == sprite)
                {
                    continue;
                }

                // Ortak veri: yalnızca icon alanı değişir.
                Undo.RecordObject(product, "Ürün İkonu");
                product.icon = sprite;
                EditorUtility.SetDirty(product);
                assigned++;
                Debug.Log($"[SalonSceneBuilder] {product.name}.icon ← {sprite.name}");
            }

            SalonSettings settings = GetOrCreateSettings();
            assigned += AssignSettingsSprite(settings, ItemPath(TrayArtName), s => s.traySprite, (s, v) => s.traySprite = v);
            assigned += AssignSettingsSprite(settings, ItemPath(PlateArtName), s => s.plateSprite, (s, v) => s.plateSprite = v);
            assigned += AssignSettingsSprite(settings, BackgroundArtPath, s => s.backgroundSprite, (s, v) => s.backgroundSprite = v);
            assigned += AssignSettingsSprite(settings, CutItemPath("table_clean"), s => s.tableCleanSprite, (s, v) => s.tableCleanSprite = v);
            assigned += AssignSettingsSprite(settings, CutItemPath("table_dirty"), s => s.tableDirtySprite, (s, v) => s.tableDirtySprite = v);
            assigned += AssignSettingsSprite(settings, CutItemPath(SalonArtPostprocessor.ShowcaseCabinetName),
                s => s.showcaseCabinetSprite, (s, v) => s.showcaseCabinetSprite = v);
            assigned += AssignSettingsSprite(settings, CutItemPath(SalonArtPostprocessor.DrinkCounterName),
                s => s.drinkCounterSprite, (s, v) => s.drinkCounterSprite = v);
            assigned += AssignSettingsSprite(settings, CutItemPath("trash_bin"), s => s.trashBinSprite, (s, v) => s.trashBinSprite = v);
            assigned += AssignSettingsSprite(settings, CutCustomerPath("customer_happy"),
                s => s.customerHappySprite, (s, v) => s.customerHappySprite = v);
            assigned += AssignSettingsSprite(settings, CutCustomerPath("customer_impatient"),
                s => s.customerImpatientSprite, (s, v) => s.customerImpatientSprite = v);
            assigned += AssignSettingsSprite(settings, CutCustomerPath("customer_angry"),
                s => s.customerAngrySprite, (s, v) => s.customerAngrySprite = v);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SalonSceneBuilder] Görsel atama bitti: {assigned} alan güncellendi. " +
                      "Sahneye yansıması için \"4 - Hazırlama Alanını Kur\" menüsünü çalıştırın.");
        }

        /// <summary>
        /// Kamerayı kaplayan arka plan nesnesi (SalonRoot/Background). Tekrar çalıştırılınca yalnızca referanslar güncellenir;
        /// görsel oyunda BackgroundFitter ile ekran oranına göre sığdırılır, burada da önizleme için bir kez sığdırılır.
        /// </summary>
        private static void SetupBackground(Transform root, SalonSettings settings)
        {
            Transform bgTf = root.Find(BackgroundObjectName);
            if (bgTf == null)
            {
                var go = new GameObject(BackgroundObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Arka Plan");
                go.transform.SetParent(root, false);
                go.transform.SetAsFirstSibling();
                bgTf = go.transform;
            }

            GetOrAddComponent<SpriteRenderer>(bgTf.gameObject);
            BackgroundFitter fitter = GetOrAddComponent<BackgroundFitter>(bgTf.gameObject);

            var so = new SerializedObject(fitter);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.ApplyModifiedProperties();

            Undo.RecordObjects(new Object[] { bgTf, bgTf.GetComponent<SpriteRenderer>() }, "Arka Plan");
            fitter.Fit();
        }

        private static int AssignSettingsSprite(SalonSettings settings, string path,
            System.Func<SalonSettings, Sprite> getter, System.Action<SalonSettings, Sprite> setter)
        {
            Sprite sprite = LoadSpriteAt(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[SalonSceneBuilder] '{path}' bulunamadı; alan değiştirilmedi.");
                return 0;
            }

            if (getter(settings) == sprite)
            {
                return 0;
            }

            Undo.RecordObject(settings, "Salon Görseli");
            setter(settings, sprite);
            EditorUtility.SetDirty(settings);
            Debug.Log($"[SalonSceneBuilder] SalonSettings ← {sprite.name}");
            return 1;
        }

        private static string ItemPath(string fileName) => $"{ItemArtFolder}/{fileName}.png";

        private static string CutItemPath(string baseName) => $"{ItemArtFolder}/{baseName}{SalonArtPostprocessor.CutSuffix}.png";

        private static string CutCustomerPath(string baseName) => $"{CustomerArtFolder}/{baseName}{SalonArtPostprocessor.CutSuffix}.png";

        /// <summary>Görselin sprite'ı (içe aktarıcı kırptığı görsellerde sprite alt varlıktır).</summary>
        private static Sprite LoadSpriteAt(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
