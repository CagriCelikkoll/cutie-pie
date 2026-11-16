using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: Art/Salon altındaki PNG'lere (Placeholders/ hariç) Salon görsel ayarlarını uygular.
    /// Ayarlar yalnızca ilk içe aktarmada (.meta yokken) uygulanır; sonradan elle yapılan ince ayarlar korunur.
    /// "Görsel Ayarlarını Yeniden Uygula" menüsü seçili (ya da tüm) görsellere ayarları zorla yeniden uygular.
    ///
    /// Görsellerin şeffaf kenar boşlukları birbirinden çok farklı olduğu için sprite, görselin dolu alanına
    /// (alpha sınırlarına) kırpılır ve pivot dolu alanın ortasına konur. Böylece SpriteFitter ve UI Image
    /// (preserveAspect) boşluğu değil görselin kendisini kutuya sığdırır.
    /// </summary>
    public class SalonArtPostprocessor : AssetPostprocessor
    {
        private const string MenuPath = "Cutie Pie/Salon/Görsel Ayarlarını Yeniden Uygula";
        private const string ArtRoot = "Assets/_Project/Art/Salon/";
        private const string ExcludedFolder = ArtRoot + "Placeholders/";
        private const string PngExtension = ".png";

        private const int MaxTextureSize = 1024;

        // Kaynak görseller 2048 px; dünya boyutunu SpriteFitter belirlediği için bu değer yalnızca editördeki ilk görünüşü etkiler.
        private const float PixelsPerUnit = 512f;

        // Bu değerin altındaki alpha (0-255) boş sayılır (kenarlardaki neredeyse görünmez gölge/kenar yumuşatma pikselleri).
        private const byte AlphaThreshold = 8;

        // Menüden yeniden uygularken "yalnızca ilk içe aktarma" kuralını atlar.
        private static bool forceApply;

        private void OnPreprocessTexture()
        {
            if (!IsSalonArt(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing && !forceApply)
            {
                return;
            }

            ApplySettings(importer, assetPath);
        }

        private static bool IsSalonArt(string path)
        {
            string normalized = path.Replace('\\', '/');
            return normalized.StartsWith(ArtRoot)
                   && !normalized.StartsWith(ExcludedFolder)
                   && Path.GetExtension(normalized).ToLowerInvariant() == PngExtension;
        }

        private static void ApplySettings(TextureImporter importer, string path)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = MaxTextureSize;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            // Dolu alan bulunamazsa (tamamen saydam / okunamayan dosya) tüm görsel tek sprite olarak kalır.
            if (TryGetOpaqueRect(path, out Rect rect))
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                SetSingleSpriteRect(importer, Path.GetFileNameWithoutExtension(path), rect);
            }
            else
            {
                Debug.LogWarning($"[SalonArtPostprocessor] '{path}' için dolu alan bulunamadı; görsel kırpılmadan içe aktarıldı.");
                importer.spriteImportMode = SpriteImportMode.Single;
            }
        }

        /// <summary>
        /// Görselde tek sprite tanımlar (2D Sprite paketinin veri sağlayıcısıyla; TextureImporter.spritesheet 2022.3'te kaldırıldı).
        /// Daha önce sprite varsa kimliği korunur ki ona bağlı referanslar (ProductData.icon vb.) kopmasın.
        /// </summary>
        private static void SetSingleSpriteRect(TextureImporter importer, string spriteName, Rect rect)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] existing = provider.GetSpriteRects();
            GUID spriteId = existing != null && existing.Length > 0 ? existing[0].spriteID : GUID.Generate();

            var spriteRect = new SpriteRect
            {
                name = spriteName,
                rect = rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = spriteId
            };

            provider.SetSpriteRects(new[] { spriteRect });

            var nameFileIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameFileIds?.SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(spriteName, spriteId) });

            provider.Apply();
        }

        /// <summary>Kaynak PNG'deki dolu piksellerin sınırı (kaynak piksel koordinatı, sol alt köşe 0,0).</summary>
        private static bool TryGetOpaqueRect(string path, out Rect rect)
        {
            rect = default;
            if (!File.Exists(path))
            {
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    return false;
                }

                int width = texture.width;
                int height = texture.height;
                Color32[] pixels = texture.GetPixels32();

                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a <= AlphaThreshold)
                        {
                            continue;
                        }

                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    return false;
                }

                rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // --- Menü ---

        [MenuItem(MenuPath)]
        private static void ReapplySettings()
        {
            string[] paths = GetTargetPaths();
            if (paths.Length == 0)
            {
                EditorUtility.DisplayDialog("Salon Görselleri", $"{ArtRoot} altında (Placeholders hariç) PNG bulunamadı.", "Tamam");
                return;
            }

            forceApply = true;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                forceApply = false;
            }

            Debug.Log($"[SalonArtPostprocessor] {paths.Length} görsele Salon ayarları yeniden uygulandı.");
        }

        /// <summary>Seçili Salon görselleri; seçim yoksa Art/Salon altındaki tüm PNG'ler (Placeholders hariç).</summary>
        private static string[] GetTargetPaths()
        {
            var selected = new System.Collections.Generic.List<string>();
            foreach (Object obj in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (IsSalonArt(path))
                {
                    selected.Add(path);
                }
            }

            if (selected.Count > 0)
            {
                return selected.ToArray();
            }

            var all = new System.Collections.Generic.List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsSalonArt(path))
                {
                    all.Add(path);
                }
            }

            return all.ToArray();
        }
    }
}
