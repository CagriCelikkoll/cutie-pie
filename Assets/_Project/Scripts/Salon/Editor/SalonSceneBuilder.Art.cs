using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: Art/Salon/Items altındaki görselleri verilere atar.
    /// - Ürünler (Data/Products): dosya adı ürün kimliğine (ProductData.id) eşitse yalnızca icon alanı yazılır.
    /// - SalonSettings: tray.png → traySprite, plate.png → plateSprite.
    /// Görseli bulunmayan alanlara dokunulmaz; tekrar çalıştırılabilir.
    /// </summary>
    public static partial class SalonSceneBuilder
    {
        private const string ItemArtFolder = "Assets/_Project/Art/Salon/Items";
        private const string ProductDataFolder = "Assets/_Project/Data/Products";
        private const string TrayArtName = "tray";
        private const string PlateArtName = "plate";

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

                Sprite sprite = LoadItemSprite(product.id);
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
            assigned += AssignSettingsSprite(settings, TrayArtName, s => s.traySprite, (s, v) => s.traySprite = v);
            assigned += AssignSettingsSprite(settings, PlateArtName, s => s.plateSprite, (s, v) => s.plateSprite = v);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SalonSceneBuilder] Görsel atama bitti: {assigned} alan güncellendi.");
        }

        private static int AssignSettingsSprite(SalonSettings settings, string artName,
            System.Func<SalonSettings, Sprite> getter, System.Action<SalonSettings, Sprite> setter)
        {
            Sprite sprite = LoadItemSprite(artName);
            if (sprite == null || getter(settings) == sprite)
            {
                return 0;
            }

            Undo.RecordObject(settings, "Salon Görseli");
            setter(settings, sprite);
            EditorUtility.SetDirty(settings);
            Debug.Log($"[SalonSceneBuilder] SalonSettings ← {sprite.name}");
            return 1;
        }

        /// <summary>Items klasöründeki görselin sprite'ı (içe aktarıcı görseli dolu alana kırptığı için alt varlıktır).</summary>
        private static Sprite LoadItemSprite(string fileName)
        {
            string path = $"{ItemArtFolder}/{fileName}.png";
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
